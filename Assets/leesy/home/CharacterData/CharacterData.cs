using System.Collections.Generic;
using UnityEngine;

namespace Lsy
{
    [CreateAssetMenu(fileName = "Character Data", menuName = "Character Data")]
    public class CharacterData : ScriptableObject
    {
        [Header("기본 정보")]
        public int charId;           // 내부 ID
        public string charName;      // 캐릭터 이름

        [Header("��ȭ �� ����")]
        public int level = 1;
        public int exp = 0;
        public int gold = 2000; // �ʱ� ���� ���

        [Header("���� ���� (�ִ�ġ ����)")]
        public float maxHp;          // �ִ� ü��
        public int maxSan;           // �ִ� ���ŷ�
        public int atk;              // ���ݷ�
        public int def;              // ����
        public int spd;              // ���ǵ�
        public int crit;             // ġ��Ÿ Ȯ��
        public int ctm;              // ġ��Ÿ ���
        public int dodge;            // ȸ����
        public int acc;              // ���߷�
        public int res;              // �����̻� ����

        [Header("�ʱ� ���� ��� ID (0 = ������)")]
        public int weaponId;
        public int armorId;
        public int trinket1Id;
        public int trinket2Id;
    }
}