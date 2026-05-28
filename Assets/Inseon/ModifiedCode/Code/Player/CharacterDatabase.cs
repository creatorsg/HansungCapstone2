using Newtonsoft.Json.Linq;
using PlayFab.ClientModels;
using System.Collections.Generic;
using UnityEngine;

public static class CharacterDatabase
{
    public static Dictionary<string, Character> Stats { get; private set; } = new Dictionary<string, Character>();
    public static bool IsLoaded { get; private set; } = false;

    public static void Load(List<CatalogItem> catalog)
    {
        Stats = new Dictionary<string, Character>();

        for (int i = 0; i < catalog.Count; i++)
        {
            var item = catalog[i];
            var c = new Character();
            c.index         = i;            
            c.characterCode = item.ItemId;

            if (!string.IsNullOrEmpty(item.CustomData))
            {
                var root = JObject.Parse(item.CustomData);
                var d = (root["characterState"] as JObject) ?? root;

                if (d["characterCode"] != null)
                    c.characterCode = d["characterCode"].Value<string>();

                c.characterName    = GetString(d, "characterName");
                c.hp               = GetInt(d, "hp");
                c.mana             = GetInt(d, "mana");
                c.attack           = GetInt(d, "attack");
                c.defense          = GetInt(d, "defense");
                c.critical         = GetInt(d, "critical");
                c.accuracy         = GetInt(d, "accuracy");
                c.evasion          = GetInt(d, "evasion");
                c.speed            = GetInt(d, "speed");
                c.stress           = GetInt(d, "stress");
                c.effectResistance = GetInt(d, "effectResistance");
                c.stunResistance   = GetInt(d, "stunResistance");
                c.level            = GetInt(d, "level");
                
            }

            Stats[item.ItemId] = c;
        }

        IsLoaded = true;
        Debug.Log($"[CharacterDatabase] {Stats.Count}개 캐릭터 로드 완료");
    }

    public static Character? Get(string characterCode)
    {
        if (Stats.TryGetValue(characterCode, out Character c))
            return c;

        Debug.LogWarning($"[CharacterDatabase] 캐릭터를 찾을 수 없습니다: {characterCode}");
        return null;
    }

    private static int GetInt(JObject obj, string key)
    {
        if (!obj.TryGetValue(key, out JToken token))
            return 0;

        if (token.Type == JTokenType.Object || token.Type == JTokenType.Array)
            return 0;

        try { return token.Value<int>(); }
        catch { return 0; }
    }

    private static string GetString(JObject obj, string key)
    {
        if (!obj.TryGetValue(key, out JToken token))
            return string.Empty;

        if (token.Type == JTokenType.Object || token.Type == JTokenType.Array)
            return string.Empty;

        return token.Value<string>() ?? string.Empty;
    }
}