/// <summary>
/// Home → GamePlay 씬 간 선택된 퀘스트를 넘기기 위한 정적 홀더.
/// 멀티플레이로 동기화가 필요해지면 SyncVar/Message 기반으로 교체.
/// </summary>
public static class SelectedQuest
{
    public static QuestData Current;
}
