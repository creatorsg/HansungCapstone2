using Jun;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;


// 아마 이부분은 서버쪽에서 계산되게끔 하는게 좋을수도?
//내 생각으로는 클라쪽에서 BattleAction으로 필요한 정보를 서버쪽으로 보내면 서버쪽에서 계산하고 클라에게 결과 값 보내주는 게 좋을 것 같음 
public class BattleLogic : MonoBehaviour
{
    public static BattleLogic Instance;

    private void Awake() => Instance = this;

    public void BattleAction(SkillInfo skill, ItemInfo item, UnitModel attacker, List<int> targets)
    {
        // 배틀 로직 구현(플레이어, 적 둘 다 사용가능하게끔)
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
