using Jun;
using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jun
{
    public class EnemyModel : NetworkBehaviour
    {
        public event Action<float> IsDamaged;
        [SerializeField] EnemyController _controller;
        [SerializeField] PlayerInfo _info; public PlayerInfo Info => _info;

        void Start()
        {
            // 인스펙터 직주입 케이스를 위한 보강 초기화
            if (_info != null)
            {
                if (_info.MaxHp <= 0f) _info.MaxHp = _info.Hp;
                if (_info.Statuses == null) _info.Statuses = new List<ActiveStatus>();
            }
        }

        public void SetUp(PlayerInfo info)
        {
            _info = info;
            if (_info == null) return;
            if (_info.MaxHp <= 0f) _info.MaxHp = _info.Hp;
            if (_info.Statuses == null) _info.Statuses = new List<ActiveStatus>();
        }

        // 서버에서 호출. HP 감소(0 미만 차단) + 시각 이벤트 + 클라 동기화 + 사망 처리
        public void Damaged(float Attack)
        {
            Debug.Log("EnemyDamaged " + Attack);
            _info.Hp = Mathf.Max(0f, _info.Hp - Attack);
            float ratio = (_info.MaxHp > 0f) ? (_info.Hp / _info.MaxHp) : 0f;
            IsDamaged?.Invoke(ratio);

            if (NetworkServer.active && BattleManager.Instance != null)
            {
                int idx = FindEnemyIndex();
                if (idx >= 0) BattleManager.Instance.RpcSyncEnemyHp(idx, _info.Hp, _info.MaxHp);
            }

            if (_info.Hp <= 0f)
            {
                if (NetworkServer.active && BattleManager.Instance != null)
                    BattleManager.Instance.OnEnemyDead(gameObject);
                else
                    _controller.CMDDead();
            }
        }

        private int FindEnemyIndex()
        {
            var manager = BattleManager.Instance;
            if (manager == null) return -1;
            if (manager.Enemys == null || manager.StageNum - 1 < 0 || manager.StageNum - 1 >= manager.Enemys.Count) return -1;

            var list = manager.Enemys[manager.StageNum - 1].Enemys;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].GetComponent<EnemyModel>() == this) return i;
            }
            return -1;
        }
    }
}
