# MonsterManager

## 연관 클래스
- ActorMonster
- MemoryPoolFactory
- WayPoint
- EnemyRecord
- (ECS 컴포넌트) HealthData, MoveData, RewardData, MonsterTag, DeadTag, ReachedEndTag, WaypointElement, DamageRequest, VisualObject

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

몬스터 프리팹 경로는 Init()에 하드코딩되어 있습니다:
- Cube: `Prefabs/Monster/Cube`
- Sphere: `Prefabs/Monster/Sphere`
- Capsule: `Prefabs/Monster/Capsule`

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

1. **스폰** — `Spawn(EnemyRecord)`: `WayPoint.GetRandomWayPoint()`로 외곽 원 위 랜덤 위치에 스폰, WaypointBuffer에 목적지(`Vector2.zero`) 추가, MemoryPoolFactory에서 ActorMonster를 꺼내 색상 설정, VisualObject로 Entity ↔ ActorMonster 연결.
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

MonoBehaviour.Update (MonsterManager)
└── DeadTag / ReachedEndTag 감지 → 이벤트 발행 → Entity 제거
```

데미지는 같은 프레임에 여러 타워가 줄 수 있으므로 즉시 적용하지 않고 DamageRequest 버퍼에 쌓아 HealthSystem에서 일괄 처리한다.

---

## 작업 내역

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
