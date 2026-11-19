import { useGameSession } from "../session/hooks/useGameSession"
import { GameContent } from "./GameContent"

export interface GameProps {
    readonly gameId: string
}

export function Game({ gameId }: GameProps) {
    const session = useGameSession(gameId)
    if (session === null) {
        return (<p>Connecting ...</p>)
    }

    return (
        <GameContent gameSession={session} />
    )
}