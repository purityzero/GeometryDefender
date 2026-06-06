# MonsterManager 사용 가이드

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

---

## 2. 몬스터 생성 (Spawn)

```csharp
Entity entity = m_MonsterManager.Spawn(enemyRecord, waypointList);
```

### 매개변수

| 매개변수 | 타입 | 설명 |
|----------|------|------|
| `_record` | `EnemyRecord` | 몬스터 스탯 데이터 (HP, 속도, 모양, 색상, 보상 등) |
| `_waypoints` | `List<WaypointRecord>` | 몬스터가 이동할 경로 (순서대로 전달) |

### EnemyRecord 주요 필드

| 필드 | 타입 | 설명 |
|------|------|------|
| `Shape` | `eEnemyShape` | 외형 (Cube / Sphere / Capsule) |
| `ColorHex` | `string` | 색상 (#FF0000 형식) |
| `MaxHp` | `int` | 최대 HP |
| `MoveSpeed` | `float` | 이동 속도 |
| `GoldReward` | `int` | 처치 시 지급 골드 |
| `DamageToBase` | `int` | 목적지 도달 시 기지에 주는 데미지 |

### WaypointRecord 주요 필드

| 필드 | 타입 | 설명 |
|------|------|------|
| `X`, `Y`, `Z` | `float` | 경로 포인트 위치 |
| `PathId` | `int` | 경로 식별자 |
| `Order` | `int` | 경로 순서 |

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

---

## 4. 보상 받기 (이벤트 구독)

MonsterManager는 두 가지 이벤트를 제공합니다.

```csharp
m_MonsterManager.OnMonsterDie     // 몬스터 처치 시
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

이벤트는 ECS 쪽에서 `DeadTag` / `ReachedEndTag`가 붙은 Entity를 매 프레임 감지하여 발동됩니다.  
이벤트 발동 직후 Entity와 비주얼 오브젝트는 자동으로 정리됩니다.

---

## 5. 전체 흐름 예시

```csharp
void Start()
{
    m_MonsterManager.Init();
    m_MonsterManager.OnMonsterDie += OnMonsterKilled;
    m_MonsterManager.OnMonsterReachEnd += OnMonsterReachedBase;
}

void SpawnEnemy(EnemyRecord enemyRecord, List<WaypointRecord> waypoints)
{
    Entity entity = m_MonsterManager.Spawn(enemyRecord, waypoints);
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
