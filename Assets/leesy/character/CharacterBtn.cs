using UnityEngine;
using UnityEngine.UI;
using Lsy;

namespace Lsy
{
    public class CharacterButton : MonoBehaviour
    {
        [SerializeField] private Lsy.UpdateCharacter infoView;
        [SerializeField] private Lsy.SelectBtn selectBtn;
        [SerializeField] private Outline[] buttonOutlines;

        private int _lastSelectIndex = -1;

        public void OnClickCharacter(int index)
        {
            if (_lastSelectIndex != -1)
            {
                buttonOutlines[_lastSelectIndex].enabled = false;
            }

            buttonOutlines[index].enabled = true;

            _lastSelectIndex = index;

            selectBtn.CharacterIcon(index);
            infoView.UpdateCharacterUI();
        }
    }
}