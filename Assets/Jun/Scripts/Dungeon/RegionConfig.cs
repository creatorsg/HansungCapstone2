using UnityEngine;

namespace Jun
{
    /// <summary>
    /// 한 지역(아지트 + 던전 씬 페어)의 설정.
    /// 디자이너가 인스펙터에서 채우고, RoundManager.StartDungeon() 에 넘깁니다.
    /// 메뉴: Create → Dungeon → Region Config
    /// </summary>
    [CreateAssetMenu(fileName = "Region_", menuName = "Dungeon/Region Config", order = 0)]
    public class RegionConfig : ScriptableObject
    {
        [Header("식별")]
        [Tooltip("지역 식별자 (예: Forest, Ruins)")]
        public string RegionId;

        [Header("씬 (Build Settings에 등록되어 있어야 함)")]
        [Tooltip("이 지역의 던전 씬 이름 (BattleManager가 있는 씬)")]
        public string DungeonSceneName;
        [Tooltip("이 지역의 아지트 씬 이름 (HideoutManager가 있는 씬)")]
        public string HideoutSceneName;

        [Header("진행")]
        [Min(1)] public int TotalRounds = 4;
    }
}
