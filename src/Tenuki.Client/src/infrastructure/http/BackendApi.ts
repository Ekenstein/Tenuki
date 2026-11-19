import type { Error } from "../../commons/Error"
import type { TimeSettings } from "../../features/game/domain/GameState"
import type { GameCommand } from "../../features/game/protocol/GameCommand"

export interface GetGameResponse {
    readonly stateToken: string
    readonly revision: number
}

export interface PostGameRequest {
    readonly boardSize: number
    readonly komi: number
    readonly handicap: number,
    readonly timeSettings: TimeSettings
}

export interface PostGameResponse {
    readonly gameId: string
    readonly stateToken: string
    readonly revision: number
}

export interface PostGameCommandRequest {
    readonly stateToken: string
    readonly command: GameCommand
}

export type GameCommandResponse_Base = {
    readonly stateToken: string
    readonly revision: number
}

export type GameCommandResponse_Applied = GameCommandResponse_Base & {
    readonly $type: "Applied"
}

export type GameCommandResponse_Rejected = GameCommandResponse_Base & {
    readonly $type: "Rejected"
    readonly errors: ReadonlyArray<Error>
}

export type GameCommandResponse_TimeExpired = GameCommandResponse_Base & {
    readonly $type: "TimeExpired"
}

export type GameCommandResponse_Conflict = GameCommandResponse_Base & {
    readonly $type: "Conflict"
}

export type GameCommandResponse_Count = GameCommandResponse_Base & {
    readonly $type: "Count"
}

export type GameCommandResponse =
    | GameCommandResponse_Applied
    | GameCommandResponse_Rejected
    | GameCommandResponse_TimeExpired
    | GameCommandResponse_Conflict
    | GameCommandResponse_Count

export const BackendApi = {
    games: {
        get: async (gameId: string): Promise<GetGameResponse> => {
            const response = await fetch(`/api/games/${gameId}`)
            if (!response.ok) {
                throw new Error("Failed to get game.")
            }

            return await response.json()
        },

        post: async (request: PostGameRequest): Promise<PostGameResponse> => {
            const httpRequest = new Request("/api/games", {
                method: "POST",
                body: JSON.stringify(request),
                headers: {
                    "Accept": "application/json",
                    "Content-Type": "application/json"
                }
            })

            const response = await fetch(httpRequest)
            if (!response.ok) {
                throw new Error("Failed to create the game")
            }

            return await response.json()
        },

        command: async(request: PostGameCommandRequest): Promise<GameCommandResponse> => {
            const httpRequest = new Request("/api/games/command", {
                method: "POST",
                body: JSON.stringify(request),
                headers: {
                    "Accept": "application/json",
                    "Content-Type": "application/json"
                }
            })

            const response = await fetch(httpRequest)
            if (!response.ok) {
                throw new Error("Failed to post the command")
            }

            return await response.json()
        }
    }
}