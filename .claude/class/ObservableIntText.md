# ObservableIntText&lt;TSource&gt;

## 연관 클래스
- SceneSingleton&lt;T&gt; (제네릭 제약, `TSource : SceneSingleton<TSource>`)
- ObservableVariable&lt;T&gt; (Partterns/Observer) — 실제 구독 대상
- BaseScene, IUpdatable — `Current`가 아직 없을 때까지의 재시도용
- KillCountText, TowerHealthText (파생 클래스)

## 개요
"씬 스코프 싱글톤(`SceneSingleton<T>`)이 들고 있는 `ObservableVariable<int>` 값을 텍스트로 표시"하는 반복 패턴을 공용화한 제네릭 베이스(Glory 라이브러리, 프로젝트 비의존). 리팩토링 전에는 [[KillCountText]]/[[TowerHealthText]]가 거의 동일한 코드(BaseScene 등록 + 매 프레임 폴링해서 텍스트 대입)를 복붙하고 있었다 — 사용자 지적으로 이 베이스로 추출.

## 현재 상태
- 경로: Assets/Scripts/Glory/UI/ObservableIntText.cs
```csharp
public abstract class ObservableIntText<TSource> : MonoBehaviour, IUpdatable where TSource : SceneSingleton<TSource>
{
    [FormerlySerializedAs("m_HpText")]
    [FormerlySerializedAs("m_KillCountText")]
    [SerializeField] private TextMeshProUGUI m_ValueText;

    private ObservableVariable<int> m_RegisteredObservable;

    private void Start() { BaseScene.Current.Register(this); }
    private void OnDestroy() { BaseScene.Current?.Unregister(this); if (m_RegisteredObservable != null) m_RegisteredObservable.UnregisterObserver(OnValueChanged); }

    public void UpdateLogic()
    {
        if (m_RegisteredObservable != null)
            return; // 이미 구독 중이면 매 프레임 아무것도 안 함(이후로는 전부 이벤트 기반)

        TSource source = GetSource();
        if (source == null)
            return;

        m_RegisteredObservable = GetObservable(source);
        m_RegisteredObservable.RegisterObserver(OnValueChanged);
    }

    private void OnValueChanged(int _oldValue, int _newValue) { m_ValueText.text = Format(_newValue); }

    protected abstract TSource GetSource();
    protected abstract ObservableVariable<int> GetObservable(TSource _source);
    protected abstract string Format(int _value);
}
```
- 동작 방식: `TSource.Current`가 아직 준비되지 않은 프레임 동안만 `IUpdatable.UpdateLogic()`으로 재시도(=폴링)하다가, 한 번 구독에 성공하면 그 뒤로는 `ObservableVariable`의 콜백으로만 갱신된다(폴링 자체를 멈추진 않지만, 구독 후에는 매 프레임 필드 null 체크 한 번뿐이라 비용이 거의 없음).
- 파생 클래스는 3개 abstract만 구현: `GetSource()`(정적 `TSource.Current` 반환), `GetObservable(TSource)`(어떤 `ObservableVariable<int>`를 볼지), `Format(int)`(텍스트 포맷).

## 설계 근거

### `GetSource()`가 필요한 이유 — C# 제약
처음엔 베이스 안에서 바로 `TSource.Current`를 쓰려고 했으나 **컴파일 에러(CS0704)** — C#은 제네릭 타입 매개변수를 통한 static 멤버 접근을 허용하지 않는다(`where TSource : SceneSingleton<TSource>` 제약이 있어도 마찬가지). 그래서 구체 타입을 아는 파생 클래스가 `GetSource() => MonsterManager.Current;` 식으로 대신 반환하도록 설계.

### `FormerlySerializedAs`가 필요했던 이유 — 실제로 겪은 버그
[[KillCountText]]/[[TowerHealthText]]를 이 베이스를 상속하도록 바꾸면서 각자 직접 갖고 있던 필드(`m_KillCountText`, `m_HpText`)가 이 베이스의 공용 필드 `m_ValueText`로 대체됐는데, **씬(InGameScene.unity)에는 여전히 옛 필드명으로 참조가 저장되어 있어 새 필드명과 매칭이 안 되고 조용히 null로 남았다.** Play Mode 실측 중 `NullReferenceException`으로 실제 재현됨(에디터 UI 클릭 없이 코드만 봐서는 못 잡는 종류의 버그 — 이런 "직렬화 필드 리네임"은 항상 씬/프리팹에 저장된 실제 값까지 같이 검증해야 한다는 교훈, [[glory]] 규칙 문서에도 반영).
- 해결: `[FormerlySerializedAs("옛이름")]`을 필드에 여러 개 스택으로 붙여서 과거 두 필드명 모두 이 새 필드로 매핑되게 함 — 씬 파일은 전혀 안 건드리고 코드만으로 해결.

### 왜 TimerText는 이 베이스를 안 쓰는가
[[TimerText]](경과 시간)는 매 프레임 값이 바뀌는 값이라 Observable로 바꿔도 콜백이 매 프레임 호출되는 건 똑같다(오히려 델리게이트 호출 오버헤드만 늘 수 있음) — "이벤트성으로 드물게 바뀌는 값"에만 이 패턴이 유효하다. TimerText는 기존 IUpdatable 폴링 방식 그대로 유지.

## 작업 내역

### 2026-07-22-0

#### 개요
사용자 제안("killCount 같은걸 옵져버로 만들면 되고, KillCountText 같은애들을 공용화 시키면 되지 않나?") — MonsterManager.killCount/TowerHealth.currentHp를 ObservableVariable로 전환하고, 그걸 표시하는 두 Text 컴포넌트를 이 공용 베이스로 통합.

#### 신규 파일
- Assets/Scripts/Glory/UI/ObservableIntText.cs (Unity MCP `manage_script`로 생성, guid 자동 발급)

#### 연관 수정
- [[MonsterManager]], [[TowerHealth]], [[KillCountText]], [[TowerHealthText]] 각 문서 참고.

#### 검증
- Unity MCP `refresh_unity` — 최초 `TSource.Current` 직접 접근 시도는 컴파일 에러(CS0704)로 즉시 발견 → `GetSource()` 패턴으로 수정 후 에러 0건.
- InGameScene 직접 Play 후 `execute_code`로 초기화(`TableManager.init()` → `MonsterManager.Init()` → `TowerHealth.Init(100)`) 직후 **NullReferenceException 재현**(위 "설계 근거"의 FormerlySerializedAs 필요성 항목) → `FormerlySerializedAs` 추가로 수정.
- 수정 후 재검증: 씬 리로드 후 에디터(Edit Mode)에서 `TowerHealthText`/`KillCountText`의 `m_ValueText`가 각각 올바른 TMP 오브젝트에 정상 바인딩된 것 확인(`mcpforunity://scene/gameobject/{id}/components` 리소스로 직접 조회).
- Play Mode 재실측: 초기 텍스트 "100/100"/"0" 정상 표시(구독 시점 즉시 콜백 — ObservableVariable의 "RegisterObserver 시 현재값으로 1회 콜백" 특성). 몬스터 1마리 스폰 후 `TakeDamage(9999)`로 즉사 처리 + `UpdateLogic()`으로 사망 처리 → 텍스트 "1"로 갱신 확인(단, ECS `HealthSystem`이 실제로 HP를 차감하는 건 다음 프레임이라 즉시 확인 시 "0"이었다가 한 프레임 대기 후 "1"로 갱신됨 — 버그 아니라 ECS 프레임 지연). `TowerHealth.TakeDamage(30)` 후 텍스트 즉시 "70/100"로 갱신 확인(이쪽은 ECS 경유가 아니라 즉시 반영).
- 컴파일 에러 0건(최종).
