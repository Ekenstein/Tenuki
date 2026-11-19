interface BoardSizeSelectorProps {
    readonly selectedSize: number
    readonly onSelectedSizeChanged: (selectedSize: number) => void
}

function BoardSizeSelector({ selectedSize, onSelectedSizeChanged } : BoardSizeSelectorProps) {
    console.log("BoardSizeSelector called")
    function isSizeSelected(size: number) {
        return selectedSize === size
    }

    return (
        <>
            <button disabled={isSizeSelected(19)} onClick={() => onSelectedSizeChanged(19)}>19x19</button>
            <button disabled={isSizeSelected(13)} onClick={() => onSelectedSizeChanged(13)}>13x13</button>
            <button disabled={isSizeSelected(9)} onClick={() => onSelectedSizeChanged(9)}>9x9</button>
        </>
    );
}

export default BoardSizeSelector;