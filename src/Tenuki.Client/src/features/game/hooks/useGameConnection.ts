import { HubConnectionState } from "@microsoft/signalr";
import { useCallback, useEffect, useState } from "react";
import { useSignalR } from "../../../infrastructure/signalr/hooks/useSignalRContext";
import type { GameMessage } from "../protocol/GameMessage";

export type GameConnectionState =
    | "disconnected"
    | "connecting"
    | "joining"
    | "joined"
    | "failed"

export interface GameConnection {
    readonly status: GameConnectionState
    readonly error: Error | null
}

export function useGameConnection(gameId: string, onGameMessage: (message: GameMessage) => void): GameConnection {
    const { status, invoke, subscribe } = useSignalR()
    const [state, setState] = useState<GameConnectionState>("disconnected")
    const [error, setError] = useState<Error | null>(null)

    const joinGame = useCallback(
        function joinGame(gameId: string): Promise<void> {
            return invoke("JoinGameAsync", gameId)
        },
        [invoke]
    )

    const leaveGame = useCallback(
        function leaveGame(gameId: string): Promise<void> {
            return invoke("LeaveGameAsync", gameId)
        },
        [invoke]
    )

    const subscribeToGameMessages = useCallback(
        function subscribeToGameMessages(handler: (message: GameMessage) => void): () => void {
            return subscribe("GameMessage", (...args: unknown[]) => {
                const message = args[0] as GameMessage
                if (message.gameId !== gameId) {
                    return
                }

                handler(message)
            })
        },
        [subscribe, gameId]
    )

    useEffect(() => {
        return subscribeToGameMessages(onGameMessage)
    }, [state, [subscribeToGameMessages, onGameMessage]])

    useEffect(() => {
        let disposed = false
        
        switch (status) {
            case HubConnectionState.Connecting:
            case HubConnectionState.Reconnecting:
                setState("connecting")
                break;
            case HubConnectionState.Disconnected:
            case HubConnectionState.Disconnecting:
                setState("disconnected")
                break;
            case HubConnectionState.Connected:
                setState("joining")
                setError(null)
                joinGame(gameId)
                    .then(() => {
                        if (!disposed) {
                            setState("joined")
                        }
                    })
                    .catch(error => {
                        if (!disposed) {
                            setState("failed")
                            setError(error)
                        }
                    })
                break;
            default:
                const exhaustive: never = status
                return exhaustive
        }

        return () => {
            disposed = true
        }
    }, [status, gameId, joinGame])

    return {
        status: state,
        error: error
    }
}