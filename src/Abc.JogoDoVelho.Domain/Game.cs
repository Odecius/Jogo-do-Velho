namespace Abc.JogoDoVelho.Domain;

public sealed class Game
{
    public Game()
    {
        Board = new Board();
        CurrentPlayer = PlayerPosition.Player1;
        Status = GameStatus.InProgress;
    }

    public Board Board { get; }

    public PlayerPosition CurrentPlayer { get; private set; }

    public GameStatus Status { get; private set; }

    public PlayerPosition? Winner { get; private set; }

    public MoveResult PlaceMove(PlayerPosition player, int cellIndex)
    {
        if (Status is not GameStatus.InProgress)
        {
            return MoveResult.GameFinished;
        }

        if (cellIndex is < 0 or >= Board.CellCount)
        {
            return MoveResult.InvalidCell;
        }

        if (player != CurrentPlayer)
        {
            return MoveResult.NotPlayersTurn;
        }

        if (!Board.IsCellEmpty(cellIndex))
        {
            return MoveResult.CellOccupied;
        }

        Board.Place(player, cellIndex);

        if (Board.HasWinningLine(player))
        {
            Status = GameStatus.Won;
            Winner = player;
        }
        else if (Board.IsFull)
        {
            Status = GameStatus.Draw;
        }
        else
        {
            CurrentPlayer = player == PlayerPosition.Player1
                ? PlayerPosition.Player2
                : PlayerPosition.Player1;
        }

        return MoveResult.Success;
    }
}

