using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

namespace Lsy
{
    [System.Serializable]
    public struct HeroPortraitEntry
    {
        public string heroCode;
        /// <summary>기본 초상화 (미선택 상태)</summary>
        public Sprite portrait;
        /// <summary>선택됐을 때 초상화 (아웃라인 등)</summary>
        public Sprite selectedPortrait;
    }

    /// <summary>
    /// p1~p4 초상화 슬롯 관리.
    ///
    /// 이미지 적용 우선순위:
    ///   1) PlayerAccount.myHeroCodes SyncList → 로컬 플레이어 캐릭터 (선택/비선택 구분)
    ///   2) CharacterUnit.OnAnyUnitReady       → 모든 플레이어 캐릭터 (기본 초상화)
    ///      단, 로컬 플레이어 슬롯은 1)이 이미 처리하므로 2)가 덮어쓰지 않음
    ///
    /// 소유권 표시:
    ///   - 내 캐릭터: alpha=ownedAlpha, CanvasGroup.interactable=true
    ///     └ 현재 선택된 캐릭터는 button.interactable=false (자기 자신으로 스왑 불가)
    ///   - 타인 캐릭터: alpha=notOwnedAlpha, CanvasGroup.interactable=false
    /// </summary>
    public class CharacterPortraitUI : MonoBehaviour
    {
        [System.Serializable]
        public class PortraitSlot
        {
            public Image       portraitImage;
            public Button      button;
            public CanvasGroup canvasGroup;
            public Slider      hpSlider;
            public Slider      sanSlider;
            public Image blacksmithImage;
            public Image informantImage;
            public Image bartenderImage;
        }

        [Header("초상화 슬롯 (heroPos 0~3 순서로 배치)")]
        public List<PortraitSlot> slots = new List<PortraitSlot>();

        [Header("HeroCode → 초상화 스프라이트 매핑")]
        public List<HeroPortraitEntry> portraitDatabase = new List<HeroPortraitEntry>();
        private Dictionary<string, Sprite> _portraitDict;
        private Dictionary<string, Sprite> _selectedPortraitDict;

        [Header("투명도")]
        [Range(0f, 1f)] public float ownedAlpha    = 1f;
        [Range(0f, 1f)] public float notOwnedAlpha = 0.35f;

        private PlayerAccount _trackedAccount;

        /// <summary>heroPos → 현재 스폰된 CharacterUnit 매핑 (슬라이더 갱신용)</summary>
        private readonly Dictionary<int, CharacterUnit> _unitByPos = new Dictionary<int, CharacterUnit>();

        // ─── 초기화 ───────────────────────────────────────────────────

        private void Awake()
        {
            _portraitDict         = new Dictionary<string, Sprite>();
            _selectedPortraitDict = new Dictionary<string, Sprite>();

            foreach (var entry in portraitDatabase)
            {
                if (string.IsNullOrEmpty(entry.heroCode)) continue;
                if (entry.portrait != null && !_portraitDict.ContainsKey(entry.heroCode))
                    _portraitDict[entry.heroCode] = entry.portrait;
                if (entry.selectedPortrait != null && !_selectedPortraitDict.ContainsKey(entry.heroCode))
                    _selectedPortraitDict[entry.heroCode] = entry.selectedPortrait;
            }

            for (int i = 0; i < slots.Count; i++)
            {
                int captured = i;
                if (slots[i].button != null)
                    slots[i].button.onClick.AddListener(() => OnPortraitClicked(captured));

                // 슬라이더는 표시 전용 — 내부 Graphic의 raycastTarget을 모두 끄고
                // 슬라이더 자체도 interactable=false 처리하여 클릭 이벤트를 막지 않도록 설정
                DisableSliderRaycast(slots[i].hpSlider);
                DisableSliderRaycast(slots[i].sanSlider);

                if (slots[i].hpSlider  != null) slots[i].hpSlider.value  = 1f;
                if (slots[i].sanSlider != null) slots[i].sanSlider.value = 1f;
            }
        }

        private void OnEnable()
        {
            PlayerAccount.OnLocalAccountReady   += OnAccountReady;
            PlayerAccount.OnAnyAccountReady     += OnAnyAccountReady;
            PlayerAccount.OnCharacterSwitched   += OnCharacterSwitched;
            CharacterUnit.OnAnyUnitReady        += OnUnitReady;
            CharacterUnit.OnAnyUnitStatsChanged += OnUnitStatsChanged;
        }

        private void OnDisable()
        {
            PlayerAccount.OnLocalAccountReady   -= OnAccountReady;
            PlayerAccount.OnAnyAccountReady     -= OnAnyAccountReady;
            PlayerAccount.OnCharacterSwitched   -= OnCharacterSwitched;
            CharacterUnit.OnAnyUnitReady        -= OnUnitReady;
            CharacterUnit.OnAnyUnitStatsChanged -= OnUnitStatsChanged;

            UnsubscribeAccount();
        }

        private void Start()
        {
            if (PlayerAccount.LocalInstance != null)
                OnAccountReady(PlayerAccount.LocalInstance);

            // 이미 스폰된 모든 PlayerAccount 처리 (씬 재진입 등)
            foreach (var account in FindObjectsByType<PlayerAccount>(FindObjectsSortMode.None))
            {
                if (account.myHeroCodes.Count > 0)
                    OnAnyAccountReady(account);
            }

            // 이미 스폰된 유닛 처리
            foreach (var unit in FindObjectsByType<CharacterUnit>(FindObjectsSortMode.None))
                RegisterUnit(unit);
        }

        // ─── PlayerAccount 연결 ───────────────────────────────────────

        private void OnAccountReady(PlayerAccount account)
        {
            UnsubscribeAccount();
            _trackedAccount = account;

            if (_trackedAccount != null)
            {
                _trackedAccount.myHeroPositions.Callback += OnHeroListChanged;
                _trackedAccount.myHeroCodes.Callback     += OnHeroListChanged;
            }

            ApplyAllPortraits();
            RefreshOwnership();
        }

        /// <summary>
        /// 모든 PlayerAccount(로컬+원격)의 초상화를 적용합니다.
        /// 로컬 계정은 ApplyAllPortraits가 처리하므로 원격 계정만 처리합니다.
        /// </summary>
        private void OnAnyAccountReady(PlayerAccount account)
        {
            // 로컬 플레이어는 OnAccountReady / ApplyAllPortraits에서 처리
            if (account == _trackedAccount || account == PlayerAccount.LocalInstance) return;

            ApplyPortraitsFromAccount(account);
            RefreshOwnership();
        }

        /// <summary>
        /// 원격 PlayerAccount의 모든 heroPos에 기본 초상화를 적용합니다.
        /// </summary>
        private void ApplyPortraitsFromAccount(PlayerAccount account)
        {
            if (account == null) return;
            int count = Mathf.Min(account.myHeroPositions.Count, account.myHeroCodes.Count);
            for (int i = 0; i < count; i++)
            {
                int    pos  = account.myHeroPositions[i];
                string code = account.myHeroCodes[i];
                ApplyRawPortrait(pos, code);
            }
        }

        private void UnsubscribeAccount()
        {
            if (_trackedAccount != null)
            {
                _trackedAccount.myHeroPositions.Callback -= OnHeroListChanged;
                _trackedAccount.myHeroCodes.Callback     -= OnHeroListChanged;
            }
            _trackedAccount = null;
        }

        // ─── SyncList 콜백 ────────────────────────────────────────────

        private void OnHeroListChanged(SyncList<int>.Operation op, int index, int oldItem, int newItem)
        {
            ApplyAllPortraits();
            RefreshOwnership();
        }

        private void OnHeroListChanged(SyncList<string>.Operation op, int index, string oldItem, string newItem)
        {
            ApplyAllPortraits();
            RefreshOwnership();
        }

        // ─── CharacterUnit 이벤트 ─────────────────────────────────────

        private void OnUnitReady(CharacterUnit unit)
        {
            RegisterUnit(unit);
        }

        private void OnUnitStatsChanged(CharacterUnit unit)
        {
            RefreshBar(unit.heroPos, unit);
        }

        private void OnCharacterSwitched()
        {
            // 선택 초상화 스왑 + 소유권 갱신
            ApplyAllPortraits();
            RefreshOwnership();
        }

        // ─── 유닛 등록 ───────────────────────────────────────────────

        private void RegisterUnit(CharacterUnit unit)
        {
            if (unit == null || unit.heroPos < 0) return;
            _unitByPos[unit.heroPos] = unit;

            // 로컬 플레이어 슬롯은 ApplyAllPortraits가 처리하므로 중복 적용하지 않음
            var account = _trackedAccount ?? PlayerAccount.LocalInstance;
            bool isLocalSlot = account != null && account.myHeroPositions.Contains(unit.heroPos);

            if (!isLocalSlot)
            {
                // 다른 플레이어 캐릭터 → 기본 초상화 적용
                ApplyRawPortrait(unit.heroPos, unit.heroCode);
            }

            RefreshBar(unit.heroPos, unit);
            RefreshOwnership();
        }

        // ─── 초상화 이미지 적용 ───────────────────────────────────────

        /// <summary>
        /// 로컬 PlayerAccount SyncList 기반으로 내 캐릭터 초상화를 적용합니다.
        /// 선택된 캐릭터는 selectedPortrait, 아니면 portrait 사용.
        /// </summary>
        private void ApplyAllPortraits()
        {
            var account = _trackedAccount ?? PlayerAccount.LocalInstance;
            if (account == null) return;

            int count = Mathf.Min(account.myHeroPositions.Count, account.myHeroCodes.Count);
            for (int i = 0; i < count; i++)
            {
                int    pos  = account.myHeroPositions[i];
                string code = account.myHeroCodes[i];

                if (pos < 0 || pos >= slots.Count) continue;
                var slot = slots[pos];
                if (slot.portraitImage == null) continue;

                bool isSelected = account.currentActiveIndex >= 0
                               && account.currentActiveIndex < account.myHeroPositions.Count
                               && account.myHeroPositions[account.currentActiveIndex] == pos;

                Sprite apply = null;
                if (isSelected && _selectedPortraitDict.TryGetValue(code, out Sprite selSprite))
                    apply = selSprite;
                else if (_portraitDict.TryGetValue(code, out Sprite sprite))
                    apply = sprite;
                else
                    Debug.LogWarning($"[PortraitUI] heroCode '{code}'에 해당하는 스프라이트 없음");

                if (apply != null)
                {
                    slot.portraitImage.sprite = apply;
                    if (slot.blacksmithImage != null) slot.blacksmithImage.sprite = apply;
                    if (slot.informantImage  != null) slot.informantImage.sprite  = apply;
                    if (slot.bartenderImage  != null) slot.bartenderImage.sprite  = apply;
                }
            }
        }

        /// <summary>
        /// 다른 플레이어 캐릭터용 — heroCode에 맞는 기본 초상화를 슬롯에 바로 적용합니다.
        /// </summary>
        private void ApplyRawPortrait(int pos, string heroCode)
        {
            if (pos < 0 || pos >= slots.Count) return;
            var slot = slots[pos];
            if (slot.portraitImage == null) return;

            if (_portraitDict.TryGetValue(heroCode, out Sprite sprite))
            {
                slot.portraitImage.sprite = sprite;
                if (slot.blacksmithImage != null) slot.blacksmithImage.sprite = sprite;
                if (slot.informantImage  != null) slot.informantImage.sprite  = sprite;
                if (slot.bartenderImage  != null) slot.bartenderImage.sprite  = sprite;
                Debug.Log($"[PortraitUI] 슬롯{pos}에 타인 캐릭터 초상화 적용: {heroCode}");
            }
            else
            {
                Debug.LogWarning($"[PortraitUI] heroCode '{heroCode}'에 해당하는 스프라이트 없음");
            }
        }

        // ─── HP / San 슬라이더 갱신 ──────────────────────────────────

        /// <summary>
        /// 슬라이더를 표시 전용으로 설정합니다.
        /// 슬라이더 자체와 자식 Graphic 모두 raycastTarget = false 처리하여
        /// 버튼 클릭을 가로채지 않도록 합니다.
        /// </summary>
        private static void DisableSliderRaycast(UnityEngine.UI.Slider slider)
        {
            if (slider == null) return;
            slider.interactable = false;
            foreach (var graphic in slider.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
        }

        private void RefreshBar(int pos, CharacterUnit unit)
        {
            if (pos < 0 || pos >= slots.Count) return;
            var slot = slots[pos];

            if (slot.hpSlider != null && unit.maxHp > 0)
                slot.hpSlider.value = Mathf.Clamp01(unit.currentHp / unit.maxHp);

            if (slot.sanSlider != null && unit.maxSan > 0)
                slot.sanSlider.value = Mathf.Clamp01((float)unit.currentSan / unit.maxSan);
        }

        // ─── 소유권 / 선택 표시 갱신 ─────────────────────────────────

        public void RefreshOwnership()
        {
            var account = _trackedAccount ?? PlayerAccount.LocalInstance;

            for (int i = 0; i < slots.Count; i++)
            {
                bool isOwned    = account != null && account.myHeroPositions.Contains(i);
                bool isSelected = isOwned
                               && account.currentActiveIndex >= 0
                               && account.currentActiveIndex < account.myHeroPositions.Count
                               && account.myHeroPositions[account.currentActiveIndex] == i;

                // CanvasGroup: alpha(투명도)만 제어.
                // interactable은 항상 true — false로 설정하면 Unity CanvasGroup 계층 특성상
                // 해당 그룹의 자식이거나 같은 부모 아래 있는 다른 슬롯 Button까지 연쇄적으로
                // 막혀서 비연속 소유 슬롯이 클릭 불가가 되는 버그가 발생.
                // 클릭 차단은 아래 Button.interactable로만 제어한다.
                if (slots[i].canvasGroup != null)
                {
                    slots[i].canvasGroup.alpha          = isOwned ? ownedAlpha : notOwnedAlpha;
                    slots[i].canvasGroup.interactable   = true;  // 항상 true — 계층 전파 방지
                    slots[i].canvasGroup.blocksRaycasts = true;
                }

                // Button: 소유 슬롯만 활성화, 현재 선택 중인 캐릭터는 비활성(자기 자신으로 스왑 방지)
                if (slots[i].button != null)
                    slots[i].button.interactable = isOwned && !isSelected;
            }
        }

        // ─── 클릭 처리 ───────────────────────────────────────────────

        private void OnPortraitClicked(int slotIndex)
        {
            var account = PlayerAccount.LocalInstance;
            if (account == null) return;

            int profileIndex = account.myHeroPositions.IndexOf(slotIndex);
            if (profileIndex < 0) return;

            account.SelectCharacter(profileIndex);
        }
    }
}
