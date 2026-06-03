using Jun;
using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyModel : NetworkBehaviour
{
    public event Action<float> IsDamaged;
    [SerializeField] EnemyController _controller;
    [SerializeField] PlayerInfo _info; public PlayerInfo Info => _info;
    private float _currentHp;
    private float _maxHp;

    private int _currentSan;
    private int _maxSan;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
    public void SetUp(PlayerInfo Info)
    {
        _info = Info;
        if (_info != null)
        {
            if (_info.MaxHp <= 0f) _info.MaxHp = Info.MaxHp;
            if (_info.MaxSan <= 0f) _info.MaxSan = Info.MaxSan;
            if (_info.Statuses == null) _info.Statuses = new List<ActiveStatus>();
        }
        _currentHp = Info.Hp;
        _maxHp = Info.MaxHp > 0f ? Info.MaxHp : Info.Hp;
        _currentSan = Info.San;
        _maxSan = Info.MaxSan > 0f ? Info.MaxSan : Info.San;
    }
    [Server]
    public void Damaged(float Attack)
    {
        Debug.Log("EnemyDamaged");
        _currentHp -= Attack;
        _info.Hp = _currentHp;
        _info.Hp = _currentHp;
        if (_info.Hp <= 0) _controller.CMDDead();
        RpcSyncEnemyHp(_currentHp);
    }
    [Server]
    public void Heal(float amount)
    {
        _currentHp = Mathf.Min(_currentHp + amount, _maxHp);
        _info.Hp = _currentHp;
        RpcSyncEnemyHp(_currentHp);
        Debug.Log($"[EnemyModel] Heal +{amount:F0} -> HP={_currentHp:F0}/{_maxHp:F0}");
    }
    [ClientRpc]
    private void RpcSyncEnemyHp(float currentHp)
    {
        _currentHp = currentHp;
        _info.Hp = currentHp;
        IsDamaged?.Invoke(_currentHp / _maxHp);
    }
}
