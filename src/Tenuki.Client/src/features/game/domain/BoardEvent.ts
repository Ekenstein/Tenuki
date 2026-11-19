import type { BoardPoint } from "./BoardPoint"
import type { Color } from "./Stone"

export type BoardEvent_Base = {
    readonly color: Color
}

export type BoardEvent_Stone = BoardEvent_Base & {
    readonly $type: "Stone"
    readonly point: BoardPoint
}

export type BoardEvent_Pass = BoardEvent_Base & {
    readonly $type: "Pass"
}

export type BoardEvent = 
    | BoardEvent_Stone
    | BoardEvent_Pass