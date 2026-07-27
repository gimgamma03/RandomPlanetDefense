using UnityEngine;

/// <summary>
/// Resources/TowerBalance.csv (또는 인스펙터 TextAsset)를 카탈로그에 적용.
/// TowerSpawner에 안 꽂아도 Resources에 CSV만 있으면 자동 적용된다.
/// </summary>
public class CsvTowerBalanceSource : MonoBehaviour, ITowerBalanceSource
{
    [SerializeField]
    private TextAsset csvOverride;

    [SerializeField]
    private string resourcesName = "TowerBalance";

    public void ApplyToCatalog(TowerCatalog catalog)
    {
        TextAsset asset = csvOverride;
        if (asset == null)
        {
            asset = Resources.Load<TextAsset>(resourcesName);
        }

        if (asset == null)
        {
            Debug.LogWarning($"[CsvTowerBalanceSource] CSV not found: Resources/{resourcesName}");
            return;
        }

        TowerBalanceCsv.Apply(catalog, asset.text);
    }
}