using Newtonsoft.Json.Linq;
using PlayFab.ClientModels;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PlayFab Characters 카탈로그에서 불러온 캐릭터 스탯을 정적으로 보관하는 DB.
/// PlayfabCommand.LoadCharacterCatalog() 호출 후 IsLoaded == true 상태가 됩니다.
/// </summary>
public static class CharacterDatabase
{
    // 캐릭터 코드(ItemId) → 스탯 구조체
    public static Dictionary<string, Character> Stats { get; private set; } = new Dictionary<string, Character>();
    public static bool IsLoaded { get; private set; } = false;

    /// <summary>
    /// PlayFab GetCatalogItems 결과를 받아 내부 DB를 채웁니다.
    /// CatalogItem.CustomData 키는 PlayFab 대시보드에 설정한 key와 동일해야 합니다.
    /// </summary>
    public static void Load(List<CatalogItem> catalog)
    {
        Stats = new Dictionary<string, Character>();

        for (int i = 0; i < catalog.Count; i++)
        {
            var item = catalog[i];
            var c = new Character();
            c.index         = i;            // spawnPrefabs 배열과 동일한 순서여야 합니다
            c.characterCode = item.ItemId;

            // CatalogItem.CustomData는 PlayFab SDK에서 JSON 인코딩된 string 타입입니다.
            // JObject로 파싱해야 중첩 오브젝트(예: characterState:{...})가 있어도 안전하게 읽을 수 있습니다.
            if (!string.IsNullOrEmpty(item.CustomData))
            {
                var root = JObject.Parse(item.CustomData);

                // CustomData 구조: { "characterState": { "characterCode": "C001", "hp": 25, ... } }
                // "characterState" 래퍼가 있으면 그 안을 읽고, 없으면 루트를 직접 읽는다.
                var d = (root["characterState"] as JObject) ?? root;

                // characterCode는 Catalog ItemId와 동일하지만 데이터 내부에도 있으면 우선 사용
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

    /// <summary>
    /// characterCode에 해당하는 캐릭터 스탯을 반환합니다.
    /// 존재하지 않을 경우 null을 반환합니다.
    /// </summary>
    public static Character? Get(string characterCode)
    {
        if (Stats.TryGetValue(characterCode, out Character c))
            return c;

        Debug.LogWarning($"[CharacterDatabase] 캐릭터를 찾을 수 없습니다: {characterCode}");
        return null;
    }

    // ─────────────────── helper ───────────────────

    /// JObject에서 숫자 값을 안전하게 읽습니다.
    /// "100"(문자열)이든 100(정수)이든 모두 처리하며,
    /// 키가 없거나 중첩 오브젝트/배열이면 0을 반환합니다.
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