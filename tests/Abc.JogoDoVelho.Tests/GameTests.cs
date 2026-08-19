using Abc.JogoDoVelho.Domain;

namespace Abc.JogoDoVelho.Tests;

public sealed class GameTests
{
    public static TheoryData<int, int, int> WinningLines => new()
    {
        { 0, 1, 2 },
        { 3, 4, 5 },
        { 6, 7, 8 },
        { 0, 3, 6 },
        { 1, 4, 7 },
        { 2, 5, 8 },
        { 0, 4, 8 },
        { 2, 4, 6 }
    };

    [Fact]
    public void NewGameStartsInProgressWithPlayer1AndEmptyBoard()
    {
        var game = new Game();

        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.Equal(PlayerPosition.Player1, game.CurrentPlayer);
        Assert.Null(game.Winner);
        Assert.Equal(Board.CellCount, game.Board.Cells.Count);
        Assert.All(game.Board.Cells, cell => Assert.Null(cell));
    }

    [Fact]
    public void ValidMovesAlternateCurrentPlayer()
    {
        var game = new Game();

        Assert.Equal(MoveResult.Success, game.PlaceMove(PlayerPosition.Player1, 4));
        Assert.Equal(PlayerPosition.Player2, game.CurrentPlayer);
        Assert.Equal(PlayerPosition.Player1, game.Board.Cells[4]);

        Assert.Equal(MoveResult.Success, game.PlaceMove(PlayerPosition.Player2, 0));
        Assert.Equal(PlayerPosition.Player1, game.CurrentPlayer);
        Assert.Equal(PlayerPosition.Player2, game.Board.Cells[0]);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(9)]
    [InlineData(10)]
    public void MoveOutsideBoardIsRejectedWithoutChangingState(int cellIndex)
    {
        var game = new Game();

        Assert.Equal(MoveResult.InvalidCell, game.PlaceMove(PlayerPosition.Player1, cellIndex));
        Assert.Equal(PlayerPosition.Player1, game.CurrentPlayer);
        Assert.All(game.Board.Cells, cell => Assert.Null(cell));
    }

    [Fact]
    public void MoveOutsideCurrentPlayersTurnIsRejected()
    {
        var game = new Game();

        Assert.Equal(MoveResult.NotPlayersTurn, game.PlaceMove(PlayerPosition.Player2, 0));
        Assert.Null(game.Board.Cells[0]);
    }

    [Fact]
    public void MoveOnOccupiedCellIsRejected()
    {
        var game = new Game();
        game.PlaceMove(PlayerPosition.Player1, 0);

        Assert.Equal(MoveResult.CellOccupied, game.PlaceMove(PlayerPosition.Player2, 0));
        Assert.Equal(PlayerPosition.Player1, game.Board.Cells[0]);
        Assert.Equal(PlayerPosition.Player2, game.CurrentPlayer);
    }

    [Theory]
    [MemberData(nameof(WinningLines))]
    public void Player1WinsWithEveryWinningLine(int first, int second, int third)
    {
        var game = new Game();
        var opponentCells = Enumerable.Range(0, Board.CellCount)
            .Except([first, second, third])
            .Take(2)
            .ToArray();

        game.PlaceMove(PlayerPosition.Player1, first);
        game.PlaceMove(PlayerPosition.Player2, opponentCells[0]);
        game.PlaceMove(PlayerPosition.Player1, second);
        game.PlaceMove(PlayerPosition.Player2, opponentCells[1]);

        Assert.Equal(MoveResult.Success, game.PlaceMove(PlayerPosition.Player1, third));
        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Equal(PlayerPosition.Player1, game.Winner);
        Assert.Equal(PlayerPosition.Player1, game.CurrentPlayer);
    }

    [Fact]
    public void Player2CanWinAndIsReportedAsWinner()
    {
        var game = new Game();
        Play(game, 0, 3, 1, 4, 8, 5);

        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Equal(PlayerPosition.Player2, game.Winner);
    }

    [Fact]
    public void MoveAfterWinIsRejectedWithoutChangingBoard()
    {
        var game = new Game();
        Play(game, 0, 3, 1, 4, 2);

        Assert.Equal(MoveResult.GameFinished, game.PlaceMove(PlayerPosition.Player2, 5));
        Assert.Null(game.Board.Cells[5]);
    }

    [Fact]
    public void FullBoardWithoutWinnerIsDraw()
    {
        var game = new Game();
        Play(game, 0, 1, 2, 4, 3, 5, 7, 6, 8);

        Assert.Equal(GameStatus.Draw, game.Status);
        Assert.Null(game.Winner);
    }

    [Fact]
    public void MoveAfterDrawIsRejected()
    {
        var game = new Game();
        Play(game, 0, 1, 2, 4, 3, 5, 7, 6, 8);

        Assert.Equal(MoveResult.GameFinished, game.PlaceMove(PlayerPosition.Player2, 0));
    }

    [Fact]
    public void ExposedCellsCannotBeChangedByConsumer()
    {
        var game = new Game();
        var cells = Assert.IsAssignableFrom<IList<PlayerPosition?>>(game.Board.Cells);

        Assert.Throws<NotSupportedException>(() => cells[0] = PlayerPosition.Player2);
        Assert.Null(game.Board.Cells[0]);
    }

    private static void Play(Game game, params int[] cells)
    {
        foreach (var cell in cells)
        {
            Assert.Equal(MoveResult.Success, game.PlaceMove(game.CurrentPlayer, cell));
        }
    }
}

