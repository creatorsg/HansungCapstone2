using UnityEngine;

public class Store : MonoBehaviour
{
    //상점 팝업 열고 닫음, 아이템 추가 코드
    [SerializeField] private GameObject _store;

    public void OpenStore()
    {
        PopupManager.Instance.ToggleObjectPopup(_store, true);
    }

    public void CloseStore()
    {
        PopupManager.Instance.ToggleObjectPopup(_store, false);
    }

}