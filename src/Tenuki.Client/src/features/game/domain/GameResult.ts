import type { Color } from "./Stone"

export type GameResult_Base = {
    readonly winner: Color
}

export type GameResult_Time = GameResult_Base & {
    readonly $type: "Time"
}

export type GameResult_Resign = GameResult_Base & {
    readonly $type: "Resign"
}

export type GameResult_Score = GameResult_Base & {
    readonly $type: "Score"
    readonly points: number
}

export type GameResult = 
    | GameResult_Time
    | GameResult_Resign
    | GameResult_Score