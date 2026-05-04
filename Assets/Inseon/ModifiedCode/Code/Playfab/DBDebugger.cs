using UnityEngine;

public class DBDebugger : MonoBehaviour
{
    [ContextMenu("CharacterDB 출력")]
    public void PrintDB()
    {
        foreach (var pair in CharacterDatabase.Stats)
        {
            var c = pair.Value;
            Debug.Log(
                $"{c.characterCode} ({c.characterName})\n" +
                $"  level={c.level} | hp={c.hp} | stress={c.stress}\n" +
                $"  atk={c.attack} | def={c.defense} | crit={c.critical}\n" +
                $"  acc={c.accuracy} | eva={c.evasion} | spd={c.speed}\n" +
                $"  effectRes={c.effectResistance} | stunRes={c.stunResistance}"
            );
        }
    }

    [ContextMenu("보유 캐릭터 출력")]
    public void PrintOwned()
    {
        var player = inseon.Playfab.User.PlayfabUserManage.Player;
        if (player == null) { Debug.LogError("Player null"); return; }

        foreach (var code in player.GetOwnedCodes())
            Debug.Log($"보유: {code}");
    }
}