import type { BoardEvent } from "./BoardEvent";
import type { BoardPosition } from "./BoardPosition";
import type { ClockState } from "./ClockState";
import type { CountingState } from "./CountingState";
import type { GameResult } from "./GameResult";
import type { GameSettings } from "./GameSettings";
import { opponentColor, type Color } from "./Stone";

export interface GameState {
    readonly id: string
    readonly revision: number
    readonly settings: GameSettings
    readonly clock: ClockState
    readonly result: GameResult | null
    readonly history: readonly BoardEvent[]
    readonly currentBoard: BoardPosition
    readonly previousPositionHash: string | number | null
    readonly undoRequestedBy: Color | null
    readonly counting: CountingState | null
}

export function getNextPlayer(gameState: GameState): Color {
    const lastPlayedMove = gameState.history.at(-1)
    if (lastPlayedMove) {
        return opponentColor(lastPlayedMove.color)
    }

    return gameState.settings.handicap > 0
        ? "White"
        : "Black"
}
