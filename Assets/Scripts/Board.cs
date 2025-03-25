using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private GameObject[] cubes; // 보드의 타일
    private GameObject selectedPiece = null; // 현재 선택된 기물
    private bool isHoldingStart = false;
    [SerializeField] private float selectedYOffset = 1f;
    private Vector2Int selectedPiecePosition = new Vector2Int(-1, -1); // 선택된 기물의 좌표
    private Vector2Int selectedCheckPosition = new Vector2Int(-1, -1); //확인용 기물 좌표
    public GameObject vfxSelectedPrefab; //선택한 기물 아래 생성할 vfx
    private GameObject selectedVfx; //선택한 기물 효과 vfx 관리용
    private GameObject myKnight;

    //GAME OBJECT 타일 material
    public Material originalTileMaterial;
    public Material rainbowTileMaterial;
    public Material selectedTileMaterial;
    private Renderer selectedTileRenderer;

    //기물 위치 관리
    [SerializeField] private float yOffset = 0.05f;
    [SerializeField] private float xzOffset = 0.5f;

    //기물이 갈 수 있는 위치 관리
    List<Vector2Int> availablePosition = new List<Vector2Int>();

    //애니메이션 처리 관리
    public float moveDuration = 0.5f; // 이동 애니메이션 지속 시간

    private Animator pieceAnimator;


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

            //첫번째 클릭 시작
            if(Input.GetMouseButtonDown(0) && selectedPiece == null) //좌클릭 + 선택한 기물 없음 + 들고잇는 기물 없음
            {
                Debug.Log($"{(int)hitPosition.x}, {(int)hitPosition.z}");
                //해당 위치에 체스 기물이 있다면
                if(chessPieces[(int)hitPosition.x, (int)hitPosition.z] != null)
                {
                    Debug.Log($"{(int)hitPosition.x}, {(int)hitPosition.z}에는 기물 존재");
                    selectedPiece = chessPieces[(int)hitPosition.x, (int)hitPosition.z]; //그 기물 선택한걸로 취급
                    pieceAnimator = selectedPiece.GetComponent<Animator>();
                    selectedTileRenderer = hitObj.GetComponent<Renderer>();
                    selectedPiecePosition = new Vector2Int((int)hitPosition.x, (int)hitPosition.z); //해당 기물 좌표 저장
                    isHoldingStart = true;
                }
            }

            if(Input.GetMouseButtonDown(0) && selectedPiece != null && !isHoldingStart) //좌클릭 + 선택한 기물 있음
            {
                selectedCheckPosition = new Vector2Int((int)hitPosition.x, (int)hitPosition.z);
            }

            if(Input.GetMouseButtonUp(0) && selectedPiece != null && !isHoldingStart) //좌클릭 해제 + 선택한 기물 있음 + 첫 기물 선택 후 떼는 동작이 아님
            {
                Debug.Log($"{(int)hitPosition.x}, {(int)hitPosition.z}입니다");
                Debug.Log($"{selectedPiecePosition.x}, {selectedPiecePosition.y}입니다!");

                //같은곳 선택했으면 이동한것으로 처리 x
                if((int)hitPosition.x == selectedPiecePosition.x && (int)hitPosition.z == selectedPiecePosition.y)
                {
                    //기물 착지시키기
                    Vector3 newPos = new Vector3(selectedPiecePosition.x + xzOffset, yOffset, selectedPiecePosition.y + xzOffset);
                    chessPieces[selectedPiecePosition.x, selectedPiecePosition.y].transform.position = newPos; //이동
                    //기물 아래 vfx 없애기
                    Destroy(selectedVfx);
                    //뜬 기물 아래 타일 효과 없애기
                    selectedTileRenderer.material = originalTileMaterial;

                    selectedPiece = null; //잡았던 기물 초기화
                    selectedTileRenderer = null;
                    pieceAnimator = null;
                    selectedPiecePosition = new Vector2Int(-1, -1); //잡았던 기물 위치 초기화
                    selectedCheckPosition = new Vector2Int(-1, -1); //확인용 좌표 초기화

                    availableTileRefresh();
                    availablePosition = new List<Vector2Int>(); //갈 수 있는 위치 초기화
                }
                //누른곳과 다른곳에 떨궈도 이동한것으로 처리 x
                else if((int)hitPosition.x != selectedCheckPosition.x && (int)hitPosition.z!=selectedCheckPosition.y)
                {
                    //기물 착지시키기
                    Vector3 newPos = new Vector3(selectedPiecePosition.x + xzOffset, yOffset, selectedPiecePosition.y + xzOffset);
                    chessPieces[selectedPiecePosition.x, selectedPiecePosition.y].transform.position = newPos; //이동
                    //기물 아래 vfx 없애기
                    Destroy(selectedVfx);
                    //뜬 기물 아래 타일 효과 없애기
                    selectedTileRenderer.material = originalTileMaterial;

                    selectedPiece = null; //잡았던 기물 초기화
                    selectedTileRenderer = null;
                    pieceAnimator = null;
                    selectedPiecePosition = new Vector2Int(-1, -1); //잡았던 기물 위치 초기화
                    selectedCheckPosition = new Vector2Int(-1, -1); //확인용 좌표 초기화

                    availableTileRefresh();
                    availablePosition = new List<Vector2Int>(); //갈 수 있는 위치 초기화
                }
                else if((int)hitPosition.x == selectedCheckPosition.x && (int)hitPosition.z ==selectedCheckPosition.y)
                {
                    if(availablePosition.Contains(selectedCheckPosition)){ //갈 수 있는 위치라면 가기
                        //선택 종료했으니 vfx 이펙트 삭제
                        Destroy(selectedVfx);
                        //뜬 기물 아래 타일 효과 없애기
                        selectedTileRenderer.material = originalTileMaterial;

                        // Vector3 newPos = new Vector3((int)hitPosition.x + xzOffset, yOffset, (int)hitPosition.z + xzOffset);
                        // chessPieces[selectedPiecePosition.x, selectedPiecePosition.y].transform.position = newPos; //이동

                        Vector3 startPos = new Vector3(selectedPiecePosition.x + xzOffset, yOffset, selectedPiecePosition.y + xzOffset);
                        Vector3 endPos = new Vector3((int)hitPosition.x + xzOffset, yOffset, (int)hitPosition.z + xzOffset);
                        StartCoroutine(MovePieceSmooth(chessPieces[selectedPiecePosition.x, selectedPiecePosition.y], startPos, endPos));

                        chessPieces[selectedPiecePosition.x, selectedPiecePosition.y] = null; //원래있던 위치 없애기
                        chessPieces[(int)hitPosition.x, (int)hitPosition.z] = selectedPiece; //잡은 기물을 이동할 위치로 옮기기
                        selectedPiece = null; //잡았던 기물 초기화
                        selectedTileRenderer = null;
                        pieceAnimator = null;
                        selectedPiecePosition = new Vector2Int(-1, -1); //잡았던 기물 위치 초기화
                        availableTileRefresh();
                        availablePosition = new List<Vector2Int>(); //갈 수 있는 위치 초기화

                        ChangeTurn();
                    }
                    else //갈 수 없는 위치라면 
                    {
                        //기물 착지시키기
                        Vector3 newPos = new Vector3(selectedPiecePosition.x + xzOffset, yOffset, selectedPiecePosition.y + xzOffset);
                        chessPieces[selectedPiecePosition.x, selectedPiecePosition.y].transform.position = newPos; //이동
                        //기물 아래 vfx 없애기
                        Destroy(selectedVfx);
                        //뜬 기물 아래 타일 효과 제거
                        selectedTileRenderer.material = originalTileMaterial;

                        selectedPiece = null; //잡았던 기물 초기화
                        selectedTileRenderer = null;
                        pieceAnimator = null;

                        availableTileRefresh();
                        availablePosition = new List<Vector2Int>(); //갈 수 있는 위치 초기화
                        selectedPiecePosition = new Vector2Int(-1, -1); //잡았던 기물 위치 초기화
                    }
                    
                }

            }

            //첫번째 고르고 타일에다가 커서를 두고 떼는 경우
            if(Input.GetMouseButtonUp(0) && isHoldingStart)
            {
                //기물 띄우기
                Vector3 newPos = new Vector3(selectedPiecePosition.x + xzOffset, selectedYOffset + yOffset, selectedPiecePosition.y + xzOffset);
                chessPieces[selectedPiecePosition.x, selectedPiecePosition.y].transform.position = newPos; //이동
                //기물 아래 vfx 나오게 하기
                Vector3 vfxPos = new Vector3(selectedPiecePosition.x + xzOffset, yOffset, selectedPiecePosition.y + xzOffset);
                selectedVfx = Instantiate(vfxSelectedPrefab, vfxPos, Quaternion.identity);
                //뜬 기물 아래 타일 효과
                selectedTileRenderer.material = rainbowTileMaterial;
                //고른 기물이 갈 수 있는 위치 계산
                findAvailableMoves(selectedPiece, selectedPiecePosition.x, selectedPiecePosition.y);

                isHoldingStart = false;
            }


        }
        //레이가 안맞은 경우
        else
        {
            //첫번째 고르고 밖에다 커서를 두고 떼는 경우
            if(Input.GetMouseButtonUp(0) && isHoldingStart)
            {
                //기물 띄우기
                Vector3 newPos = new Vector3(selectedPiecePosition.x + xzOffset, selectedYOffset + yOffset, selectedPiecePosition.y + xzOffset);
                chessPieces[selectedPiecePosition.x, selectedPiecePosition.y].transform.position = newPos; //이동
                //기물 아래 vfx 나오게 하기
                Vector3 vfxPos = new Vector3(selectedPiecePosition.x + xzOffset, yOffset, selectedPiecePosition.y + xzOffset);
                selectedVfx = Instantiate(vfxSelectedPrefab, vfxPos, Quaternion.identity);
                //뜬 기물 아래 타일 효과
                selectedTileRenderer.material = rainbowTileMaterial;

                //고른 기물이 갈 수 있는 위치 계산
                findAvailableMoves(selectedPiece, selectedPiecePosition.x, selectedPiecePosition.y);

                isHoldingStart = false;
            }

            //고른 상태에서 밖에 커서를 두고 떼는 경우
            if(Input.GetMouseButtonUp(0) && selectedPiece != null && !isHoldingStart)
            {
                //기물 착지시키기
                Vector3 newPos = new Vector3(selectedPiecePosition.x + xzOffset, yOffset, selectedPiecePosition.y + xzOffset);
                chessPieces[selectedPiecePosition.x, selectedPiecePosition.y].transform.position = newPos; //이동
                //기물 아래 vfx 없애기
                Destroy(selectedVfx);
                //뜬 기물 아래 타일 효과 제거
                selectedTileRenderer.material = originalTileMaterial;

                selectedPiece = null; //잡았던 기물 초기화
                selectedTileRenderer = null;
                pieceAnimator = null;
                selectedPiecePosition = new Vector2Int(-1, -1); //잡았던 기물 위치 초기화
                
                availableTileRefresh();
                availablePosition = new List<Vector2Int>(); //갈 수 있는 위치 초기화
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

    //기물 관련련
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

    private void findAvailableMoves(GameObject objectName, int x, int y)
    {
        if (objectName == myKnight) // 나이트인 경우
        {
            availablePosition = new List<Vector2Int>();

            // 이동 방향 (상, 하, 좌, 우)
            int[,] directions = new int[,] {
                { -1, 0 }, { 1, 0 }, { 0, -1 }, { 0, 1 } // 위, 아래, 왼쪽, 오른쪽
            };

            for (int d = 0; d < 4; d++) // 네 방향 탐색
            {
                for (int step = 1; step <= 2; step++) // 최대 2칸까지 이동 가능
                {
                    int newX = x + directions[d, 0] * step;
                    int newY = y + directions[d, 1] * step;

                    // 보드 범위를 벗어나지 않는지 확인
                    if (newX >= 0 && newX < 8 && newY >= 0 && newY < 8)
                    {
                        // 해당 위치에 기물이 없을 경우만 추가
                        if (chessPieces[newX, newY] == null)
                        {
                            availablePosition.Add(new Vector2Int(newX, newY));
                        }
                        else // 기물이 있으면 더 이상 해당 방향으로 이동 불가
                        {
                            break;
                        }
                    }
                }
            }

            //availablePosition으로 해당 타일 변경하기
            foreach (Vector2Int pos in availablePosition)
            {
                int index = pos.x + pos.y * 8;
                if (index < cubes.Length && cubes[index] != null)
                {
                    Renderer renderer = cubes[index].GetComponent<Renderer>();
                    renderer.material = selectedTileMaterial;
                }
            }


            // 가능한 위치 출력 (디버깅용)
            foreach (var move in availablePosition)
            {
                Debug.Log($"특수 나이트 이동 가능: ({move.x}, {move.y})");
            }
        }
    }

    private void availableTileRefresh(){

        //availablePosition 타일 복귀귀
        foreach (Vector2Int pos in availablePosition)
        {
            int index = pos.x + pos.y * 8;
            if (index < cubes.Length && cubes[index] != null)
            {
                Renderer renderer = cubes[index].GetComponent<Renderer>();
                renderer.material = originalTileMaterial;
            }
        }

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

    //애니메이션 관련

    private IEnumerator MovePieceSmooth(GameObject piece, Vector3 startPos, Vector3 endPos)
    {
        float elapsedTime = 0;
        pieceAnimator.SetTrigger("Attack");
        yield return new WaitForSeconds(0.49f);
        while (elapsedTime < moveDuration)
        {
            piece.transform.position = Vector3.Lerp(startPos, endPos, elapsedTime / moveDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        piece.transform.position = endPos; // 정확한 최종 위치 설정
    }
    
}
