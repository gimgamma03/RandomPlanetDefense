# Random Planet Defense

2D 탑다운 랜덤 타워 디펜스. 중앙 (목표)행성으로 향하는 적을 벽을 설치하고, 그 위에 타워를 설치하여 막는 게임입니다.

Unity 6 (`6000.3.x`) 1인 프로젝트이며, 플레이 가능 빌드(WebGL / itch.io)는 포트폴리오 제출용으로 준비 중입니다.

---

## 플레이 개요

1. **벽 설치** — 맵에 벽을 놓으면 **A**로 적 이동 경로를 다시 잡습니다. 길을 돌려 적의 이동 시간을 벌 수 있습니다.
2. **타워 소환** — 벽 칸 위에 골드로 타워를 뽑습니다. (랜덤 계열 · 기본 등급)
3. **조합** — 같은 타워 3개를 합치면 상위 등급 타워가 나옵니다.
4. **웨이브** — 준비가 되면 웨이브를 시작하고, 골까지 도달한 적만큼 피해를 받습니다.
5. **메타** — 스테이지 클리어·영구 강화 등은 로컬에 저장됩니다. (스테이지 2·3은 1 클리어 후 해금)

조작은 인게임 안내(`?`)에서도 볼 수 있습니다.

---

## 핵심 기술 (포트폴리오 요약)

아트 완성도보다 **클라이언트 구조·데이터·배포·세션 기록**을 정리한 개인 준메인 작품입니다.

### 1. 구조 — Service Locator / 역할 분리

- `GameBootstrapper` + `ServiceLocator`로 점수·플레이어·풀 등 **순수 C# 서비스**와 씬의 MonoBehaviour를 나눕니다.
- 타워 공격은 `TowerWeapon`(호스트) + `ITowerBehavior`(Strategy)로 분리합니다. 계열만 바꿔도 발사 로직을 교체할 수 있습니다.

### 2. 데이터 드리븐 — ScriptableObject / CSV

- `TowerData` · `EnemyData` · `StageData`를 Resources에서 모아 Catalog로 씁니다.
- 밸런스 수치는 SO와 `TowerBalance.csv`로 관리해, **동작 코드와 수치를 분리**합니다.

### 3. Addressables

- 타워 Base 프리팹을 주소로 로드하는 파일럿을 적용했습니다. (시작 시 프리로드로 첫 소환 히치 완화)
- 작은 SO 데이터는 Resources를 유지합니다.

### 4. Profiler 기반 최적화

- Unity Profiler(Player Build Deep)로 병목을 확인한 뒤, TMP dirty·AoE 분산·탐색 주기 등 **필요한 구간만** 손봤습니다.
- "60FPS 보장" 같은 절대 수치는 주장하지 않습니다.

### 5. 세션 통계 → API → DB

- 한 판이 끝나면 `PlaySessionStats`(웨이브·타워 스폰/합성/판매·점수·종료 사유 등)를 수집합니다.
- Unity 클라 → 로컬 ASP.NET API(`RpdSessionApi`) → DB 저장까지 연결해 두었습니다.
(포트폴리오·학습용 로컬 파이프라인. 공개 클라우드 운영 서버는 이번 범위에 넣지 않음)

### 기타

- 적·투사체·HP바 등 **오브젝트 풀링** (`IPoolService`)
- 공유 A* 경로 + 벽 변경 시 재경로
- WebGL 빌드 → itch.io 바로 플레이 (제출용)

---

## 기술 스택


| 항목  | 내용                                                       |
| --- | -------------------------------------------------------- |
| 엔진  | Unity 6000.3.x                                           |
| 언어  | C#                                                       |
| 관련  | Addressables, ScriptableObject, ASP.NET (세션 API, 별도 저장소) |


---

## 저장소

- 게임: [https://github.com/gimgamma03/RandomPlanetDefense](https://github.com/gimgamma03/RandomPlanetDefense)
- 세션 API: `RpdSessionApi` (로컬 Web API + DB)

---

## 비고

- 이전 로컬/원격 이름: RandomTowerDefense (히스토리 유지)
- 상세 개발 메모·프로파일러 기록은 포트폴리오용 문서에 정리해 두었습니다.

