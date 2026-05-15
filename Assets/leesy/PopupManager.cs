using UnityEngine;
namespace Lsy
{
    public class PopupManager : MonoBehaviour
    {
        public static PopupManager Instance { get; private set; }
        public Transform canvasTransform;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void ToggleObjectPopup(GameObject popupObject, bool isActive)
        {
            if (popupObject == null) return;

            popupObject.SetActive(isActive);

            if (isActive)
            {
                popupObject.transform.SetAsLastSibling();
            }
        }
    }
}
