using Jun;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 스킬 슬롯 아이콘에 붙여두는 hover 감지 컴포넌트.
/// CharacterSelectManager.InitSkillSlots()에서 자동으로 AddComponent됩니다.
/// </summary>
public class SkillSlotHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private SkillInfo _skill;

    /// <summary>현재 캐릭터가 바뀔 때 Manager에서 호출해 스킬 정보를 갱신합니다.</summary>
    public void SetSkill(SkillInfo skill)
    {
        _skill = skill;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_skill != null)
            CharacterSelectManager.Instance?.OnSkillHoverEnter(_skill);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        CharacterSelectManager.Instance?.OnSkillHoverExit();
    }
}
