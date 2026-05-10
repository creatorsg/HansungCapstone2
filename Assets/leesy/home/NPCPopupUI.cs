using Lsy;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lsy
{
    public class NPCPopupUI : MonoBehaviour
    {
        public Transform itemSlotContainer;
        public GameObject itemSlotPrefab;
        public TextMeshProUGUI upgradeProgressText;

        [Header("대장장이 전용 UI (대장장이 NPC에만 연결)")]
        public BlacksmithUI blacksmithUI;

        [Header("정보상 전용 UI (정보상 NPC에만 연결)")]
        public InformantUI informantUI;

        private NPCState _currentState;

        public void InitializeUI(NPCState state)
        {
            Debug.Log($"<color=cyan>[NPCPopupUI] InitializeUI - NPC:{(state != null ? state.name : "NULL")}</color>");

            if (_currentState != null)
                _currentState.OnStateChanged -= RefreshUI;

            _currentState = state;
            _currentState.OnStateChanged += RefreshUI;

            if (blacksmithUI != null)
                blacksmithUI.InitializeUI(state);

            if (informantUI != null)
                informantUI.InitializeUI(state);

            // 서버에서 받아온다
            RefreshUI();
        }

        private void RefreshUI()
        {
            // 서버에서 받아온다
            if (_currentState == null || _currentState.npcData == null) return;

            if (_currentState.currentLevel < _currentState.npcData.maxLevel)
            {
                int idx = _currentState.currentLevel - 1;
                if (idx >= 0 && idx < _currentState.npcData.upgradeTargetGold.Count)
                {
                    int target = _currentState.npcData.upgradeTargetGold[idx];
                    upgradeProgressText.text = $"Lv.{_currentState.currentLevel} 투자: {_currentState.currentInvestedGold} / {target} G";
                }
                else
                {
                    upgradeProgressText.text = $"Lv.{_currentState.currentLevel} (데이터 오류)";
                }
            }
            else
            {
                upgradeProgressText.text = "최대 레벨";
            }

            foreach (Transform child in itemSlotContainer) Destroy(child.gameObject);

            if (_currentState.npcData.sellingItems != null)
            {
                foreach (var item in _currentState.npcData.sellingItems)
                {
                    GameObject slotObj = Instantiate(itemSlotPrefab, itemSlotContainer);

                    int currentPrice = 0;
                    if (item.priceLevel != null && item.priceLevel.Count > 0)
                    {
                        int levelIndex = Mathf.Min(_currentState.currentLevel - 1, item.priceLevel.Count - 1);
                        currentPrice = item.priceLevel[levelIndex];
                    }

                    ItemSlotUI slotUI = slotObj.GetComponent<ItemSlotUI>();
                    if (slotUI != null)
                        slotUI.Setup(item, currentPrice, () => OnBuyItemClicked(item, currentPrice));
                }
            }
        }

        private void OnBuyItemClicked(ItemData item, int price)
        {
            CharacterShop shop = GetLocalShop();
            if (shop == null) return;
            // 서버로 보낸다
            shop.CmdBuyItem(item.itemName, price);
        }

        public void OnInvestButtonClicked(int amount)
        {
            CharacterShop shop = GetLocalShop();
            if (shop == null) return;
            // 서버로 보낸다
            shop.CmdInvestToNPC(_currentState.netId, amount);
        }

        public void OnClickBartenderHeal()
        {
            CharacterShop shop = GetLocalShop();
            if (shop == null) return;
            // 서버로 보낸다
            shop.CmdUseBartender(100);
        }

        private CharacterShop GetLocalShop()
        {
            if (PlayerAccount.LocalInstance == null || PlayerAccount.LocalInstance.currentSelectedCharacter == null)
            {
                Debug.LogWarning("[NPCPopupUI] 선택된 캐릭터가 없습니다!");
                return null;
            }

            CharacterShop shop = PlayerAccount.LocalInstance.currentSelectedCharacter.GetComponent<CharacterShop>();
            if (shop == null)
                Debug.LogError("[NPCPopupUI] CharacterShop 컴포넌트가 프리팹에 없습니다!");

            return shop;
        }

        private void OnDisable()
        {
            if (_currentState != null)
                _currentState.OnStateChanged -= RefreshUI;
        }
    }
}
