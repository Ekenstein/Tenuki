import { useSyncExternalStore } from "react";
import type { GameSession, GameSessionSnapshot } from "../GameSession";

export function useGameSessionSnapshot(session: GameSession): GameSessionSnapshot {
    return useSyncExternalStore(session.subscribe, session.getSnapshot)
}