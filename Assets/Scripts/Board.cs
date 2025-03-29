using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Unity.VisualScripting;

[RequireComponent(typeof(PhotonView))]
public class Board : MonoBehaviourPunCallbacks
{

    [SerializeField] private TileManager tileManager; // TileManager 참조
    [SerializeField] private Camera mainCamera; // 메인 카메라 참조
    [SerializeField] private GameObject[] cubes; // 보드의 타일
    [SerializeField] private string knightWhitePrefabName = "knightWhite"; // White Knight 프리팹 이름
    [SerializeField] private string knightBlackPrefabName = "knightBlack"; // Black Knight 프리팹 이름
    private GameObject[,] chessPieces = new GameObject[8, 8]; // 보드의 기물 배열
    private GameObject selectedPiece = null; // 현재 선택된 기물
    private Vector2Int selectedPiecePosition = new Vector2Int(-1, -1); // 선택된 기물의 좌표

    private PhotonView photonView; // PhotonView 참조
    private int myTurn = -1; // 현재 플레이어의 턴 상태 (1 = 내 턴, 0 = 상대 턴)
    private int myTeam = -1;
    
    //UI
    private int originalLayer;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        photonView = GetComponent<PhotonView>();
        if (photonView == null)
        {
            Debug.LogError("PhotonView component not found!");
            return;
        }

        // 타일 초기화
        tileManager.InitializeTiles(cubes);

        // Photon 네트워크 관련 초기화
        PhotonNetwork.AddCallbackTarget(this);
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0)) // 좌클릭 입력 처리
        {
            HandleMouseClick();
        }
    }

    private void HandleMouseClick()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector3 hitPosition = hit.point;
            GameObject hitObject = hit.collider.gameObject;

            int x = Mathf.FloorToInt(hitPosition.x);
            int y = Mathf.FloorToInt(hitPosition.z);

            if (selectedPiece == null) // 기물을 선택하는 경우
            {
                SelectPiece(x, y);
            }
            else // 선택된 기물을 이동시키는 경우
            {   
                MoveSelectedPiece(x, y);
            }
        }
    }

    private void SelectPiece(int x, int y)
    {
        if (chessPieces[x, y] != null) // 해당 위치에 기물이 있는 경우
        {
            selectedPiece = chessPieces[x, y];
            selectedPiecePosition = new Vector2Int(x, y);

            // 이동 가능한 위치 계산 및 하이라이트 요청
            List<Vector2Int> availableMoves = selectedPiece.GetComponent<BasePiece>().GetAvailableMoves();
            tileManager.HighlightTiles(availableMoves);
        }
    }

    private void MoveSelectedPiece(int targetX, int targetY)
    {
        Vector2Int targetPosition = new Vector2Int(targetX, targetY);

        if (tileManager.IsHighlighted(targetPosition)) // 타일이 하이라이트된 위치인지 확인
        {
            BasePiece pieceScript = selectedPiece.GetComponent<BasePiece>();
            pieceScript.Move(targetPosition); // 기물 이동

            chessPieces[selectedPiecePosition.x, selectedPiecePosition.y] = null; // 기존 위치 비우기
            chessPieces[targetX, targetY] = selectedPiece; // 새 위치에 기물 배치

            tileManager.ResetTiles(); // 타일 상태 초기화
            selectedPiece = null; // 선택된 기물 초기화

            ChangeTurn(); // 턴 변경 처리
        }
    }

    public override void OnJoinedRoom()
    {
        StartCoroutine(SetTeamAndSpawn());
    }

    private IEnumerator SetTeamAndSpawn()
    {
        // 커스텀 프로퍼티 설정
        int teamValue = PhotonNetwork.IsMasterClient ? 0 : 1;
        int turnValue = PhotonNetwork.IsMasterClient ? 1 : 0;
        Hashtable props = new Hashtable();
        props["MyTeam"] = teamValue;
        props["Myturn"] = turnValue;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props); //SetCustomProperties는 비동기 작업! -> yield return으로 대기하자자

        // 프로퍼티 업데이트 대기 (최대 2초)
        float timeout = Time.time + 5f;
        while (!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("MyTeam") && Time.time < timeout)
        {
            yield return null;
        }

        // 팀 정보 확인 후 생성
        FindMyTeam();
        //턴 정보 생성
        FindMyTurn();
        if(myTeam != -1 && myTurn != -1)
        {
            SpawnAllPieces();
        }
        else
        {
            Debug.LogError("팀 설정 실패!");
        }
    }

    private void FindMyTeam()
    {
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("MyTeam", out object teamValue))
        {
            myTeam = (int)teamValue;
            Debug.Log($"내 팀 확인 완료: {(myTeam == 0 ? "팀 A" : "팀 B")}");
        }
        else
        {
            Debug.LogError("팀 정보를 찾을 수 없습니다!");
        }
    }

    //턴 관련
    private void FindMyTurn()
    {
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("Myturn", out object turnValue))
        {
            myTurn = (int)turnValue;
            Debug.Log($"내 턴 확인 완료: {(myTurn == 1 ? "내 턴!" : "상대 턴!")}");
        }
        else
        {
            Debug.LogError("턴턴 정보를 찾을 수 없습니다!");
        }
    }

    private void ChangeTurn()
    {
        myTurn = (myTurn == 1) ? 0 : 1;

        // 네트워크 동기화
        Hashtable props = new Hashtable();
        props["Myturn"] = myTurn;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        // UI 업데이트 등 필요한 작업 수행
        Debug.Log($"턴 변경: {(myTurn == 1 ? "내 턴" : "상대 턴")}");
    }

    //기물 관련련
    private void SpawnAllPieces()
    {
        // 자신의 팀에 맞는 나이트만 생성
        if (myTeam == 0) // 팀 A (White)
        {
            SpawnPiece<Knight>(new Vector2Int(0, 0)); // White Knight 생성
        }
        else if (myTeam == 1) // 팀 B (Black)
        {
            SpawnPiece<Knight>(new Vector2Int(7, 7)); // Black Knight 생성
        }
    }

    private void SpawnPiece<T>(Vector2Int position) where T : BasePiece
    {
        // 팀에 따라 사용할 프리팹 이름 결정
        string prefabName = (myTeam == 0) ? knightWhitePrefabName : knightBlackPrefabName;

        // PhotonNetwork를 통해 기물 생성
        GameObject pieceObj = PhotonNetwork.Instantiate(prefabName,
        new Vector3(position.x + 0.5f, 0.05f, position.y + 0.5f), Quaternion.identity);

        // 생성된 기물 초기화
        T pieceScript = pieceObj.GetComponent<T>();
        pieceScript.Initialize(position, this);

        // 보드 배열에 기물 등록
        chessPieces[position.x, position.y] = pieceObj;
    }


    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (targetPlayer != PhotonNetwork.LocalPlayer) 
        {
            if (changedProps.ContainsKey("Myturn")) //내가 아닌 다른 누군가가 턴이 바뀌면
            {
                if((int)changedProps["Myturn"] == 1) //그사람의 턴이 1이댔다면
                {
                    myTurn = 0; //내턴은 0
                }
                else
                {
                    myTurn = 1;
                }
                
                Debug.Log($"상대방 턴 변경: {(myTurn == 1 ? "내 턴" : "상대 턴")}");
                // UI 업데이트 등 필요한 작업 수행
            }
        }
    }

}
