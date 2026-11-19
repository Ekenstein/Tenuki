export type TimeSettings_Base = {
    readonly mainTimeMs: number
}

export type TimeSettings_Fixed = TimeSettings_Base & {
    readonly $type: "Fixed"
}

export type TimeSettings_ByoYomi = TimeSettings_Base & {
    readonly $type: "ByoYomi"
    readonly periods: number
    readonly periodTimeMs: number
}

export type TimeSettings =
    | TimeSettings_Fixed
    | TimeSettings_ByoYomi

export function createByoYomiTimeSettings(mainTimeSeconds: number, periods: number, periodTimeSeconds: number): TimeSettings_ByoYomi {
    return {
        $type: "ByoYomi",
        mainTimeMs: mainTimeSeconds * 1000,
        periods: periods,
        periodTimeMs: periodTimeSeconds * 1000
    }
}