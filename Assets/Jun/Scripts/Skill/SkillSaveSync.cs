using UnityEngine;
using Mirror;

namespace Jun
{
    /// <summary>
    /// 호스트(서버)가 스킬트리 세이브 데이터를 소유하고, SyncList로 클라이언트에 동기화.
    /// 씬에 1개 배치 (NetworkIdentity 필수).
    /// </summary>
    public class SkillSaveSync : NetworkBehaviour
    {
        public static SkillSaveSync Instance;

        public readonly SyncList<SkillTreeSaveData> saves = new SyncList<SkillTreeSaveData>();

        private void Awake()
        {
            // 씬 전환(로비→전투) 간 세이브 유지
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        public SkillTreeSaveData Find(string characterName)
        {
            foreach (var s in saves)
                if (s != null && s.characterName == characterName) return s;
            return null;
        }

        [Server]
        private SkillTreeSaveData GetOrCreate(string characterName)
        {
            var found = Find(characterName);
            if (found != null) return found;

            var created = new SkillTreeSaveData { characterName = characterName };
            saves.Add(created);
            return created;
        }

        [Command(requiresAuthority = false)]
        public void CmdUpgradeTier2(string characterName, int skillIndex, int choice)
        {
            if (skillIndex < 0 || skillIndex >= 4) return;
            if (choice != 0 && choice != 1) return;

            var save = GetOrCreate(characterName);

            // TODO: 골드 보유량 확인 + 차감 (아지트 골드 시스템 연동)

            save.tier2Choice[skillIndex] = choice;
            save.tier3Choice[skillIndex] = -1; // Tier2 변경 시 Tier3 초기화
            ForceResync(save);
        }

        [Command(requiresAuthority = false)]
        public void CmdUpgradeTier3(string characterName, int skillIndex, int choice)
        {
            if (skillIndex < 0 || skillIndex >= 4) return;
            if (choice != 0 && choice != 1) return;

            var save = GetOrCreate(characterName);
            if (save.tier2Choice[skillIndex] == -1) return; // Tier2 선행 필수

            // TODO: 골드 보유량 확인 + 차감

            save.tier3Choice[skillIndex] = choice;
            ForceResync(save);
        }

        // SyncList<class>는 내부 필드 변경을 자동 감지하지 않으므로
        // 동일 인덱스에 재할당하여 Mirror가 직렬화를 다시 수행하도록 강제
        [Server]
        private void ForceResync(SkillTreeSaveData save)
        {
            int idx = saves.IndexOf(save);
            if (idx >= 0) saves[idx] = save;
        }
    }
}
