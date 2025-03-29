using System.Collections.Generic;
using UnityEngine;

public class Knight : BasePiece
{
    public override List<Vector2Int> GetAvailableMoves()
    {
        List<Vector2Int> moves = new List<Vector2Int>();

        int[,] directions = new int[,]
        {
            { -1, 0 }, { 1, 0 }, { 0, -1 }, { 0, 1 }
        };

        for (int d = 0; d < directions.GetLength(0); d++)
        {
            for (int step = 1; step <= 2; step++)
            {
                int newX = Position.x + directions[d, 0] * step;
                int newY = Position.y + directions[d, 1] * step;

                // if (board.IsValidTile(newX, newY) && board.IsTileEmpty(newX, newY))
                if(true)
                {
                    moves.Add(new Vector2Int(newX, newY));
                }
            }
        }

        return moves;
    }
}