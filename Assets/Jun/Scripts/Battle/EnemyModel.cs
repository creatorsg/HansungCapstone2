using Jun;
using Mirror;
using System;
using UnityEngine;

public class EnemyModel : MonoBehaviour
{
    public event Action<float> IsDamaged; 
    [SerializeField] PlayerInfo _info; public PlayerInfo Info => _info;
    float _maxHp;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
    public void SetUp(PlayerInfo Info)
    {
        _info = Info;
        _maxHp = Info.Hp;
    }
    // Update is called once per frame
    public void Damaged(float Attack)
    {
        Info.Hp -= Attack;
        IsDamaged?.Invoke(Info.Hp/_maxHp);
    }
}
