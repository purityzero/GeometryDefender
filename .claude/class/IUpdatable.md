# IUpdatable

## 연관 클래스
- BaseScene (이 인터페이스를 구현한 컴포넌트들의 UpdateLogic()을 대신 호출)

## 개요
BaseScene이 중앙에서 대신 구동해주는 컴포넌트가 구현하는 인터페이스. 매 프레임 로직이 필요한 씬 내 매니저는 자기 자신의 MonoBehaviour `Update()` 대신 이 인터페이스의 `UpdateLogic()`을 구현하고, `Start()`에서 `BaseScene.Current.Register(this)`로 등록한다. 상세 배경은 [[BaseScene]] 참고.

## 현재 상태
- 경로: Assets/Scripts/Glory/Scene/IUpdatable.cs
- `void UpdateLogic();` 메서드 하나만 정의.
- 구현체: MonsterManager, SpawnManager, TitleSquareEffect (2026-07-21 기준).

## 작업 내역

### 2026-07-21-0
- 개요: BaseScene과 함께 신규 도입. 상세는 [[BaseScene]] 2026-07-21-0 참고.
