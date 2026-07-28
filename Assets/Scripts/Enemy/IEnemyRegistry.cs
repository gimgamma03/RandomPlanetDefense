/// <summary>
/// 필드에 있는 적 목록 조회 전용.
/// 추가/제거는 EnemySpawner만 담당한다.
/// </summary>
public interface IEnemyRegistry
{
    int Count { get; }

    Enemy GetEnemy(int index);

    bool Contains(Enemy enemy);
}
