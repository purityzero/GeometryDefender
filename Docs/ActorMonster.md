# ActorMonster 동작 방식

## 구조 개요

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

---

## 생명주기

### 1. 스폰
`MonsterManager.Spawn(EnemyRecord)`

- `WayPoint.GetRandomWayPoint()`로 외곽 원 위의 랜덤 위치에 스폰
- `WaypointBuffer`에 목적지(`Vector2.zero`) 추가
- `MemoryPoolFactory`에서 `ActorMonster` 풀링으로 꺼내 색상 설정
- `VisualObject`로 Entity ↔ ActorMonster 연결

### 2. 이동 (매 프레임 — MoveSystem)
`SimulationSystemGroup`에서 실행

- `WaypointBuffer[CurrentWaypointIndex]` 방향으로 직선 이동
- 도착(`distance < 0.05f`) 시 `CurrentWaypointIndex++`
- 웨이포인트 소진 시 `ReachedEndTag` 추가

### 3. 데미지 (매 프레임 — HealthSystem)
`MoveSystem` 이후 실행 (`[UpdateAfter(typeof(MoveSystem))]`)

- `DamageRequestBuffer`에 쌓인 데미지를 모두 합산해 `CurrentHp` 감소
- `CurrentHp <= 0` 이면 `DeadTag` 추가

### 4. 위치 동기화 (매 프레임 — VisualSyncSystem)
`PresentationSystemGroup`에서 실행 (SimulationSystemGroup 이후)

- Entity의 `LocalTransform.Position` → `ActorMonster.transform.position` 복사
- ECS 데이터가 실제 GameObject 위치를 결정

### 5. 종료 (MonsterManager.Update)

| 조건 | 처리 |
|---|---|
| `DeadTag` | `OnMonsterDie` 이벤트 발행 → 골드 지급 |
| `ReachedEndTag` | `OnMonsterReachEnd` 이벤트 발행 → 기지 피해 |

두 경우 모두 `ActorMonster`는 풀에 반납, Entity는 Destroy.

---

## 시스템 실행 순서

```
SimulationSystemGroup
├── MoveSystem          이동 처리
└── HealthSystem        데미지 처리 (MoveSystem 이후)

PresentationSystemGroup
└── VisualSyncSystem    GameObject 위치 동기화

MonoBehaviour.Update (MonsterManager)
└── DeadTag / ReachedEndTag 감지 → 이벤트 발행 → Entity 제거
```

---

## 데미지 요청 방식

타워가 같은 프레임에 여러 번 데미지를 줄 수 있으므로 즉시 적용 대신 버퍼에 쌓는다.

```csharp
// 타워 측
monsterManager.TakeDamage(entity, damage);

// 내부 처리
m_EntityManager.GetBuffer<DamageRequest>(entity).Add(new DamageRequest { Amount = damage });

// HealthSystem에서 일괄 처리
for (int i = 0; i < damageRequests.Length; ++i)
    currentHp -= damageRequests[i].Amount;
damageRequests.Clear();
```
