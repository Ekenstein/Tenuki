import GoBoard from "./GoBoard"
import { useGame } from "../hooks/useGame"
import { useCallback } from "react"
import type { BoardPoint } from "../domain/BoardPoint"

function GameBoard() {
    const { state, dispatch } = useGame()

    const onIntersectionClick = useCallback(
        (point: BoardPoint) => {
            dispatch({type: "addStone", point: point})
        },
        [dispatch]
    )

    return (
        <GoBoard 
            size={state.settings.boardSize} 
            stones={[...state.currentBoard.position]}
            onIntersectionClick={onIntersectionClick}
        />
    )
}

export default GameBoard