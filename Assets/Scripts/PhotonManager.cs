using UnityEngine;
using Photon;
using Photon.Realtime;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;


public class PhotonManager : MonoBehaviourPunCallbacks
{
    void Start()
    {
        PhotonNetwork.ConnectUsingSettings(); //서버연결

    }
    public override void OnConnectedToMaster()
    {
        Debug.Log("서버 연결 성공.");
        //서버에 접속하면 자동으로 랜덤 룸 참가하도록
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("방 참가 실패. 새로운 방을 생성합니다.");
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 2});
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("방 참가 성공.");
    }



}
