import { BackendApi } from "../../../infrastructure/http/BackendApi"
import type { GameState } from "../domain/GameState"
import type { GameCommand } from "../protocol/GameCommand"

export type GameSessionStatus =
    | "connecting"
    | "synchronizing"
    | "ready"
    | "failed"

export interface GameSessionSnapshot {
    readonly status: GameSessionStatus
    readonly stateToken: string | null
    readonly revision: number | null
    readonly error: Error | null
    readonly gameState: GameState | null
}

export interface GameSession {
    getSnapshot(): GameSessionSnapshot
    subscribe(listener: () => void): () => void
    dispatch(command: GameCommand): Promise<void>
}

export class GameSessionImpl implements GameSession {
    private currentSnapshot: GameSessionSnapshot
    private readonly listeners = new Set<() => void>()

    constructor(snapshot: GameSessionSnapshot) {
        this.currentSnapshot = snapshot
    }

    async dispatch(command: GameCommand): Promise<void> {
        if (this.currentSnapshot.stateToken === null) {
            throw new Error("Game has not yet been initialized")
        }

        await BackendApi.games.command({
            stateToken: this.currentSnapshot.stateToken,
            command: command
        })
    }

    setSnapshot(snapshot: GameSessionSnapshot): void {
        if (Object.is(snapshot, this.currentSnapshot)) {
            return
        }

        this.currentSnapshot = snapshot
        this.notify()
    }

    getSnapshot = (): GameSessionSnapshot => {
        return this.currentSnapshot
    }

    updateSnapshot(handler: (current: GameSessionSnapshot) => GameSessionSnapshot): void {
        const updatedSnapshot = handler(this.currentSnapshot)
        this.setSnapshot(updatedSnapshot)
    }

    setError(error: Error) {
        this.setSnapshot({
            ...this.currentSnapshot,
            error: error,
            status: "failed"
        })
    }
    
    setSynchronizing() {
        this.setSnapshot({
            ...this.currentSnapshot,
            status: "synchronizing"
        })
    }

    private notify(): void {
        this.listeners.forEach(listener => listener())
    }

    subscribe = (listener: () => void): (() => void) => {
        this.listeners.add(listener)

        return () => {
            this.listeners.delete(listener)
        }
    }
}