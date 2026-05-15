using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Lsy
{
    public class CharacterSlotManager : NetworkBehaviour
    {
        public static CharacterSlotManager Instance;

        public readonly SyncDictionary<int, uint> slotOwners = new SyncDictionary<int, uint>();

        public static event System.Action OnSlotChanged;

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            slotOwners.OnChange += OnSlotOwnersChanged;
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            slotOwners.OnChange -= OnSlotOwnersChanged;
        }

        private void OnSlotOwnersChanged(SyncDictionary<int, uint>.Operation op, int key, uint value)
        {
            // 서버에서 받아온다: 슬롯 소유자 맵 변경(characterIndex -> ownerNetId)
            Debug.Log($"[CharacterSlotManager] 슬롯 변경 - index:{key}, ownerNetId:{value}");
            OnSlotChanged?.Invoke();
        }

        public static bool TryGetLocalNetId(out uint localNetId)
        {
            localNetId = 0;
            if (NetworkClient.connection != null && NetworkClient.connection.identity != null)
            {
                localNetId = NetworkClient.connection.identity.netId;
                return true;
            }

            if (PlayerAccount.LocalInstance != null)
            {
                localNetId = PlayerAccount.LocalInstance.netId;
                return true;
            }

            return false;
        }

        public bool IsMySlot(int characterIndex)
        {
            if (!TryGetLocalNetId(out uint myNetId)) return false;

            if (slotOwners.TryGetValue(characterIndex, out uint ownerNetId))
                return ownerNetId == myNetId;

            return false;
        }

        public bool IsSlotAvailable(int characterIndex)
        {
            return !slotOwners.ContainsKey(characterIndex);
        }

        public bool TryGetSlotOwner(int characterIndex, out uint ownerNetId)
        {
            return slotOwners.TryGetValue(characterIndex, out ownerNetId);
        }

        public bool CanInteract(int characterIndex)
        {
            if (IsMySlot(characterIndex)) return true;
            if (IsSlotAvailable(characterIndex)) return true;
            return false;
        }
    }
}
