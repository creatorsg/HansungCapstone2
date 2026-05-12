using Lsy;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 아지트(Home) 씬의 "스킬트리" 메뉴 버튼.
/// NPC 콜라이더 클릭 대신 UI 버튼으로 정보상 UI를 진입시킨다.
///
/// - npcInteract 또는 (popup + npcState) 중 하나만 연결하면 동작.
/// - NPC GameObject가 씬에 있으면 NPCInteract 참조가 가장 간단.
/// - NPC를 두지 않고 패널만 띄우려면 popup + npcState를 직접 연결.
/// </summary>
public class SkillTreeButton : MonoBehaviour
{
    [Header("진입 방식 1: 기존 NPCInteract 재사용 (권장)")]
    [SerializeField] private NPCInteract npcInteract;

    [Header("진입 방식 2: NPC 없이 패널만 직접 열기")]
    [SerializeField] private NPCPopupUI popupUI;
    [SerializeField] private NPCState informantNpcState;

    [Header("패널 루트 (열 때 활성화)")]
    [SerializeField] private GameObject panelRoot;

    [Header("버튼 (없으면 같은 GameObject에서 자동 검색)")]
    [SerializeField] private Button button;

    void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(Open);
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(Open);
    }

    public void Open()
    {
        if (panelRoot != null) panelRoot.SetActive(true);

        if (npcInteract != null)
        {
            npcInteract.ClickNPC();
            return;
        }

        if (popupUI != null && informantNpcState != null)
        {
            popupUI.InitializeUI(informantNpcState);
            return;
        }

        Debug.LogWarning("[SkillTreeButton] 진입 대상이 없음. npcInteract 또는 (popupUI + informantNpcState) 둘 중 하나는 연결해야 합니다.");
    }
}
