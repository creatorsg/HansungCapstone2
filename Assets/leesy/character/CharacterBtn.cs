using UnityEngine;
using UnityEngine.UI;

namespace MyProject.UI.CharacterSelect
{
    public class CharacterButton : MonoBehaviour
    {
        [SerializeField] private UpdateCharacter infoView;
        [SerializeField] private Outline[] buttonOutlines;

        private int lastSelectedIndex = -1;

        public void OnClickThisCharacter(int index)
        {
            if (lastSelectedIndex != -1)
            {
                buttonOutlines[lastSelectedIndex].enabled = false;
            }

            buttonOutlines[index].enabled = true;

            lastSelectedIndex = index;

            infoView.UpdateCharacterUI();

            // 여기서 나중에 '현재 선택된 캐릭터 ID' 같은 걸 저장해두면 
            // 나중에 확정 버튼을 눌렀을 때 동기화하기 편합니다. 
        }
    }
}