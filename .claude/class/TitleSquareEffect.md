# TitleSquareEffect

## 연관 클래스
- SceneManager (Glory) — 씬 전환 중이면 이동 정지
- BaseScene, IUpdatable, [[UpdatableBehaviour]] (부모, 2026-07-23부터 — Update 대신 구동 + 등록/해제 자동 처리)

## 현재 상태
- 경로: Assets/Scripts/Title/TitleSquareEffect.cs
- `public class TitleSquareEffect : UpdatableBehaviour`(2026-07-23부터, 이전엔 `MonoBehaviour, IUpdatable` 직접 구현) — 등록/해제는 [[UpdatableBehaviour]]가 `OnEnable()`/`OnDisable()`에서 자동 처리.
- 타이틀 화면 배경 사각형 연출: 랜덤 시작 위치/방향/속도(1~5)/회전속도(±30~120)로 떠다니며 카메라(직교) 경계에서 반사(bounce).
- `Start()`에서 Camera.main 없으면 이후 초기화(방향/속도/위치 계산)를 건너뜀 — **단, 2026-07-23부터 등록 자체는 OnEnable(Start보다 먼저, 무조건 실행)에서 이미 끝난 뒤라 카메라가 없어도 `UpdateLogic()`은 계속 호출된다.** `m_Direction`/`m_RotationSpeed`가 기본값(0)으로 남아 Move/Rotate가 사실상 무해한 공회전만 하고 CheckBounce는 카메라 null 가드가 있어 안전 — 카메라 없는 상황 자체가 비정상 케이스라 실질적 영향은 없음(이전엔 등록 자체를 스킵해서 UpdateLogic이 아예 안 불렸음, 이제는 불리지만 무해).
- SpriteRenderer bounds로 오브젝트 반크기 계산 후 `SetRandomPosition()`으로 화면 내 랜덤 배치.
- `UpdateLogic()`에서 `SceneManager.instance.IsSceneTransitioning == true`면 정지 → Move / Rotate / CheckBounce 순서로 처리. BaseScene이 대신 호출([[BaseScene]] 참고).
- 이동 가능 영역 계산은 `GetMoveArea()`(Rect 반환, 카메라 크기 - 오브젝트 반크기)로 통일 — SetRandomPosition / CheckBounce 공용.
- 부착 대상: TitleScene의 `Game` 하위 Square ~ Square (6) 7개(단, TitleScene 오브젝트 자체의 자식은 아니고 별도 "Squares" 컨테이너 하위 — [[BaseScene]] 2026-07-21-0 설계 판단 참고).
- `[SerializeField] private SpriteRenderer m_SpriteRenderer;`(2026-07-23) — 프리팹/씬에서 미리 연결 가능, 비어있으면 `Start()`에서 `GetComponent<SpriteRenderer>()`로 폴백. 씬에 이미 배치된 7개 인스턴스는 아직 미와이어링 상태(폴백으로 기존과 동일하게 동작) — 필요시 인스펙터에서 자기 자신의 SpriteRenderer를 드래그해 채워도 됨.

## 작업 내역

### 2026-07-27-4 — InGame→Title 복귀 시 사각형이 화면 밖으로 나가는 버그 수정

#### 개요
사용자 리포트: "TitleScene에서 Square들 InGame → Title로 다시 돌아가면 밖으로 나가버려." — 2026-07-22-0의 회전-경계 버그(이미 수정/확인 완료)와는 증상 발생 조건이 다름(그건 회전각에 따라 상시 발생, 이번 건 InGame→Title **씬 전환 복귀 시점**에 한정) — 별개 원인으로 판단.

#### 원인
[[CullingObject]] 2026-07-27-2와 동일한 클래스의 버그. `m_MainCamera`가 `Start()`에서 딱 1회만 캐싱되는데, `SceneManager`의 씬 전환(additive 로드 → 잠시 후 이전 씬 언로드)이 InGameScene→TitleScene으로 돌아올 때도 두 씬의 카메라가 잠깐 동시에 존재하는 창이 있다. 이 창 안에서(또는 그 근접 타이밍에) TitleScene의 사각형이 `Start()`를 실행하면 `Camera.main`이 곧 파괴될 InGameScene 쪽 카메라를 반환할 수 있고, 그 참조가 굳어버린 채 InGameScene이 언로드되며 파괴된다. 이후 `CheckBounce()`의 `if (m_MainCamera == null) return;` 가드가 매 프레임 조용히 조기 종료되는데, **`Move()`는 카메라와 무관하게 계속 실행되므로** 반사(bounce) 판정만 영구히 멈추고 사각형은 원래 방향으로 계속 이동해 화면 밖으로 나가버린다.

#### 파일
- Assets/Scripts/Title/TitleSquareEffect.cs

#### 수정 (함수 단위)
**CheckBounce()**
- 전: `if (m_MainCamera == null) return;`
- 후: `if (m_MainCamera == null) m_MainCamera = Camera.main;` 재조회를 추가한 뒤에도 여전히 null이면 그때 return — 파괴된 참조를 만나면 다음 프레임에 자연 복구.

#### 검증
IDE 진단 컴파일 에러 0건. Play Mode 실측(InGame→Title 반복 전환 후 사각형이 실제로 경계 안에 계속 머무는지)은 미완 — 다음 세션에서 확인 필요.

---

### 2026-07-23-1

#### 개요
사용자 요청("IUpdatable 인터페이스로 만들지 말고, UIBase 등등 최상위 클래스에서 등록") — 상세 배경은 [[SceneSingleton]] 2026-07-23-0, [[UpdatableBehaviour]] 참고.

#### 파일
- Assets/Scripts/Title/TitleSquareEffect.cs

#### 수정 (함수 단위)
**클래스 선언**: `MonoBehaviour, IUpdatable` → `UpdatableBehaviour`.
**Start()**: 맨 끝의 `BaseScene.Current.Register(this);` 호출 제거(위 "현재 상태"의 카메라 null 케이스 동작 변화 참고), 로그 메시지를 "업데이트를 건너뜁니다" → "초기화를 건너뜁니다"로 정정(더 이상 정확히 업데이트를 건너뛰지 않으므로).
**OnDestroy()**(Unregister만 하던 것): 삭제.
**UpdateLogic()**: `public void` → `public override void`.

#### 미검증
[[SceneSingleton]] 2026-07-23-0 참고. 카메라 없는 극단 케이스(정상 플레이에선 발생 안 함)에서 실제로 문제없는지는 별도 확인 안 함(코드 분석상 무해 판단).

---

### 2026-07-23-0

#### 개요
사용자 요청(신규 코드 규칙): "Awake/Start에서 GetComponent 대신, Unity 내장 컴포넌트는 왠만하면 멤버 변수로 선언해 Prefab에 연동". 기존 코드 전수 검사 중 이 클래스가 해당돼 수정.

#### 파일
- Assets/Scripts/Title/TitleSquareEffect.cs

#### 수정 (함수 단위)
**클래스 선언**: `[SerializeField] private SpriteRenderer m_SpriteRenderer;` 필드 추가.
**Start()**: 전: `SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();`(지역 변수) → 후: `if (m_SpriteRenderer == null) m_SpriteRenderer = GetComponent<SpriteRenderer>();`로 필드에 캐싱(폴백), 이하 로직은 `m_SpriteRenderer` 사용(동작 동일).

#### 검증
필드가 비어있을 때 기존과 동일하게 `GetComponent`로 폴백하므로 TitleScene의 기존 7개 인스턴스는 동작 변화 없음(미검증 — Play Mode 재확인 필요).

---

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

---

## 2026-07-22-0

### 개요
사용자 리포트: "TitleSquareEffect가 설정 영역 밖으로 빠져나가는 버그" — 실제로 Play Mode에서 사각형이 카메라 화면 경계를 넘어가는 현상 확인.

### 원인
`Start()`에서 `m_HalfObjectSize = spriteRenderer.bounds.extents;`로 **한 번만** 캐싱하는데, 이 값은 축정렬 경계(AABB)의 절반 크기라 오브젝트가 `Rotate()`로 계속 회전하면 회전각에 따라 실제 시각적 경계(대각선 방향 폭)가 최대 `halfSide × √2`(45° 부근)까지 커진다. `CheckBounce()`/`GetMoveArea()`는 이 오래된(대부분 더 작은) 캐시값 기준으로 카메라 경계 - 반크기만큼 클램프하므로, 오브젝트가 회전할수록 실제 모서리가 카메라 밖으로 삐져나감. 실측(71.9° 회전 시점): 캐싱값 0.25 vs 실제 `bounds.extents` 0.32.

### 파일
- Assets/Scripts/Title/TitleSquareEffect.cs

### 수정 (함수 단위)

**Start() — 오브젝트 반크기 계산**
- 전: `m_HalfObjectSize = spriteRenderer.bounds.extents;` (회전 시점의 축정렬 경계, 매 순간 값이 달라지는데 최초 1회만 캐싱)
- 후:
```csharp
if (spriteRenderer != null)
{
    float diagonalHalfExtent = spriteRenderer.bounds.extents.magnitude;
    m_HalfObjectSize = new Vector2(diagonalHalfExtent, diagonalHalfExtent);
}
```
`Start()` 시점(오브젝트 초기 회전 0°)의 `bounds.extents.magnitude` = `halfSide × √2`를 계산해 고정 사용. 정사각형은 축정렬 경계 반폭의 회전각별 최댓값이 정확히 `halfSide × √2`이므로(코시-슈바르츠, 45°에서 최댓값 도달), 이 상수값이 이후 어떤 회전각에서도 실제 경계보다 항상 크거나 같음을 보장하는 안전 반경이 된다.

### 검증
- Play Mode 실측: 수정 전 71.9° 회전 시점 `cachedHalfSize=(0.25,0.25)` vs 실제 `bounds.extents=(0.32,0.32)` — 캐시값이 실제보다 작아 위험 확인.
- 수정 후 재측정: `cachedHalfSize=(0.37,0.37)`(Start 시점 계산값), 이는 `halfSide×√2`와 정확히 일치하는 수학적 상한이므로 임의 회전각에서도 항상 안전.
- **사용자가 직접 에디터에서 육안 확인 완료 — "정상적으로 확인됨".**
- 콘솔 에러 0건.
