using UnityEngine;

[CreateAssetMenu(fileName = "Quest_", menuName = "Quest Data")]
public class QuestData : ScriptableObject
{
    [Header("식별")]
    public DistrictType districtType;

    [Header("표시 정보")]
    public string stageName;

    [TextArea(3, 6)] public string questDescription;
    [TextArea(2, 4)] public string clearCondition;
    [TextArea(2, 4)] public string recommendCondition;

    [Header("등장 적 (최대 4)")]
    public Sprite[] enemyPortraits = new Sprite[4];
}
