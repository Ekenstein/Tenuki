import type { BoardPoint } from "../domain/BoardPoint"
import type { Color } from "../domain/Stone"
import { v4 as uuid } from "uuid";

export interface GameCommandBase {
    readonly commandId: string
    readonly color: Color
}

export type GameCommand_Pass = GameCommandBase & {
    readonly $type: "Pass"
}

export type GameCommand_Stone = GameCommandBase & {
    readonly $type: "Stone"
    readonly point: BoardPoint
}

export type GameCommand_Resign = GameCommandBase & {
    readonly $type: "Resign"
}

export type GameCommand_RequestUndo = GameCommandBase & {
    readonly $type: "RequestUndo"
}

export type GameCommand_AcceptUndo = GameCommandBase & {
    readonly $type: "AcceptUndo"
}

export type GameCommand_RejectUndo = GameCommandBase & {
    readonly $type: "RejectUndo"
}

export type GameCommand_MarkDeadStones = GameCommandBase & {
    readonly $type: "MarkDeadStones"
    readonly points: ReadonlyArray<BoardPoint>
}

export type GameCommand_UnmarkDeadStones = GameCommandBase & {
    readonly $type: "UnmarkDeadStones"
    readonly points: ReadonlyArray<BoardPoint>
}

export type GameCommand_AcceptScore = GameCommandBase & {
    readonly $type: "AcceptScore"
}

export type GameCommand_ResumePlay = GameCommandBase & {
    readonly $type: "ResumePlay"
}

export type GameCommand = 
    | GameCommand_Pass
    | GameCommand_Stone
    | GameCommand_Resign
    | GameCommand_RequestUndo
    | GameCommand_AcceptUndo
    | GameCommand_RejectUndo
    | GameCommand_MarkDeadStones
    | GameCommand_UnmarkDeadStones
    | GameCommand_AcceptScore
    | GameCommand_ResumePlay

export function placeStoneCommand(color: Color, point: BoardPoint): GameCommand_Stone {
    return {
        $type: "Stone",
        color: color,
        commandId: uuid(),
        point: point
    }
}