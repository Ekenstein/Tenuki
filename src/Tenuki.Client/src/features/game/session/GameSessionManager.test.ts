import { describe, expect, it, vi } from "vitest";
import { GameSessionManagerImpl } from "./GameSessionManager";
import type { GameSessionTransport } from "./GameSessionTransport";

describe("GameSessionManager", () => {
    const transport: GameSessionTransport = {
        joinGame: vi.fn().mockResolvedValue(undefined),
        leaveGame: vi.fn().mockResolvedValue(undefined),
        getGame: vi.fn().mockResolvedValue({
            stateToken: "token-42",
            revision: 42
        }),
        subscribeToGame: vi.fn().mockResolvedValue(() => vi.fn())
    }

    function deferred<T>() {
        let resolve!: (value: T) => void
        let reject!: (reason?: unknown) => void

        const promise = new Promise<T>((res, rej) => {
            resolve = res
            reject = rej
        })

        return { promise, resolve, reject }
    }

    it("returns the same session for the same game id", () => {
        const manager = new GameSessionManagerImpl(transport)
        const gameId = "game-id-42"

        const lease1 = manager.openSession(gameId)
        const lease2 = manager.openSession(gameId)

        expect(lease1.session).toBe(lease2.session)
    })

    it("returns different sessions for different game ids", () => {
        const manager = new GameSessionManagerImpl(transport)
        const lease1 = manager.openSession("1")
        const lease2 = manager.openSession("2")

        expect(lease1.session).not.toBe(lease2.session)
    })

    it("creates a new session when all leases have been released", () => {
        const manager = new GameSessionManagerImpl(transport)
        const gameId = "1"
        const lease1 = manager.openSession(gameId)
        lease1.release()

        const lease2 = manager.openSession(gameId)
        expect(lease1.session).not.toBe(lease2.session)
    })

    it("reuses the session while consumers remain", () => {
        const manager = new GameSessionManagerImpl(transport)
        const gameId = "1"
        const lease1 = manager.openSession(gameId)
        const lease2 = manager.openSession(gameId)

        lease1.release()
        const lease3 = manager.openSession(gameId)
        expect(lease2.session).toBe(lease3.session)
    })

    it("returns an idempotent lease when opening a session", () => {
        const manager = new GameSessionManagerImpl(transport)
        const gameId = "1"
        const lease1 = manager.openSession(gameId)
        const lease2 = manager.openSession(gameId)

        lease1.release()
        lease1.release()

        const lease3 = manager.openSession(gameId)
        expect(lease2.session).toBe(lease3.session)
    })

    it("removes the session only when the last lease is released", () => {
        const manager = new GameSessionManagerImpl(transport)
        const gameId = "1"

        const lease1 = manager.openSession(gameId)
        const lease2 = manager.openSession(gameId)

        lease1.release()

        const lease3 = manager.openSession(gameId)
        expect(lease3.session).toBe(lease2.session)

        lease2.release()
        lease3.release()

        const lease4 = manager.openSession(gameId)
        expect(lease4.session).not.toBe(lease3.session)
    })

    it("initializes a session only once for multiple consumers", () => {
        // Öppna samma gameId två gånger.
        // Verifiera att subscribeToGame anropas en gång.
        // Verifiera att joinGame anropas en gång.
        const transport: GameSessionTransport = {
            getGame: vi.fn().mockResolvedValue({
                stateToken: "state-42",
                revision: 1
            }),
            joinGame: vi.fn().mockResolvedValue(undefined),
            subscribeToGame: vi.fn().mockResolvedValue(() => vi.fn()),
            leaveGame: vi.fn().mockResolvedValue(undefined)
        }

        const manager = new GameSessionManagerImpl(transport)
        const gameId = "game-id-42"

        manager.openSession(gameId)
        manager.openSession(gameId)

        expect(transport.getGame).toHaveBeenCalledOnce()
        expect(transport.subscribeToGame).toHaveBeenCalledOnce()
    })

    it("leaves the group after a pending join completes when the last consumer releases", async () => {
        const pendingJoin = deferred<void>()

        const transport: GameSessionTransport = {
            joinGame: vi.fn(() => pendingJoin.promise),
            leaveGame: vi.fn().mockResolvedValue(undefined),
            getGame: vi.fn().mockResolvedValue({
                stateToken: "token-42",
                revision: 42
            }),
            subscribeToGame: vi.fn(() => vi.fn())
        }

        const manager = new GameSessionManagerImpl(transport)

        const lease = manager.openSession("A")

        expect(transport.joinGame).toHaveBeenCalledExactlyOnceWith("A")

        // JoinGame pågår fortfarande.
        lease.release()

        // LeaveGame får inte köras före JoinGame är klart.
        expect(transport.leaveGame).not.toHaveBeenCalled()

        // Nu låter vi JoinGame slutföras.
        pendingJoin.resolve()

        await vi.waitFor(() => {
            expect(transport.leaveGame).toHaveBeenCalledExactlyOnceWith("A")
        })

        expect(transport.getGame).not.toHaveBeenCalled()
    })

    it("leaves the group when the last consumer releases after joining", async () => {
        const transport: GameSessionTransport = {
            joinGame: vi.fn().mockResolvedValue(undefined),
            leaveGame: vi.fn().mockResolvedValue(undefined),
            getGame: vi.fn().mockResolvedValue({
                stateToken: "token-42",
                revision: 42
            }),
            subscribeToGame: vi.fn(() => vi.fn())
        }

        const manager = new GameSessionManagerImpl(transport)
        const lease = manager.openSession("A")

        await vi.waitFor(() => {
            expect(transport.getGame).toHaveBeenCalledOnce()
        })

        lease.release()

        await vi.waitFor(() => {
            expect(transport.leaveGame).toHaveBeenCalledExactlyOnceWith("A")
        })
    })

    it("waits for the previous session to leave before joining the same game again", async () => {
        const pendingLeave = deferred<void>()

        const transport: GameSessionTransport = {
            joinGame: vi.fn().mockResolvedValue(undefined),
            leaveGame: vi.fn(() => pendingLeave.promise),
            getGame: vi.fn().mockResolvedValue({
                stateToken: "token-42",
                revision: 42
            }),
            subscribeToGame: vi.fn(() => vi.fn())
        }

        const manager = new GameSessionManagerImpl(transport)

        // Öppna första sessionen.
        const lease1 = manager.openSession("A")

        await vi.waitFor(() => {
            expect(transport.getGame).toHaveBeenCalledOnce()
        })

        // Stäng den första sessionen.
        lease1.release()

        await vi.waitFor(() => {
            expect(transport.leaveGame).toHaveBeenCalledOnce()
        })

        // LeaveGame pågår fortfarande.
        const lease2 = manager.openSession("A")

        // Den nya sessionen får inte gå med innan
        // den gamla sessionen har lämnat gruppen.
        expect(transport.joinGame).toHaveBeenCalledTimes(1)

        // Slutför LeaveGame.
        pendingLeave.resolve()

        // Nu får den nya sessionen gå med.
        await vi.waitFor(() => {
            expect(transport.joinGame).toHaveBeenCalledTimes(2)
        })

        expect(lease1.session).not.toBe(lease2.session)
    })

    it("allows a new session to join after a previous join failed", async () => {
        const transport: GameSessionTransport = {
            joinGame: vi.fn()
                .mockRejectedValueOnce(new Error("Connection lost"))
                .mockResolvedValueOnce(undefined),

            leaveGame: vi.fn().mockResolvedValue(undefined),

            getGame: vi.fn().mockResolvedValue({
                stateToken: "token-42",
                revision: 42
            }),

            subscribeToGame: vi.fn(() => vi.fn())
        }

        const manager = new GameSessionManagerImpl(transport)

        const lease1 = manager.openSession("A")

        await vi.waitFor(() => {
            expect(lease1.session.getSnapshot().status).toBe("failed")
        })

        lease1.release()

        const lease2 = manager.openSession("A")

        await vi.waitFor(() => {
            expect(lease2.session.getSnapshot().status).toBe("ready")
        })

        expect(transport.joinGame).toHaveBeenCalledTimes(2)
    })
})