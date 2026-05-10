using Mirror;
using TMPro;
using UnityEngine;

namespace Lsy
{
    /// <summary>
    /// 아지트 화면 골드 표시 텍스트.
    /// 씬 로드 시, 골드 변경 시, 캐릭터 전환 시 자동 갱신.
    /// </summary>
    public class GoldUI : MonoBehaviour
    {
        public static GoldUI Instance;

        [Header("골드 표시 텍스트")]
        public TextMeshProUGUI goldText;

        private CharacterUnit _trackedUnit;

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        // 씬 로드 시 초기화
        private void Start()
        {
            RefreshGold();
        }

        // ==========================================
        // 외부에서 호출 - 추적할 캐릭터 교체
        // PlayerAccount에서 캐릭터 전환 시 호출
        // ==========================================
        public void SetTrackedUnit(CharacterUnit unit)
        {
            // 기존 구독 해제
            if (_trackedUnit != null)
                _trackedUnit.OnGoldChanged -= OnGoldChanged;

            _trackedUnit = unit;

            // 새 캐릭터 골드 변경 구독
            if (_trackedUnit != null)
                _trackedUnit.OnGoldChanged += OnGoldChanged;

            RefreshGold();
        }

        // 골드 변경 이벤트 수신
        private void OnGoldChanged(int newGold)
        {
            UpdateText(newGold);
        }

        // 텍스트 갱신
        public void RefreshGold()
        {
            if (PlayerAccount.LocalInstance == null ||
                PlayerAccount.LocalInstance.currentSelectedCharacter == null)
            {
                UpdateText(0);
                return;
            }

            CharacterUnit unit = PlayerAccount.LocalInstance.currentSelectedCharacter;

            // 추적 유닛이 바뀌었으면 재등록
            if (_trackedUnit != unit)
                SetTrackedUnit(unit);
            else
                UpdateText(unit.currentGold);
        }

        private void UpdateText(int gold)
        {
            if (goldText != null)
                goldText.text = $"{gold:N0} G";
        }

        private void OnDestroy()
        {
            if (_trackedUnit != null)
                _trackedUnit.OnGoldChanged -= OnGoldChanged;
        }
    }
}