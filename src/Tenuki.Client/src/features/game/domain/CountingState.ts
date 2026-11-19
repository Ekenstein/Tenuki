import type { BoardPoint } from "./BoardPoint"
import type { Color } from "./Stone"

export interface CountingState {
    readonly deadStones: ReadonlySet<BoardPoint>
    readonly acceptedScore: ReadonlySet<Color>
}
