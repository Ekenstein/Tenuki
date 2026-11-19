import { createContext } from "react";
import type { SignalRConnection } from "../hooks/useSignalRConnection";

export const SignalRContext = createContext<SignalRConnection | null>(null)