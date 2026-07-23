/// <summary>
/// 구글 시트 등 외부 밸런스 소스.
/// 구현체가 TowerData.ApplyBalance를 호출해 로컬 SO 수치를 덮어쓴다.
/// (Unity Gaming Services가 아니라 Sheets → CSV/JSON → Unity 파싱을 말함)
/// </summary>
public interface ITowerBalanceSource
{
    /// <summary>카탈로그의 각 TowerData에 시트 값을 적용. 실패해도 로컬 SO로 플레이 가능해야 함.</summary>
    void ApplyToCatalog(TowerCatalog catalog);
}