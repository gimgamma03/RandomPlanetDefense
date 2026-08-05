using UnityEngine;

/// <summary>
/// 행성 주위 위성 N개가 공전하며 접촉 피해. G1 기본 2개.
/// </summary>
public sealed class OrbitSatelliteBehavior : ITowerBehavior
{
    private const string PivotName = "OrbitPivot";
    /// <summary>G1 range 2.5 기준 ≈ 1칸 궤도</summary>
    private const float OrbitRadiusScale = 0.5f;
    private const float SatelliteScale = 0.34f;
    private const float SatelliteColliderRadius = 0.2f;
    private const float PivotSpinSpeed = 55f;
    private const float SatelliteSelfSpin = 120f;

    private TowerWeapon tower;
    private Transform orbitPivot;
    private OrbitSatelliteBody[] satelliteBodies;

    public void Initialize(TowerWeapon towerWeapon)
    {
        tower = towerWeapon;
    }

    public void Activate()
    {
        BuildOrbit();
    }

    public void Deactivate()
    {
        DestroyOrbit();
    }

    public void OnUpgraded()
    {
        RefreshSatellites();
    }

    private void BuildOrbit()
    {
        DestroyOrbit();

        GameObject pivotObject = new GameObject(PivotName);
        orbitPivot = pivotObject.transform;
        orbitPivot.SetParent(tower.transform, false);
        orbitPivot.localPosition = Vector3.zero;

        OrbitSatellitePivot spin = pivotObject.AddComponent<OrbitSatellitePivot>();
        spin.DegreesPerSecond = PivotSpinSpeed;

        int count = tower.OrbitSatelliteCount;
        float radius = tower.range * OrbitRadiusScale;
        satelliteBodies = new OrbitSatelliteBody[count];

        for (int i = 0; i < count; i++)
        {
            float angle = (Mathf.PI * 2f / count) * i;
            Vector3 localPosition = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            GameObject satellite = CreateSatellite(i, localPosition);
            satelliteBodies[i] = satellite.GetComponent<OrbitSatelliteBody>();
            if (satelliteBodies[i] != null)
            {
                satelliteBodies[i].Configure(tower.damage);
            }
        }
    }

    private GameObject CreateSatellite(int index, Vector3 localPosition)
    {
        TowerVisualLibrary library = TowerVisualLibrary.Load();
        if (library != null && library.satelliteBodyPrefab != null)
        {
            GameObject instance = Object.Instantiate(library.satelliteBodyPrefab, orbitPivot);
            instance.name = $"Satellite_{index + 1}";
            instance.transform.localPosition = localPosition;
            instance.transform.localScale = Vector3.one * SatelliteScale;

            OrbitSatelliteBody body = instance.GetComponent<OrbitSatelliteBody>();
            if (body == null)
            {
                body = instance.AddComponent<OrbitSatelliteBody>();
            }

            RotateObject spin = instance.GetComponent<RotateObject>();
            if (spin == null)
            {
                spin = instance.AddComponent<RotateObject>();
            }

            spin.SetSpeed(SatelliteSelfSpin);
            SatelliteOutlineView.Attach(instance);
            return instance;
        }

        return CreateSatelliteFromCode(index, localPosition, library);
    }

    private GameObject CreateSatelliteFromCode(int index, Vector3 localPosition, TowerVisualLibrary library)
    {
        GameObject satelliteObject = new GameObject($"Satellite_{index + 1}");
        satelliteObject.transform.SetParent(orbitPivot, false);
        satelliteObject.transform.localPosition = localPosition;
        satelliteObject.transform.localScale = Vector3.one * SatelliteScale;

        SpriteRenderer renderer = satelliteObject.AddComponent<SpriteRenderer>();
        if (library != null && library.satelliteSprite != null)
        {
            renderer.sprite = library.satelliteSprite;
        }

        SpriteRenderer hostRenderer = tower.GetComponent<SpriteRenderer>();
        if (hostRenderer != null)
        {
            renderer.sortingLayerID = hostRenderer.sortingLayerID;
            renderer.sortingOrder = hostRenderer.sortingOrder + 2;
        }

        CircleCollider2D collider = satelliteObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = SatelliteColliderRadius;

        OrbitSatelliteBody body = satelliteObject.AddComponent<OrbitSatelliteBody>();
        RotateObject spin = satelliteObject.AddComponent<RotateObject>();
        spin.SetSpeed(SatelliteSelfSpin);
        SatelliteOutlineView.Attach(satelliteObject);

        return satelliteObject;
    }

    private void RefreshSatellites()
    {
        if (satelliteBodies == null)
        {
            BuildOrbit();
            return;
        }

        int desiredCount = tower.OrbitSatelliteCount;
        if (satelliteBodies.Length != desiredCount)
        {
            BuildOrbit();
            return;
        }

        float radius = tower.range * OrbitRadiusScale;
        for (int i = 0; i < satelliteBodies.Length; i++)
        {
            if (satelliteBodies[i] == null)
            {
                continue;
            }

            float angle = (Mathf.PI * 2f / satelliteBodies.Length) * i;
            satelliteBodies[i].transform.localPosition =
                new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            satelliteBodies[i].Configure(tower.damage);
        }
    }

    private void DestroyOrbit()
    {
        if (orbitPivot != null)
        {
            Object.Destroy(orbitPivot.gameObject);
            orbitPivot = null;
        }

        satelliteBodies = null;
    }
}
