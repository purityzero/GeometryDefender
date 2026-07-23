# UpdatableBehaviour

연관 클래스: BaseScene, IUpdatable, [[SceneSingleton]]/[[UIManager]](UIBase) — 동일한 등록 패턴을 가진 자매 베이스

## 개요
`SceneSingleton<T>`(씬 스코프 싱글톤)나 `UIBase`(화면 UI)처럼 공용 베이스가 이미 있는 경우가 아닌, 그냥 일반 `MonoBehaviour`가 `BaseScene`의 중앙 갱신 루프(`IUpdatable`)를 타야 할 때 상속하는 베이스(Glory 라이브러리, 프로젝트 비의존). 이 3개 베이스는 전부 동일하게 `OnEnable()`에서 `BaseScene.Current.Register(this)`, `OnDisable()`에서 `BaseScene.Current?.Unregister(this)`를 자동 호출하고 `UpdateLogic()`은 빈 virtual 기본값이라, 파생 클래스는 `UpdateLogic()`만 override하면 된다.

## 현재 상태
- 경로: Assets/Scripts/Glory/Scene/UpdatableBehaviour.cs
```csharp
public abstract class UpdatableBehaviour : MonoBehaviour, IUpdatable
{
    protected virtual void OnEnable()
    {
        BaseScene.Current.Register(this);
    }

    protected virtual void OnDisable()
    {
        BaseScene.Current?.Unregister(this);
    }

    public virtual void UpdateLogic() { }
}
```
- 파생 클래스가 `Awake()`/`OnEnable()`/`OnDisable()`을 추가로 쓰면 반드시 `base.XXX()`를 호출해야 등록/해제가 유지된다.
- 현재 파생 클래스: TitleSquareEffect, TowerController, SpawnManager, TowerColorEffect(2026-07-23 기준, 전부 이 클래스 신설과 함께 `MonoBehaviour, IUpdatable` 직접 구현에서 전환됨).
- **`OnEnable()`의 `BaseScene.Current.Register(this)`는 `BaseScene.Current`가 이미 설정돼 있어야 안전한데, 이걸 보장해주는 건 [[BaseScene]] 파생 클래스(InGameScene/TitleScene)에 붙은 `[DefaultExecutionOrder(-1000)]`다(2026-07-24)** — 이게 없으면 씬 로드 순서에 따라 이 줄에서 NRE가 실제로 난다(2026-07-24-0 참고). 새 씬을 추가하면서 `BaseScene`을 새로 상속하는 진입점 클래스를 만들 땐 이 attribute를 반드시 같이 붙일 것.

## 작업 내역

### 2026-07-23-0

#### 개요
사용자 요청("IUpdatable 인터페이스로 만들지 말고, UIBase 등등 최상위 클래스에서 BaseScene.Current.Register 등록 할 수 있게 해줘")에 따라 `SceneSingleton<T>`/`UIBase`에는 각각 등록 로직을 흡수시켰으나, 공용 베이스가 아예 없던 4개 클래스(TitleSquareEffect/TowerController/SpawnManager/TowerColorEffect)를 위해 이 클래스를 신설. 상세 설계 배경(OnEnable/OnDisable 채택 이유)은 [[SceneSingleton]] 2026-07-23-0 참고 — 동일한 패턴을 그대로 적용.

#### 신규 파일
- Assets/Scripts/Glory/Scene/UpdatableBehaviour.cs

#### 연관 수정
- TitleSquareEffect: `: MonoBehaviour, IUpdatable` → `: UpdatableBehaviour`, `Start()`의 `BaseScene.Current.Register(this)` 호출 제거(단, 카메라 없으면 등록을 스킵하던 기존 조건부 로직은 더 이상 유지 불가 — OnEnable이 Start보다 먼저 무조건 실행되므로. `UpdateLogic()`이 카메라 없을 때 사실상 무해한 공회전만 하도록 재확인, 상세는 [[TitleSquareEffect]] 참고), `OnDestroy()`(Unregister만 하던 것) 삭제, `UpdateLogic()` → `public override`.
- TowerController: `: MonoBehaviour, IUpdatable` → `: UpdatableBehaviour`, `Start()`(Register만 하던 것) 삭제, `OnDestroy()`의 수동 Unregister 호출 제거(EntityQuery Dispose 로직은 유지), `UpdateLogic()` → `public override`.
- SpawnManager: `: MonoBehaviour, IUpdatable` → `: UpdatableBehaviour`, `Start()`/`OnDestroy()`(각각 Register/Unregister만 하던 것) 전부 삭제, `UpdateLogic()` → `public override`.
- TowerColorEffect: `: MonoBehaviour, IUpdatable` → `: UpdatableBehaviour`, `Start()`에서 Register 호출만 제거(SpriteRenderer 틴트 초기화 로직은 유지), `OnDestroy()`의 수동 Unregister 호출 제거(observable 해제/tween kill 로직은 유지), `UpdateLogic()` → `public override`.

#### 미검증
컴파일/에디터 미실행 상태 편집. Play Mode에서 4개 클래스 모두 계속 정상 틱되는지 확인 필요.

---

### 2026-07-24-0

#### 개요
사용자가 실제 재현한 `NullReferenceException: ... UpdatableBehaviour.OnEnable () (at Assets/Scripts/Glory/Scene/UpdatableBehaviour.cs:8)` — 2026-07-23-0에서 전제했던 "Awake는 항상 OnEnable보다 먼저"라는 Unity 순서 보장이 서로 다른 오브젝트 간에는 성립하지 않아서 발생. 상세 원인/수정은 [[SceneSingleton]] 2026-07-24-0, [[InGameScene]] 2026-07-24-0 참고 — 이 파일 자체는 코드 변경 없음, 수정은 `InGameScene`/`TitleScene`에 `[DefaultExecutionOrder(-1000)]`을 붙이는 쪽에서 처리됨.

#### 파일
변경 없음(UpdatableBehaviour.cs 자체는 그대로).

#### 미검증
[[InGameScene]] 2026-07-24-0 참고.
