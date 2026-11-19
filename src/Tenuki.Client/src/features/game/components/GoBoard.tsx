import type { BoardPoint } from "../domain/BoardPoint"
import type { Stone } from "../domain/Stone"

interface GoBoardProps {
    readonly size: number
    readonly onIntersectionClick: (point: BoardPoint) => void
    readonly stones: ReadonlyArray<Stone>
} 

function GoBoard({ size, onIntersectionClick, stones } : GoBoardProps) {
    console.log("GoBoard called")
    const boardSize = 100
    const margin = 5
    const spacing = (boardSize - 2 * margin)/(size - 1)
    const positions = Array.from({ length: size}).map((_, i) => toSvgCoord(i))
    const stoneRadius = spacing * 0.45

    function toSvgCoord(i: number) {
        return margin + i * spacing
    }

    function onBoardClick(event: React.MouseEvent<SVGSVGElement>) {
        const svgElement = event.currentTarget
        const matrix = svgElement.getScreenCTM()
        if (matrix === null) {
            return
        }

        const inverseMatrix = matrix.inverse()

        const point = new DOMPoint(event.clientX, event.clientY)
        console.log(point)

        const transformedPoint = point.matrixTransform(inverseMatrix)
        console.log(transformedPoint)

        const xBoard = Math.round((transformedPoint.x - margin) / spacing)
        const yBoard = Math.round((transformedPoint.y - margin) / spacing)

        if (xBoard < 0 || xBoard > size - 1 || yBoard < 0 || yBoard > size - 1) {
            return
        }

        console.log(`x: ${xBoard}, y: ${yBoard}`)

        onIntersectionClick({x: xBoard, y: yBoard})
    }

    return (
        <>
            <svg 
                onClick={onBoardClick} 
                viewBox={`0 0 ${boardSize} ${boardSize}`} 
                width="500" 
                height="500" 
                fill="rgba(0, 79, 92)"
                xmlns="http://www.w3.org/2000/svg"
            >
                <rect 
                    width="100%" 
                    height="100%" 
                    fill="0, 79, 92" 
                />
                {positions.map((pos) => 
                    <path 
                        key={pos} 
                        d={`M${pos} ${margin} L${pos} ${boardSize - margin}`} 
                        stroke="black" 
                    />
                )}
                {positions.map((pos) => 
                    <path 
                        key={pos} 
                        d={`M${margin} ${pos} L${boardSize - margin} ${pos}`} 
                        stroke="black" 
                    />
                )}
                {stones.map((stone) => 
                    <circle 
                        key={`${stone.point.x},${stone.point.y}`}
                        cx={toSvgCoord(stone.point.x)}
                        cy={toSvgCoord(stone.point.y)}
                        r={stoneRadius} 
                        fill={stone.color === "White" ? "white" : "black"}
                    />
                )}
            </svg>
        </>
    );
}

export default GoBoard;