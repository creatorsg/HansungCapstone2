using Jun;
using Mirror;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Jun
{
    public class GamePlayerController : NetworkBehaviour
    {
        [SerializeField] private UnitModel _model;
        [SerializeField] private PlayerView _view;
        public PlayerView View => _view;

        /// <summary>CharacterCard.CharacterCode - 프리팹 룩업 기준 식별자</summary>
        [SyncVar(hook = nameof(OnHeroCodeChanged))] public string FinalHeroCode = "";

        /// <summary>CharacterDatabase.index - 레거시 UI 호환용</summary>
        [SyncVar] public int    FinalHeroIndex = -1;

        [SyncVar(hook = nameof(OnPosIndexChanged))] public int FinalHeroPos = -1;
        [SyncVar] public PlayerInfo Info;
        [SyncVar] public int PingIndex;

        [Header("�� �ý���")]
        public Transform PingLayout; // �� ������ ����

        public bool IsMovePos = false;

        // ── 애니메이션 래퍼 ────────────────────────────────────────────
        // BattleLogic / EnemyController 등 서버 코드에서 호출합니다.

        /// <summary>피격 애니메이션을 모든 클라이언트에 재생합니다.</summary>
        [ClientRpc]
        public void RpcPlayDamagedAnim()
        {
            _view.PlayDamaged();
        }

        /// <summary>회피 애니메이션을 모든 클라이언트에 재생합니다.</summary>
        [ClientRpc]
        public void RpcPlayDodgeAnim()
        {
            _view.PlayDodge();
        }

        /// <summary>사망 애니메이션을 모든 클라이언트에 재생합니다.</summary>
        [ClientRpc]
        public void RpcPlayDeadAnim()
        {
            _view.PlayDead();
        }

        // 데이터 주입
        [Server]
        public void InjectData(PlayerData data)
        {
            this.Info           = data.Info;
            this.PingIndex      = data.PingIndex;
            this.FinalHeroCode  = data.FinalHeroCode;
            this.FinalHeroIndex = data.FinalHeroIndex;
            this.FinalHeroPos   = data.FinalHeroPos;
        }
        // ��ġ�� ��� ������ ���� �Լ��� �и��ؼ� ȣ��
        void OnPosIndexChanged(int oldPos, int newPos)
        {
            if (oldPos == -1)
            {
                // ó�� ������ ��
                transform.position = BattleManager.Instance.SpawnPoints[newPos].position;
            }
            else
            {
                // �� ���߿� �ڸ��� �ٲ���� �� (�ε巴�� �̵�)
                StopAllCoroutines();
                StartCoroutine(MoveRoutine(BattleManager.Instance.SpawnPoints[newPos].position));
            }
        }
        //�ε巴�� �����̰� ���ִ� �Լ�
        System.Collections.IEnumerator MoveRoutine(Vector3 targetPos)
        {
            while (Vector3.Distance(transform.position, targetPos) > 0.01f)
            {
                transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 5f);
                yield return null;
            }
            transform.position = targetPos;
        }

        /// <summary>
        /// FinalHeroCode SyncVar 동기화 훅 — 클라이언트에서 코드가 설정되는 순간
        /// CharacterRegistry에서 스프라이트를 꺼내 SpriteRenderer에 적용합니다.
        /// </summary>
        private void OnHeroCodeChanged(string oldCode, string newCode)
        {
            ApplyCharacterSprite(newCode);
        }

        /// <summary>
        /// 코드에 해당하는 스프라이트를 SpriteRenderer에 적용합니다.
        /// Start()와 OnHeroCodeChanged() 양쪽에서 호출합니다.
        /// </summary>
        private void ApplyCharacterSprite(string code)
        {
            if (string.IsNullOrEmpty(code)) return;

            if (!CharacterRegistry.TryGet(code, out var entry))
            {
                Debug.LogWarning($"[GamePlayerController] '{code}' 스프라이트 적용 실패: Registry에 없음");
                return;
            }

            if (entry.CharacterSprite == null)
            {
                Debug.LogWarning($"[GamePlayerController] '{code}' CharacterSprite가 null입니다. CharacterCard에 Sprite를 연결하세요.");
                return;
            }

            // GetComponentInChildren으로 루트·자식 오브젝트를 모두 탐색합니다.
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = entry.CharacterSprite;
                Debug.Log($"[GamePlayerController] '{code}' → SpriteRenderer({sr.gameObject.name}) 스프라이트 적용 완료");
            }
            else
            {
                // SpriteRenderer가 없으면 UI Image로 폴백
                var img = GetComponentInChildren<UnityEngine.UI.Image>();
                if (img != null)
                {
                    img.sprite = entry.CharacterSprite;
                    Debug.Log($"[GamePlayerController] '{code}' → Image({img.gameObject.name}) 스프라이트 적용 완료");
                }
                else
                {
                    Debug.LogWarning($"[GamePlayerController] SpriteRenderer/Image를 찾지 못했습니다. ({gameObject.name})");
                }
            }
        }

        /// <summary>
        /// 이 유닛의 캐릭터 스프라이트를 반환합니다.
        /// CharacterRegistry → SpriteRenderer 순으로 조회합니다.
        /// BattleManager 등 외부에서 이 메서드를 사용하면 null 안전하게 스프라이트를 얻을 수 있습니다.
        /// </summary>
        public Sprite GetCharacterSprite()
        {
            if (!string.IsNullOrEmpty(FinalHeroCode) &&
                CharacterRegistry.TryGet(FinalHeroCode, out var entry) &&
                entry.CharacterSprite != null)
            {
                return entry.CharacterSprite;
            }
            // 폴백: SpriteRenderer에서 직접 읽기
            return GetComponentInChildren<SpriteRenderer>()?.sprite;
        }

        public void Start()
        {
            _view.EndMyTurn += EndMyTurn;
            _model.SetUp(Info);

            // 서버에서 이미 FinalHeroCode가 설정된 채로 클라이언트에 스폰될 경우
            // SyncVar 훅이 트리거되지 않으므로 Start()에서도 명시적으로 적용합니다.
            ApplyCharacterSprite(FinalHeroCode);
        }
        public void MyTurn(bool IsMyTurn)
        {
            _view.SetSel(IsMyTurn);
        }
        // ��ų ��ư�� ������ ������ ��ų�� ������ ������ �ǰ� (���� ���� �������� �����ߴٸ� �����, Ÿ�ٵ鵵 �����)
        // ������ ��ų�� Ÿ�� ���� ���� ���� ������ Ÿ�� �� ����
        // Ÿ�� ��ư Ȱ��ȭ
        // ������ ��ư�� ������ ����

        public void OnClickSkillBtn(int index)
        {
            // 내 유닛이고 현재 내 턴일 때만 동작
            if (isOwned && BattleManager.Instance.CurrentTurnUnit == this)
            {
                _model.SelectSkill(index);
                _view.SetButtonsInteractable(true, _view.EnemyBtn);
            }
        }

        public void OnClickItemBtn(int index)
        {
            // 내 유닛이고 현재 내 턴일 때만 동작
            if (isOwned && BattleManager.Instance.CurrentTurnUnit == this)
            {
                _model.SelectItem(index);
                _view.SetButtonsInteractable(true, _view.EnemyBtn);
            }
        }

        public void OnClickEnemyBtn(int index) //����ư
        {
            if (_model.SelectedItem == -1 && _model.SelectedSkill == -1)
            {
                Debug.Log("��UI�г� ������ "+ index);
                BattleManager.Instance.UpdateEnemyUI(index);
            }
            // �� �� �߰�
            GameObject enemyObj = BattleManager.Instance.Enemys[BattleManager.Instance.StageNum - 1].Enemys[index].gameObject;
            foreach (var unit in BattleManager.Instance._players)
            {
                if (unit.isOwned)
                {
                    unit.CmdSendPing(unit.PingIndex, enemyObj);
                    break;
                }
            }

            if (isOwned)
            {
                _model.SelectEnemy(index);
            }
        }
        public void OnClickMoveBtn()
        {
            if (!isOwned) return;
            IsMovePos = true;
            Debug.Log("�ڸ��̵�" + IsMovePos);
        }
        // �ٲ� ���(�ٸ� �Ʊ� ����)�� Ŭ������ �� ����
        public void OnClickedUnit()
        {
            var currentUnit = BattleManager.Instance.CurrentTurnUnit;

            if (currentUnit != null && currentUnit.IsMovePos)
            {
                currentUnit.CmdRequestChangePos(this.gameObject);
                currentUnit.IsMovePos = false;
            }
            else
            { // ���� ����
                BattleManager.Instance.UpdateUnitUI(this);

                foreach (var unit in BattleManager.Instance._players)
                {
                    if (unit.isOwned) 
                    {
                        unit.CmdSendPing(unit.PingIndex,this.gameObject); 
                        break;
                    }
                }
            }
        }

        [Command]
        public void CmdSendPing(int ping, GameObject target)
        {
            Debug.Log("Ping1");
            BattleManager.Instance.RpcShowPing(PingIndex, target);
        }

        // ������ �ڸ� ��ü ��û
        [Command]
        public void CmdRequestChangePos(GameObject targetUnitObj)
        {
            GamePlayerController targetUnit = targetUnitObj.GetComponent<GamePlayerController>();
            if (targetUnit != null)
            {
                BattleManager.Instance.ChangeUnitPos(this, targetUnit);
            }
        }

        // ���� ����
        public void PlDamaged(float Attack)
        {
            _view.PlDamaged(_model.PlDamaged(Attack)/Info.Hp);
            
        }

        //��ų ����� ������ ��û
        //��Ʋ �Ŵ������� ���Ἲ �˻� ��û
        [Command]
        public void CMDSelectionComplete(int skillIndex, int itemIndex, bool isEnemy, List<int> tagets)
        {
            BattleManager.Instance.VerifyClientRequest(this, skillIndex, itemIndex, isEnemy, tagets);
        }


        // �ִϸ��̼� ����
        [ClientRpc]
        public void RpcPlaySkillAnim(string animName)
        {
            //���� ��ų�� ���� ���ص� �ɵ�
            _view.SkillAnim(animName);
        }

        // ���� ������ -> �� �ѱ��
        [Command]
        public void EndMyTurn()
        {
            _model.Reset();
            BattleManager.Instance.NextTurn();
        }
    }
}

