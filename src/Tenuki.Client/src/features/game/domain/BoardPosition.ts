import type { Stone } from "./Stone"

export type BoardPosition = {
    readonly position: ReadonlySet<Stone>
    readonly whiteCaptures: number
    readonly blackCaptures: number
    readonly hash: number
}