/// <summary>
/// 적 전투 역할. EnemyType(아트/카탈로그 키)과 별개.
/// 같은 역할 + 다른 스탯/색으로 변형(엘리트 러너 등) 가능.
/// 새 역할은 끝에 추가.
/// </summary>
public enum EnemyRole
{
    Runner = 0,
    Tank,
    Swarm,
    Shielded,
    Splitter,
}
