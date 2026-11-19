import { useCallback } from "react";
import type { GameSession } from "../session/GameSession";
import { useGameSessionSnapshot } from "../session/hooks/useGameSessionSnapshot";
import GoBoard from "./GoBoard";
import type { BoardPoint } from "../domain/BoardPoint";
import { placeStoneCommand } from "../protocol/GameCommand";
import { getNextPlayer } from "../domain/GameState";
import NesGoBoard from "./NesGoBoard";

export interface GameContentProps {
    readonly gameSession: GameSession
}

export function GameContent({ gameSession }: GameContentProps) {
    const snapshot = useGameSessionSnapshot(gameSession)
    
    const onIntersectionClicked = useCallback((point: BoardPoint) => {
        if (snapshot.gameState === null) {
            return
        }

        const nextPlayer = getNextPlayer(snapshot.gameState)
        const command = placeStoneCommand(nextPlayer, point)

        gameSession.dispatch(command)
    }, [gameSession, snapshot])
    
    if (snapshot.gameState === null) {
        return (<p>Loading ...</p>)
    }

    return (
        <NesGoBoard 
            onIntersectionClick={onIntersectionClicked}
            size={snapshot.gameState.settings.boardSize}
            stones={[... snapshot.gameState.currentBoard.position]}
        />
    )
}