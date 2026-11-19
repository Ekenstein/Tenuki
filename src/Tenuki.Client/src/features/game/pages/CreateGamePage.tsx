import { useNavigate } from "react-router";
import { CreateGame } from "../components/CreateGame";

export function CreateGamePage() {
    const navigate = useNavigate()
    return (
        <CreateGame onGameCreated={gameId => navigate(`/games/${gameId}`)} />
    )
}