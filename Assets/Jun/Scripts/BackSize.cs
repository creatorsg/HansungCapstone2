using UnityEngine;

public class BackgroundRatioScaler : MonoBehaviour
{
    [Header("디자인 기준 화면 비율 (예: 16:9 = 1.7778)")]
    public float referenceAspect = 16f / 9f;

    [Header("화면이 더 넓어질 때도 배경을 키울지 여부")]
    public bool scaleUpOnWiderScreen = false;

    private Vector3 originalScale;

    void Awake()
    {
        // 1. 에디터(예: 16:9 창)에서 원래 여백을 고려해 예쁘게 맞춰둔 초기 스케일을 기억합니다.
        originalScale = transform.localScale;

        AdjustScale();
    }

    void AdjustScale()
    {
        // 2. 현재 실행 중인 화면의 실제 가로세로 비율 계산
        float currentAspect = (float)Screen.width / Screen.height;

        // 3. 기준 비율(16:9) 대비 화면 비율이 얼마나 변했는지 계산 (가로 가중치)
        float aspectFactor = currentAspect / referenceAspect;

        float uniformScale = 1f;

        if (currentAspect < referenceAspect)
        {
            // [경우 A] 화면이 기준보다 가로로 좁아질 때 (예: 16:9 -> 4:3)
            // 배경 양옆이 뚝 잘리는 것을 막기 위해, 줄어든 가로 비율만큼 똑같이 줄여줍니다.
            uniformScale = aspectFactor;
        }
        else
        {
            // [경우 B] 화면이 기준보다 가로로 더 넓어질 때 (예: 16:9 -> 21:9)
            if (scaleUpOnWiderScreen)
            {
                // 체크해두면 넓어진 만큼 가로세로가 똑같이 커집니다.
                uniformScale = aspectFactor;
            }
            else
            {
                // 체크를 꺼두면 세로 높이를 기준으로 원래 그리셨던 여백 크기를 그대로 유지합니다.
                uniformScale = 1f;
            }
        }

        // 4. 핵심: X, Y, Z 스케일에 완전히 '동일한 배율(uniformScale)'을 곱해줍니다.
        // 이로 인해 원본 수묵화 이미지가 절대 찌그러지지 않고 비율을 유지하며 크기만 조절됩니다.
        transform.localScale = originalScale * uniformScale;
    }
}