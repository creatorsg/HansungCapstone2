using UnityEngine;

namespace Lsy
{
    public class Popup : MonoBehaviour
    {
        //Äù½ºÆ® ÆË¾÷ ¿­°í ´ÝÀ½, Äù½ºÆ® Ãß°¡ ÄÚµå
        [SerializeField] private GameObject _popup;

        public void OpenPopup()
        {
            PopupManager.Instance.ToggleObjectPopup(_popup, true);
        }

        public void ClosePopup()
        {
            PopupManager.Instance.ToggleObjectPopup(_popup, false);
        }

    }
}

