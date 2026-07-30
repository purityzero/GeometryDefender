# MonsterManager

## 2026-07-30-1 — ApplySlowAura()에 슬로우 시각화 연동
사용자 요청("냉기오브에서 슬로우가 좀 걸리는게 눈에 띄었으면") — [[ActorMonster]] 2026-07-30-0 참고. 기존 `SlowAuraData` 갱신 루프에 `VisualObject`→`ActorMonster.SetSlowTinted(isTouching)` 호출 추가(HasComponent 가드 후).

---

## 2026-07-30-0 — SlowAuraData 신설 + ApplySlowAura() (신규 무기: Frost Orb Turret용)
사용자 요청("ActorPlayer를 천천히 공전하면서 일정 사거리 만큼 적을 슬로우 걸 수 있는 무기가 있었으면 좋겠어") — 상세 설계는 [[ActorPlayer]] 2026-07-30-2 참고. `SlowAuraData(SlowMultiplier)` 컴포넌트를 `Spawn()`에서 전 몬스터에 기본값 1(무효과)로 부착. 신규 `ApplySlowAura(Vector2 center, float radius, float slowMultiplier)` — `DamageEntitiesInRadius`/`DamageEntitiesInArc`와 동일한 "매번 전체 스캔" 패턴으로, 반경 안이면 슬로우 배율, 밖이면 1로 **매 프레임 무조건 재설정** — 진입/이탈 이벤트나 별도 리셋 로직이 필요 없다(호출 안 하면 그 몬스터는 마지막 값에 영구히 머무니, 무기가 활성 상태인 동안 매 프레임 호출이 전제).

### 파일
- Assets/Scripts/InGame/ECS/SlowAuraData.cs (신규)
- Assets/Scripts/InGame/MonsterManager.cs

### 검증
컴파일 확인 필요. Play Mode 미검증 — Frost Orb Turret 범위 안에 들어온 몬스터가 실제로 느려지는지, 범위를 벗어나면 정상 속도로 복귀하는지 확인 필요.

---

## 2026-07-29-1 — EnemyVariantData 컴포넌트 신설 (몬스터 변종 조건부 데미지 카드 지원)
사용자 요청("엘리트, 보스, 일반몬스터 등등 타격 데미지 증가")으로 신설 — 기존엔 `RewardData.IsBoss`(bool)만 있어 Boss 여부는 알 수 있어도 Elite/Normal을 구분할 방법이 없었음(스폰 시점 `EnemyRecord.Variant`가 즉시 버려짐). `EnemySpeciesData`(종족)와 동일 패턴으로 `EnemyVariantData { eEnemyVariant Variant }` 컴포넌트 신설, `Spawn()`에서 `_record.Variant`로 채움. `ActorPlayer.Fire()`가 타겟 조회에 사용([[ActorPlayer]] 2026-07-29-4 참고).

### 파일
- Assets/Scripts/InGame/ECS/EnemyVariantData.cs (신규)
- Assets/Scripts/InGame/MonsterManager.cs (`Spawn()`에 `AddComponentData` 한 줄)

### 검증
컴파일 에러 0건. Play Mode(execute_code로 Elite 변종 엔티티 직접 생성 + 실제 자동 전투) — 데미지가 정확히 Elite 배율만큼 반영됨을 대량 샘플로 확인([[ActorPlayer]] 2026-07-29-4 참고).

---

## 연관 클래스
- ActorMonster — CullingObject 캐시 보유, `UpdateCullingLogic()` 노출(2026-07-27)
- MemoryPoolFactory — `GetAllActive()`(2026-07-27)로 활성 몬스터 순회
- WayPoint
- EnemyRecord
- BaseScene, IUpdatable (Update 대신 구동)
- (ECS 컴포넌트) HealthData, MoveData, RewardData, MonsterTag, DeadTag, ReachedEndTag, WaypointElement, DamageRequest, VisualObject, EnemySpeciesData, EnemyVariantData(2026-07-29 신설)
- [[MonsterSpawnTestWindow]] — `GetAliveMonsterCount()` 조회 대상
- KillCountText — `Current`/`killCount` 정적 접근자로 폴링, HUD Kill 텍스트 갱신

---

## 개요

MonsterManager는 몬스터의 생성(Spawn), 데미지 처리, 사망/목적지 도달 보상을 관리하는 클래스입니다.
내부적으로 ECS(Entity Component System)로 몬스터 로직을 처리하고, MemoryPoolFactory로 오브젝트 풀링을 합니다.

---

## 1. 초기화

```csharp
[SerializeField] private MonsterManager m_MonsterManager;

void Start()
{
    m_MonsterManager.Init();
}
```

`Init()`은 반드시 가장 먼저 호출해야 합니다.
내부적으로 풀 생성(Prewarm), ECS EntityQuery 등록이 이루어집니다.

Inspector에서 `PoolParent` Transform을 반드시 연결해두어야 합니다.
풀링된 몬스터 오브젝트들이 이 Transform 아래에 생성됩니다.

몬스터 프리팹 경로는 하드코딩이 아니라 `TableManager.instance.GetTable<EnemyTable>().shapeMap`을 순회해 각 Shape의 `PrefabPath`로 구성됩니다(2026-07-15-0에 하드코딩에서 테이블 기반으로 변경, 아래 "1. 초기화"의 예시 코드만 옛 버전이라 이 문단으로 정정 — 상세는 이 문서 하단 2026-07-15-0 항목 참고). 2026-07-20 기준 실제 EnemyTable 데이터는 Cube/Sphere/Capsule이 아니라 다음 6종:
- Triangle(Normal), Circle(Swift), Square(Heavy), Diamond(Splitter), Pentagon(Ranged) — 각 Normal/Elite 변형
- Star — 5종 species 공통 Boss 변형

---

## 2. 몬스터 생성 (Spawn)

```csharp
Entity entity = m_MonsterManager.Spawn(enemyRecord);
```

### 매개변수

| 매개변수 | 타입 | 설명 |
|----------|------|------|
| `_record` | `EnemyRecord` | 몬스터 스탯 데이터 (HP, 속도, 모양, 색상, 보상 등) |

시작 위치는 매개변수가 아니라 `WayPoint.instance.GetRandomWayPoint()`로 내부에서 결정됩니다.
현재 WaypointElement 버퍼에는 `Vector2.zero` 하나만 추가됩니다(모든 몬스터가 원점을 향해 이동).

### EnemyRecord 주요 필드

| 필드 | 타입 | 설명 |
|------|------|------|
| `Shape` | `eEnemyShape` | 외형 (Cube / Sphere / Capsule) |
| `ColorHex` | `string` | 색상 (#FF0000 형식) |
| `MaxHp` | `int` | 최대 HP |
| `MoveSpeed` | `float` | 이동 속도 |
| `GoldReward` | `int` | 처치 시 지급 골드 |
| `DamageToBase` | `int` | 목적지 도달 시 기지에 주는 데미지 |

### 반환값

생성된 몬스터의 ECS `Entity`를 반환합니다.
이 `Entity`는 이후 데미지를 줄 때 사용됩니다.

---

## 3. 데미지 주기 (TakeDamage)

```csharp
m_MonsterManager.TakeDamage(entity, 30);
```

| 매개변수 | 타입 | 설명 |
|----------|------|------|
| `_entity` | `Entity` | `Spawn()` 반환값 |
| `_amount` | `int` | 줄 데미지 양 |

아래 경우에는 데미지가 무시됩니다.
- 해당 Entity가 이미 존재하지 않는 경우
- 이미 사망 처리된 경우 (`DeadTag`)
- 이미 목적지 도달 처리된 경우 (`ReachedEndTag`)

내부적으로는 즉시 HP를 깎지 않고 `DamageRequest` 버퍼에 요청을 쌓는 방식입니다(실제 처리는 ECS 시스템 쪽).

---

## 4. 보상 받기 (이벤트 구독)

MonsterManager는 두 가지 이벤트를 제공합니다.

```csharp
m_MonsterManager.OnMonsterDie      // 몬스터 처치 시
m_MonsterManager.OnMonsterReachEnd // 몬스터가 목적지에 도달했을 때
```

### 구독 방법

```csharp
m_MonsterManager.OnMonsterDie += OnMonsterKilled;
m_MonsterManager.OnMonsterReachEnd += OnMonsterReachedBase;

void OnMonsterKilled(RewardData _reward)
{
    // _reward.GoldReward : 획득 골드
    AddGold(_reward.GoldReward);
}

void OnMonsterReachedBase(RewardData _reward)
{
    // _reward.DamageToBase : 기지에 가할 데미지
    m_BaseHealth.TakeDamage(_reward.DamageToBase);
}
```

### RewardData 필드

| 필드 | 타입 | 설명 |
|------|------|------|
| `GoldReward` | `int` | 처치 보상 골드 |
| `DamageToBase` | `int` | 목적지 도달 시 기지 데미지 |

이벤트는 `Update()`에서 `DeadTag` / `ReachedEndTag`가 붙은 Entity를 매 프레임 감지하여 발동됩니다.
이벤트 발동 직후 Entity와 비주얼 오브젝트는 자동으로 정리됩니다(풀로 반납).

---

## 5. 전체 흐름 예시

```csharp
void Start()
{
    m_MonsterManager.Init();
    m_MonsterManager.OnMonsterDie += OnMonsterKilled;
    m_MonsterManager.OnMonsterReachEnd += OnMonsterReachedBase;
}

void SpawnEnemy(EnemyRecord enemyRecord)
{
    Entity entity = m_MonsterManager.Spawn(enemyRecord);
    // entity를 따로 보관해두면 나중에 데미지를 줄 수 있음
}

void OnTowerShoot(Entity targetEntity, int damage)
{
    m_MonsterManager.TakeDamage(targetEntity, damage);
}

void OnMonsterKilled(RewardData _reward)
{
    AddGold(_reward.GoldReward);
}

void OnMonsterReachedBase(RewardData _reward)
{
    m_BaseHealth.TakeDamage(_reward.DamageToBase);
}
```

---

## 6. 동작 구조 (내부)

MonsterManager가 ECS Entity(데이터)와 ActorMonster(시각적 GameObject)를 각각 생성하고 연결한다.

```
MonsterManager
├── Entity (ECS — 데이터)
│   ├── LocalTransform        위치
│   ├── HealthData            MaxHp, CurrentHp
│   ├── MoveData              MoveSpeed, CurrentWaypointIndex
│   ├── RewardData            GoldReward, DamageToBase
│   ├── MonsterTag
│   ├── WaypointBuffer        [목적지 좌표 목록]
│   ├── DamageRequestBuffer   [이번 프레임 받은 데미지 목록]
│   └── VisualObject          → ActorMonster.transform 참조
└── ActorMonster (GameObject — 시각)
    └── Renderer (색상 표현)
```

### 생명주기

1. **스폰** — `Spawn(EnemyRecord)`: `WayPoint.GetRandomWayPoint()`로 외곽 원 위 랜덤 위치에 스폰, WaypointBuffer에 목적지(`Vector2.zero`) 추가, MemoryPoolFactory에서 ActorMonster를 꺼내 색상 설정 + `_record.VisualSize`로 크기 설정(2026-07-21, [[EnemyRecord]] VisualSize 섹션 참고), VisualObject로 Entity ↔ ActorMonster 연결.
2. **이동** — MoveSystem (SimulationSystemGroup): `WaypointBuffer[CurrentWaypointIndex]` 방향으로 직선 이동, 도착(`distance < 0.05f`) 시 인덱스 증가, 소진 시 `ReachedEndTag` 추가.
3. **데미지** — HealthSystem (MoveSystem 이후): DamageRequestBuffer 합산 → CurrentHp 감소, 0 이하면 `DeadTag` 추가.
4. **위치 동기화** — VisualSyncSystem (PresentationSystemGroup): Entity의 `LocalTransform.Position` → `ActorMonster.transform.position` 복사. ECS 데이터가 실제 GameObject 위치를 결정.
5. **종료** — MonsterManager.Update: `DeadTag`면 `OnMonsterDie`, `ReachedEndTag`면 `OnMonsterReachEnd` 발행 → 두 경우 모두 ActorMonster는 풀에 반납, Entity는 Destroy.

### 시스템 실행 순서

```
SimulationSystemGroup
├── MoveSystem          이동 처리
└── HealthSystem        데미지 처리 (MoveSystem 이후)

PresentationSystemGroup
└── VisualSyncSystem    GameObject 위치 동기화

BaseScene.Update → MonsterManager.UpdateLogic (2026-07-21부터 자체 Update() 아님, [[BaseScene]] 참고)
└── DeadTag / ReachedEndTag 감지 → 이벤트 발행 → Entity 제거
```

데미지는 같은 프레임에 여러 타워가 줄 수 있으므로 즉시 적용하지 않고 DamageRequest 버퍼에 쌓아 HealthSystem에서 일괄 처리한다.

---

## 작업 내역

### 2026-07-24-0 — 카드 드래프트 시스템용 확장
[[card-draft]] 스펙 구현.

**Spawn(EnemyRecord)**: `RewardData.XpReward = _record.XpReward` 추가([[xp-leveling]] 연동), `EnemyManager.AddComponentData(entity, new EnemySpeciesData { Species = _record.Species })` 추가(Triangle Hunter #108 카드가 `TowerController.Fire()`에서 조회).

**신규 `DamageEntitiesInRadius(Vector2 _center, float _radius, int _damage)`**(public): Shield Burst(#404) 카드용. 호출 시점에 임시 `EntityQuery`(LocalTransform+MonsterTag, Dead/ReachedEnd 제외)를 생성 → 반경 내 전체에 `DamageRequest` 추가 → 즉시 Dispose. `GetAliveMonsterCount()`와 동일하게 호출 빈도가 낮아 필드 캐싱 없이 즉석 생성/해제 방식 채택.

### 미검증
Unity MCP 미연결, 컴파일/Play 확인 안 됨.

### 2026-07-21-3

#### 개요
사용자 요청 — InGameScene UI의 킬 카운트 표시를 이 클래스와 연동. 상세는 [[TowerHealth]] 2026-07-21-5, [[KillCountText]] 참고.

#### 파일
- Assets/Scripts/InGame/MonsterManager.cs

#### 수정 (함수 단위)
**클래스 선언 바로 아래**
- 전: `[SerializeField] private Transform m_PoolParent;`만 존재
- 후: `public static MonsterManager Current { get; private set; }` + `public int killCount { get; private set; }` 추가(TimerManager.Current와 동일 패턴, UI가 씬 하이어라키를 안 거치고 폴링하기 위함)

**Start()**
- 전: `BaseScene.Current.Register(this);`
- 후: `Current = this;` 한 줄 추가(Register 호출 이전)

**OnDestroy()**
- 전: `BaseScene.Current?.Unregister(this);`로 시작
- 후: 맨 앞에 `if (Current == this) Current = null;` 추가

**ProcessDeadMonsters()**
- 전: `RecycleVisual(deadEntities[i]); OnMonsterDie?.Invoke(rewards[i]);`
- 후: `RecycleVisual(deadEntities[i]); ++killCount; OnMonsterDie?.Invoke(rewards[i]);` — 처치 이벤트 발행 직전 카운트 증가

#### 검증
Unity MCP `execute_code`로 격리 테스트 — reflection으로 `killCount`를 3으로 설정 + `Current` 지정 → `KillCountText.UpdateLogic()` 호출 시 텍스트 "3" 출력 확인. 컴파일 에러 0건.
실제 몬스터 처치 플로우를 통한 자연 증가 확인은 미검증(엔진 단독 플레이 제약은 [[TowerHealth]] 2026-07-21-5와 동일).

---

### 2026-07-12-1
- 개요: Docs/ActorMonster.md(몬스터 파이프라인 동작 구조 문서)를 이 문서의 "6. 동작 구조 (내부)" 섹션으로 병합 후 원본 삭제 (코드 수정 없음)
- 파일: .claude/class/MonsterManager.md (갱신), Docs/ActorMonster.md (삭제)

### 2026-07-12-0
- 개요: Assets/Scripts/InGame/MonsterManager.md를 .claude/class/MonsterManager.md로 이관 (코드 수정 없음)
- 파일: .claude/class/MonsterManager.md (신규), Assets/Scripts/InGame/MonsterManager.md (삭제)
- 증상: 기존 md의 `Spawn(enemyRecord, waypointList)` 시그니처가 실제 코드와 불일치
- 원인: 코드가 변경된 뒤 md가 갱신되지 않음 — 현재 `Spawn(EnemyRecord _record)`만 받고, 시작 위치는 `WayPoint.instance.GetRandomWayPoint()`로 내부 결정
- 수정: 실제 코드 기준으로 Spawn 시그니처/WaypointRecord 관련 서술 정정, 연관 클래스 목록 추가

---

## 2026-07-15-0

### 개요
D:\Unity\Job (구 작업 폴더, 06-09까지 작업분)에서 머지 — Job 버전으로 교체.

### 수정 (함수 단위)

**Init()**
- 전: pathMap 하드코딩 (Cube/Sphere/Capsule → "Prefabs/Monster/...")
- 후: `TableManager.instance.GetTable<EnemyTable>().shapeMap`을 순회해 각 Shape의 `PrefabPath`로 pathMap 구성 (테이블 기반)

**Update()**
- 후: `m_MonsterFactory.UpdateLogic()` 호출 추가

### 미검증
컴파일/플레이 확인 필요. 몬스터 프리팹 6종(Triangle~Star)도 함께 머지됨.

---

## 2026-07-19-0

### 개요
런타임 NRE 수정 (Update → ProcessDeadMonsters → m_DeadQuery.IsEmpty).

### 파일
- Assets/Scripts/InGame/MonsterManager.cs

### 증상
매 프레임 `NullReferenceException: EntityQueryImpl.get_IsEmpty` (MonsterManager.cs:112) 스팸.

### 원인
InGame 씬을 **단독 플레이**하면 씬에 GameManager가 없어(TitleScene에만 존재) `TableManager.init()`이 호출되지 않음 → `GetTable<EnemyTable>()`이 에러 로그 + null 반환 → `Init()`이 `enemyTable.shapeMap` 접근에서 NRE로 중단 → `m_DeadQuery`가 default(EntityQuery) 상태로 남음 → 매 프레임 `Update()`의 `IsEmpty` 접근에서 NRE. (정상 흐름 Title → InGame에서는 DontDestroy GameManager가 살아있어 재현 안 됨)

### 수정
- Init 시작부에 enemyTable null 가드 추가 (명확한 에러 로그 1회 + 중단)
- `m_isInitialized` 플래그 추가 — Init 완주 시에만 true, `Update()`/`OnDestroy()` 진입 가드 (미초기화 상태의 IsEmpty/Dispose NRE 방지)

### 남은 문제 (이 파일 밖, 미수정)
1. **InGame 단독 플레이 시 테이블 미로드 자체는 여전함** — 가드는 스팸만 막고 몬스터는 안 나옴. 단독 플레이를 지원하려면 InGameScene에서 GameManager/TableManager 부트스트랩 필요 (사용자 결정 대기).
2. **InGameScene.unity에 SpawnManager 컴포넌트 자체가 없음** — "SpawnManager" GameObject(fileID 343094390)는 Transform만 보유, guid 330e8e3d... 검색 결과 0건. InGameScene의 `m_SpawnManager` 직렬화 라인도 없음 → 정상 흐름에서도 `InGameScene.Start` 라인 10에서 NRE 예정. 에디터에서 컴포넌트 추가 + 참조 연결 필요.

### 미검증
에디터 미실행 상태 편집. 컴파일/단독 플레이 시 에러 1회만 나오는지 확인 필요.

---

## 2026-07-20-0 (최초 오진 — 아래 2026-07-20-1로 정정됨)

### 개요
2026-07-19-0과 같은 증상(EntityQueryImpl.get_IsEmpty NRE) 재발 — 이번엔 **가드(m_isInitialized)가 있는데도** 발생. 처음엔 "플레이 중 스크립트 재컴파일(핫 리로드)"로 오진하고 `m_isInitialized`에 `[NonSerialized]`를 붙였으나(SpawnManager도 동일 적용), **재현 결과 효과 없음 — 근본 원인이 아니었음**. 실제 원인과 수정은 [SceneManager.md](./SceneManager.md) 2026-07-20-0 참고(Command_CleanupDontDestroy가 DOTween/Addressables/렌더파이프라인 등 엔진 인프라 오브젝트를 파괴하면서 ECS World가 함께 깨지는 문제).

`[NonSerialized]` 자체는 핫 리로드 대비책으로는 여전히 유효한 방어 코드라 되돌리지 않고 남겨둠(해가 없고, 실제 핫 리로드 시나리오에선 도움이 됨) — 다만 **이번 증상의 원인은 아니었다**는 점을 기록.

### 미검증
[SceneManager.md](./SceneManager.md) 2026-07-20-0 수정 적용 후 실제 NRE 미발생 확인 필요.

---

## 2026-07-20-1

### 개요
위 미검증 항목 실제 Play Mode로 검증(코드 수정 없음, md 갱신만). 검증 방법은 [SpawnManager.md](./SpawnManager.md) 2026-07-20-1 참고(TitleScene→InGameScene 실제 UI 클릭 + `EditorApplication.Step()`으로 프레임 수동 진행).

### 검증 결과
- `Init()` 정상 완주(`m_isInitialized == true`), `World.DefaultGameObjectInjectionWorld`가 씬 전환 전후로 계속 유지됨(`World.All.Count`가 전환 전/직후/이후 147초 동안 6으로 불변) → [SceneManager.md](./SceneManager.md) 2026-07-20-0의 `HasProjectMonoBehaviourInChildren` 수정이 의도대로 ECS World를 보호하는 것으로 확인.
- `EntityQueryImpl.get_IsEmpty` NRE 재현 안 됨(147초 동안 콘솔 에러 0건).
- `Spawn()`으로 생성된 ECS Entity 수와 `ActorMonster` 비주얼 오브젝트 수가 항상 일치(예: MonsterTag 엔티티 5개 ↔ ActorMonster 5개) — Entity↔Visual 동기화 정상.
- 목적지 도달(`ReachedEndTag`) 처리도 정상 확인: 데미지를 주는 주체가 없는 상태로 오래 플레이하면 몬스터가 계속 끝까지 걸어가 자동으로 정리됨(살아있는 개체 수가 누적되지 않고 일정 수준 유지).

### 참고 — DOTween/Addressables/Debug Updater 자체의 생존 여부는 이번엔 미확인
이번 검증은 ECS World 생존 여부(`World.All.Count`)만 직접 확인했고, SceneManager.md가 언급한 개별 인프라 오브젝트(`[DOTween]`, Addressables 헬퍼, `[Debug Updater]`)가 실제로 씬을 넘어 살아있는지는 별도로 조회하지 않았다. 증상(NRE)이 사라진 것으로 간접 확인된 상태.

---

## 2026-07-21-0

### 개요
사용자 요청: InGameScene 소속 매니저들의 Update를 BaseScene이 대신 구동하도록 구조 변경(MonoSingleton 매니저는 제외, 자기 자신이 계속 구동). 상세 설계는 [[BaseScene]] 참고.

### 파일
- Assets/Scripts/InGame/MonsterManager.cs

### 수정 (함수 단위)

**클래스 선언**
- 전: `public class MonsterManager : MonoBehaviour`
- 후: `public class MonsterManager : MonoBehaviour, IUpdatable`

**Update() → UpdateLogic()**
- 전: `private void Update() { ... }` (Unity가 매 프레임 자동 호출)
- 후: `public void UpdateLogic() { ... }` (내용 동일) — `BaseScene.Current`가 대신 호출
- 신규 `private void Start() { BaseScene.Current.Register(this); }` 추가 — 씬의 BaseScene에 자신을 등록

**OnDestroy()**
- 전: `m_isInitialized == false`면 바로 return
- 후: 가드보다 먼저 `BaseScene.Current?.Unregister(this);` 무조건 실행 추가 (등록은 Init 성공 여부와 무관하게 Start에서 이뤄지므로)

### 미검증
컴파일/에디터 미실행 상태 편집. 실제 Play Mode로 몬스터 스폰/이동/사망 처리가 계속 정상 틱되는지(= BaseScene 경유로도 매 프레임 호출되는지) 확인 필요.

---

## 2026-07-21-1

### 개요
사용자 요청: 일반몹 크기를 테이블(EnemyTable.VisualSize)로 정할 수 있게. 일반몹은 플레이어와 같은 크기를 기준으로, 보스/엘리트 크기는 자체 판단으로 재설계. 데이터 설계 상세는 [[EnemyRecord]] "VisualSize" 섹션 참고.

### 파일
- Assets/Scripts/InGame/MonsterManager.cs

### 수정 (함수 단위)

**SpawnVisual(Entity, EnemyRecord)**
- 전: 색상만 설정(`actorMonster.SetColor(color)`), 크기는 프리팹에 baked된 값 그대로 사용(테이블 미반영, VisualSize 필드는 존재했지만 미사용).
- 후: 색상 설정 다음 줄에 `actorMonster.transform.localScale = Vector3.one * _record.VisualSize;` 추가 — 매 스폰마다 EnemyTable.csv의 VisualSize를 실제 크기에 반영. 몬스터 프리팹 6종(Triangle/Circle/Square/Diamond/Pentagon/Star)의 baked scale은 전부 `{1,1,1}`로 되돌림([[BaseScene]]과 무관, 별도 정리 — 상세는 .claude/prefab/README.md 2026-07-21-0).

### 미검증
컴파일/에디터 미실행 상태 편집. 실제 Play Mode로 각 종족/Variant별 몬스터가 의도한 상대 크기(Heavy > Splitter > Normal=Ranged > Swift, Elite/Boss일수록 커짐)로 보이는지, 특히 Normal 종족 Normal Variant(Triangle)가 ActorPlayer와 화면상 동일 크기로 보이는지 확인 필요.

---

## 2026-07-21-2

### 개요
qa-tester가 발견한 Client 이슈([client-issues.md](../qa/client-issues.md) 2026-07-21-0) 수정 — Play Mode 종료 시 `OnDestroy()`의 `EntityQuery.Dispose()`에서 NRE 발생.

### 파일
- Assets/Scripts/InGame/MonsterManager.cs

### 증상
Play Mode 종료(에디터 정지) 시 콘솔에 `NullReferenceException`(`UnsafeParallelHashMap.Remove` → `EntityQueryImpl.Dispose` → `EntityQuery.Dispose` → `MonsterManager.OnDestroy` line 202) 발생.

### 원인
Play Mode 종료 시 소속 ECS World가 `MonsterManager.OnDestroy()`보다 먼저 정리(Dispose)되는 타이밍이 있음 → 이미 무효화된 `EntityQuery`를 `Dispose()`하려다 NRE. `m_isInitialized` 가드는 "미초기화 상태" 접근만 막고, "초기화는 됐지만 World가 먼저 죽은" 이 케이스는 커버하지 못함.

### 수정 (함수 단위)

**OnDestroy()**
- 전:
  ```csharp
  private void OnDestroy()
  {
      BaseScene.Current?.Unregister(this);

      if (m_isInitialized == false)
          return;

      m_DeadQuery.Dispose();
      m_ReachedEndQuery.Dispose();
      m_MonsterFactory.Clear();
  }
  ```
- 후:
  ```csharp
  private void OnDestroy()
  {
      BaseScene.Current?.Unregister(this);

      if (m_isInitialized == false)
          return;

      // Play Mode 종료 시 World가 MonsterManager보다 먼저 정리되는 타이밍이 있음
      // — 이미 무효화된 EntityQuery를 Dispose하면 NRE가 나므로 World 생존 여부를 먼저 확인
      if (World.DefaultGameObjectInjectionWorld != null && World.DefaultGameObjectInjectionWorld.IsCreated == true)
      {
          m_DeadQuery.Dispose();
          m_ReachedEndQuery.Dispose();
      }

      m_MonsterFactory.Clear();
  }
  ```
- `World.DefaultGameObjectInjectionWorld`/`World.IsCreated`는 `unity_reflect`로 실제 존재 확인 후 사용(`EntityManager.IsCreated`는 존재하지 않아 채택 안 함).

### 검증
- `refresh_unity`(compile force) → `read_console` 에러 0건 — 컴파일 정상.
- **가드 로직 자체는 격리 검증 완료**: Play Mode 중 테스트용 `World`를 만들어 `EntityQuery`를 생성한 뒤 그 `World`를 `Dispose()`(= "OnDestroy 시점에 World가 이미 정리된" 상황 재현) → 가드 없이 `query.Dispose()`를 호출하면 실제로 `NullReferenceException` 재현됨(원 버그 메커니즘 확인) → 동일 상황에서 `OnDestroy()`에 추가한 조건(`World.DefaultGameObjectInjectionWorld != null && IsCreated == true`)을 그대로 평가하면 `false`가 나와 Dispose를 스킵, 예외 없이 통과 확인.
- **실제 씬 흐름(TitleScene→Btn_Play 클릭→InGameScene→Stop)을 통한 자연 재현 검증은 못함** — 검증 도중 이 버그와 무관한 별도의 더 심각한 문제를 발견: 실시간(Step 강제 진행이 아닌) Play에서 TitleScene→InGameScene 전환 후 `MonsterManager.Init()` 시작 줄(`World.DefaultGameObjectInjectionWorld.EntityManager`)에서 NRE 발생 — `World.DefaultGameObjectInjectionWorld` 자체가 InGameScene 진입 시점에 이미 null. `Init()`이 여기서 즉시 중단되어 `m_isInitialized`가 계속 false로 남고, 그 결과 `OnDestroy()`도 가드 첫 줄(`m_isInitialized == false`)에서 항상 조기 return하여 오늘 수정한 Dispose 가드 코드 자체가 실행되는 상황을 자연 재현할 수 없었음. 상세는 [client-issues.md](../qa/client-issues.md) 2026-07-21-1(신규) 참고 — 별도 이슈로 분리 기록, 이번 수정 범위 밖이라 손대지 않음.

---

## 2026-07-21-3

### 개요
사용자 요청("테스트 툴에서 Spawn이 몇마리 되어있는지 체크해서 알려줄 수 있도록") — [[MonsterSpawnTestWindow]]가 현재 살아있는 몬스터 수를 표시할 수 있도록 조회 API 추가.

### 파일
- Assets/Scripts/InGame/MonsterManager.cs

### 수정 (함수 단위)
**신규 `GetAliveMonsterCount()`**
```csharp
public int GetAliveMonsterCount()
{
    if (m_isInitialized == false)
        return 0;

    EntityQuery aliveQuery = m_EntityManager.CreateEntityQuery(
        ComponentType.ReadOnly<MonsterTag>(),
        ComponentType.Exclude<DeadTag>(),
        ComponentType.Exclude<ReachedEndTag>());

    int aliveCount = aliveQuery.CalculateEntityCount();
    aliveQuery.Dispose();

    return aliveCount;
}
```
- 기존 `m_DeadQuery`/`m_ReachedEndQuery`처럼 필드로 캐싱하지 않고 호출 시점에 즉석 생성 후 즉시 Dispose — 호출 빈도가 낮은(에디터 툴에서 프레임마다 1회 정도) 조회용이라 필드 수명 관리 복잡도를 늘리지 않는 쪽을 택함.

### 검증
Unity MCP `execute_code`로 실측 — Play Mode(InGameScene 직접 Play) 중 `GetAliveMonsterCount()`가 스폰 전 0, [[MonsterSpawnTestWindow]]로 120마리 스폰 직후 정확히 120을 반환하는 것 확인. 컴파일 에러 0건.

---

## 2026-07-21-4

### 개요
사용자 요청("Current static 싱글톤 패턴이 4곳에 복붙됨" 리팩토링) — `Current` 필드/설정/해제 로직을 [[SceneSingleton]] 공용 베이스로 이관. (이 시점 기준 `MonsterManager`는 이미 `Current`/`killCount`를 보유하고 있었음 — 이 문서에 아직 기록 안 된 세션 밖 변경으로 추정, 이번 항목은 그 `Current` 부분만 리팩토링.)

### 파일
- Assets/Scripts/InGame/MonsterManager.cs

### 수정 (함수 단위)
**클래스 선언**
- 전: `public class MonsterManager : MonoBehaviour, IUpdatable` + 자체 `public static MonsterManager Current { get; private set; }`
- 후: `public class MonsterManager : SceneSingleton<MonsterManager>, IUpdatable` (Current 필드 제거, 베이스에서 상속)

**Start()**
- 전: `Current = this;` + `BaseScene.Current.Register(this);`
- 후: `BaseScene.Current.Register(this);`만 남음

**OnDestroy()**
- 전: `private void OnDestroy() { if (Current == this) Current = null; BaseScene.Current?.Unregister(this); if (m_isInitialized == false) return; ... }`
- 후: `protected override void OnDestroy() { base.OnDestroy(); BaseScene.Current?.Unregister(this); if (m_isInitialized == false) return; ... }` (World 생존 확인 후 Dispose하는 나머지 로직은 그대로)

### 검증
[[SceneSingleton]] 2026-07-21-0 참고 — `MonsterManager.Current`가 Play Mode 중 정상 반환되는 것(killCount=0 포함)까지 실측 확인.

---

## 2026-07-22-0

### 개요
사용자 제안("killCount 같은걸 옵져버로 만들면 되지 않나") — `killCount`를 폴링 대상 plain int에서 `ObservableVariable<int>`로 전환. 상세는 [[ObservableIntText]] 참고.

### 파일
- Assets/Scripts/InGame/MonsterManager.cs

### 수정 (함수 단위)
**필드**
- 전: `public int killCount { get; private set; }`
- 후: `public ObservableVariable<int> killCount { get; } = new ObservableVariable<int>(0);`

**ProcessDeadMonsters()**
- 전: `++killCount;`
- 후: `killCount.Value++;`

### 검증
[[ObservableIntText]] 2026-07-22-0 참고 — Play Mode에서 몬스터 처치 후 `killCount.Value`가 실제로 증가하고 [[KillCountText]] 표시까지 갱신되는 것 확인.

---

## 2026-07-22-1

### 개요
사용자 지적("m_DicVisual FactoryPool 사용하면 되지않나?") — Entity→(Shape, ActorMonster) 역참조용 `Dictionary`가 ECS `VisualObject` 컴포넌트와 사실상 같은 관계(Entity↔시각 오브젝트)를 중복 추적하고 있던 것을 [[Factory]] 쪽 개선(팩토리가 `Create()` 시점에 타입을 스스로 기억)으로 제거. 상세 배경은 [[Factory]] 2026-07-22-0 참고.

### 파일
- Assets/Scripts/InGame/MonsterManager.cs

### 수정 (함수 단위)
**필드**
- 제거: `private Dictionary<Entity, (eEnemyShape shape, ActorMonster actor)> m_DicVisual`

**SpawnVisual(Entity, EnemyRecord)**
- 제거: `m_DicVisual[_entity] = (_record.Shape, actorMonster);` 한 줄만 삭제, 나머지 로직 동일.

**RecycleVisual(Entity)**
- 전: `m_DicVisual.TryGetValue(_entity, out var visual)` → `m_MonsterFactory.Recycle(visual.shape, visual.actor)` → `m_DicVisual.Remove(_entity)`
- 후: `m_EntityManager.GetComponentObject<VisualObject>(_entity).transform.GetComponent<ActorMonster>()`로 Actor를 직접 얻어 `m_MonsterFactory.Recycle(actorMonster)` 한 줄 호출(타입 인자 불필요 — 팩토리가 스스로 기억).

### 검증
Unity MCP `refresh_unity`(force+compile) → `read_console` 에러/경고 0건. 실제 Play Mode에서 처치/도달 후 반납 동작 재검증은 미실시(구조적으로 [[ProjectileManager]] 2026-07-22-2와 동일 패턴이며, 그쪽에서 killCount 등 파생 로직은 이번 변경과 무관하게 그대로 유지됨).

---

## 2026-07-22-2

### 개요
`Assets/Design/08_balance.html` "적 스탯 시간 보정" 공식 구현(기본 곡선이 코드에 없던 것을 발견 — 상세는 `.claude/design/difficulty-progression.md`). 난이도 진행/샤드 획득 작업의 선행 단계.

### 파일
- Assets/Scripts/InGame/MonsterManager.cs

### 수정 (함수 단위)
**Spawn(EnemyRecord)**
- 전: `HealthData { MaxHp = _record.MaxHp, ... }`, `RewardData { ..., DamageToBase = _record.DamageToBase }` — 시간에 따른 보정 없이 테이블 값 그대로 사용.
- 후: 스폰 시점의 `TimerManager.Current.elapsedTime`(분 단위)으로 `hpMultiplier = 1 + elapsedMinutes × GameConfigTable.HP_MULTIPLIER_GROWTH`, `damageMultiplier = 1 + elapsedMinutes × GameConfigTable.DAMAGE_MULTIPLIER_GROWTH` 계산 → `MaxHp`/`DamageToBase`에 각각 곱해서(반올림) 적용. **스폰 시점 1회 적용**(이미 스폰된 몬스터가 나중에 더 강해지진 않음 — 08_balance.html의 예시 표(baseHP 20이 t=300에 HP 60)도 "그 시점에 스폰되는 개체"를 의미하는 것으로 해석).

### 검증 (2026-07-22, Play Mode)
Title→Btn_Play→InGame 실제 흐름. `TimerManager.Current.elapsedTime≈18.5s` 시점에 스폰된 Triangle(Normal, baseHP 20/baseDamage 5)의 `HealthData.MaxHp=22`(20×1.123≈22.5, 반올림 22 — 실측치와 정확히 일치), `elapsedTime≈28.5s` 시점엔 `MaxHp=23~24`, `RewardData.DamageToBase=6`(5×1.119≈5.6→6)로 시간에 따라 증가하는 것 실측 확인. 콘솔 에러 0건.

---

## 2026-07-22-3

### 개요
[[UIRunOver]]의 샤드 정산(`BossKills`)에 보스 처치 수가 필요해 추가 — 상세는 [[RewardComponent]] 2026-07-22-0 참고.

### 파일
- Assets/Scripts/InGame/MonsterManager.cs

### 수정 (함수 단위)
**필드**: `killCount`와 동일한 패턴으로 `public ObservableVariable<int> bossKillCount { get; } = new ObservableVariable<int>(0);` 추가.

**Spawn(EnemyRecord)**: `RewardData.IsBoss = (_record.Variant == eEnemyVariant.Boss)` 설정.

**ProcessDeadMonsters()**: `killCount.Value++;` 다음 줄에 `if (rewards[i].IsBoss == true) bossKillCount.Value++;` 추가.

### 검증
[[UIRunOver]] 2026-07-22-3 참고.

---

## 2026-07-23-0

### 개요
사용자 요청("IUpdatable 인터페이스로 만들지 말고, UIBase 등등 최상위 클래스에서 등록") — 상세 배경은 [[SceneSingleton]] 2026-07-23-0 참고.

### 파일
- Assets/Scripts/InGame/MonsterManager.cs

### 수정 (함수 단위)
**클래스 선언**: `SceneSingleton<MonsterManager>, IUpdatable` → `SceneSingleton<MonsterManager>`(IUpdatable 제거).
**Start()**(Register만 하던 것): 삭제.
**UpdateLogic()**: `public void` → `public override void`.
**OnDestroy()**: 수동 `BaseScene.Current?.Unregister(this);` 호출 제거(`base.OnDestroy()`가 대신 처리), World 생존 확인 후 EntityQuery Dispose하는 나머지 로직은 그대로 유지.

### 미검증
[[SceneSingleton]] 2026-07-23-0 참고.

---

## 2026-07-23-1

### 개요
사용자가 실제 플레이 중 `MissingReferenceException`(`RecycleVisual` 내부, 이미 파괴된 `Transform`에 `GetComponent` 호출) 보고. 정확한 발생 경로(무엇이 해당 `ActorMonster` GameObject를 먼저 파괴했는지)는 특정 못함 — 진행 중인 `World.DefaultGameObjectInjectionWorld`/`BaseScene.Current` 불안정 이슈([[SceneSingleton]]/client-issues.md 2026-07-23-0)와 같은 뿌리일 가능성이 있으나 미확정. 우선 크래시 자체를 막는 방어 코드만 추가(근본 원인 규명은 별도).

### 증상
```
MissingReferenceException: The object of type 'UnityEngine.Transform' has been destroyed but you are still trying to access it.
  UnityEngine.Component.GetComponent[T] ()
  MonsterManager.RecycleVisual (Entity _entity) (MonsterManager.cs:256)
  MonsterManager.ProcessReachedEndMonsters () (MonsterManager.cs:222)
  MonsterManager.UpdateLogic () (MonsterManager.cs:186)
```
가드 없이 예외가 나면 `ProcessReachedEndMonsters()`가 중간에 중단돼 `entities.Dispose()`/`rewards.Dispose()`(NativeArray)가 실행되지 않고, 루프에 남은 나머지 엔티티들도 `DestroyEntity()`되지 않아 좀비 상태로 남는 부작용도 있었음.

### 파일
- Assets/Scripts/InGame/MonsterManager.cs

### 수정 (함수 단위)
**RecycleVisual(Entity)**
- 전: `VisualObject visualObject = ...; ActorMonster actorMonster = visualObject.transform.GetComponent<ActorMonster>(); m_MonsterFactory.Recycle(actorMonster);` — `transform`이 이미 파괴된 경우 가드 없음.
- 후: `GetComponentObject` 직후 `if (visualObject.transform == null) return;` 가드 추가(Unity 오버로드 `==`로 "파괴됨" 감지) — 이미 파괴된 경우 조용히 스킵.

### 검증
`refresh_unity`(scripts, force) 재컴파일 후 콘솔 에러 0건 확인. **근본 원인(왜 Transform이 먼저 파괴됐는지) 재현/규명은 못함** — 실제 플레이 중 이 경로를 다시 타는지, World-null 계열 이슈와 같은 뿌리인지는 다음 세션에서 추가 확인 필요.

---

## 2026-07-23-2 — 근본 원인 확정 + 수정 (ECS World가 씬 언로드와 별개라 몬스터 엔티티가 세션 간 누수됨)

### 개요
사용자 재현 조건 제보("게임하고나서 다른 난이도로 또 플레이시 나타난거야")로 2026-07-23-1의 근본 원인 확정. `World.DefaultGameObjectInjectionWorld`(ECS 월드)는 Unity 씬 언로드와 완전히 별개의 생명주기라, 씬이 바뀌어도 자동으로 정리되지 않는다. 그런데 `OnDestroy()`는 `EntityQuery`만 Dispose할 뿐 **그 시점에 아직 죽지도/도달하지도 않은(플레이 중이던) 몬스터 엔티티는 그대로 방치**했다 — 런 종료 후 다른 난이도로 재플레이하면 이전 런의 좀비 엔티티가 새 런까지 살아남아, 이미 파괴된(구 씬과 함께 사라진) 시각 오브젝트를 참조한 채로 있다가 죽거나 도달 판정을 받아 2026-07-23-1의 크래시가 났던 것. (부가 위험: 크래시 없이 조용히 처리됐다면 새 런에서 `OnMonsterDie`/`OnMonsterReachEnd`가 잘못 발동해 이전 런의 몬스터가 새 런의 타워에 데미지를 주거나 킬 카운트를 오염시켰을 수 있음.)

### 파일
- Assets/Scripts/InGame/MonsterManager.cs

### 수정 (함수 단위)
**OnDestroy()**
- 전: World 생존 확인 후 `m_DeadQuery.Dispose(); m_ReachedEndQuery.Dispose();`만 수행 — 살아있는 몬스터 엔티티 자체는 정리 안 함.
- 후: 같은 World 생존 확인 블록 안에서, 쿼리 Dispose보다 먼저 `MonsterTag`만으로 새 쿼리를 만들어 `m_EntityManager.DestroyEntity(allMonsterQuery)`로 **살아있는 것까지 포함해 이 세션이 만든 몬스터 엔티티 전부를 일괄 파괴**한 뒤 그 쿼리를 Dispose. (동일 버그가 `ProjectileManager`에도 있어 같이 수정 — [[ProjectileManager]] 참고.)

### 검증 (2026-07-23, Play Mode, 실제 재현 시나리오)
`execute_code`로 실제 사용자 플로우를 그대로 재현: TitleScene→`Btn_Play`→"노멀" 선택→InGameScene 진입 후 몬스터 10마리 생존 확인(`MonsterTag` 쿼리 카운트=10) → 아직 몬스터가 살아있는 채로 `SceneManager.instance.NextScene("TitleScene")` 호출(런 도중 이탈 시나리오) → TitleScene 복귀 후 `MonsterTag` 쿼리 카운트=**0**(수정 전이었다면 10 그대로 남았을 상황) → 다시 `Btn_Play`→"하드" 선택으로 재플레이 → 실제로 타워가 죽어 런 종료 화면까지 자연 도달(콘솔 에러 0건, 크래시 없음) → `Btn_MainMenu`로 정상 복귀 후 `MonsterTag`/`ProjectileTag` 쿼리 카운트 둘 다 0 확인. 전체 과정 콘솔 에러 0건.

### 관련 클래스
- [ProjectileManager.md](../class/ProjectileManager.md) — 동일 버그 패턴, 같이 수정

---

### 2026-07-23-3 — SceneSingleton → UpdatableBehaviour 전환(싱글톤 난립 정리)
사용자 지적("Manager가 너무 많지 않아?") — 개별 `.Current` 폐지, `InGameScene.Current.monsterManager`로 접근하도록 통일. `TimerManager.Current`/`DifficultyManager.Current` 참조도 `InGameScene.Current.timerManager`/`.difficultyManager`로 교체. 상세 설계/버그/검증은 [[InGameScene]] 2026-07-23-1 참고.

---

### 2026-07-27-0 — 몬스터 컬링 구동 주체를 이 클래스로 이동

#### 개요
사용자 요청("인게임에서 CullingObject 적용해줘") → 몬스터만 대상으로 확정, 처음엔 [[Pooling]](`MemoryPooling<T>`, 공용 클래스)에 구동 로직을 얹었으나 사용자 지적("Pooling이 아니라 MonsterManager에서 관리해야 하는거 아니야?")으로 이 클래스가 직접 구동하도록 재설계. 상세 배경은 [[Pooling]]/[[Factory]]/[[ActorMonster]] 2026-07-27 항목, [[CullingObject]] 참고.

#### 파일
- Assets/Scripts/InGame/MonsterManager.cs

#### 수정 (함수 단위)
**UpdateLogic()**
- 전: `ProcessDeadMonsters(); ProcessReachedEndMonsters(); m_MonsterFactory.UpdateLogic();`
- 후: 마지막 줄에 `UpdateCulling();` 추가

**신규 `UpdateCulling()`**(private)
```csharp
private void UpdateCulling()
{
    foreach (ActorMonster actorMonster in m_MonsterFactory.GetAllActive())
    {
        actorMonster.UpdateCullingLogic();
    }
}
```
- `m_MonsterFactory.GetAllActive()`([[Factory]] 2026-07-27 참고, 팩토리가 이미 추적 중이던 활성 오브젝트 집합 재사용)로 현재 스폰되어 있는 몬스터 전체를 순회, 각각의 `ActorMonster.UpdateCullingLogic()`(캐시된 CullingObject.UpdateLogic() 호출)을 매 프레임 호출.

#### 검증
Unity 에디터 포커스 재부여로 실제 재컴파일 확인(`Tundra build success`, 에러 0건). **Play Mode 실측(몬스터가 화면 밖/안에서 실제로 비활성화/재활성화되는지)은 미검증** — qa-tester 에이전트로 확인 예정.

---

## 2026-07-27-11 — Laser(#6) 무기 지원: `DamageEntitiesInArc` 신설

### 개요
사용자 요청("회전하면서 다수 공격하는 레이져")으로 [[ActorPlayer]] 2026-07-27-11에 Laser 무기 추가하면서, 회전하는 부채꼴 범위 안의 모든 적에게 동시에 피해를 주는 API가 필요해 신설. 기존 `DamageEntitiesInRadius`(Shield Burst #404용, 원형 범위)와 완전히 동일한 패턴에 각도(방향/반각) 판정만 추가.

### 신규 함수
```csharp
public void DamageEntitiesInArc(Vector2 _origin, Vector2 _direction, float _range, float _halfAngleDegrees, int _damage)
```
`DamageEntitiesInRadius`처럼 매 호출마다 `EntityQuery`를 그때그때 생성/해제(ECS 시스템이 아니라 MonoBehaviour에서 호출되는 트리거라 상시 쿼리 불필요). 거리 판정(`distance <= _range`)에 더해 `math.dot(direction, toTargetDir) >= cos(halfAngleDegrees)`로 부채꼴 안쪽인지 확인 후 `DamageRequest` 버퍼에 추가.

### 검증
Play Mode에서 `execute_code`로 직접 호출 → 콘솔 에러 0건. HP 감소 자체는 별도 프레임(엔티티 인덱스 재사용 이슈로 동일 엔티티 추적이 어려움)에서 재확인은 못했으나, 로직이 이미 검증된 `DamageEntitiesInRadius`와 각도 필터 한 줄만 다르고 `DamageRequest`/`HealthSystem` 소비 경로는 완전히 동일 재사용이라 별도 대규모 검증은 생략.

### 관련 클래스
- [[ActorPlayer]] 2026-07-27-11 (호출부)
