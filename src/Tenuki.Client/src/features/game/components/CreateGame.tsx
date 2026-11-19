import { useCallback, useState } from "react"
import { BackendApi } from "../../../infrastructure/http/BackendApi"
import { createDefaultGameSettings } from "../domain/GameSettings"

export interface CreateGameProps {
    onGameCreated: (gameId: string) => void
}

export function CreateGame({ onGameCreated }: CreateGameProps) {
    const [gameSettings, setGameSettings] = useState(createDefaultGameSettings())

    const onBoardSizeChanged = useCallback((boardSize: number) => {
        setGameSettings(prev => {
            if (prev.boardSize === boardSize) {
                return prev
            }

            return {
                ...prev,
                boardSize: boardSize
            }
        })
    }, [setGameSettings])

    const createGame = useCallback(() => {
        BackendApi.games.post({
            boardSize: gameSettings.boardSize,
            handicap: gameSettings.handicap,
            komi: gameSettings.komi,
            timeSettings: gameSettings.timeSettings
        }).then(response => {
            onGameCreated(response.gameId)
        })
    }, [gameSettings, onGameCreated])

    return (
        <>
        <p>
            Board size:
            <label><input type="radio" value={19} onChange={() => onBoardSizeChanged(19)} checked={gameSettings.boardSize === 19} /> 19x19</label>
            <label><input type="radio" value={13} onChange={() => onBoardSizeChanged(13)} checked={gameSettings.boardSize === 13} /> 13x13</label>
            <label><input type="radio" value={9} onChange={() => onBoardSizeChanged(9)} checked={gameSettings.boardSize === 9} /> 9x9</label>
        </p>
        <p>
            <input type="button" value="Create" onClick={createGame} />
        </p>
        </>
    )
}