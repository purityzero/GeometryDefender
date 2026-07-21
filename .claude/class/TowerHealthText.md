# TowerHealthText

## 연관 클래스
- TowerHealth (`Current.currentHp` — `ObservableVariable<int>`, 2026-07-22부터. `Current.maxHp`는 여전히 plain int)
- [[ObservableIntText]] (부모, 2026-07-22부터 — 폴링/구독/텍스트 대입 로직이 여기로 이동)
- BaseScene, IUpdatable (Current 준비될 때까지만 재시도용 — [[ObservableIntText]] 참고)

## 개요
InGameScene.unity에 이미 손으로 배치돼 있던 실제 HUD(`Canvas/Top/Hp`)의 HP 텍스트를 갱신하는 컴포넌트. 2026-07-22부터 [[ObservableIntText]]&lt;TowerHealth&gt;를 상속 — `TowerHealth.Current.currentHp`(ObservableVariable) 변경 이벤트로 "현재/최대" 형식 텍스트가 갱신된다(이전엔 매 프레임 폴링).

## 현재 상태
- 경로: Assets/Scripts/InGame/TowerHealthText.cs
```csharp
public class TowerHealthText : ObservableIntText<TowerHealth>
{
    protected override TowerHealth GetSource() => TowerHealth.Current;
    protected override ObservableVariable<int> GetObservable(TowerHealth _source) => _source.currentHp;
    protected override string Format(int _value) => $"{_value}/{TowerHealth.Current.maxHp}";
}
```
- `maxHp`는 게임 중 안 바뀌는 값(Init 시 1회 설정)이라 Observable로 만들지 않고, `Format()`에서 `TowerHealth.Current.maxHp`를 직접 읽음(이 시점엔 이미 구독이 성공한 뒤라 `Current`가 null일 걱정 없음).
- 클래스 자체엔 필드가 없음 — `m_ValueText`(TMP 참조), 등록/해제/구독 로직 전부 [[ObservableIntText]] 베이스가 담당.
- 씬 배치: InGameScene.unity의 `Canvas/Top/Hp` 오브젝트에 컴포넌트로 부착, `m_ValueText`는 그 아래 `frame/Text (TMP)`를 참조 — 필드명이 `m_HpText`→`m_ValueText`로 바뀌었지만 `FormerlySerializedAs`로 기존 씬 참조 그대로 유지됨([[ObservableIntText]] 참고).

## 설계 근거
- 07_ui.html "HP Pill" 표시 스펙은 좌측 상단, HP 변경 시 펄스 효과만 언급 — 정확한 텍스트 포맷은 명시 안 됨. "현재/최대" 형식은 합리적 기본값으로 채택(펄스 등 연출은 이번 범위 밖).
- (2026-07-22 이전 근거, 참고용으로 남김) 처음엔 [[TowerHealth]]가 `OnDie`만 이벤트로 노출하고 HP 변경 자체는 이벤트가 없어서, 매 데미지마다 이벤트를 추가하는 대신 폴링 패턴([[TimerText]] 재사용)을 택했었음. 이후 사용자 제안으로 `currentHp` 자체가 `ObservableVariable`이 되면서 이 클래스도 구독 방식으로 전환.

## 작업 내역

### 2026-07-22-0

#### 개요
사용자 제안("TowerHealthText 같은애들을 공용화 시키면 되지 않나") — [[ObservableIntText]]&lt;TowerHealth&gt; 상속으로 전환.

#### 파일
- Assets/Scripts/InGame/TowerHealthText.cs

#### 수정
- 전: `MonoBehaviour, IUpdatable` 직접 구현 + `m_HpText` 필드 + `Start`/`OnDestroy`/`UpdateLogic` 전부 자체 구현(위 "현재 상태" 이전 버전 참고)
- 후: `ObservableIntText<TowerHealth>` 상속, `GetSource`/`GetObservable`/`Format` 3개 메서드만 구현(위 "현재 상태" 코드 참고) — 클래스 코드가 25줄 → 7줄로 축소

#### 검증
[[ObservableIntText]] 2026-07-22-0 참고 — 필드 리네임으로 씬 참조가 끊겼던 걸 `FormerlySerializedAs`로 해결, Play Mode에서 `TakeDamage(30)` 후 텍스트가 "70/100"으로 즉시 갱신되는 것까지 실측 확인.

---

### 2026-07-21-0
- 개요: 사용자 요청 — InGameScene UI의 HP 표시를 TowerHealth와 연동. 상세는 [[TowerHealth]] 2026-07-21-5 참고.
- 파일: Assets/Scripts/InGame/TowerHealthText.cs(신규), Assets/Scenes/InGameScene.unity
- 검증: Unity MCP `execute_code` 격리 테스트로 텍스트 갱신 로직 확인(Init(100) → "100/100", TakeDamage(30) 후 → "70/100"). 컴파일 에러 0건. 실제 몬스터 도달로 화면이 갱신되는지는 미검증(선행 버그로 End-to-End 플레이 테스트 불가, 상세는 [[TowerHealth]] 2026-07-21-5).
