using Jun;
using Mirror;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Jun
{
    public class GamePlayerController : NetworkBehaviour
    {
        [SerializeField] private UnitModel _model;
        [SerializeField] private PlayerView _view;

        [SyncVar] public int FinalHeroIndex = -1;
        [SyncVar(hook = nameof(OnPosIndexChanged))] public int FinalHeroPos = -1;
        [SyncVar] public PlayerInfo Info;
        [SyncVar] public int PingIndex;

        public readonly SyncList<ActiveEffect> Effects = new SyncList<ActiveEffect>();

        public int EffectiveAtk => CombatCalculator.GetEffectiveAtk(Info.Atk, Effects);
        public int EffectiveDef => CombatCalculator.GetEffectiveDef(Info.Def, Effects);
        public int EffectiveAcc => CombatCalculator.GetEffectiveAcc(Info.Acc, Effects);
        public int EffectiveDodge => CombatCalculator.GetEffectiveDodge(Info.Dodge, Effects);

        public Sprite GetCharacterSprite()
        {
            var sr = GetComponent<SpriteRenderer>();
            return sr != null ? sr.sprite : null;
        }

       [Header("�� �ý���")]
        public Transform PingLayout; // �� ������ ����

        public bool IsMovePos = false;

        // ���� ����
        [Server]
        public void InjectData(PlayerData data)
        {
            var info = data.Info;
            info.MaxHp = info.Hp; 
            this.Info = info;
            this.PingIndex = data.PingIndex;
            this.FinalHeroIndex = data.FinalHeroIndex;
            this.FinalHeroPos = data.FinalHeroPos;
            this.PingIndex = data.PingIndex;
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

        public void Start()
        {
            _view.EndMyTurn += EndMyTurn;
            _model.SetUp(Info);
        }
        public void MyTurn(bool IsMyTurn)
        {
            _view.SetSel(IsMyTurn);
        }
        // ��ų ��ư�� ������ ������ ��ų�� ������ ������ �ǰ� (���� ���� �������� �����ߴٸ� �����, Ÿ�ٵ鵵 �����)
        // ������ ��ų�� Ÿ�� ���� ���� ���� ������ Ÿ�� �� ����
        // Ÿ�� ��ư Ȱ��ȭ
        // ������ ��ư�� ������ ����

        public void OnClickSkillBtn(int index) //��ų��ư
        {
            if (isOwned)
            {
                if (!isOwned) return;
                _model.SelectSkill(index);

                var skillType = Info.Skills[index].Type;
                if (skillType == SkillType.Atk || skillType == SkillType.Debuff)
                {
                    // �� ��� ��ų �� �� ��ư Ȱ��ȭ
                    _view.SetButtonsInteractable(true, _view.EnemyBtn);
                }
            }

        }
        public void OnClickItemBtn(int index) //������ ��ư
        {
            if (isOwned)
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
            else if (currentUnit != null && currentUnit.isOwned)
            {
                var model = currentUnit.GetComponent<UnitModel>();
                if (model.SelectedSkill != -1)
                {
                    var skillType = currentUnit.Info.Skills[model.SelectedSkill].Type;
                    bool isAllyTarget = skillType == SkillType.Heal || skillType == SkillType.Buff;
                    if (isAllyTarget)
                    {
                        model.SelectAlly(BattleManager.Instance._players.IndexOf(this));
                        return;
                    }
                }
                BattleManager.Instance.UpdateUnitUI(this);
            }
            else
            {
                BattleManager.Instance.UpdateUnitUI(this);
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
            _view.PlDamaged(_model.PlDamaged(Attack) / Info.MaxHp);

        }

        // 모든 클라이언트에 데미지 시각 효과를 트리거. SyncVar Info와 함께 도착하므로 Hp/MaxHp 비율로 HP바 갱신.
        [ClientRpc]
        public void RpcShowDamage(float damage)
        {
            if (_view != null && Info != null && Info.MaxHp > 0f)
                _view.PlDamaged(Info.Hp / Info.MaxHp);
        }
        [Server]
        public void ApplyHpChange(float delta)
        {
            var info = Info;
            info.Hp = Mathf.Clamp(info.Hp + delta, 0f, info.MaxHp);
            Info = info; // SyncVar ���Ҵ��ؾ� Ŭ���̾�Ʈ�� ����ȭ��
        }

        [Server]
        public void AddEffect(ActiveEffect effect)
        {
            Effects.Add(effect); // SyncList�� �ڵ����� Ŭ���̾�Ʈ�� ����ȭ��
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

            // ActiveEffect�� �� �� ���� ���� tick
            var ticked = CombatCalculator.TickEffects(Effects);
            Effects.Clear();
            foreach (var e in ticked) Effects.Add(e);
            Debug.Log($"[EndMyTurn] tick �� Effects ��: {Effects.Count}");

            BattleManager.Instance.QueueNextTurn();
        }
    }
}

