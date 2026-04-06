using Jun;
using Mirror;
using System.Collections.Generic;
using UnityEngine;


namespace Jun
{
    public class BattleLogic : MonoBehaviour
    {
        // ���߿��� ���� �����ְԲ� ���� ����
        [Server]
        public void BattleAction(GamePlayerController caster, int skillIndex, int itemIndex, bool isEnemy, List<int>targets)
        {
            var manager = BattleManager.Instance;
            // ��Ʋ ���� ����(�÷��̾�, �� �� �� ��밡���ϰԲ�)
            if (skillIndex != -1)
            {
                SkillInfo skill = caster.Info.Skills[skillIndex];

                foreach (int targetIdx in targets)
                {
                    // Ÿ�� ���ݵ� ���� ��Ʋ ����
                    var target = manager.Enemys[manager.StageNum-1].Enemys[targetIdx].GetComponent<EnemyModel>();
                    target.Damaged(caster.Info.Atk);
                }

                // �������� �ִϸ��̼� ȣ��
                caster.RpcPlaySkillAnim(skill.anim);
                caster.MyTurn(false);
                //������ ���� ������Ʈ

                //�� �г� �����
                BattleManager.Instance.EnemyPanel.SetActive(false);
            }
        }

    }
}
