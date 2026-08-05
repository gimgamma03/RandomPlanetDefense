========================================================
Assets/Legacy — 플레이·빌드에 안 쓰는 것 보관
========================================================

구조
  Legacy/Scripts/   ← 안 쓰는 C# (여기)
  Legacy/Prefabs/   ← 나중에 옛 프리팹 (예정)
  Legacy/Images/    ← 나중에 보관용 아트 (예정)

규칙
  - Resources / Addressables / GameScene에 붙은 건 넣지 말 것
  - 옮길 때 .meta 동반 (GUID 유지)
  - 여기 수정해도 인게임 안 바뀌는 게 정상

■ Scripts에 넣지 말 것
  ChainLightning.cs  — Behaviors가 호출하는 프리팹 MB
  Slow.cs            — Behaviors가 호출하는 프리팹 MB

■ 현재 Scripts 보관
  ServiceLocatorUsageExample.cs
  TestTileScript.cs
  CollisionDamage.cs
  CsvTowerBalanceSource.cs