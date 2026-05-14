using Mirror;
using UnityEngine;

namespace Jun
{
    public enum DungeonState
    {
        None,
        InBattle,
        Cleared,
        Failed
    }

    public class RoundManager : NetworkBehaviour
    {
        public static RoundManager Instance;

        [Header("Session")]
        [SyncVar(hook = nameof(OnRegionChanged))] public string CurrentRegionId = "";
        [SyncVar(hook = nameof(OnRoundChanged))] public int CurrentRound = 0;
        [SyncVar] public int TotalRounds = 0;
        [SyncVar] public string HideoutSceneName = "";
        [SyncVar(hook = nameof(OnStateChanged))] public DungeonState State = DungeonState.None;

        [Header("Debug")]
        [SerializeField] private bool _verbose = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                if (_verbose) Debug.LogWarning("[RoundManager] Duplicate instance destroyed.");
                Destroy(gameObject);
                return;
            }

            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            Instance = this;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            if (_verbose) Debug.Log("[RoundManager] OnStartServer");
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (_verbose) Debug.Log($"[RoundManager] OnStartClient Region={CurrentRegionId} Round={CurrentRound}/{TotalRounds} State={State}");
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            if (Instance == this) Instance = null;
        }

        [Server]
        public void StartDungeon(RegionConfig cfg)
        {
            if (cfg == null)
            {
                Debug.LogError("[RoundManager] StartDungeon: RegionConfig is null.");
                return;
            }

            CurrentRegionId = cfg.RegionId;
            CurrentRound = 1;
            TotalRounds = cfg.TotalRounds;
            HideoutSceneName = cfg.HideoutSceneName;
            State = DungeonState.InBattle;

            if (_verbose) Debug.Log($"[RoundManager] StartDungeon Region={cfg.RegionId} Rounds={cfg.TotalRounds} -> ServerChangeScene({cfg.DungeonSceneName})");
            NetworkManager.singleton.ServerChangeScene(cfg.DungeonSceneName);
        }

        [Server]
        public void OnRoundCleared()
        {
            if (State != DungeonState.InBattle)
            {
                if (_verbose) Debug.LogWarning($"[RoundManager] OnRoundCleared ignored. State={State}");
                return;
            }

            if (CurrentRound >= TotalRounds)
            {
                CompleteDungeon();
                return;
            }

            CurrentRound++;
            if (_verbose) Debug.Log($"[RoundManager] Next round {CurrentRound}/{TotalRounds}");
        }

        [Server]
        public void OnAllPlayersDead()
        {
            if (State != DungeonState.InBattle) return;

            State = DungeonState.Failed;
            if (_verbose) Debug.Log("[RoundManager] All players dead. Returning to hideout.");
            ReturnToHideout();
        }

        [Server]
        private void CompleteDungeon()
        {
            State = DungeonState.Cleared;
            if (_verbose) Debug.Log("[RoundManager] Dungeon cleared. Returning to hideout.");
            ReturnToHideout();
        }

        [Server]
        private void ReturnToHideout()
        {
            if (string.IsNullOrEmpty(HideoutSceneName))
            {
                Debug.LogError("[RoundManager] HideoutSceneName is empty.");
                return;
            }

            NetworkManager.singleton.ServerChangeScene(HideoutSceneName);
        }

        [Server]
        public void ResetSession()
        {
            CurrentRegionId = "";
            CurrentRound = 0;
            TotalRounds = 0;
            HideoutSceneName = "";
            State = DungeonState.None;
        }

        private void OnRegionChanged(string oldId, string newId)
        {
            if (_verbose) Debug.Log($"[RoundManager] Region {oldId} -> {newId}");
        }

        private void OnRoundChanged(int oldRound, int newRound)
        {
            if (_verbose) Debug.Log($"[RoundManager] Round {oldRound} -> {newRound}");

            if (NetworkServer.active && newRound > 0 && BattleManager.Instance != null)
            {
                BattleManager.Instance.ServerSetupRound(newRound);
            }
        }

        private void OnStateChanged(DungeonState oldState, DungeonState newState)
        {
            if (_verbose) Debug.Log($"[RoundManager] State {oldState} -> {newState}");
        }
    }
}
