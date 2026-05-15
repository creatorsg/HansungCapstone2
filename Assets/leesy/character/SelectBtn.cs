using UnityEngine;
using UnityEngine.UI;

namespace Lsy
{
    public class SelectBtn : MonoBehaviour
    {
        [SerializeField] Image user;
        [SerializeField] Sprite[] characterSprite;
        private int _chracterNum = 0;

        public void CharacterSelectBtn(int index)
        {
            UpdateCharacterSprite(_chracterNum);
        }

        private void UpdateCharacterSprite(int _selectIndex)
        {
            user.sprite = characterSprite[_selectIndex]; 
        }
        public void CharacterIcon (int index)
        {
            _chracterNum = index;
        }
    }
}
