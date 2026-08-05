using UnityEngine;

[CreateAssetMenu(menuName = "RPD/Enemy Data", fileName = "EnemyData")]
public class EnemyData : ScriptableObject
{
    [Tooltip("밸런스/웨이브 키. 비우면 asset 이름")]
    public string enemyId;

    [Tooltip("스테이지 웨이브 드롭다운과 매칭. Catalog 조회 키")]
    public EnemyType enemyType;

    [Tooltip("UI·로그용. 비우면 Id")]
    public string displayName;

    [Tooltip("전투 역할. 스탯 변형은 SO 복제+색으로")]
    public EnemyRole enemyRole = EnemyRole.Swarm;

    [Tooltip("강도 티어 1~3. 웨이브에서 Type+Tier로 고른다")]
    public EnemyTier enemyTier = EnemyTier.Tier1;

    public int gold = 1;
    public int scorePoint = 10;
    public float maxHp = 10f;
    public float moveSpeed = 2f;
    public float rotateSpeed = 0f;

    [Min(0.1f)]
    [Tooltip("표시·콜라이더 기준 스케일. 분열 잔해는 보통 0.6~0.8")]
    public float visualScale = 1f;

    [Tooltip("Shielded: 본체 HP 전에 깎이는 실드량. 0이면 역할만 표시")]
    public float shieldHp = 0f;

    [Min(0)]
    [Tooltip("Splitter: 사망 시 스폰 수. 0이면 분열 안 함")]
    public int splitCount = 2;

    [Tooltip("Splitter: 분열체 EnemyType (보통 Swarm)")]
    public EnemyType splitChildType = EnemyType.Swarm;

    public Sprite sprite;

    [Tooltip("같은 스프라이트도 색으로 구분 (엘리트 러너 등)")]
    public Color spriteColor = Color.white;

    [Header("Boss")]
    [Tooltip("보스 실루엣 네온 + 왕관 마커")]
    public bool isBoss;

    [Tooltip("보스 주위를 도는 보조 왕관 스프라이트 (노란 네온 등)")]
    public Sprite bossCrownSprite;

    [Tooltip("차징 후 그 자리에서 쫄 소환 (이동 정지 = 딜 타이밍)")]
    public bool enableSummonSkill;

    [Tooltip("소환할 쫄 EnemyType (예: PinkStar)")]
    public EnemyType summonMinionType = EnemyType.PinkStar;

    [Min(0)]
    public int summonCountMin = 3;

    [Min(0)]
    public int summonCountMax = 5;

    [Min(0.5f)]
    [Tooltip("머리 위 차징 게이지가 차는 시간(초)")]
    public float summonChargeDuration = 7f;

    [Min(0.1f)]
    [Tooltip("게이지 풀 후 멈춰 서 있는 시간(초) — 소환 끝난 뒤 추가 홀드")]
    public float summonCastHoldDuration = 0.3f;

    [Min(0.05f)]
    [Tooltip("쫄 한 마리씩 소환 간격(초)")]
    public float summonInterval = 0.5f;

    [Tooltip("차징 게이지 머리 위 오프셋")]
    public float summonChargeGaugeYOffset = 0.9f;

    public string Id => string.IsNullOrEmpty(enemyId) ? name : enemyId;

    public string DisplayName =>
        string.IsNullOrEmpty(displayName) ? Id : displayName;

    public bool HasShield => enemyRole == EnemyRole.Shielded && shieldHp > 0f;

    public bool CanSplit => enemyRole == EnemyRole.Splitter && splitCount > 0;
}
