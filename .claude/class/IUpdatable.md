# IUpdatable

## 연관 클래스
- BaseScene (이 인터페이스를 구현한 컴포넌트들의 UpdateLogic()을 대신 호출)
- [[SceneSingleton]], [[UIManager]](UIBase), [[UpdatableBehaviour]] — 이 인터페이스를 실제로 구현하고 등록/해제까지 자동 처리하는 3개 공용 베이스(2026-07-23부터)

## 개요
BaseScene이 중앙에서 대신 구동해주는 컴포넌트가 구현하는 인터페이스. 매 프레임 로직이 필요한 씬 내 매니저는 자기 자신의 MonoBehaviour `Update()` 대신 이 인터페이스의 `UpdateLogic()`을 구현한다. **2026-07-23부터 개별 클래스가 이 인터페이스를 직접 `: X, IUpdatable`로 선언하지 않는다** — [[SceneSingleton]]/[[UIManager]]의 UIBase/[[UpdatableBehaviour]] 3개 공용 베이스가 대신 구현하고 `OnEnable()`/`OnDisable()`에서 자동으로 `BaseScene.Current.Register`/`Unregister`를 호출한다. 파생 클래스는 이 베이스들 중 하나를 상속하고 `UpdateLogic()`만 `override`하면 된다. 상세 배경은 [[BaseScene]] 참고.

## 현재 상태
- 경로: Assets/Scripts/Glory/Scene/IUpdatable.cs
- `void UpdateLogic();` 메서드 하나만 정의(변경 없음).
- 직접 구현체: [[SceneSingleton]]&lt;T&gt;, UIBase([[UIManager]]), [[UpdatableBehaviour]] — 3개뿐(2026-07-23 기준). 실제 로직을 갖는 최종 클래스(TimerManager/MonsterManager/DifficultyManager/ProjectileManager/TowerHealth 등은 SceneSingleton 경유, UI 화면들은 UIBase 경유, TitleSquareEffect/TowerController/SpawnManager/TowerColorEffect는 UpdatableBehaviour 경유)는 `IUpdatable`을 선언하지 않고 상속으로 얻는다.

## 작업 내역

### 2026-07-23-0

#### 개요
사용자 요청: "IUpdatable 인터페이스로 만들지 말고, UIBase 등등 최상위 클래스에서 BaseScene.Current.Register 등록 할 수 있게 해줘". 상세는 [[SceneSingleton]] 2026-07-23-0, [[UpdatableBehaviour]] 참고.

#### 파일
변경 없음(인터페이스 자체는 그대로) — 이 인터페이스를 직접 선언하던 9개 클래스(TimerManager/MonsterManager/DifficultyManager/ProjectileManager/TowerController/SpawnManager/TowerColorEffect/TitleSquareEffect/UIInGameHUD)에서 `IUpdatable` 선언과 `Start()`/`OnDestroy()` 보일러플레이트가 전부 제거됨.

---

### 2026-07-21-0
- 개요: BaseScene과 함께 신규 도입. 상세는 [[BaseScene]] 2026-07-21-0 참고.
