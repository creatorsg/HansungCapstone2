using UnityEngine;
using Mirror;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;


namespace Jun
{
    public class LobbyManager : NetworkBehaviour
    {
        public static LobbyManager Instance;
        public List<GameObject> Go;
        [SerializeField] private List<Button> _heroBTN;
        [SerializeField] private Button _startBTN;
        [SerializeField] private Button _readyBTN;
        [SerializeField] private TextMeshProUGUI _playerNumText;

        private int _playerNum = 0;

        private void Awake()
        {
            Instance = this;
        }
        public  void ActiveBTN(bool IsServer)
        {
            if (IsServer) _startBTN.gameObject.SetActive(true);
            else _readyBTN.gameObject.SetActive(true);
        }
        //캐릭터 선택 버튼
        public void OnClickedHero(int index)
        {
            var player = NetworkClient.localPlayer.GetComponent<GameRoomPlayer>();
            player.CMDChoiceHero(index);
        }
        public void UpdatePlayerNum(bool In)
        {
            _playerNum = In ? _playerNum + 1 : _playerNum - 1;
            _playerNumText.text = _playerNum.ToString();
        }
        public void OnClickedReady()
        {
            // 로컬 플레이어의 레디 상태 변경
            var localPlayer = NetworkClient.localPlayer.GetComponent<GameRoomPlayer>();
            //준비 해제
            if (localPlayer.readyToBegin == true)
            {
                localPlayer.CmdChangeReadyState(!localPlayer.readyToBegin); // readyToBegin을 바꾸려면CmdChangeReadyState함수 필요
                _readyBTN.GetComponent<Image>().color = Color.white;
                foreach (var hero in _heroBTN) hero.interactable = true;
                return;
            }
            //준비완료( 선택한 영웅이 없다면 준비완료 x)
            if (localPlayer.CharaterNum.Count == 0) return;
            localPlayer.CmdChangeReadyState(!localPlayer.readyToBegin);
            _readyBTN.GetComponent<Image>().color = Color.gray;
            foreach (var hero in _heroBTN) hero.interactable = false;
        }
        public void OnClickedStart()
        {
            var localPlayer = NetworkClient.localPlayer.GetComponent<GameRoomPlayer>();
            var manager = NetworkManager.singleton as GameRoomManager;

            // 다른 모든 플레이어들의 준비상태 확인
            bool isReadyAllPlayer = true;
            foreach(var player in manager.roomSlots)
            {
                if (player == localPlayer) continue;
                if (player.readyToBegin == false) isReadyAllPlayer = false;
            }
            if (isReadyAllPlayer) manager.ServerChangeScene(manager.GameplayScene);
            else
            {
                Debug.Log("모든 플레이어가 준비되지 않았습니다.");
            }
        }
        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
