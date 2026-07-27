using UnityEngine;

/// <summary>
/// 차징 유성·궤도 위성 등 Behavior 전용 스프라이트/프리팹.
/// Resources/TowerVisualLibrary.asset
/// </summary>
[CreateAssetMenu(menuName = "RPD/Tower Visual Library", fileName = "TowerVisualLibrary")]
public sealed class TowerVisualLibrary : ScriptableObject
{
    [Tooltip("ChargePierce 발사 시 랜덤 선택")]
    public Sprite[] meteoSprites;

    [Tooltip("OrbitSatellite 위성 스프라이트 (프리팹 없을 때 폴백)")]
    public Sprite satelliteSprite;

    [Tooltip("있으면 Instantiate, 없으면 코드 생성")]
    public GameObject satelliteBodyPrefab;

    private static TowerVisualLibrary cached;

    public static TowerVisualLibrary Load()
    {
        if (cached == null)
        {
            cached = Resources.Load<TowerVisualLibrary>("TowerVisualLibrary");
        }

        return cached;
    }

    public Sprite PickRandomMeteo()
    {
        if (meteoSprites == null || meteoSprites.Length == 0)
        {
            return null;
        }

        return meteoSprites[Random.Range(0, meteoSprites.Length)];
    }
}
