import { useContext } from "react";
import { GameContext, type GameContextValue } from "../context/GameContextValue";

export function useGame(): GameContextValue {
    const context = useContext(GameContext)
    if (context === null) {
        throw new Error("Component must be rendered inside GameContext.Provider")
    }

    return context
}