/// <summary>
/// 플레이 중 마우스 좌클릭이 의미하는 행동.
/// None일 때만 ObjectDetector가 타워 정보를 연다.
/// </summary>
public enum BuildMode
{
    None = 0,
    SpawnTower,
    Combine,
    Sell,
    PlaceWall,
    RemoveWall,
}
