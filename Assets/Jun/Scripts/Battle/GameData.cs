using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

namespace Jun
{
    // 스킬 타입
    public enum SkillType
    {
        Atk,
        Heal,
        Debuff,
        Buff,
        Enforce     // 자기강화
    }

    // 유닛 행동 상태
    public enum UnitState
    {
        Waiting,       // 대기
        Acting,        // 행동 중
        Incapacitated  // 행동불가(기절 등)
    }

    // 스킬/아이템 타깃 종류 (신 시스템: SkillSO / EnemyAI / UnitModel)
    public enum TargetType
    {
        SingleEnemy,
        AllEnemies,
        SingleAlly,
        AllAllies,
        Self,
    }

    // 신 상태이상 종류 (StatusProcessor 기반)
    public enum StatusType
    {
        Bleed,
        Poison,
        Stun,
        AtkUp, AtkDown,
        DefUp, DefDown,
        SpdUp, SpdDown,
    }

    // PlayerInfo 에 누적되는 신 상태이상 인스턴스 (StatusProcessor 가 직접 가공)
    [System.Serializable]
    public class ActiveStatus
    {
        public StatusType Type;
        public int RemainingTurns;
        public int Value;
    }

    // 스킬/AI 가 부여하는 상태이상 정의 (확률 + 지속 + 값)
    [System.Serializable]
    public class StatusApply
    {
        public StatusType Type;
        [Range(0f, 1f)] public float Chance = 1f;
        public int Duration = 1;
        public int Value;
    }

    // 상태이상 종류
    public enum EffectType
    {
        // 버프
        AtkUp,
        DefUp,
        SpdUp,
        DodgeUp,
        AccUp,

        // 디버프
        AtkDown,
        DefDown,
        SpdDown,
        AccDown,

        // 특수 디버프
        Stunned,    // 행동 불가 (턴 스킵)
        Bleeding,   // 매 턴 HP 감소 (value = 피해량)
        Burning,    // 매 턴 HP 감소 + 방어력 무시
    }

    // 상태이상 인스턴스 (Mirror SyncList 호환 struct)
    [System.Serializable]
    public struct ActiveEffect
    {
        public EffectType type;
        public float value;     // 수치 (AtkUp이면 증가량, Bleeding이면 피해량)
        public int duration;    // 남은 턴 수

        public ActiveEffect(EffectType type, float value, int duration)
        {
            this.type = type;
            this.value = value;
            this.duration = duration;
        }

        public bool IsBuff =>
            type == EffectType.AtkUp || type == EffectType.DefUp ||
            type == EffectType.SpdUp || type == EffectType.DodgeUp ||
            type == EffectType.AccUp;
    }

    // ── 추가: UnitModel / EnemyModel 의 스탯 직접 수정 방식 상태이상 ──
    // BattleManager.NextTurn() 에서 UnitModel.TickEffects() 로 관리됨
    [System.Serializable]
    public class StatusEffect
    {
        public int RemainTurn;  // 남은 턴
        public int AtkBonus;    // 공격력 보정
        public int DefBonus;    // 방어력 보정
        public int SpdBonus;    // 속도 보정

        public StatusEffect(int atkBonus, int defBonus, int spdBonus, int duration)
        {
            AtkBonus = atkBonus;
            DefBonus = defBonus;
            SpdBonus = spdBonus;
            RemainTurn = duration;
        }
    }

    // 전투 결과 (RPC로 클라이언트에 전달)
    [System.Serializable]
    public struct CombatResult
    {
        public bool isHit;
        public bool isCrit;
        public float value;     // 피해량 또는 회복량
        public int targetIndex; // 대상 인덱스
        public bool isEnemy;    // 대상이 적인지
    }

    // 플레이어/적 공통 스탯 정보
    [System.Serializable]
    public class PlayerInfo
    {
        public int Id;
        public string Name;
        public List<SkillInfo> Skills;
        public List<ItemInfo> Items;
        public int Lvl;
        public int Exp;

        public float Hp;        // 현재 HP
        public float MaxHp;     // 최대 HP (SetUp 시 초기화)

        public int Atk;         // 공격력
        public int Def;         // 방어력
        public int Spd;         // 속도 (턴 순서)
        public int San;         // 정신력
        public int Crit;        // 치명타 확률 (%)
        public int Ctm;         // 치명타 배율 추가 (%)  ex) 50 → 1.5배
        public int Dodge;       // 회피율 (%)
        public int Acc;         // 명중률 (%)
        public int Res;         // 상태이상 저항 (%)

        public int WpnId;
        public int ArmId;
        public int Trk1;
        public int Trk2;

        // 신 시스템: StatusProcessor 가 누적/소비하는 상태이상 리스트
        public List<ActiveStatus> Statuses;
    }

    // 스킬 정보
    [System.Serializable]
    public class SkillInfo
    {
        public string Name;
        public SkillType Type;
        public string anim;
        public int TagetNum;            // 대상 수

        // 공격
        public float DamageRate;        // 공격력 배율  ex) 1.2 = 120%

        // 힐
        public float HealRate;          // 최대HP 비율  ex) 0.3 = 30% 회복

        // 버프/디버프
        public EffectType EffectType;   // 어떤 효과를
        public float EffectValue;       // 얼마나
        public int EffectDuration;      // 몇 턴

        // 스킬 트리 연동
        public SkillTierData TierData;

        // 신 시스템 (SkillSO / EnemyAI / UnitModel) — 옛 필드와 공존
        public TargetType Target;
        public float DamageMultiplier;
        public int HealAmount;
        public List<StatusApply> StatusEffects;
    }

    // 아이템 정보
    [System.Serializable]
    public class ItemInfo
    {
        public string Name;
        public int TagetNum;
        public float HealRate;          // 회복 비율

        // 신 시스템: 자동 타깃 라우팅 (Self / AllAllies / SingleAlly 등)
        public TargetType Target = TargetType.SingleAlly;
    }

    // 장비 정보
    [System.Serializable]
    public class EqpInfo
    {
        public string Name;
        public int EqpId;
    }

    // 턴 데이터 (속도 정렬용)
    [System.Serializable]
    public class TurnData
    {
        public string type;     // "Player" or "Enemy"
        public int speed;
        public int num;         // _players 또는 _enemys 인덱스

        public TurnData() { }
        public TurnData(string type, int speed, int num)
        {
            this.type = type;
            this.speed = speed;
            this.num = num;
        }
    }

    // 스테이지별 적 구성
    [System.Serializable]
    public class BattleEnemyInfo
    {
        public string BattleStage;
        public List<Button> Enemys;
    }
}
