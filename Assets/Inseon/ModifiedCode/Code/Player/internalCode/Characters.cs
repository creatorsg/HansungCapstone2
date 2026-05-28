using UnityEngine;

public struct Character
{
    /// <summary>
    /// PlayFab Catalog 로드 순서 기반 인덱스.
    /// GameRoomManager.spawnPrefabs[index] 와 대응됩니다.
    /// </summary>
    public int index;

    public string characterCode;
    public string characterName;         // 표시용 이름 (예: "Glue")
    public CharacterEquipment equipment;
    public int level;
    public int hp;
    public int mana;
    public int stress;
    public int effectResistance;
    public int stunResistance;
    public int attack;
    public int defense;
    public int critical;
    public int accuracy;
    public int evasion;
    public int speed;
    public int gold;
}

public struct CharacterEquipment
{
    public string weaponId;
    public string armorId;
}

public struct skillUnlock
{
    
}
