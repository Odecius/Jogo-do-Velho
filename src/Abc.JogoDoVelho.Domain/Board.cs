using System.Collections.ObjectModel;

namespace Abc.JogoDoVelho.Domain;

public sealed class Board
{
    public const int CellCount = 9;

    private static readonly int[][] WinningLines =
    [
        [0, 1, 2],
        [3, 4, 5],
        [6, 7, 8],
        [0, 3, 6],
        [1, 4, 7],
        [2, 5, 8],
        [0, 4, 8],
        [2, 4, 6]
    ];

    private readonly PlayerPosition?[] _cells = new PlayerPosition?[CellCount];
    private readonly ReadOnlyCollection<PlayerPosition?> _readOnlyCells;

    public Board()
    {
        _readOnlyCells = Array.AsReadOnly(_cells);
    }

    public IReadOnlyList<PlayerPosition?> Cells => _readOnlyCells;

    internal bool IsCellEmpty(int cellIndex) => _cells[cellIndex] is null;

    internal void Place(PlayerPosition player, int cellIndex) => _cells[cellIndex] = player;

    internal bool IsFull => _cells.All(cell => cell is not null);

    internal bool HasWinningLine(PlayerPosition player) => WinningLines.Any(
        line => line.All(cellIndex => _cells[cellIndex] == player));
}

