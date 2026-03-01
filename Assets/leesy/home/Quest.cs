using UnityEngine;

public class Quest : MonoBehaviour
{
    //퀘스트 팝업 열고 닫음, 퀘스트 추가 코드
    [SerializeField] private GameObject _quest;
    [SerializeField] private GameObject _questItemPrefab;
    [SerializeField] private Transform _contentTransform;

    public void OpenQuest()
    {
        PopupManager.Instance.ToggleObjectPopup(_quest, true);
    }

    public void AddQuest()
    {
        GameObject newQuest = Instantiate(_questItemPrefab, _contentTransform);
        newQuest.transform.localScale = Vector3.one;
        //퀘스트를 받아서 넣음
    }

    public void CloseQuest()
    {
        PopupManager.Instance.ToggleObjectPopup(_quest, false);
    }

}

