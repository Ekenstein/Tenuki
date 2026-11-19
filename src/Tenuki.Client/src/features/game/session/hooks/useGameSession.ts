import { useEffect, useState } from "react";
import { useGameSessionManager } from "../components/GameSessionProvider";
import { type GameSession } from "../GameSession";
import type { GameSessionManager } from "../GameSessionManager";

interface ActiveSession {
    readonly manager: GameSessionManager,
    readonly gameId: string,
    readonly session: GameSession
}

export function useGameSession(gameId: string): GameSession | null {
    const manager = useGameSessionManager()
    const [session, setSession] = useState<ActiveSession | null>(null)
    
    useEffect(() => {
        const lease = manager.openSession(gameId)
        setSession({
            manager,
            gameId,
            session: lease.session
        })

        return () => {
            lease.release()
        }
    }, [manager, gameId])

    if (session?.manager !== manager || session.gameId !== gameId) {
        return null
    }

    return session.session
}