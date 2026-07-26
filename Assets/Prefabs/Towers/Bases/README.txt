Tower Base Prefabs (구조 A)

TowerBase.prefab     — 원형 참고용 (지금은 Cannon과 동일 복사본)
CannonBase           — Cannon
LaserBase            — Laser / MultiLaser
SlowBase             — Slow
ChainLightningBase   — ChainLightning
BombBase             — Bomb
MultiShotBase        — MultiWayShooting
MultiBombBase        — GroundBombLine (프리팹 이름은 주소 등록 때문에 유지)

소환 시 TowerBaseLibrary 가 weaponType → 위 베이스로 해석.
등급·이름·스프라이트·스탯은 TowerData(+CSV)가 BindDefinition 으로 덮음.

나중에 Prefab Variant로 정리:
  1) TowerBase를 공통만 남기고 정리
  2) 각 *Base를 TowerBase의 Variant로 재생성 (Unity Prefab > Save as Variant)
  메뉴: RPD/Towers/Open Bases Folder