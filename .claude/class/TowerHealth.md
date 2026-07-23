# TowerHealth

> **2026-07-23부로 [[TowerController]]에 병합되어 이 클래스는 삭제됨.** 같은 오브젝트(ActorPlayer)를 다루는 "타워" 하나의 개념이라 분리 실익이 없다는 사용자 지적으로 병합. 이 문서는 과거 기록 보존용이며, 현재 이 클래스의 필드/메서드(`currentHp`/`maxHp`/`TakeDamage`/`OnDie` 등)는 전부 [[TowerController]]에 그대로 존재한다. 상세는 TowerController.md 2026-07-23 항목 참고.

## 연관 클래스
- MonsterManager — `OnMonsterReachEnd` 이벤트 구독 대상 (`OnEnemyReachTower`)
- InGameScene — `Init()` 호출 + 이벤트 구독 배선
- RewardData — `OnEnemyReachTower` 매개변수 (`DamageToBase`)
- GameConfigRecord (GameConfigTable) — `TowerMaxHp` 값 조회
- TowerHealthText — `Current` 정적 접근자로 폴링, HUD Hp 텍스트 갱신
- [[SceneSingleton]] (부모, 2026-07-21부터 — `Current` 필드/Awake/OnDestroy가 여기로 이동)

## 현재 상태
- 경로: Assets/Scripts/InGame/TowerHealth.cs
- 플레이어(타워)의 HP를 보관하는 순수 MonoBehaviour. ActorPlayer 오브젝트에 부착(InGameScene.unity, fileID 1165160032).
- `TowerHealth.Current`는 [[SceneSingleton]]&lt;TowerHealth&gt; 상속으로 얻는다(2026-07-21부터, 아래 참고) — `Awake()`에서 자동 설정/`OnDestroy()`에서 자동 해제. UI가 씬 하이어라키를 안 거치고 폴링하기 위함.
- 필드: `m_MaxHp`(int, private) / `maxHp`(공개 읽기 전용 프로퍼티) / `currentHp`(2026-07-22부터 `ObservableVariable<int>` — 아래 참고).
- `Init(int _maxHp)` — 최대/현재 HP 초기화(`currentHp.Value = _maxHp`).
- `TakeDamage(int _amount)` — HP 차감, 0 미만 클램프, 0 도달 시 `OnDie` 이벤트 발행. 이미 0이면 무시(중복 사망 처리 방지). `currentHp.Value` 대입 시점에 구독자(예: [[TowerHealthText]])에게 자동 통지됨.
- `OnEnemyReachTower(RewardData _reward)` — `MonsterManager.OnMonsterReachEnd`(`event Action<RewardData>`)에 그대로 `+=` 구독 가능한 시그니처. 내부에서 `TakeDamage(_reward.DamageToBase)` 호출.
- `event Action OnDie` — HP 0 도달 시 1회 발행. 현재 구독자 없음(게임오버 UI는 미구현 — [[UIRunOver]]가 빈 스텁이라 후속 작업 필요).

## 설계 근거
- Assets/Design/02_combat.html: "적이 타워에 닿는 즉시 타워 HP 감소... 타워 HP가 0이 되면 게임 종료" — 이 문서의 "타워"가 이 클래스의 대상. Max HP 100(문서 스펙)은 하드코딩하지 않고 GameConfigTable.csv에 `TowerMaxHp` 행으로 데이터화(기존 GameConfigTable 재사용, [[GameConfigRecord]] 참고).
- "적이 타워에 닿는다"는 판정을 위한 별도 충돌체(Collider)는 만들지 않음 — 기존 MonsterManager의 ReachedEndTag(몬스터가 WaypointBuffer의 목적지 (0,0)에 도달) 메커니즘을 그대로 재사용. [[MonsterManager]] 문서의 "4. 보상 받기" 섹션에 이미 이 연동 패턴(`m_BaseHealth.TakeDamage(_reward.DamageToBase)`)이 예시 코드로 문서화되어 있었음 — 이번 구현이 그 예시를 실제 코드로 완성한 것.

## 미구현 범위 (이번 요청 밖, 후속 검토 필요)
- 타워 HP 게이지/글로우 시각 표현(02_combat.html "타워 HP 시각 표현" 섹션 — HP 구간별 글로우 색/강도 변화). 이번 작업은 "닿으면 HP가 닳는다"는 핵심 로직만 구현, 시각 연출은 범위 밖.
- HP 0 도달 시 게임 종료 처리(UIRunOver 연동) — UIRunOver가 현재 빈 스텁이라 이번 범위에 포함하지 않음. `OnDie` 이벤트만 노출해둠(추후 구독해서 연결 가능).

## 작업 내역

### 2026-07-24-0 — 카드 드래프트 시스템용 확장
[[card-draft]] 스펙 구현. 연관: `TowerController`(Shield Burst 데미지 산정), `MonsterManager.DamageEntitiesInRadius`(Shield Burst 폭발).

**필드 추가**: `m_BaseMaxHp`(카드 가산 전 원본 최대치), `m_MaxHpPercentBonus`(Glass Cannon 등 % 보정), `m_MaxHp`(파생값, `RecalculateMaxHp()`로 재계산), `m_DamageTakenReductionPercent`, `m_HealPerSecond`+`m_HealAccumulator`(Regeneration), `m_ShieldBurstThresholdPercent`+`m_isShieldBurstArmed`(1회성 무장 플래그), `m_hasRevive`+`m_ReviveHpPercent`(Phoenix).

**신규 public API**: `Heal(int)`, `AddMaxHp(int)`(가산 시 델타만큼 즉시 회복도 같이 적용), `AddMaxHpPercent(float)`(회복 없이 클램프만), `AddDamageTakenReductionPercent(float)`, `AddHealPerSecond(float)`, `SetShieldBurstThreshold(float)`, `SetReviveOnce(float)`.

**신규 private**: `RecalculateMaxHp(bool _healByDelta)`(maxHp 재계산 + 조건부 회복/클램프), `CheckShieldBurst()`(HP 30% 미만 최초 진입 감지 → `TowerController.Current.GetShieldBurstDamage()` + `MonsterManager.Current.DamageEntitiesInRadius()` 호출).

**TakeDamage() 변경**: 데미지에 `m_DamageTakenReductionPercent` 적용 → 사망 조건(`<=0`) 도달 시 Phoenix 발동 가능하면 부활(HP를 `m_ReviveHpPercent`로 리필, `OnDie` 미발행) → 아니면 기존대로 `OnDie` 발행 → `CheckShieldBurst()` 호출.

**`UpdateLogic()` 신규 override**: Regeneration의 초당 회복 틱(`m_HealAccumulator` 누적 후 1초마다 `Heal(Mathf.RoundToInt(m_HealPerSecond))`).

### 미검증
Unity MCP 미연결, 컴파일/Play 확인 안 됨.

### 2026-07-22-0

#### 개요
사용자 제안("TowerHealthText 같은애들을 공용화 시키면 되지 않나") — `currentHp`를 폴링 대상 plain int에서 `ObservableVariable<int>`로 전환. 상세는 [[ObservableIntText]] 참고.

#### 파일
- Assets/Scripts/InGame/TowerHealth.cs

#### 수정 (함수 단위)
**필드**
- 전: `private int m_CurrentHp;` + `public int currentHp { get { return m_CurrentHp; } }`
- 후: `private int m_CurrentHp;` 제거, `public ObservableVariable<int> currentHp { get; } = new ObservableVariable<int>(0);`

**Init(int _maxHp)**
- 전: `m_CurrentHp = _maxHp;`
- 후: `currentHp.Value = _maxHp;`

**TakeDamage(int _amount)**
- 전: `m_CurrentHp -= _amount; if (m_CurrentHp < 0) m_CurrentHp = 0;` 이후 `m_CurrentHp` 직접 참조
- 후: 로컬 변수 `newHp`로 클램프 계산 후 `currentHp.Value = newHp;` 한 번만 대입(같은 값이면 `ObservableVariable`이 통지 안 함 — `TakeDamage(0)` 같은 호출도 안전)

#### 검증
[[ObservableIntText]] 2026-07-22-0 참고 — Play Mode에서 `TakeDamage(30)` 호출 직후 [[TowerHealthText]] 표시가 "70/100"으로 즉시 갱신되는 것 확인.

---

### 2026-07-21-7

#### 개요
사용자 요청("Current static 싱글톤 패턴이 4곳에 복붙됨" 리팩토링) — `Current` 필드/설정/해제 로직을 [[SceneSingleton]] 공용 베이스로 이관.

#### 파일
- Assets/Scripts/InGame/TowerHealth.cs

#### 수정 (함수 단위)
**클래스 선언**
- 전: `public class TowerHealth : MonoBehaviour` + 자체 `public static TowerHealth Current { get; private set; }`
- 후: `public class TowerHealth : SceneSingleton<TowerHealth>` (Current 필드 제거, 베이스에서 상속)

**Start() / OnDestroy()**
- 전: 각각 `Current = this;` / `if (Current == this) Current = null;`만 하던 메서드
- 후: 둘 다 통째로 삭제 — TowerHealth는 Current 관리 외에 다른 Start/OnDestroy 로직이 없었어서 베이스의 Awake/OnDestroy로 완전히 대체 가능했음(4개 파생 클래스 중 가장 단순해짐).

#### 검증
[[SceneSingleton]] 2026-07-21-0 참고 — `TowerHealth.Current`가 Play Mode 중 정상 반환되는 것(hp=100/100)까지 실측 확인.

---

### 2026-07-21-6

#### 개요
사용자 지적 — `TakeDamage()`에서 `Debug.Log`를 그대로 써서 glory.md의 "빌드에서 제거돼야 할 로그는 `Debug.Log` 대신 `Logger.Log`/`Error` 사용" 규칙을 놓침. `Logger.Log`로 교체(같은 실수가 InGameScene.cs에도 있어 함께 수정, [[InGameScene]] 참고).

#### 파일
- Assets/Scripts/InGame/TowerHealth.cs

#### 수정 (함수 단위)
**TakeDamage(int _amount)**
- 전: `Debug.Log($"[TowerHealth] TakeDamage - amount:{_amount}, currentHp:{m_CurrentHp}/{m_MaxHp}");`
- 후: `Logger.Log($"[TowerHealth] TakeDamage - amount:{_amount}, currentHp:{m_CurrentHp}/{m_MaxHp}");`

#### 검증
컴파일 확인(Unity MCP `refresh_unity` + `read_console`, 에러 0건).

---

### 2026-07-21-5

#### 개요
사용자 요청 — InGameScene UI의 킬/HP 표시를 방금 만든 코드(TowerHealth, MonsterManager)와 연동. HUD의 Hp 표시는 [[TowerHealthText]] 참고, Kill 표시는 [[MonsterManager]] 2026-07-21-3 / [[KillCountText]] 참고.

#### 파일
- Assets/Scripts/InGame/TowerHealth.cs

#### 수정 (함수 단위)
**클래스 선언 바로 아래**
- 전: (없음)
- 후: `public static TowerHealth Current { get; private set; }` 추가

**신규 `Start()`/`OnDestroy()`**
```csharp
private void Start()
{
    Current = this;
}

private void OnDestroy()
{
    if (Current == this)
        Current = null;
}
```

#### 검증
Unity MCP `execute_code`로 격리 테스트 — `TowerHealth` 인스턴스 생성 → `Init(100)` → reflection으로 `Current` 설정 → `TowerHealthText.UpdateLogic()` 호출 시 "100/100" 출력, `TakeDamage(30)` 후 재호출 시 "70/100" 출력 확인. 컴파일 에러 0건(`refresh_unity` + `read_console`).
실제 씬 흐름 End-to-End 검증은 여전히 2026-07-21-4에 기록된 선행 버그(client-issues.md 2026-07-21-1)에 막혀 있음 — InGameScene을 에디터에서 직접 로드해 컴포넌트 부착/저장은 완료했지만, TitleScene→Btn_Play 경유 실제 플레이로 HUD가 화면에서 갱신되는지는 미확인.

---

### 2026-07-21-4
- 개요: 사용자 요청("적군에 닿으면 HP가 닳고") — 몬스터가 타워(ActorPlayer)에 도달했을 때 HP가 감소하는 기능 신규 구현.
- 파일: Assets/Scripts/InGame/TowerHealth.cs(신규), Assets/Scripts/InGame/InGameScene.cs, Assets/Resources/Table/GameConfigTable.csv, Assets/Scenes/InGameScene.unity
- 검증: Unity MCP `execute_code`로 격리 테스트 — `Init(100)` → `TakeDamage(30)` → 70, `OnEnemyReachTower(DamageToBase=15)` → 55, `TakeDamage(1000)` → 0(클램프) + `OnDie` 발행, 모두 의도대로 동작 확인. 단, 실제 씬 흐름(TitleScene→Btn_Play→InGameScene)을 통한 End-to-End 검증은 기존에 이미 보고된 별개 버그([client-issues.md](../qa/client-issues.md) 2026-07-21-1, `World.DefaultGameObjectInjectionWorld`가 씬 전환 후 null이 되어 `MonsterManager.Init()`이 NRE로 중단)에 막혀 못함 — `InGameScene.OnSetup()`이 `MonsterManager.Init()` 줄에서 예외로 중단되면서 그 아래 TowerHealth 배선 코드 자체가 실행되지 않음. 상세는 client-issues.md 2026-07-21-2 참고.

### 2026-07-23-0 — 데미지 텍스트 연동
사용자 요청("데미지 폰트도 넣어줘 적군 아군 둘다") — `TakeDamage(int)`에서 실제 감쇄 반영된 `reducedAmount` 계산 직후(Phoenix 부활 분기보다 먼저, 데미지는 항상 표시돼야 하므로) `DamageTextManager.Current?.ShowAllyDamage(transform.position, reducedAmount)` 호출 추가. 검증: 컴파일 에러 0건. 실제 씬 흐름(TitleScene→Btn_Play→InGameScene)으로 End-to-End 검증 완료 — 이번 세션에서 `World.DefaultGameObjectInjectionWorld` null 블로커 없이 정상 재현됨(타워가 실제로 피격당해 HP 감소 + 빨간 데미지 텍스트 표시, 런 종료까지 확인). 상세는 [[DamageTextManager]] 참고.
