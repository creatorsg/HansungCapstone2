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
        public static BattleManager Instance;

        [Header("접속한 플레이어들")]
        [SerializeField] private List<GamePlayerController> _players;
        [Header("영웅 생성 위치")]
        [SerializeField] private List<Transform> _spawnPoints;
        [Header("캐릭터 창")]
        [SerializeField] private CanvasGroup _panel;
        [Header("버튼들")]
        [SerializeField] private List<Button> _skillBTN;  public List<Button> SkillBTN => _skillBTN;
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
            // 현재 플레이어 추가
            _players.Add(pl);
            pl.gameObject.SetActive(true);
            if (isServer && _players.Count == NetworkServer.connections.Count)
            {
                RPCSettingPlayer();
                NextTurn();
            }
        }
        //위치에 맞는 플레이어 배치
        [ClientRpc]
        public void RPCSettingPlayer()
        {
            for (int i = 0; i < _players.Count; i++)
            {
                Debug.Log(_players[i].FinalHeroPos);
                Debug.Log(_spawnPoints[_players[i].FinalHeroPos].position);

                _players[i].transform.position = _spawnPoints[_players[i].FinalHeroPos].position;
            }
            Debug.Log("배치완료");
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
            _turnUI.text = "Turn: " + _players[Order].Info.Id.ToString();
            bool isMyTurn = _players[Order].isLocalPlayer;
            // 내턴인지에 따라 패널 비활성화
            _panel.interactable = isMyTurn;
            _panel.blocksRaycasts = isMyTurn;
            _panel.alpha = isMyTurn ? 1.0f : 0.5f;
        }
    }
}
