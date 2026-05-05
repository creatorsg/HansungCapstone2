//using PlayFab.EventsModels;
using UnityEngine;

public class Membership: MonoBehaviour
{
    //회원가입으로 전환하는 버튼용 코드
    [SerializeField] private GameObject _membershipPanel;
    public void OpenMembership()
    {
        gameObject.SetActive(false);
        _membershipPanel.SetActive(true);
    }
    public void CloseMembership()
    {
        _membershipPanel.SetActive(false);
        gameObject.SetActive(true);
    }
}