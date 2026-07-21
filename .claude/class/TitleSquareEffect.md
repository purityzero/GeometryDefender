# TitleSquareEffect

## 연관 클래스
- SceneManager (Glory) — 씬 전환 중이면 이동 정지
- BaseScene, IUpdatable (Update 대신 구동, 2026-07-21)

## 현재 상태
- 경로: Assets/Scripts/Title/TitleSquareEffect.cs
- 타이틀 화면 배경 사각형 연출: 랜덤 시작 위치/방향/속도(1~5)/회전속도(±30~120)로 떠다니며 카메라(직교) 경계에서 반사(bounce).
- `Start()`에서 Camera.main 없으면 이후 로직(Register 포함)을 건너뜀(2026-07-21 이전엔 `enabled = false`로 비활성화했으나, Update가 더 이상 Unity 자동 호출이 아니라 `enabled` 플래그가 의미 없어져 제거 — 대신 Register 자체를 안 함).
- SpriteRenderer bounds로 오브젝트 반크기 계산 후 `SetRandomPosition()`으로 화면 내 랜덤 배치, 마지막에 `BaseScene.Current.Register(this)`로 등록.
- `UpdateLogic()`(2026-07-21 이전엔 `Update()`)에서 `SceneManager.instance.IsSceneTransitioning == true`면 정지 → Move / Rotate / CheckBounce 순서로 처리. BaseScene이 대신 호출([[BaseScene]] 참고).
- 이동 가능 영역 계산은 `GetMoveArea()`(Rect 반환, 카메라 크기 - 오브젝트 반크기)로 통일 — SetRandomPosition / CheckBounce 공용.
- 부착 대상: TitleScene의 `Game` 하위 Square ~ Square (6) 7개(단, TitleScene 오브젝트 자체의 자식은 아니고 별도 "Squares" 컨테이너 하위 — [[BaseScene]] 2026-07-21-0 설계 판단 참고).

## 작업 내역

### 2026-07-12-0
- 개요: 프로젝트 전체 스캔으로 기본 정보 문서 초기 생성 (코드 수정 없음)

### 2026-07-14-0
- 개요: Start 시 화면 범위 내 랜덤 위치 배치 추가 (TitleScene의 Game 하위 Square들이 실행마다 다른 위치에서 시작).
- 파일: Assets/Scripts/Title/TitleSquareEffect.cs
- 수정:
  - `Start()` — 전: 방향/속도/회전만 랜덤 → 후: 마지막에 `SetRandomPosition()` 호출 추가
  - `SetRandomPosition()` 신규 — GetMoveArea 범위 내 Random.Range로 x, y 설정 (z 유지)
  - `GetMoveArea()` 신규 — CheckBounce에 있던 카메라 경계 계산을 추출 (Rect.MinMaxRect 반환)
  - `CheckBounce()` — 전: 경계 계산 인라인 → 후: GetMoveArea 사용 (동작 동일)
- 미검증: 에디터 미실행 상태 편집. 타이틀 씬 플레이로 확인 필요.

---

## 2026-07-21-0

### 개요
사용자 요청: TitleScene 소속 연출 스크립트의 Update를 BaseScene이 대신 구동하도록 구조 변경. 상세 설계는 [[BaseScene]] 참고.

### 파일
- Assets/Scripts/Title/TitleSquareEffect.cs

### 수정 (함수 단위)

**클래스 선언**
- 전: `public class TitleSquareEffect : MonoBehaviour`
- 후: `public class TitleSquareEffect : MonoBehaviour, IUpdatable`

**Start()**
- 전: Camera.main 없으면 `enabled = false; return;`
- 후: `enabled = false` 제거, 그냥 `return;`(Update가 더 이상 Unity 콜백이 아니므로 enabled는 의미 없음). 정상 경로 맨 끝에 `BaseScene.Current.Register(this);` 추가.

**Update() → UpdateLogic()**
- 전: `private void Update() { ... }`
- 후: `public void UpdateLogic() { ... }` (내용 동일) — BaseScene이 대신 호출. Camera.main 없는 경우는 애초에 Register가 안 되므로 별도 null 가드 불필요.

**OnDestroy() 신규**
- 후: `private void OnDestroy() { BaseScene.Current?.Unregister(this); }` 추가 (이전엔 OnDestroy 자체가 없었음)

### 미검증
컴파일/에디터 미실행 상태 편집. 실제 Play Mode로 사각형 7개가 계속 정상 틱(이동/회전/반사)되는지 확인 필요.
