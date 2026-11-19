import { HttpTransportType, HubConnection, HubConnectionBuilder, HubConnectionState } from "@microsoft/signalr";
import { useCallback, useEffect, useRef, useState } from "react";

export interface SignalRConnection {
    readonly status: HubConnectionState

    invoke<T>(methodName: string, ...args: unknown[]): Promise<T>
    subscribe(methodName: string, handler: (...args: unknown[]) => void): () => void
}

interface Subscription {
    readonly methodName: string,
    readonly handler: (...args: unknown[]) => void
}

export function useSignalRConnection(url: string): SignalRConnection {
    const connectionRef = useRef<HubConnection | null>(null)
    const [status, setStatus] = useState<HubConnectionState>(HubConnectionState.Disconnected)
    const subscriptions = useRef<Set<Subscription>>(new Set<Subscription>())

    const invoke = useCallback(
        function invoke<T>(methodName: string, ...args: unknown[]): Promise<T> {
            const connection = connectionRef.current

            if (connection === null || connection.state !== HubConnectionState.Connected) {
                const error = new Error("The SignalR connection has not yet been established")
                return Promise.reject(error)
            }

            return connection.invoke<T>(methodName, ...args)
        },
        []
    )
    const subscribe = useCallback(
        function subscribe(methodName: string, handler: (...args: unknown[]) => void): () => void {
            const connection = connectionRef.current

            const subscription = {
                methodName: methodName,
                handler: handler
            }

            subscriptions.current.add(subscription)
            connection?.on(methodName, subscription.handler)

            return () => {
                subscriptions.current.delete(subscription)
                connectionRef.current?.off(subscription.methodName, subscription.handler)
            }
        }, 
        []
    )

    useEffect(() => {
        let disposed = false

        setStatus(HubConnectionState.Connecting)

        const connection = new HubConnectionBuilder()
            .withUrl(url, {
                transport: HttpTransportType.WebSockets
            })
            .withAutomaticReconnect()
            .build()

        connectionRef.current = connection

        subscriptions.current.forEach(subscription => {
            connection.on(subscription.methodName, subscription.handler)
        })

        connection.onclose(() => {
            if (!disposed) {
                setStatus(HubConnectionState.Disconnected)
            }
        })

        connection.onreconnected(() => {
            if (!disposed) {
                setStatus(HubConnectionState.Connected)
            }
        })

        connection.onreconnecting(() => {
            if (!disposed) {
                setStatus(HubConnectionState.Reconnecting)
            }
        })

        connection
            .start()
            .then(() => {
                if (!disposed) {
                    console.log("Successfully connected with SignalR with connection id:", connectionRef.current?.connectionId)
                    setStatus(HubConnectionState.Connected)
                }
            })
            .catch(error => { 
                if (!disposed) {
                    console.error(error, "failed to connect to the server.")
                    setStatus(HubConnectionState.Disconnected)
                }
            })
        
        return () => {
            disposed = true
            void connection.stop()
            if (connectionRef.current === connection) {
                connectionRef.current = null
            }
        }
    }, [url])

    return {
        status: status,
        invoke: invoke,
        subscribe: subscribe
    }
}