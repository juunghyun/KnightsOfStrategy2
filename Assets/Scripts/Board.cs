using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using System.Collections;
using Hashtable = ExitGames.Client.Photon.Hashtable;
public class Board : MonoBehaviourPunCallbacks
{
    //UI
    
    private int originalLayer;
    private int myTeam = -1;
    private int myTurn = -1;

    //GAME OBJECT 관리
    public Camera camera;
    private GameObject lastHitObject;
    private GameObject[,] chessPieces = new GameObject[8,8]; //각 좌표에 할당될 기물들 생성
    private GameObject selectedPiece = null; // 현재 선택된 기물
    private Vector2Int selectedPiecePosition = new Vector2Int(-1, -1); // 선택된 기물의 좌표

    private GameObject myKnight;

    //기물 위치 관리
    [SerializeField] private float yOffset = 0.05f;
    [SerializeField] private float xzOffset = 0.5f;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    private void Update()
    {
        RaycastHit info;
        Ray ray = camera.ScreenPointToRay(Input.mousePosition);

        if(Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover")) && myTurn == 1) //타일 오브젝트에 레이 발사해서 정보 가져오기
        {
            GameObject hitObj = info.collider.gameObject;
            int tileLayer = LayerMask.NameToLayer("Tile");
            Vector3 hitPosition = info.point;


            //ray맞은 타일이랑 lastHit타일이랑 다르면, lastHit타일을 원래 레이어인 tile로 변경경
            if(lastHitObject != null && lastHitObject != hitObj)
            {
                lastHitObject.layer = originalLayer;
            }

            //ray맞은 타일이 tile 레이어면, 즉 레이에 맞게된 시작 시점이면, hover로 변경.
            if(hitObj.layer == tileLayer)
            {
                originalLayer = hitObj.layer;
                hitObj.layer = LayerMask.NameToLayer("Hover");
                lastHitObject = hitObj;
            }

            //클릭 시작
            if(Input.GetMouseButtonDown(0) && selectedPiece == null) //좌클릭 + 선택한 기물 없음
            {
                Debug.Log($"{(int)hitPosition.x}, {(int)hitPosition.z}");
                //해당 위치에 체스 기물이 있다면
                if(chessPieces[(int)hitPosition.x, (int)hitPosition.z] != null)
                {
                    Debug.Log($"{(int)hitPosition.x}, {(int)hitPosition.z}에는 기물 존재");
                    selectedPiece = chessPieces[(int)hitPosition.x, (int)hitPosition.z]; //그 기물 선택한걸로 취급
                    selectedPiecePosition = new Vector2Int((int)hitPosition.x, (int)hitPosition.z);
                }
            }

            if(Input.GetMouseButtonUp(0) && selectedPiece != null) //좌클릭 해제 + 선택한 기물 있음
            {
                Debug.Log($"{(int)hitPosition.x}, {(int)hitPosition.z}입니다");
                Debug.Log($"{selectedPiecePosition.x}, {selectedPiecePosition.y}입니다!");
                Vector3 newPos = new Vector3((int)hitPosition.x + xzOffset, yOffset, (int)hitPosition.z + xzOffset);
                chessPieces[selectedPiecePosition.x, selectedPiecePosition.y].transform.position = newPos; //이동동
                chessPieces[selectedPiecePosition.x, selectedPiecePosition.y] = null; //원래있던 위치 없애기
                chessPieces[(int)hitPosition.x, (int)hitPosition.z] = selectedPiece; //잡은 기물을 이동할 위치로 옮기기
                selectedPiece = null; //잡았던 기물 초기화
                selectedPiecePosition = new Vector2Int(-1, -1); //잡았던 기물 위치 초기화

                ChangeTurn();
            }


        }
        //레이가 안맞은 경우
        else
        {

            if(Input.GetMouseButtonUp(0) && selectedPiece != null) //좌클릭 해제 + 선택한 기물 있음
            {
                selectedPiece = null; //잡았던 기물 초기화
                selectedPiecePosition = new Vector2Int(-1, -1); //잡았던 기물 위치 초기화
            }

            if(lastHitObject != null)
            {
                lastHitObject.layer = originalLayer;
                lastHitObject = null;
            }   
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


    private void SpawnAllPieces()
    {
        //추가 예정정
        SpawnMyKnight();
    }


    

    private void SpawnMyKnight() //퀸 생성
    {
        Vector3 spawnPos = myTeam == 0 
        ? new Vector3(0 + xzOffset, 0 + yOffset, 0 + xzOffset)
        : new Vector3(0 + xzOffset, 0 + yOffset, 7 + xzOffset);

        string knightPrefab = myTeam == 0 ? "KnightLight" : "KnightBlack";
        Quaternion rotation = myTeam == 0 ? Quaternion.Euler(0, 0, 0) : Quaternion.Euler(0, 180, 0);

        myKnight = PhotonNetwork.Instantiate(knightPrefab, spawnPos, rotation);

        int gridX = myTeam == 0 ? 0 : 0;
        int gridY = myTeam == 0 ? 0 : 7;

        chessPieces[gridX, gridY] = myKnight;
        Debug.Log($"{myTeam}팀이고,{gridX}, {gridY}에{chessPieces[gridX, gridY]}생성완료");
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
