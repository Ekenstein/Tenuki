import { useEffect, useState } from "react";

export function useTimer(): number {
    const [seconds, setSeconds] = useState<number>(0)

    useEffect(() => {
        const intervalId = setInterval(() => {
            setSeconds(prev => prev + 1)
        }, 1000)

        return () => clearInterval(intervalId)
    }, [])

    return seconds
}