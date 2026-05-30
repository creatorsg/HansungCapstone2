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
            if (PlayerAccount.LocalInstance?.currentSelectedCharacter != null)
            {
                Debug.Log("���� �غ�");
                PlayerAccount.LocalInstance.currentSelectedCharacter.CmdSyncEquipmentToPlayerData();
            }

            PopupManager.Instance.ToggleObjectPopup(_popup, false);
        }
    }
}