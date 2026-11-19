import { createByoYomiTimeSettings, type TimeSettings } from "./TimeSettings"

export interface GameSettings {
    readonly boardSize: number
    readonly komi: number
    readonly handicap: number
    readonly timeSettings: TimeSettings
}

export function createDefaultGameSettings(): GameSettings {
    return {
        boardSize: 19,
        komi: 6.5,
        handicap: 0,
        timeSettings: createByoYomiTimeSettings(5 * 60, 5, 30)
    }
}
