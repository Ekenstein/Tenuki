import { useMemo, type ReactNode } from "react";
import { useSignalR } from "../../../../infrastructure/signalr/hooks/useSignalRContext";
import { type GameSessionManager, GameSessionManagerImpl } from "../GameSessionManager";
import { createGameSessionTransport } from "../GameSessionTransport";
import { GameSessionProvider } from "./GameSessionProvider";

export function GameSessionManagerProvider({ children }: { children: ReactNode }) {
    const signalR = useSignalR()
    const manager = useMemo<GameSessionManager>(() => {
        const transport = createGameSessionTransport(signalR)
        return new GameSessionManagerImpl(transport)
    }, [signalR])

    return (
        <GameSessionProvider manager={manager}>
            {children}
        </GameSessionProvider>
    )
}