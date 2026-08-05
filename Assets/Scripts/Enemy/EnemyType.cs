/// <summary>
/// 적 카탈로그/웨이브 키. 인스펙터 드롭다운용.
/// Unity는 이 이름을 저장하지 않고 정수(0,1,2…)를 저장한다.
/// 순서를 바꾸면 기존 Stage SO 선택이 밀리므로 새 타입은 끝에 추가한다.
/// </summary>
public enum EnemyType
{
    Swarm = 0,       // 구 Enemy01
    Runner = 1,      // 구 Enemy02
    Tank = 2,        // 구 Enemy03
    Shielded = 3,    // 구 Enemy04
    Splitter = 4,    // 구 Enemy05
    RunnerElite = 5, // 레거시. 웨이브에 남아 있으면 Runner T2로 해석
    Boss = 6,        // 스테이지 최종 웨이브 보스
    PinkStar = 7,    // 보스 소환 쫄따구 (러너급)
}
