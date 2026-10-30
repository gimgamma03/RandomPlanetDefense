# Random Planet Defense

2D 탑다운 랜덤 타워 디펜스. 중앙 행성으로 향하는 적을 벽으로 길을 막고, 벽 위에 타워를 설치해 막는 게임입니다.

Unity 6 (`6000.3.x`) 1인 프로젝트입니다. 같은 프로젝트에서 **WebGL(itch.io 브라우저 플레이)** 과 **Android 가로 APK** 를 빌드합니다.

---

## 플레이 개요

1. **벽 설치** — 맵에 벽을 놓으면 **A\***로 적 이동 경로를 다시 잡습니다. 길을 돌려 적의 이동 시간을 벌 수 있습니다.
2. **타워 소환** — 벽 칸 위에 골드로 타워를 뽑습니다. (랜덤 계열 · 기본 등급)
3. **조합** — 같은 타워 3개를 합치면 상위 등급 타워가 나옵니다.
4. **웨이브** — 준비가 되면 웨이브를 시작하고, 골까지 도달한 적만큼 피해를 받습니다.
5. **메타** — 스테이지 클리어·영구 강화 등은 로컬에 저장됩니다. (스테이지 2·3은 1 클리어 후 해금)

조작은 인게임 안내(`?`)에서도 볼 수 있습니다.

---

## 플랫폼 · 화면

| 빌드 | 용도 | 비고 |
|:--|:--|:--|
| **WebGL → itch.io** | 브라우저에서 바로 플레이 | 임베드 16:9 (예: 1280×720), 템플릿이 iframe 크기에 맞춤 |
| **Android APK** | 모바일 가로 실행 | IL2CPP · ARM64 · 가로 고정 |

- UI 기준 해상도는 **1920×1080 (16:9)** 입니다.
- 요즘 폰처럼 가로가 더 긴 **20:9 (예: 2400×1080)** 에서도 Canvas Scaler(Match Height)와 코너 앵커로 HUD가 모서리에 붙도록 맞췄습니다.
- 노치·제스처 바는 `SafeAreaFitter`로 피하고, 마우스/터치 입력은 `PointerInput`으로 통일했습니다. (터치에서 UI 위 입력이 맵으로 넘어가지 않게 처리)
- 폰에는 우클릭이 없어서, 벽 버튼으로 **설치 → 해체 → 끄기** 모드를 순환하도록 바꿨습니다.

---

## 핵심 기술 (포트폴리오 요약)

아트 완성도보다 **클라이언트 구조·데이터·배포·세션 기록**을 정리한 개인 작품입니다.

### 1. 구조 — Service Locator / 역할 분리

- `GameBootstrapper` + `ServiceLocator`로 점수·플레이어·풀 등 **순수 C# 서비스**와 씬의 MonoBehaviour를 나눕니다.
- 타워 공격은 `TowerWeapon`(호스트) + `ITowerBehavior`(Strategy)로 분리합니다. 계열만 바꿔도 발사 로직을 교체할 수 있습니다.

### 2. 데이터 드리븐 — ScriptableObject / CSV

- `TowerData` · `EnemyData` · `StageData`를 Resources에서 모아 Catalog로 씁니다.
- 밸런스 수치는 SO와 `TowerBalance.csv`로 관리해, **동작 코드와 수치를 분리**합니다.
- 스테이지 웨이브는 Custom Inspector로 한 화면에서 편집합니다.

### 3. Addressables

- 타워 Base 프리팹을 주소로 로드합니다. 게임 시작 시 계열별 Base를 미리 로드해 첫 소환 지연을 줄이고, 씬을 나갈 때 Release합니다.
- 용량이 작은 SO 데이터는 Resources를 유지합니다.

### 4. Profiler 기반 최적화

- Unity Profiler(Player Build Deep)로 병목을 확인한 뒤, TMP 갱신·AoE 피해 분산·타겟 탐색 주기 등 **필요한 구간만** 손봤습니다.
- "60FPS 보장" 같은 절대 수치는 주장하지 않습니다.

### 5. 세션 기록 — ASP.NET Core API → DB → 조회

- 한 판이 끝나면 `PlaySessionStats`(웨이브·타워 스폰/합성/판매·점수·종료 사유 등)를 JSON으로 모읍니다.
- Unity → **로컬 ASP.NET Core API**(`RpdSessionApi`) → DB에 저장하고, Razor 화면에서 **플레이 기록**으로 조회합니다.
- 조회 페이지를 외부에서 접근 가능한 형태로 한 번 확인했습니다. 상시 공개 서버로 운영하지는 않으며, itch.io 빌드와는 별개입니다.
- 전송이 실패해도 플레이에는 영향이 없습니다. (로컬 큐에도 남김)

### 기타

- 적·투사체·HP바·데미지 숫자 **오브젝트 풀링** (`IPoolService`)
- 공유 A* 경로 + 벽 변경 시 재경로
- 카테고리별 스프라이트 아틀라스

---

## 기술 스택

| 항목 | 내용 |
|:--|:--|
| 엔진 | Unity 6000.3.x |
| 언어 | C# |
| 빌드 | WebGL, Android (IL2CPP · ARM64) |
| 관련 | Addressables, ScriptableObject, ASP.NET Core (세션 API, 별도 저장소) |

---

## 저장소

- 게임: https://github.com/gimgamma03/RandomPlanetDefense
- 세션 API: `RpdSessionApi` (ASP.NET Core Web API + Razor 조회 + DB)

---

## 비고

- 이전 로컬/원격 이름: RandomTowerDefense (히스토리 유지)
- 모바일 작업 이전 WebGL 기준 코드는 `archive/webgl-main` 브랜치에 보관했습니다.
- 상세 개발 메모·프로파일러 기록은 포트폴리오용 문서에 정리해 두었습니다.
