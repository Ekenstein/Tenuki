import { BackendApi, type GetGameResponse } from "../../../infrastructure/http/BackendApi"
import type { SignalRConnection } from "../../../infrastructure/signalr/hooks/useSignalRConnection"
import type { GameMessage } from "../protocol/GameMessage"

export interface GameSessionTransport {
    joinGame(gameId: string): Promise<void>
    leaveGame(gameId: string): Promise<void>
    getGame(gameId: string): Promise<GetGameResponse>
    subscribeToGame(
        gameId: string, 
        handler: (message: GameMessage) => void
    ): () => void
}

class GameSessionTransportImpl implements GameSessionTransport {
    private readonly signalR: SignalRConnection

    constructor(signalR: SignalRConnection) {
        this.signalR = signalR
    }

    joinGame(gameId: string): Promise<void> {
        return this.signalR.invoke("JoinGame", gameId)
    }

    leaveGame(gameId: string): Promise<void> {
        return this.signalR.invoke("LeaveGame", gameId)
    }

    async getGame(gameId: string): Promise<GetGameResponse> {
        return BackendApi.games.get(gameId)
    }

    subscribeToGame(gameId: string, handler: (message: GameMessage) => void): () => void {
        return this.signalR.subscribe("GameMessage", (...args: unknown[]) => {
            const message = args[0] as GameMessage
            if (message.gameId !== gameId) {
                return
            }

            handler(message)
        })
    }

}

export function createGameSessionTransport(signalR: SignalRConnection): GameSessionTransport {
    return new GameSessionTransportImpl(signalR)
}