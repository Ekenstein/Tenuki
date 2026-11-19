import type { GameCommand } from "./GameCommand"

export interface GameMessageBase {
    readonly gameId: string
}

export type GameMessage_StateUpdated = GameMessageBase & {
    readonly $type: "stateUpdated"
    readonly stateToken: string,
    readonly previousRevision: number,
    readonly revision: number,
    readonly committedCommands: ReadonlyArray<GameCommand>
}

export type GameMessage =
    | GameMessage_StateUpdated