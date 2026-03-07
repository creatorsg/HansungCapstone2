using UnityEngine;

public struct Character
{
    string characterCode;
    CharacterEquipment equipment;
    int level;
    int hp;
    int mana;
    int stress;
    int effectResistance;
    int stunResistance;
    int attack;
    int defense;
    int critical;
    int accuracy;
    int evasion;
    int speed;
}

public struct CharacterEquipment
{
    public string weaponId;
    public string armorId;
}

public struct skillUnlock
{
    
}
