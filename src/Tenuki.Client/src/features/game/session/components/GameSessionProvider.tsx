import { createContext, useContext, type ReactNode } from "react";
import type { GameSessionManager } from "../GameSessionManager";

const GameSessionManagerContext = createContext<GameSessionManager | null>(null)

export interface GameSessionProviderProps {
    readonly manager: GameSessionManager,
    readonly children: ReactNode
}

export function GameSessionProvider({ manager, children }: GameSessionProviderProps) {
    return (
        <GameSessionManagerContext.Provider value={manager}>
            {children}
        </GameSessionManagerContext.Provider>
    )
}

export function useGameSessionManager(): GameSessionManager {
    const manager = useContext(GameSessionManagerContext)
    if (!manager) {
        throw new Error("GameSessionProvider is missing")
    }

    return manager
}
