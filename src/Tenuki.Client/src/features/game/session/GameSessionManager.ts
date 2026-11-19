import type { GameState } from "../domain/GameState"
import type { GameMessage } from "../protocol/GameMessage"
import { GameSessionImpl, type GameSession, type GameSessionSnapshot } from "./GameSession"
import type { GameSessionTransport } from "./GameSessionTransport"

export interface GameSessionLease {
    readonly session: GameSession
    release(): void
}

export interface GameSessionManager {
    openSession(gameId: string): GameSessionLease
}

interface SessionEntry {
    readonly session: GameSessionImpl
    unsubscribe: () => void
    consumers: number
    disposed: boolean
}

export class GameSessionManagerImpl implements GameSessionManager {
    private readonly transport: GameSessionTransport
    private readonly sessionEntriesByGameId = new Map<string, SessionEntry>()
    private readonly membershipOperations = new Map<string, Promise<void>>()

    constructor(transport: GameSessionTransport) {
        this.transport = transport
    }

    openSession = (gameId: string): GameSessionLease => {
        const sessionEntry = this.getOrCreateSessionEntry(gameId)
        sessionEntry.consumers++        

        let released = false

        return {
            session: sessionEntry.session,
            release: () => {
                if (released) {
                    return
                }
                released = true
                this.releaseConsumerFromSession(gameId, sessionEntry)
            }
        }
    }

    private releaseConsumerFromSession(gameId: string, sessionEntry: SessionEntry) {
        if (this.sessionEntriesByGameId.get(gameId) !== sessionEntry) {
            return
        }

        sessionEntry.consumers--
        if (sessionEntry.consumers <= 0) {
            sessionEntry.unsubscribe()
            void this.queueOperation(gameId, () => this.transport.leaveGame(gameId)).catch(error => {
                console.error(`Failed to leave game ${gameId}`, error)
            })
            sessionEntry.disposed = true
            this.sessionEntriesByGameId.delete(gameId)
        }
    }

    private queueOperation<T>(gameId: string, operation: () => Promise<T>): Promise<T> {
        const previous = this.membershipOperations.get(gameId)
            ?? Promise.resolve()

        const result = previous
            .catch(() => {
                // Ett tidigare fel ska inte blockera kön.
            })
            .then(operation)

        const tail = result.then(
            () => undefined,
            () => undefined
        )

        this.membershipOperations.set(gameId, tail)

        void tail.then(() => {
            if (this.membershipOperations.get(gameId) === tail) {
                this.membershipOperations.delete(gameId)
            }
        })

        return result
    }

    private getOrCreateSessionEntry(gameId: string): SessionEntry {
        const existingEntry = this.sessionEntriesByGameId.get(gameId)
        if (existingEntry) {
            return existingEntry
        }

        const initialEntry = this.createSessionEntry()
        this.sessionEntriesByGameId.set(gameId, initialEntry)

        this.startSession(gameId, initialEntry)

        return initialEntry
    }

    private createSessionEntry(): SessionEntry {
        const initialSnapshot: GameSessionSnapshot = {
            error: null,
            revision: null,
            stateToken: null,
            status: "connecting",
            gameState: null,
        }

        const sessionEntry: SessionEntry = {
            session: new GameSessionImpl(initialSnapshot),
            consumers: 0,
            unsubscribe: () => {},
            disposed: false
        }
        return sessionEntry
    }

    private onMessageReceived(message: GameMessage, sessionEntry: SessionEntry) {
        if (sessionEntry.disposed) {
            return
        }

        const currentSnapshot = sessionEntry.session.getSnapshot()
        if (currentSnapshot.revision !== null && currentSnapshot.revision >= message.revision) {
            return
        }
        
        const decodedState = this.decodeStateToken(message.stateToken)

        sessionEntry.session.setSnapshot({
            ...currentSnapshot,
            revision: message.revision,
            stateToken: message.stateToken,
            gameState: decodedState
        })
    }

    private async startSession(gameId: string, sessionEntry: SessionEntry): Promise<void> {
        sessionEntry.unsubscribe = this.transport.subscribeToGame(gameId, message => {
            this.onMessageReceived(message, sessionEntry)
        })

        try {
            await this.queueOperation(gameId, () => this.transport.joinGame(gameId))
        } catch(exception) {
            if (sessionEntry.disposed) {
                return
            }

            const error: Error = {
                message: "Failed to join game",
                name: "Failed to join game",
                cause: exception
            }

            sessionEntry.session.setError(error)
            return
        }

        if (sessionEntry.disposed) {
            return
        }

        await this.synchronizeSession(gameId, sessionEntry)
    }

    private async synchronizeSession(gameId: string, sessionEntry: SessionEntry): Promise<void> {
        if (sessionEntry.disposed) {
            return
        }

        try {
            sessionEntry.session.setSynchronizing()

            const { stateToken, revision } = await this.transport.getGame(gameId)

            if (sessionEntry.disposed) {
                return
            }
            
            sessionEntry.session.updateSnapshot(current => {
                if (
                    current.stateToken !== null &&
                    current.revision !== null &&
                    current.revision >= revision
                ) {
                    return {
                        ...current,
                        status: "ready"
                    }
                }

                return {
                    ...current,
                    stateToken: stateToken,
                    revision: revision,
                    status: "ready",
                    gameState: this.decodeStateToken(stateToken)
                }
            })
        }
        catch (error) {
            if (sessionEntry.disposed) {
                return
            }

            sessionEntry.session.setError({
                message: "Failed to synchronize the game",
                cause: error,
                name: "Synchronization failure"
            })
        }
    }

    private decodeStateToken(stateToken: string): GameState {
        const parts = stateToken.split(".")

        if (parts.length !== 2) {
            throw new Error("Invalid state token format")
        }

        const bytes = Uint8Array.from(
            atob(parts[0]),
            character => character.charCodeAt(0)
        )

        const json = new TextDecoder().decode(bytes)

        return JSON.parse(json) as GameState
    }
}