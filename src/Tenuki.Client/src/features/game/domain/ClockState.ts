export interface ClockState {
    readonly black: PlayerClock
    readonly white: PlayerClock
    readonly turnStartedAt: string
}

export interface PlayerClock {
    mainTimeRemainingMs: number
    remainingPeriods: number
    periodTimeRemainingMs: number
}
