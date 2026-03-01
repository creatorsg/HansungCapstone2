using UnityEngine;
using UnityEngine.UI;

public class BtnShape : MonoBehaviour
{
    //이미지에 맞춰서 버튼 모양 바꾸기
    public float AlphaThreshold = 0.1f;

    void Start()
    {
        this.GetComponent<Image>().alphaHitTestMinimumThreshold = AlphaThreshold; // 이미지에서 지정된 투명도보다 높은 부분만 활성화
    }
}
