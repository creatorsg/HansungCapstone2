using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Jun
{
    /// <summary>
    /// 배틀 종료 시 결과(승리/패배)를 표시하고,
    /// 모든 플레이어가 동의 버튼을 눌러야 다음 단계로 넘어갑니다.
    ///
    /// ▶ Unity Editor 연결 방법
    ///   1. Battle씬 Canvas 하위에 Panel(BattleResult)을 만들고 이 컴포넌트 추가.
    ///   2. resultTitleText  → 큰 글씨 (VICTORY / DEFEAT)
    ///   3. resultSubText    → 부연 설명 텍스트
    ///   4. agreeButton      → "동의" 버튼
    ///   5. agreeCountText   → "1 / 3 동의" 표시 텍스트 (선택)
    ///   6. checkImage       → 동의 클릭 시 나타날 체크 이미지 (초기: 비활성)
    ///   7. stampImage       → 전원 동의 후 찍힐 도장 이미지 (초기: 비활성)
    ///   8. BattleManager Inspector의 _battleResultPanel 슬롯에 연결.
    /// </summary>
    public class BattleResultPanel : MonoBehaviour
    {
        [Header("UI 요소")]
        [SerializeField] private TextMeshProUGUI resultTitleText;
        [SerializeField] private TextMeshProUGUI resultSubText;
        [SerializeField] private Button          agreeButton;
        [SerializeField] private TextMeshProUGUI agreeCountText;

        [Header("이미지 연출")]
        [SerializeField] private Image checkImage;   // 동의 클릭 시 표시되는 체크 이미지
        [SerializeField] private Image stampImage;   // 전원 동의 후 찍히는 도장 이미지

        [Header("색상")]
        [SerializeField] private Color victoryColor = new Color(1f, 0.85f, 0.2f);
        [SerializeField] private Color defeatColor  = new Color(0.8f, 0.2f, 0.2f);

        [Header("도장 애니메이션")]
        [Tooltip("도장이 찍히는 데 걸리는 시간 (초)")]
        [SerializeField] private float stampDuration = 0.4f;
        [Tooltip("도장이 찍힐 때 살짝 기울어지는 각도")]
        [SerializeField] private float stampRotation = -12f;

        private bool _hasAgreed;   // 이 클라이언트가 이미 동의했는지

        // ──────────────────────────────────────────────────────

        private void Awake()
        {
            gameObject.SetActive(false);

            if (agreeButton != null)
                agreeButton.onClick.AddListener(OnClickAgree);

            // 체크·도장 이미지는 초기에 숨김
            if (checkImage != null) checkImage.gameObject.SetActive(false);
            if (stampImage != null) stampImage.gameObject.SetActive(false);
        }

        // ──────────────────────────────────────────────────────
        //  외부 호출 API
        // ──────────────────────────────────────────────────────

        /// <summary>
        /// 결과 패널을 열고 승리/패배 내용을 표시합니다.
        /// BattleManager의 [ClientRpc] 안에서 호출하세요.
        /// </summary>
        public void Show(bool isVictory, int totalPlayers)
        {
            _hasAgreed = false;

            if (agreeButton != null) agreeButton.interactable = true;
            if (checkImage  != null) checkImage.gameObject.SetActive(false);
            if (stampImage  != null) stampImage.gameObject.SetActive(false);

            UpdateAgreeCount(0, totalPlayers);

            if (resultTitleText != null)
            {
                resultTitleText.text  = isVictory ? "VICTORY" : "DEFEAT";
                resultTitleText.color = isVictory ? victoryColor : defeatColor;
            }

            if (resultSubText != null)
            {
                resultSubText.text = isVictory
                    ? "전투에서 승리했습니다!\n보상을 선택하세요."
                    : "전투에서 패배했습니다.\n홈으로 돌아갑니다.";
            }

            gameObject.SetActive(true);
        }

        /// <summary>동의 카운트 UI를 갱신합니다.</summary>
        public void UpdateAgreeCount(int current, int total)
        {
            if (agreeCountText != null)
                agreeCountText.text = $"{current} / {total} 동의";
        }

        /// <summary>
        /// 전원 동의 완료 시 도장 애니메이션을 재생합니다.
        /// BattleManager의 RpcShowStamp에서 호출됩니다.
        /// </summary>
        public void PlayStampAnimation()
        {
            if (stampImage == null) return;
            StartCoroutine(StampCoroutine());
        }

        /// <summary>패널을 닫습니다 (도장 연출 완료 후 호출).</summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        // ──────────────────────────────────────────────────────
        //  버튼 이벤트
        // ──────────────────────────────────────────────────────

        private void OnClickAgree()
        {
            if (_hasAgreed) return;
            _hasAgreed = true;

            if (agreeButton != null) agreeButton.interactable = false;

            // 체크 이미지 표시
            if (checkImage != null) checkImage.gameObject.SetActive(true);

            // 서버에 동의 전달
            BattleManager.Instance?.CmdAgreeResult();
        }

        // ──────────────────────────────────────────────────────
        //  도장 애니메이션
        // ──────────────────────────────────────────────────────

        /// <summary>
        /// 도장이 위에서 쾅 찍히는 연출.
        ///   1단계: 크고 기울어진 상태, 투명하게 즉시 등장
        ///   2단계: 빠르게 제자리로 눌리며 불투명도 증가  ← 도장 찍히는 충격
        ///   3단계: 아주 살짝 튕기며 최종 크기로 안착
        /// </summary>
        private IEnumerator StampCoroutine()
        {
            stampImage.gameObject.SetActive(true);

            var rt          = stampImage.rectTransform;
            var canvasGroup = stampImage.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = stampImage.gameObject.AddComponent<CanvasGroup>();

            // ── 초기 상태: 크고 기울어진 채로 투명 ──────────────
            rt.localScale    = Vector3.one * 1.6f;
            rt.localRotation = Quaternion.Euler(0f, 0f, stampRotation);
            canvasGroup.alpha = 0f;

            // ── 1단계: 빠르게 눌리며 등장 ─────────────────────────
            float phase1  = stampDuration * 0.65f;
            float elapsed = 0f;

            while (elapsed < phase1)
            {
                elapsed += Time.deltaTime;
                float t     = Mathf.Clamp01(elapsed / phase1);
                float eased = 1f - Mathf.Pow(1f - t, 3f); // ease-out cubic

                rt.localScale    = Vector3.one * Mathf.Lerp(1.6f, 0.92f, eased);
                rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(stampRotation, 0f, eased));
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, eased);

                yield return null;
            }

            // ── 2단계: 튕김 (0.92 → 1.05 → 1.0) ─────────────────
            float phase2 = stampDuration * 0.35f;
            elapsed = 0f;

            while (elapsed < phase2)
            {
                elapsed += Time.deltaTime;
                float t      = Mathf.Clamp01(elapsed / phase2);
                float bounce = Mathf.Sin(t * Mathf.PI);          // 0 → 1 → 0
                float scale  = Mathf.Lerp(0.92f, 1f, t) + bounce * 0.08f;

                rt.localScale    = Vector3.one * scale;
                rt.localRotation = Quaternion.identity;
                canvasGroup.alpha = 1f;

                yield return null;
            }

            // ── 최종 상태 확정 ────────────────────────────────────
            rt.localScale    = Vector3.one;
            rt.localRotation = Quaternion.identity;
            canvasGroup.alpha = 1f;
        }
    }
}
