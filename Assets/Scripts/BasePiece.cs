using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;

[RequireComponent(typeof(PhotonView), typeof(PhotonTransformView))]
public abstract class BasePiece : MonoBehaviourPun
{
    public Vector2Int Position { get; private set; }
    public int HP { get; set; } = 100;

    protected Board board;

    public virtual void Initialize(Vector2Int startPosition, Board boardRef)
    {
        Position = startPosition;
        board = boardRef;
    }

    public abstract List<Vector2Int> GetAvailableMoves();

    [PunRPC]
    public void Move(Vector2Int newPosition)
    {
        if (!photonView.IsMine) return;

        Position = newPosition;
        transform.position = new Vector3(newPosition.x + 0.5f, 0.05f, newPosition.y + 0.5f);
    }
}