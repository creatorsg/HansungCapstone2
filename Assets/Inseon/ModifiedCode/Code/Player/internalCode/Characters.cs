using UnityEngine;

public struct Character
{
    public string characterCode;
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
}

public struct CharacterEquipment
{
    public string weaponId;
    public string armorId;
}

public struct skillUnlock
{
    
}
