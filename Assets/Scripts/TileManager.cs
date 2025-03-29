using UnityEngine;
using System.Collections.Generic;

public class TileManager : MonoBehaviour
{
    private GameObject[,] tiles = new GameObject[8, 8]; // 8x8 크기의 보드 타일 배열
    private HashSet<Vector2Int> highlightedTiles = new HashSet<Vector2Int>(); // 하이라이트된 타일 좌표 저장
    public Material originalTileMaterial; // 기본 타일 색상
    public Material selectedTileMaterial; // 선택된 타일 색상

    // 타일 배열 초기화 (보드 생성 시 호출)
    public void InitializeTiles(GameObject[] tileObjects)
    {
        int index = 0;
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                tiles[x, y] = tileObjects[index];
                index++;
            }
        }
    }

    // 특정 위치의 타일을 선택 상태로 변경
    public void HighlightTiles(List<Vector2Int> positions)
    {
        foreach (Vector2Int pos in positions)
        {
            if (IsValidPosition(pos))
            {
                Renderer renderer = tiles[pos.y, pos.x].GetComponent<Renderer>();
                renderer.material = selectedTileMaterial;
                highlightedTiles.Add(pos); // 하이라이트된 위치 저장
            }else
            {
                Debug.LogWarning($"Invalid position for highlighting: {pos}");
            }
        }
    }

    // 모든 타일을 원래 상태로 복구
    public void ResetTiles()
    {
        foreach (Vector2Int pos in highlightedTiles)
        {
            if (IsValidPosition(pos))
            {
                Renderer renderer = tiles[pos.x, pos.y].GetComponent<Renderer>();
                renderer.material = originalTileMaterial; // 기본 색상으로 복구
            }
        }
        highlightedTiles.Clear(); // 하이라이트된 위치 초기화
    }

    // 위치가 유효한지 확인 (보드 경계 체크)
    private bool IsValidPosition(Vector2Int position)
    {
        return position.x >= 0 && position.x < 8 && position.y >= 0 && position.y < 8;
    }

    // 특정 위치가 하이라이트된 상태인지 확인
    public bool IsHighlighted(Vector2Int position)
    {
        return highlightedTiles.Contains(position);
    }
    
}