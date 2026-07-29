using UnityEngine;

/// <summary>
/// 보스 등장 연출용 데코 스프라이트 묶음.
/// Resources/BossIntroDecoCatalog 로 로드.
/// </summary>
[CreateAssetMenu(menuName = "RPD/Boss Intro Deco Catalog", fileName = "BossIntroDecoCatalog")]
public sealed class BossIntroDecoCatalog : ScriptableObject
{
    public Sprite[] sprites;
}
