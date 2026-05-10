using System.Collections.Generic;

/// <summary>
/// PlayFab에 영구 저장되는 게임 세이브 데이터.
/// Host가 PlayfabCommand.SaveGameState()로 저장하고
/// PlayfabCommand.LoadGameState()로 불러옵니다.
///
/// 새 방을 만들면 PlayfabCommand.ResetSaveData()로 초기화됩니다.
/// </summary>
[System.Serializable]
public class SaveData
{
    // ── 진행 상태 ──
    public int clearedStages;           // 클리어한 스테이지 수
    public int currentFloor;            // 현재 층/단계
    public int gold;                    // 보유 골드

    // ── 선택된 캐릭터들 상태 ──
    public List<CharacterSaveData> characters = new List<CharacterSaveData>();

    // ── 아지트 NPC 업그레이드 레벨 ──
    // key: NPC 식별자(예: "blacksmith", "merchant"), value: 레벨
    public Dictionary<string, int> npcLevels = new Dictionary<string, int>();
}

/// <summary>
/// 개별 캐릭터의 영구 저장 데이터.
/// </summary>
[System.Serializable]
public class CharacterSaveData
{
    public string characterCode;        // PlayFab 카탈로그 ItemId (예: "C001")
    public int    currentHp;
    public int    stress;
    public string weaponId;             // 장착 무기 ID
    public string armorId;             // 장착 방어구 ID
    public List<string> unlockedSkills = new List<string>(); // 해금된 스킬 목록
}
