# MonsterSpawnTestWindow

## 연관 클래스
- MonsterManager — `Spawn(EnemyRecord)`을 반복 호출해 스폰, `GetAliveMonsterCount()`로 현재 스폰 수 조회
- EnemyRecord / EnemyTable — `variantMap`(Normal/Elite/Boss)에서 후보 레코드 목록 조회
- TableManager — `GetTable<EnemyTable>()`
- [[TimeScaleWindow]] — 같은 `Assets/Editor/QA/` 폴더, 같은 컨벤션(Play Mode 전용 EditorWindow, `Tools/QA/...` 메뉴, `Update()`에서 `Repaint()` 호출해 실시간 갱신)을 따름

## 개요
QA/성능 테스트용 Unity 에디터 Tool. `Tools/QA/Monster Spawn Test` 메뉴로 여는 EditorWindow — Play Mode 중 버튼 한 번으로 몬스터 다수(기본 150마리, 1~300 조절 가능)를 한꺼번에 랜덤한 타입 조합으로 스폰한다. 몬스터 수 폭증 시 풀링/이동/충돌 처리 성능이나 시각적 혼잡도를 확인하는 용도.

## 현재 상태
- 경로: Assets/Editor/QA/MonsterSpawnTestWindow.cs
- `[MenuItem("Tools/QA/Monster Spawn Test")]` → `MonsterSpawnTestWindow` 창 오픈.
- `EditorApplication.isPlaying == false`면 안내 메시지만 표시([[TimeScaleWindow]]와 동일 패턴).
- UI: 상단에 "현재 스폰된 몬스터: N마리" 굵은 라벨(2026-07-21-1) + 스폰 수 슬라이더(`MIN_SPAWN_COUNT=1` ~ `MAX_SPAWN_COUNT=300`, 기본 150) + Normal/Elite/Boss 포함 여부 토글(기본 전부 켜짐) + "몬스터 N마리 랜덤 스폰" 버튼.
- `Update()`에서 `Repaint()` 호출([[TimeScaleWindow]]와 동일 패턴) — 창을 열어두면 몬스터 수 라벨이 실시간으로 갱신됨(스폰/이동/도달 소멸 전부 반영).
- `SpawnRandomMix()`: 씬에서 `FindFirstObjectByType<MonsterManager>()`로 대상을 찾고, `TableManager.instance.GetTable<EnemyTable>().variantMap`에서 켜진 Variant(Normal/Elite/Boss)의 레코드를 전부 후보 리스트에 모은 뒤, 스폰 수만큼 후보 중 `Random.Range`로 하나씩 뽑아 `MonsterManager.Spawn(record)` 반복 호출 — 종/변종이 뒤섞인 조합이 나온다.
- MonsterManager/EnemyTable을 못 찾으면(Play Mode인데 아직 `Init()` 전이거나 InGame 단독 플레이로 테이블 미로드인 경우 등) `Logger.Error`만 남기고 조용히 중단(예외 없음).
- 빌드에 포함되지 않음(Assets/Editor 폴더는 에디터 전용, 자동 제외).

## 설계 판단
- 별도의 "스폰 대상 좌표/웨이포인트"를 지정하지 않음 — `MonsterManager.Spawn()`이 내부적으로 `WayPoint.instance.GetRandomWayPoint()`로 알아서 처리하므로 그대로 재사용(중복 로직 없음).
- Variant 토글(Normal/Elite/Boss)만 제공하고 Species(도형)별 개별 토글은 만들지 않음 — "타입을 섞는다"는 요청의 핵심은 다양한 조합이 한꺼번에 나오는 것이고, Boss(HP 최대 2400, 크기 3배)처럼 특정 시나리오에선 빼고 싶을 수 있는 축은 Variant뿐이라 그것만 제어 가능하게 함(요청 이상으로 세분화하지 않음).

## 작업 내역

### 2026-07-21-0

#### 개요
사용자 요청: "몬스터 100~200마리 한꺼번에 소환하고, 타입 막 섞을 수 있게 해주는 테스트 환경".

#### 신규 파일
- Assets/Editor/QA/MonsterSpawnTestWindow.cs (+.meta, Unity MCP `manage_script`로 생성, guid 자동 발급)

#### 검증
- Unity MCP `refresh_unity` — 컴파일 에러 0건.
- InGameScene을 에디터에서 직접 Play(= TitleScene 경유 씬 전환을 타지 않아 client-issues.md 2026-07-21-1의 선행 버그를 피함) 후, `execute_code`로 `TableManager.instance.init()` + `MonsterManager.Init()` 수동 호출로 초기화 완료 상태를 만든 뒤, 리플렉션으로 `SpawnRandomMix()`를 직접 호출해 검증:
  - `m_SpawnCount=150`, Variant 토글 전부 기본값(전부 포함) 상태로 실행 → `MonsterTag` 엔티티 150개가 즉시 생성됨.
  - 스폰된 개체들의 `HealthData.MaxHp` 값이 서로 다른 15종 전부 관측됨(`450,600,15,80,45,750,300,75,20,2400,240,25,10,30,60` — EnemyTable 15행의 MaxHp와 정확히 일치) → Normal/Elite/Boss, 5종족이 실제로 고르게 섞여 스폰되는 것 확인.
  - 콘솔 에러/경고 0건(풀링이 초기 Prewarm(10)을 넘어 150개로 늘어나는 과정도 문제없이 처리됨).
  - 시간이 좀 지난 뒤 재조회하니 `MonsterTag` 수가 0으로 돌아옴 — 몬스터들이 타워(원점)에 도달해 `MonsterManager`가 정상적으로 정리(destroy)한 것으로, 버그가 아니라 스폰~이동~도달~정리 파이프라인이 끝까지 정상 동작함을 보여주는 정황.
- **실제 에디터 UI(슬라이더/토글/버튼 클릭)를 통한 수동 조작 자체는 검증 못함** — 위 검증은 `SpawnRandomMix()` 메서드 자체를 리플렉션으로 직접 호출한 것. `OnGUI()`의 `EditorGUILayout` 위젯 배치/라벨 표시는 미검증(로직 오류 가능성은 낮으나, 실제 창을 열어 버튼을 눌러보는 확인 필요).

---

### 2026-07-21-1

#### 개요
사용자 요청: "테스트 툴에서 Spawn이 몇마리 되어있는지 체크해서 알려줄 수 있도록". 현재 스폰된(살아있는) 몬스터 수를 창에 표시.

#### 파일
- Assets/Editor/QA/MonsterSpawnTestWindow.cs
- Assets/Scripts/InGame/MonsterManager.cs (신규 `GetAliveMonsterCount()`, [[MonsterManager]] 2026-07-21-3 참고)

#### 수정 (함수 단위)
**OnGUI()**
- 전: Play Mode 가드 다음 바로 스폰 수 슬라이더로 시작.
- 후: 가드 다음, `FindFirstObjectByType<MonsterManager>()`로 찾은 매니저의 `GetAliveMonsterCount()` 값을 `EditorGUILayout.LabelField($"현재 스폰된 몬스터: {aliveCount}마리", EditorStyles.boldLabel)`로 표시 후 스폰 수 슬라이더로 이어짐. 매니저를 못 찾으면 0으로 표시(에러 없이).

**신규 `Update()`**
```csharp
private void Update()
{
    Repaint();
}
```
- [[TimeScaleWindow]]와 동일 패턴 — 창이 열려있는 동안 몬스터 수 라벨이 실시간으로 갱신되게 함.

#### 검증
Unity MCP `execute_code`로 실측 — InGameScene 직접 Play 후 `MonsterManager.GetAliveMonsterCount()`가 스폰 전 0, 리플렉션으로 `SpawnRandomMix()`(120마리) 호출 직후 정확히 120을 반환. 컴파일 에러 0건. 실제 창을 띄워 라벨이 화면에 실시간으로 갱신되는 모습 자체(육안 확인)는 미검증.

---

### 2026-07-22-0

#### 개요
사용자 규칙("숫자를 비교할 때 == 는 쓰지 않는다", CODE.MD에 반영) — `SpawnRandomMix()`의 `candidateList.Count == 0`이 숫자 비교라 규칙 위반.

#### 파일
- Assets/Editor/QA/MonsterSpawnTestWindow.cs

#### 수정
- 전: `if (candidateList.Count == 0)`
- 후: `if (candidateList.Count <= 0)`

#### 검증
컴파일 확인(에러 0건).
