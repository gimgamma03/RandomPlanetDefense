/// <summary>
/// 한 판(런)의 종료 상태. 웨이브 클리어와 게임오버가 서로 덮어쓰지 않게 한다.
/// </summary>
public enum RunPhase
{
    Playing = 0,
    Cleared = 1,
    GameOver = 2,
}
