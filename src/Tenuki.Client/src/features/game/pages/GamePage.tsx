import { useParams } from "react-router";
import { Game } from "../components/Game";

export function GamePage() {
    const { gameId } = useParams<{ gameId: string }>()
    if (!gameId) {
        return (
            <p>Page not found</p>
        )
    }

    return (
        <Game gameId={gameId} />
    )
}