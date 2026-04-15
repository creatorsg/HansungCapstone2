using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
namespace Jun
{
    public enum SkillType //��ų ����
    {
        Atk,
        Heal,
        Buff,
        Debuff,
        Enforce
    }
    public enum UnitState //���� ���� ����
    {
        Waiting,       // ��ٸ��� ��
        Acting,        // �ൿ ��
        Incapacitated  // �ൿ �Ҵ�
    }


    [System.Serializable]
    public class PlayerInfo //�÷��̾� ����
    {
        public int Id;      //�÷��̾� id
        public string Name; //�̸�
        public List<SkillInfo> Skills;
        public List<ItemInfo> Items;
        public int Lvl;     //����
        public int Exp;     //����ġ
        public float Hp;      //ü��
        public int Atk;     //���ݷ�
        public int Def;     //����
        public int Spd;     //���ǵ�
        public int San;     //���ŷ�
        public int Crit;    //ġ��Ÿ Ȯ��
        public int Ctm;     //ġ��Ÿ ���
        public int Dodge;     //ȸ����
        public int Acc;     //���߷�
        public int Res;     //�����̻�����

        public int WpnId; // ���� ���� ���� ID
        public int ArmId; // ���� ���� �� ID
        public int Trk1;  // ��ű� ���� 1
        public int Trk2;  // ��ű� ���� 2
    }

    [System.Serializable]
    public class SkillInfo // ��ų ����
    {
        public string Name;
        public SkillType Type;
        public string anim;
        public int TagetNum;

        // A안 브릿지: 스킬트리에서 선택된 활성 Tier 데이터 (효과/수치/설명 참조용)
        public SkillTierData TierData;
    }
    [System.Serializable]
    public class ItemInfo //������ ����
    {
        public string Name;
        public int TagetNum;

        //�ٸ� ������
    }
    [System.Serializable]
    public class EqpInfo // ��� ����
    {
        public string Name;
        public int EqpId;   // ��� ���� ��ȣ
                            //�ٸ� ������
    }

    [System.Serializable]
    public class TurnData
    {
        public string type;
        public int speed;
        public int num;

        public TurnData() { }
        public TurnData(string type, int speed, int num)
        {
            this.type = type;
            this.speed = speed;
            this.num = num;
        }
    }

    [System.Serializable]
    public class BattleEnemyInfo
    {
        public string BattleStage;
        public List<Button> Enemys;
    }
}
