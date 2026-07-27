/// <summary>
/// 아웃게임 → 인게임으로 넘기는 한 판 선택값.
/// 씬 전환 사이에만 유지 (정적).
/// </summary>
public static class GameSession
{
    public const int DefaultStageId = 1;

    public static int SelectedStageId { get; private set; } = DefaultStageId;

    public static void SelectStage(int stageId)
    {
        SelectedStageId = stageId > 0 ? stageId : DefaultStageId;
    }

    public static void Clear()
    {
        SelectedStageId = DefaultStageId;
    }
}
