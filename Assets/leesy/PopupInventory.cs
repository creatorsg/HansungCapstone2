using UnityEngine;

namespace Lsy
{
    public class PopupInventory : MonoBehaviour
    {
        [SerializeField] private GameObject _popup;

        public void OpenPopup()
        {
            PopupManager.Instance.ToggleObjectPopup(_popup, true);
        }

        public void ClosePopup()
        {
            // 장착 정보 동기화
            if (PlayerAccount.LocalInstance?.currentSelectedCharacter != null)
            {
                Debug.Log("동기화 준비");
                PlayerAccount.LocalInstance.currentSelectedCharacter.CmdSyncEquipmentToPlayerData();
            }

            PopupManager.Instance.ToggleObjectPopup(_popup, false);
        }
    }
}