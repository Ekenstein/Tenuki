import { useTimer } from "./features/game/hooks/useTimer"

function GameTimer() {
    const seconds = useTimer()

    return (
        <>
        <p>{seconds}s</p>
        </>
    )
}

export default GameTimer