using UnityEngine;

[CreateAssetMenu(fileName = "Quest_", menuName = "Quest Data")]
public class QuestData : ScriptableObject
{
    [Header("식별")]
    public DistrictType districtType;

    [Header("표시 정보")]
    public string stageName;

    [TextArea(1, 6)] public string questDescription;
    [TextArea(1, 4)] public string clearCondition;
    [TextArea(1, 4)] public string recommendCondition;

    [Header("등장 적 (최대 4)")]
    public Sprite[] enemyPortraits = new Sprite[4];

    [Header("이동할 전투 씬")]
    public string battleSceneName;
}
