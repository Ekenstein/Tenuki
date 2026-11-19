import type { BoardPoint } from "./BoardPoint"

export type GameAction_SetBoardSize = { 
    readonly type: "setBoardSize"
    readonly size: number
}

export type GameAction_AddStone = {
    readonly type: "addStone"
    readonly point: BoardPoint
}

export type GameAction =
    | GameAction_SetBoardSize
    | GameAction_AddStone
