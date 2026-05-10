using UnityEngine;
using Mirror;
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

        [Header("스킬 강화 시설")]
        [SerializeField] private SkillTreeUI _skillTreeUI;
        [SerializeField] private CharacterSkillSetSO[] _characterSkillSets; // 인덱스 = HeroIndex

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
        //ĳ���� ���� ��ư
        public void OnClickedHero(int index)
        {
            var player = NetworkClient.localPlayer.GetComponent<GameRoomPlayer>();

            string code = null;
            foreach (var kvp in CharacterDatabase.Stats)
            {
                if (kvp.Value.index == index) { code = kvp.Key; break; }
            }
            if (string.IsNullOrEmpty(code))
            {
                Debug.LogWarning($"[LobbyManager] HeroIndex {index}에 해당하는 CharacterCode를 찾지 못했습니다.");
                return;
            }
            player.CMDChoiceHero(code);
        }

        // 아지트 스킬 강화 시설 버튼 (HeroIndex 전달)
        public void OnClickedSkillFacility(int heroIndex)
        {
            if (_skillTreeUI == null || _characterSkillSets == null) return;
            if (heroIndex < 0 || heroIndex >= _characterSkillSets.Length) return;

            _skillTreeUI.Open(_characterSkillSets[heroIndex]);
        }
        public void UpdatePlayerNum(bool In)
        {
            _playerNum = In ? _playerNum + 1 : _playerNum - 1;
            _playerNumText.text = _playerNum.ToString();
        }
        public void OnClickedReady()
        {
            // ���� �÷��̾��� ���� ���� ����
            var localPlayer = NetworkClient.localPlayer.GetComponent<GameRoomPlayer>();
            //�غ� ����
            if (localPlayer.readyToBegin == true)
            {
                localPlayer.CmdChangeReadyState(!localPlayer.readyToBegin); // readyToBegin�� �ٲٷ���CmdChangeReadyState�Լ� �ʿ�
                _readyBTN.GetComponent<Image>().color = Color.white;
                foreach (var hero in _heroBTN) hero.interactable = true;
                return;
            }
            //�غ�Ϸ�( ������ ������ ���ٸ� �غ�Ϸ� x)
            if (localPlayer.CharaterNum.Count == 0) return;
            localPlayer.CmdChangeReadyState(!localPlayer.readyToBegin);
            _readyBTN.GetComponent<Image>().color = Color.gray;
            foreach (var hero in _heroBTN) hero.interactable = false;
        }
        public void OnClickedStart()
        {
            var localPlayer = NetworkClient.localPlayer.GetComponent<GameRoomPlayer>();
            var manager = NetworkManager.singleton as GameRoomManager;

            // �ٸ� ��� �÷��̾���� �غ���� Ȯ��
            bool isReadyAllPlayer = true;
            foreach(var player in manager.roomSlots)
            {
                if (player == localPlayer) continue;
                if (player.readyToBegin == false) isReadyAllPlayer = false;
            }
            if (isReadyAllPlayer) manager.ServerChangeScene(manager.GameplayScene);
            else
            {
                Debug.Log("��� �÷��̾ �غ���� �ʾҽ��ϴ�.");
            }
        }
        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
