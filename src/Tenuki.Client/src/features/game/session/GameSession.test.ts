import { describe, expect, it, vi } from "vitest"
import { GameSessionImpl, type GameSessionSnapshot } from "./GameSession"

describe("GameSession", () => {
    const initialSnapshot: GameSessionSnapshot = {
        error: null,
        revision: 1,
        stateToken: null,
        status: "connecting"
    }

    it("returns the initial snapshot", () => {
        const session = new GameSessionImpl(initialSnapshot)
        expect(session.getSnapshot()).toBe(initialSnapshot)
    })

    it("notifies when snapshot changes", () => {
        const session = new GameSessionImpl(initialSnapshot)
        const updatedSnapshot = {
            ...initialSnapshot,
            revision: 42
        }

        const listener = vi.fn()
        session.subscribe(listener)

        session.setSnapshot(updatedSnapshot)
        expect(listener).toHaveBeenCalledOnce()
        expect(session.getSnapshot()).toBe(updatedSnapshot)
    })

    it("notifies nothing when snapshot has not been changed", () => {
        const session = new GameSessionImpl(initialSnapshot)
        const listener = vi.fn()
        session.subscribe(listener)
        session.setSnapshot(initialSnapshot)
        expect(listener).not.toHaveBeenCalled()
        expect(session.getSnapshot()).toBe(initialSnapshot)
    })

    it("does not notify an unsubscribed listener", () => {
        const session = new GameSessionImpl(initialSnapshot)
        const listener1 = vi.fn()
        const listener2 = vi.fn()

        session.subscribe(listener1)
        const unsubscribe = session.subscribe(listener2)
        unsubscribe()

        const updatedSnapshot = {
            ...initialSnapshot,
            revision: 42
        }

        session.setSnapshot(updatedSnapshot)
        expect(listener1).toHaveBeenCalledOnce()
        expect(listener2).not.toHaveBeenCalled()
    })

    it("notifies subscribers for each snapshot change", () => {
        const session = new GameSessionImpl(initialSnapshot)
        const listener = vi.fn()

        session.subscribe(listener)

        session.setSnapshot({
            ...initialSnapshot,
            revision: 2
        })

        session.setSnapshot({
            ...initialSnapshot,
            revision: 3
        })

        expect(listener).toHaveBeenCalledTimes(2)
        expect(session.getSnapshot().revision).toBe(3)
    })
})