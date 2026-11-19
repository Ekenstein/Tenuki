import { act } from "react"
import type { GameCommand } from "../protocol/GameCommand"
import type { GameAction, GameAction_AddStone, GameAction_SetBoardSize } from "./GameAction"
import type { GameState } from "./GameState"
import { opponentColor } from "./Stone"

export function reduce(previousState: GameState, action: GameCommand): GameState {
    switch (action.$type) {
        case "Stone":
            return {
                ...previousState,
                currentBoard: previousState.currentBoard.
            }
    }
}

function addStone(previousState: GameState, action: GameAction_AddStone): GameState {
    if (previousState.stones.some(stone => stone.point.x === action.point.x && stone.point.y === action.point.y)) {
        console.log("point occupied")
        return previousState
    }

    return { 
        ... previousState, 
        stones: [ ... previousState.stones, { color: previousState.nextPlayer, point: action.point }],
        nextPlayer: opponentColor(previousState.nextPlayer)
    }
}

function setBoardSize(previousState: GameState, action: GameAction_SetBoardSize): GameState {
    if (previousState.boardSize === action.size) {
        return previousState
    }

    return { ...previousState, stones: [], boardSize: action.size, nextPlayer: "black" }
}