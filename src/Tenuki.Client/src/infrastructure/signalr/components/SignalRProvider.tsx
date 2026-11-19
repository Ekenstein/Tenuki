import type { PropsWithChildren } from "react";
import { useSignalRConnection } from "../hooks/useSignalRConnection";
import { SignalRContext } from "../context/SignalRContext";

export function SignalRProvider({ children }: PropsWithChildren) {
    const connection = useSignalRConnection("/api/hub")
    return (
        <SignalRContext.Provider value={connection}>
            {children}
        </SignalRContext.Provider>
    )
}