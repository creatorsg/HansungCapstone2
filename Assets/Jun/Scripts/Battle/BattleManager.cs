using Mirror;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;
using UnityEngine.UI;

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
        [Header("캐릭터 관련 UI들")]
        [SerializeField] private List<Button> _skillBTN; public List<Button> SkillBTN => _skillBTN;
        [SerializeField] private List<Image> _equiIMG; public List<Image> EquiIMG => _equiIMG;
        [SerializeField] private List<Button> _items; public List<Button> Items => _items;
        [SerializeField] private TextMeshProUGUI _hp; public TextMeshProUGUI Hp => _hp;
        [SerializeField] private TextMeshProUGUI _san; public TextMeshProUGUI San => _san;
        [SerializeField] private TextMeshProUGUI _acc; public TextMeshProUGUI Acc => _acc;
        [SerializeField] private TextMeshProUGUI _crit; public TextMeshProUGUI Crit => _crit;
        [SerializeField] private TextMeshProUGUI _dmg; public TextMeshProUGUI Dmg => _dmg;
        [SerializeField] private TextMeshProUGUI _prot; public TextMeshProUGUI Prot => _prot;
        [SerializeField] private TextMeshProUGUI _res; public TextMeshProUGUI Res => _res;
        [SerializeField] private TextMeshProUGUI _dodge; public TextMeshProUGUI Dodge => _dodge;
        [SerializeField] private TextMeshProUGUI _name; public TextMeshProUGUI Name => _name;

        [Header("적")]// 실험을 위해 잠시 버튼으로 만듬, 후에 GameObject로 바꿀 예정
        [SerializeField] private List<Button> _enemys; public List<Button> Enemys => _enemys;
        [SerializeField] private GameObject _enemyPanel; public GameObject EnemyPanel => _enemyPanel;
        [SerializeField] private Image _enemyUI;
        [SerializeField] private TextMeshProUGUI _enemyName;

        [Header("턴 정보")]
        [SerializeField] private Transform _turnPanel;
        [SerializeField] private TextMeshProUGUI _turnUI;
        [SerializeField] private GameObject _turnUi;
        [SerializeField] private List<TurnData> _turnList = new List<TurnData>();
        [SyncVar(hook = nameof(ChangeTurn))]
        public int Order = -1;


        private void Awake()
        {
            Instance = this;
            //  패널 비활성 처리
            _panel.interactable = false;
            _panel.blocksRaycasts = false;
            _panel.alpha =  0.5f;
            for(int i = 0; i < _enemys.Count; i++)
            {
                _turnList.Add(new TurnData("Enemy", _enemys[i].GetComponent<EnemyController>().Info.Spd, i));
            }
        }
        //접속 후 플레이어 추가(관리하기 위해)
        public void RegisterPlayer(GamePlayerController pl)
        {
            var manager = NetworkManager.singleton as GameRoomManager;
            // 현재 플레이어 추가
            _players.Add(pl);
            _turnList.Add(new TurnData("Player", pl.Info.Spd, _players.Count-1));
            
            pl.gameObject.SetActive(true);
            if (isServer && _players.Count == manager.HeroNum)
            {
                _turnList.Sort((a, b) => b.speed.CompareTo(a.speed));
                NextTurn();
            }
        }
        
        // 다음 턴으로 진행
        [Server]
        public void NextTurn()
        {
            Order = (Order + 1) % (_players.Count + _enemys.Count);
        }
        // 턴 변경
        public void ChangeTurn(int oldId, int newId)
        {
            var typeTurn = _turnList[newId].type;
            if (typeTurn == "Enemy")
            {
                _turnUI.text = "Enemy" + _turnList[newId].num.ToString();
                //  패널 비활성 처리
                _panel.interactable = false;
                _panel.blocksRaycasts = false;
                _panel.alpha = 0.5f;
                StartCoroutine(EnemyTurn());
            }
            else
            {
                var targetPlayer = _players[_turnList[newId].num];
                _turnUI.text = "Turn: " + _players[_turnList[newId].num].Info.Id.ToString();

                bool isMyTurn = targetPlayer.isOwned;
                if (isMyTurn)
                {
                    UpdateUnitUI(targetPlayer);
                    targetPlayer.MyTurn(true);
                }

                // 내턴인지에 따라 패널 활성/비활성 처리
                _panel.interactable = isMyTurn;
                _panel.blocksRaycasts = isMyTurn;
                _panel.alpha = isMyTurn ? 1.0f : 0.5f;
            }
        }
        // 적 턴일떄 잠시 코루틴으로 넘기기
        IEnumerator EnemyTurn()
        {
            yield return new WaitForSeconds(1.0f);
            NextTurn();
        }
        public void UpdateUnitUI(GamePlayerController unit)
        {
            bool isMyUnit = unit.isOwned;
            // 내 유닛인지에 따라 패널 활성/비활성 처리
            _panel.interactable = isMyUnit;
            _panel.blocksRaycasts = isMyUnit;
            _panel.alpha = isMyUnit ? 1.0f : 0.5f;
            // 초상화 교체 
            _charaterIMG.sprite = unit.GetComponent<SpriteRenderer>().sprite;

            //유닛 정보들 교체
            _hp.text = unit.Info.Hp.ToString();
            _san.text = unit.Info.San.ToString();
            _acc.text = unit.Info.Acc.ToString();
            _crit.text = unit.Info.Crit.ToString();
            _dmg.text = unit.Info.Atk.ToString();
            _prot.text = unit.Info.Def.ToString();
            _res.text = unit.Info.Hp.ToString();
            _dodge.text = unit.Info.Dodge.ToString();

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
        //적 선택 이미지 변경
        public void UpdateEnemyUI(int index)
        {
            var enemy = _enemys[index].GetComponent<EnemyController>();
            _enemyPanel.SetActive(true);
            _enemyUI.sprite = enemy.GetComponent<Image>().sprite;
        }
        //무결성 검사
        public void VerifyClientRequest(GamePlayerController caster, int skillIndex, int itemIndex, bool isEnemy,List<int> targets)
        {
            // 규칙 확인 후(아직 생각안해둠)



            _logic.BattleAction(caster, skillIndex,itemIndex, isEnemy, targets);
        }

    }
}
