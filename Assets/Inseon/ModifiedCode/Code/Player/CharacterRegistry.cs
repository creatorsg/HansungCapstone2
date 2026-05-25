using Jun;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CharacterCode(문자열)를 키로 사용하는 캐릭터 리소스 등록소.
///
/// CharacterSelectManager가 씬 로드 시 CharacterCard에서 읽어 등록합니다.
/// GameRoomManager.SpawnPlayerDataForPlayer / BattleManager.SetupBattleFlow에서
/// 프리팹 조회에 사용합니다.
///
/// ─ 왜 이게 필요한가? ─────────────────────────────────────────────────
///   기존: spawnPrefabs[CharacterDatabase.index] → PlayFab 카탈로그 순서에 종속
///   개선: CharacterCard에 프리팹을 직접 연결 → 순서 무관, 코드(문자열) 기반 조회
/// </summary>
public static class CharacterRegistry
{
    public struct Entry
    {
        /// <summary>PlayerData NetworkBehaviour 데이터 컨테이너 프리팹</summary>
        public GameObject PlayerDataPrefab;

        /// <summary>배틀 씬의 GamePlayerController 유닛 프리팹</summary>
        public GameObject BattleUnitPrefab;

        /// <summary>배틀 씬에서 유닛에 적용할 캐릭터 스프라이트</summary>
        public Sprite CharacterSprite;

        /// <summary>이 캐릭터가 사용할 스킬 목록</summary>
        public List<SkillInfo> Skills;

        /// <summary>이 캐릭터가 사용할 아이템 목록</summary>
        public List<ConsumableInfo> Items;

        /// <summary>이 캐릭터가 사용할 장비 목록</summary>
        public EqpInfo Weapon;
        public EqpInfo Armor;
    }

    private static readonly Dictionary<string, Entry> _map = new Dictionary<string, Entry>();

    /// <summary>등록된 모든 캐릭터 읽기 전용 접근</summary>
    public static IReadOnlyDictionary<string, Entry> All => _map;

    /// <summary>다음 씬 초기화 전에 이전 등록 정보를 지웁니다.</summary>
    public static void Clear()
    {
        _map.Clear();
        Debug.Log("[CharacterRegistry] 초기화");
    }

    /// <summary>
    /// CharacterCard 기반으로 캐릭터 정보를 등록합니다.
    /// CharacterSelectManager.InitCards()에서 호출합니다.
    /// </summary>
    public static void Register(string code, Entry entry)
    {
        if (string.IsNullOrEmpty(code))
        {
            Debug.LogWarning("[CharacterRegistry] 빈 코드로 등록 시도 무시");
            return;
        }
        _map[code] = entry;
        Debug.Log($"[CharacterRegistry] 등록 완료: {code} " +
                  $"(PlayerData={entry.PlayerDataPrefab?.name ?? "null"}, " +
                  $"BattleUnit={entry.BattleUnitPrefab?.name ?? "null"}, " +
                  $"Sprite={entry.CharacterSprite?.name ?? "null"}, " +
                  $"Skills={entry.Skills?.Count ?? 0})");
    }

    /// <summary>
    /// 코드로 Entry를 조회합니다.
    /// </summary>
    public static bool TryGet(string code, out Entry entry)
    {
        if (_map.TryGetValue(code, out entry)) return true;
        Debug.LogWarning($"[CharacterRegistry] 코드 '{code}'를 찾을 수 없습니다. CharacterCard에 프리팹이 연결됐는지 확인하세요.");
        return false;
    }

    public static bool Has(string code) => _map.ContainsKey(code);
}
