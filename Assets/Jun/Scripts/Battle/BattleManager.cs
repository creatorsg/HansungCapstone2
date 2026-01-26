using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting.Dependencies.NCalc;

using Mirror;
using UnityEngine.UI;
using TMPro;
// 배틀을 전반적으로 운영하는 서버
namespace Jun
{
    public class BattleManager : NetworkBehaviour
    {
        [SerializeField] private BattleLogic _logic;
        public static BattleManager Instance;

        [Header("전투 유닛 리스트")]
        public readonly SyncList<GamePlayerController> _players = new SyncList<GamePlayerController>();
        [Header("영웅 생성 위치")]
        [SerializeField] private List<Transform> _spawnPoints; public List<Transform> SpawnPoints => _spawnPoints;
        [Header("캐릭터들의 이미지")]
        [SerializeField] private List<Sprite> _playerImages; public List<Sprite> PlayerImages => _playerImages;
        [Header("캐릭터 정보 창")]
        [SerializeField] private CanvasGroup _panel;
        [SerializeField] private Image _charaterIMG; public Image CharaterIMG => _charaterIMG;
        [Header("캐릭터 관련 버튼들")]
        [SerializeField] private List<Button> _skillBTN; public List<Button> SkillBTN => _skillBTN;
        [SerializeField] private List<Image> _equiIMG; public List<Image> EquiIMG => _equiIMG;
        [SerializeField] private List<Button> _items; public List<Button> Items => _items;
        [Header("적")]// 실험을 위해 잠시 버튼으로 만듬, 후에 GameObject로 바꿀 예정
        [SerializeField] private List<Button> _enemys;
        [Header("턴 정보")]
        [SerializeField] private TextMeshProUGUI _turnUI;

        [SyncVar(hook = nameof(ChangeTurn))]
        public int Order = -1;


        private void Awake()
        {
            Instance = this;
        }
        //접속 후 플레이어 추가(관리하기 위해)
        public void RegisterPlayer(GamePlayerController pl)
        {
            var manager = NetworkManager.singleton as GameRoomManager;
            // 현재 플레이어 추가
            _players.Add(pl);
            pl.gameObject.SetActive(true);
            if (isServer && _players.Count == manager.HeroNum)
            {
                NextTurn();
            }
        }
        
        // 다음 턴으로 진행
        [Server]
        public void NextTurn()
        {
            Order = (Order + 1) % _players.Count;
        }
        // 턴 변경
        public void ChangeTurn(int oldId, int newId)
        {
            var targetPlayer = _players[newId];
            _turnUI.text = "Turn: " + newId.ToString();

            bool isMyTurn = targetPlayer.isOwned;
            if (isMyTurn)
            {
                UpdateUnitUI(targetPlayer);
            }

            // 내턴인지에 따라 패널 활성/비활성 처리
            _panel.interactable = isMyTurn;
            _panel.blocksRaycasts = isMyTurn;
            _panel.alpha = isMyTurn ? 1.0f : 0.5f;
        }
        public void UpdateUnitUI(GamePlayerController unit)
        {
            // 초상화 교체 
            _charaterIMG.sprite = unit.GetComponent<SpriteRenderer>().sprite;

            // 스킬 버튼 이벤트 재연결
            for (int i = 0; i < _skillBTN.Count; i++)
            {
                int index = i;
                _skillBTN[i].onClick.RemoveAllListeners();
                _skillBTN[i].onClick.AddListener(() => unit.OnClickSkillBtn(index));

                // 스킬 아이콘도 유닛에 맞게 변경 가능
                // _skillBTN[i].image.sprite = unit.SkillSprites[i];

            }
            for (int i = 0; i < _enemys.Count; i++)
            {
                int index = i;
                var EnemyBTN = _enemys[i].GetComponent<Button>();
                EnemyBTN.onClick.RemoveAllListeners();
                EnemyBTN.onClick.AddListener(() => unit.OnClickEnemyBtn(index));
            }

        }
        //무결성 검사
        public void VerifyClientRequest(GamePlayerController caster, int skillIndex, int itemIndex, List<int> tagets)
        {
            // 규칙 확인 후(아직 생각안해둠)
            _logic.BattleAction(caster, skillIndex, itemIndex, tagets);
        }

    }
}
