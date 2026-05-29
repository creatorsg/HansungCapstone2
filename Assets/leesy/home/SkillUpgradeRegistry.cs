using System.Collections.Generic;
using UnityEngine;

namespace Lsy
{
    /// <summary>
    /// 강화 노드 조회소. (characterCode, skillIndex, targetLevel) → SkillUpgradeNode.
    ///
    /// 브릿지(서버)와 정보상 UI가 공통으로 쓰며, UI에 의존하지 않는다(서버에서 InformantUI를 못 믿음).
    /// SkillUpgradeData 에셋을 Resources 폴더 아래(권장: Assets/.../Resources/SkillUpgrades/)에 두면
    /// 첫 조회 시 자동 로드된다. Resources 밖 데이터는 Register()로 수동 등록 가능.
    /// </summary>
    public static class SkillUpgradeRegistry
    {
        // 키는 문자열로 고정해 Unity 버전별 ValueTuple 키 이슈를 피한다.
        private static Dictionary<string, SkillUpgradeNode> _map;

        private static string Key(string code, int skillIndex, int targetLevel)
            => $"{code}|{skillIndex}|{targetLevel}";

        private static void EnsureLoaded()
        {
            if (_map != null) return;
            _map = new Dictionary<string, SkillUpgradeNode>();

            // 모든 Resources 폴더에서 SkillUpgradeData를 긁어온다.
            var all = Resources.LoadAll<SkillUpgradeData>("");
            foreach (var data in all)
                AddData(data);

            Debug.Log($"[SkillUpgradeRegistry] 로드 완료: SO {all.Length}개, 노드 {_map.Count}개");
        }

        private static void AddData(SkillUpgradeData data)
        {
            if (data == null || data.nodes == null) return;
            string code = data.characterCode;
            foreach (var node in data.nodes)
            {
                if (node == null) continue;
                string key = Key(code, node.skillIndex, node.targetLevel);
                if (_map.ContainsKey(key))
                {
                    Debug.LogWarning($"[SkillUpgradeRegistry] 중복 키 무시: (code={code}, idx={node.skillIndex}, lv={node.targetLevel}) — SO '{data.name}'");
                    continue;
                }
                _map[key] = node;
            }
        }

        /// <summary>코드/스킬인덱스/목표레벨로 강화 노드를 조회. 없으면 false.</summary>
        public static bool TryGet(string characterCode, int skillIndex, int targetLevel, out SkillUpgradeNode node)
        {
            EnsureLoaded();
            return _map.TryGetValue(Key(characterCode, skillIndex, targetLevel), out node);
        }

        /// <summary>Resources 밖(씬 배치 등)의 SO를 수동 등록.</summary>
        public static void Register(SkillUpgradeData data)
        {
            EnsureLoaded();
            AddData(data);
        }

        /// <summary>에셋 재로딩이 필요할 때 캐시 무효화(에디터/테스트용).</summary>
        public static void Clear() => _map = null;
    }
}
