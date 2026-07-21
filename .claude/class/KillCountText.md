# KillCountText

## 연관 클래스
- MonsterManager (`Current.killCount` — `ObservableVariable<int>`, 2026-07-22부터)
- [[ObservableIntText]] (부모, 2026-07-22부터 — 폴링/구독/텍스트 대입 로직이 여기로 이동)
- BaseScene, IUpdatable (Current 준비될 때까지만 재시도용 — [[ObservableIntText]] 참고)

## 개요
InGameScene.unity에 이미 손으로 배치돼 있던 실제 HUD(`Canvas/Top/Kill`)의 킬 카운트 텍스트를 갱신하는 컴포넌트. 2026-07-22부터 [[ObservableIntText]]&lt;MonsterManager&gt;를 상속 — `MonsterManager.Current.killCount`(ObservableVariable) 변경 이벤트로 갱신된다(이전엔 매 프레임 폴링).

## 현재 상태
- 경로: Assets/Scripts/InGame/KillCountText.cs
```csharp
public class KillCountText : ObservableIntText<MonsterManager>
{
    protected override MonsterManager GetSource() => MonsterManager.Current;
    protected override ObservableVariable<int> GetObservable(MonsterManager _source) => _source.killCount;
    protected override string Format(int _value) => _value.ToString();
}
```
- 클래스 자체엔 필드가 없음 — `m_ValueText`(TMP 참조), 등록/해제/구독 로직 전부 [[ObservableIntText]] 베이스가 담당.
- 씬 배치: InGameScene.unity의 `Canvas/Top/Kill` 오브젝트에 컴포넌트로 부착, `m_ValueText`는 그 아래 `frame/Text (TMP)`를 참조 — 필드명이 `m_KillCountText`→`m_ValueText`로 바뀌었지만 `FormerlySerializedAs`로 기존 씬 참조 그대로 유지됨([[ObservableIntText]] 참고).

## 설계 근거
- 07_ui.html "Kill Counter" 표시 스펙은 우측 상단, 처치 시 펄스 효과만 언급 — 정확한 텍스트 포맷은 명시 안 됨. 펄스 등 연출은 이번 범위 밖(순수 숫자 표시만 구현).
- (2026-07-22 이전 근거, 참고용으로 남김) 처음엔 이벤트 구독 대신 폴링 방식(IUpdatable, [[TimerText]]와 동일 패턴)을 채택했었음 — Start() 호출 순서 경합을 신경 쓸 필요가 없다는 이점 때문. 이후 사용자 제안으로 `killCount` 자체가 `ObservableVariable`이 되면서 이 클래스도 구독 방식으로 전환(위 "현재 상태" 참고) — 순서 경합 문제는 [[ObservableIntText]]가 "Current 준비될 때까지 폴링, 이후 구독"으로 해결.

## 작업 내역

### 2026-07-22-0

#### 개요
사용자 제안("KillCountText 같은애들을 공용화 시키면 되지 않나") — [[ObservableIntText]]&lt;MonsterManager&gt; 상속으로 전환.

#### 파일
- Assets/Scripts/InGame/KillCountText.cs

#### 수정
- 전: `MonoBehaviour, IUpdatable` 직접 구현 + `m_KillCountText` 필드 + `Start`/`OnDestroy`/`UpdateLogic` 전부 자체 구현(위 "현재 상태" 이전 버전 참고)
- 후: `ObservableIntText<MonsterManager>` 상속, `GetSource`/`GetObservable`/`Format` 3개 메서드만 구현(위 "현재 상태" 코드 참고) — 클래스 코드가 24줄 → 6줄로 축소

#### 검증
[[ObservableIntText]] 2026-07-22-0 참고 — 필드 리네임으로 씬 참조가 끊겼던 걸 `FormerlySerializedAs`로 해결, Play Mode에서 몬스터 처치 후 텍스트가 "1"로 갱신되는 것까지 실측 확인.

---

### 2026-07-21-0
- 개요: 사용자 요청 — InGameScene UI의 킬 카운트 표시를 MonsterManager와 연동. 상세는 [[MonsterManager]] 2026-07-21-3, [[TowerHealth]] 2026-07-21-5 참고.
- 파일: Assets/Scripts/InGame/KillCountText.cs(신규), Assets/Scenes/InGameScene.unity
- 검증: Unity MCP `execute_code` 격리 테스트로 텍스트 갱신 로직 확인(killCount=3 → "3" 출력). 컴파일 에러 0건. 실제 몬스터 처치로 화면이 갱신되는지는 미검증(선행 버그로 End-to-End 플레이 테스트 불가, 상세는 [[TowerHealth]] 2026-07-21-5).
