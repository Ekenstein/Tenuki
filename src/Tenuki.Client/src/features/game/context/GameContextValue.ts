import { createContext, type Dispatch } from "react";
import type { GameState } from "../domain/GameState";
import type { GameAction } from "../domain/GameAction";

export interface GameContextValue {
    readonly state: GameState
    readonly dispatch: Dispatch<GameAction>
}

export const GameContext = createContext<GameContextValue | null>(null)