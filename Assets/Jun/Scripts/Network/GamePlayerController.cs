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

        [Header("스킬 시스템 (캐릭터 프리팹마다 고유 SO 지정)")]
        [SerializeField] private CharacterSkillSetSO _skillSet;
        public CharacterSkillSetSO SkillSet => _skillSet;

        [SyncVar] public int FinalHeroIndex = -1;
        [SyncVar(hook = nameof(OnPosIndexChanged))] public int FinalHeroPos = -1;
        [SyncVar] public PlayerInfo Info;
        [SyncVar] public int PingIndex;

        [Header("�� �ý���")]
        public Transform PingLayout; // �� ������ ����

        public bool IsMovePos = false;
        public override void OnStartServer()
        {
            base.OnStartServer();
            ApplyActiveSkills(); // 스킬트리 선택 결과를 Info.Skills에 주입
            _model.SetUp(Info);
            BattleManager.Instance.RegisterPlayer(this);

            Transform targetPoint = BattleManager.Instance.SpawnPoints[FinalHeroPos];
            if (targetPoint != null)
            {
                transform.position = targetPoint.position;
                Debug.Log($"{gameObject.name}�� {FinalHeroPos}�� ��ġ�� ��ġ�Ǿ����ϴ�.");
            }
        }
        public override void OnStartClient()
        {
            base.OnStartClient();

            // ���� ���� ����(ȣ��Ʈ)��� OnStartServer���� �̹� �¾��� �����Ƿ� �ߺ� ������ �����ݴϴ�.
            if (!isServer)
            {
                Transform targetPoint = BattleManager.Instance.SpawnPoints[FinalHeroPos];
                if (targetPoint != null)
                {
                    transform.position = targetPoint.position;
                    Debug.Log($"{gameObject.name}�� {FinalHeroPos}�� ��ġ�� ��ġ�Ǿ����ϴ�.");
                }
                // Ŭ���̾�Ʈ�� �ڱ� ȭ�鿡�� ��ų �����͸� ���������� �ε��մϴ�!
                _model.SetUp(Info);
            }
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
                _model.SelectSkill(index);
                _view.SetButtonsInteractable(true, _view.EnemyBtn);
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

        // 스킬트리 선택을 기존 Info.Skills에 반영 (A안 브릿지)
        // SkillInfo.Name 을 활성 티어 이름으로 덮어쓰고 TierData 참조를 연결.
        // anim/TargetNum 등 기본 파라미터는 프리팹에 프리셋된 값 유지.
        [Server]
        private void ApplyActiveSkills()
        {
            if (_skillSet == null)
            {
                Debug.LogWarning($"[GamePlayerController] _skillSet 미지정: {name}");
                return;
            }
            if (Info == null || Info.Skills == null)
            {
                Debug.LogWarning($"[GamePlayerController] Info.Skills 미지정: {name}");
                return;
            }

            var save = SkillSaveSync.Instance != null
                ? SkillSaveSync.Instance.Find(_skillSet.characterName)
                : null;
            if (save == null) save = new SkillTreeSaveData { characterName = _skillSet.characterName };

            int count = Mathf.Min(4, Info.Skills.Count);
            for (int i = 0; i < count; i++)
            {
                var active = SkillManager.GetCurrentSkill(_skillSet, save, i);
                if (active == null || Info.Skills[i] == null) continue;
                Info.Skills[i].Name = active.skillName;
                Info.Skills[i].TierData = active;
            }
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

