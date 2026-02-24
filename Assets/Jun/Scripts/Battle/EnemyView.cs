using UnityEngine;
using UnityEngine.UI;
public class EnemyView : MonoBehaviour
{
    [SerializeField] private Slider HpBar;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    public void Damaged(float _currentHp)
    {
        Debug.Log("EnmeyDamaged");
        HpBar.value = _currentHp;
    }
}
