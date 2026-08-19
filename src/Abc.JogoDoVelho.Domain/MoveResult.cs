namespace Abc.JogoDoVelho.Domain;

public enum MoveResult
{
    Success,
    InvalidCell,
    NotPlayersTurn,
    CellOccupied,
    GameFinished
}

