import type { BoardPoint } from "./BoardPoint";
export type White = "White"
export type Black = "Black"

export type Color = White | Black

export function opponentColor(color: Color): Color {
    switch (color) {
        case "White": return "Black"
        case "Black": return "White"
        default:
            const exhaustive: never = color
                return exhaustive
    }
}

export interface Stone {
    readonly point: BoardPoint
    readonly color: Color
}