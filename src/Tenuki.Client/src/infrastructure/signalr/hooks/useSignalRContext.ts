import { useContext } from "react";
import { SignalRContext } from "../context/SignalRContext";
import type { SignalRConnection } from "./useSignalRConnection";

export function useSignalR(): SignalRConnection {
    const context = useContext(SignalRContext)
    if (context === null) {
        throw new Error("Component must be rendered inside SignalRContext.Provider")
    }

    return context
}