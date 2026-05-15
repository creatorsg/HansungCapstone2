using TMPro;
using UnityEngine;

namespace Lsy
{
    /// <summary>
    /// 골드 표시 UI. 골드는 PlayerAccount 귀속이므로 PlayerAccount를 추적합니다.
    /// </summary>
    public class GoldUI : MonoBehaviour
    {
        public static GoldUI Instance;

        [Header("골드 표시 텍스트")]
        public TextMeshProUGUI goldText;

        private PlayerAccount _trackedAccount;

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void OnEnable()
        {
            PlayerAccount.OnLocalAccountReady += SetTrackedAccount;
            PlayerAccount.OnCharacterSwitched += RefreshGold;

            // 이미 LocalInstance가 있으면 바로 연결 (씬 재진입 등)
            if (PlayerAccount.LocalInstance != null)
                SetTrackedAccount(PlayerAccount.LocalInstance);
        }

        private void OnDisable()
        {
            PlayerAccount.OnLocalAccountReady -= SetTrackedAccount;
            PlayerAccount.OnCharacterSwitched -= RefreshGold;

            if (_trackedAccount != null)
                _trackedAccount.OnGoldChanged -= OnGoldChanged;
        }

        private void OnDestroy()
        {
            PlayerAccount.OnLocalAccountReady -= SetTrackedAccount;
            PlayerAccount.OnCharacterSwitched -= RefreshGold;

            if (_trackedAccount != null)
                _trackedAccount.OnGoldChanged -= OnGoldChanged;
        }

        public void SetTrackedAccount(PlayerAccount account)
        {
            if (_trackedAccount != null)
                _trackedAccount.OnGoldChanged -= OnGoldChanged;

            _trackedAccount = account;

            if (_trackedAccount != null)
                _trackedAccount.OnGoldChanged += OnGoldChanged;

            RefreshGold();
        }

        private void OnGoldChanged(int newGold)
        {
            UpdateText(newGold);
        }

        public void RefreshGold()
        {
            int gold = _trackedAccount != null ? _trackedAccount.currentGold
                     : PlayerAccount.LocalInstance != null ? PlayerAccount.LocalInstance.currentGold
                     : 0;
            UpdateText(gold);
        }

        private void UpdateText(int gold)
        {
            if (goldText != null)
                goldText.text = $"{gold:N0} G";
        }
    }
}
