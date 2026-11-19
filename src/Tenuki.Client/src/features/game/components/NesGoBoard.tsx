import { useId } from "react"
import type { Color, Stone } from "../domain/Stone"
import type { BoardPoint } from "../domain/BoardPoint"

export interface NesGoBoardProps {
    readonly size: number
    readonly stones: readonly Stone[]
    readonly onIntersectionClick?: (point: BoardPoint) => void
    readonly lastMove?: BoardPoint | null
}

const BG = "#004F5C"
const WOOD = "#A25C00"
const BLACK = "#000000"
const WHITE = "#FFFFFF"
const MARKER = "#D4F0A0"
const MARKER_SHADOW = "#565D18"

function starPoints(size: number): readonly [number, number][] {
    if (size === 9) return [[2,2],[6,2],[4,4],[2,6],[6,6]]
    if (size === 13) return [[3,3],[9,3],[3,9],[9,9],[6,6]]
    if (size === 19) {
        const positions = [3,9,15]
        return positions.flatMap(y => positions.map(x => [x,y] as [number,number]))
    }
    return []
}

function PixelStone({ x, y, color, width, height, marked }: {
    x: number; y: number; color: Color; width: number; height: number; marked: boolean
}) {
    const isBlack = color === "Black"
    return (
        <svg x={x - width / 2} y={y - height / 2} width={width} height={height}
             viewBox="0 0 7 8" preserveAspectRatio="none" overflow="visible">
            <rect width="7" height="8" fill={isBlack ? BLACK : WHITE} />
            {/* Pixelated corners, exactly as the Kotlin theme builds them. */}
            <g fill={WOOD}>
                <rect x="0" y="0" width="2" height="1" />
                <rect x="5" y="0" width="2" height="1" />
                <rect x="0" y="1" width="1" height="1" />
                <rect x="6" y="1" width="1" height="1" />
                <rect x="0" y="7" width="2" height="1" />
                <rect x={isBlack ? 5 : 6} y="7" width={isBlack ? 2 : 1} height="1" />
                <rect x="0" y="6" width="1" height="1" />
                {isBlack && <rect x="6" y="6" width="1" height="1" />}
            </g>
            {isBlack ? (
                <g fill={WHITE}>
                    <rect x="2" y="1" width="1" height="1" />
                    <rect x="1" y="2" width="1" height="3" />
                </g>
            ) : (
                <g fill={BLACK}>
                    <rect x="6" y="5" width="1" height="2" />
                    <rect x="5" y="6" width="2" height="1" />
                    <rect x="2" y="7" width="4" height="1" />
                </g>
            )}
            {marked && (
                <>
                    <g fill={MARKER}>
                        <rect x="0" y="0" width="3" height="1" />
                        <rect x="4" y="0" width="3" height="1" />
                        <rect x="0" y="1" width="1" height="1" />
                        <rect x="6" y="1" width="1" height="1" />
                        <rect x="0" y="7" width="3" height="1" />
                        <rect x="4" y="7" width="3" height="1" />
                        <rect x="0" y="6" width="1" height="1" />
                        <rect x="6" y="6" width="1" height="1" />
                        <rect x="2.5" y="3" width="2" height="2" />
                    </g>
                    <g fill={MARKER_SHADOW}>
                        <rect x="2.5" y="5" width="2" height="1" />
                        <rect x="1" y="1" width="2" height="1" />
                        <rect x="4" y="1" width="2" height="1" />
                        <rect x="0" y="2" width="1" height="1" />
                        <rect x="6" y="2" width="1" height="1" />
                        <rect x="0" y="8" width="3" height="1" />
                        <rect x="4" y="8" width="3" height="1" />
                    </g>
                </>
            )}
        </svg>
    )
}

export default function NesGoBoard({ size, stones, onIntersectionClick, lastMove }: NesGoBoardProps) {
    const id = useId().replace(/:/g, "")
    const canvas = 900
    const boardX = 45
    const boardY = 45
    const boardWidth = 810
    const boardHeight = 810
    const pixel = 5
    const thickness = boardHeight * 0.13
    const perspective = (boardHeight + thickness) * 0.01
    const pillarHeight = 7 * pixel
    const playHeight = boardHeight - thickness - perspective - pillarHeight
    const offsetX = boardWidth * 0.03
    const offsetY = playHeight * 0.03
    const left = boardX + offsetX
    const top = boardY + offsetY
    const stepX = (boardWidth - offsetX * 2) / (size - 1)
    const stepY = (playHeight - offsetY * 2) / (size - 1)
    const stoneWidth = stepX * 0.9
    const stoneHeight = stepY * 0.9
    const thicknessRows = Math.floor(thickness / pixel)
    const thicknessCols = Math.floor(boardWidth / pixel)
    const px = (x: number) => left + x * stepX
    const py = (y: number) => top + y * stepY

    if (!Number.isInteger(size) || size < 2) throw new Error("Board size must be at least 2")

    return (
        <svg viewBox={`0 0 ${canvas} ${canvas}`} width="100%" role="img"
             aria-label={`${size} by ${size} NES-style Go board`}
             xmlns="http://www.w3.org/2000/svg" style={{ display: "block", imageRendering: "pixelated" }}>
            <rect width={canvas} height={canvas} fill={BG} />
            {/* Drop shadow */}
            <rect x={boardX * 1.7} y={boardY * 1.7} width={boardWidth} height={boardHeight} fill={BLACK} />
            {/* Wooden playing surface */}
            <rect x={boardX} y={boardY} width={boardWidth} height={playHeight} fill={WOOD} />
            {/* Checkerboard-like alternating pixel pattern on the board's front edge */}
            <rect x={boardX} y={boardY + playHeight} width={thicknessCols * pixel}
                  height={thicknessRows * pixel} fill={BLACK} />
            {Array.from({ length: thicknessRows }, (_, row) =>
                Array.from({ length: thicknessCols }, (_, col) =>
                    ((row + col) % 2 === 1) &&
                    <rect key={`edge-${row}-${col}`} x={boardX + col * pixel}
                          y={boardY + playHeight + row * pixel} width={pixel} height={pixel} fill={WOOD} />
                )
            )}
            {/* Two blocky legs; shadowed horizontal strips */}
            {[boardX + boardWidth / 6 - 35, boardX + boardWidth * 5 / 6 - 35].map((legX, i) => {
                const legY = boardY + playHeight + thicknessRows * pixel
                return (
                    <g key={i}>
                        {[0, 2, 4, 6].map((row) => {
                            const inset = row === 0 || row === 6 ? 2 * pixel : 0
                            const count = row === 0 || row === 6 ? 11 : 15
                            return Array.from({ length: count }, (_, col) =>
                                <rect key={`${row}-${col}`} x={legX + inset + col * pixel}
                                      y={legY + row * pixel} width={pixel} height={pixel}
                                      fill={col % 2 === 0 ? WOOD : BLACK} />
                            )
                        })}
                    </g>
                )
            })}
            {/* Grid */}
            <g stroke={BLACK} strokeWidth={pixel}>
                {Array.from({ length: size }, (_, x) =>
                    <line key={`v${x}`} x1={px(x)} x2={px(x)} y1={py(0)} y2={py(size - 1)} />
                )}
                {Array.from({ length: size }, (_, y) =>
                    <line key={`h${y}`} x1={px(0)} x2={px(size - 1)} y1={py(y)} y2={py(y)} />
                )}
            </g>
            {/* Square hoshi */}
            {starPoints(size).map(([x, y]) =>
                <rect key={`star-${x}-${y}`} x={px(x) - stoneWidth * 2 / 7}
                      y={py(y) - stoneHeight * 3 / 16}
                      width={stoneWidth * 4 / 7} height={stoneHeight * 3 / 8} fill={BLACK} />
            )}
            {/* Stones */}
            {stones.map(stone =>
                <PixelStone key={`${stone.point.x}-${stone.point.y}`} x={px(stone.point.x)} y={py(stone.point.y)}
                            color={stone.color} width={stoneWidth} height={stoneHeight}
                            marked={lastMove?.x === stone.point.x && lastMove?.y === stone.point.y} />
            )}
            {/* Hit targets are transparent and rendered last */}
            {onIntersectionClick && Array.from({ length: size * size }, (_, index) => {
                const x = index % size
                const y = Math.floor(index / size)
                return <rect key={`hit-${x}-${y}`} x={px(x) - stepX / 2} y={py(y) - stepY / 2}
                             width={stepX} height={stepY} fill="transparent"
                             style={{ cursor: "pointer" }}
                             onClick={() => onIntersectionClick({ x, y })} />
            })}
            <title id={id}>NES-inspired Go board</title>
        </svg>
    )
}
