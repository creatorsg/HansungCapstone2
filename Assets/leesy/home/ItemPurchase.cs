using UnityEngine;

public class ItemPurchase : MonoBehaviour
{
    [SerializeField] GameObject BuyImage;

    public void OpenPurchase()
    {
        PopupManager.Instance.ToggleObjectPopup(BuyImage, true);
    }

    public void BuyItemBtn()
    {
        //해당 아이템 서버에 전달 및 플레이어 소지품에 추가
    }

    public void ClosePurchase()
    {
        PopupManager.Instance.ToggleObjectPopup(BuyImage, false);
    }
}
