# TweenUtil

연관 클래스: TweenSequenceBuilder, Command_Tween

## 개요
DOTween 공용 헬퍼 정적 클래스. 모든 메서드가 `Tween`을 반환해 단독 재생/시퀀스 조립 양쪽에 쓸 수 있다. (Assets/Scripts/Glory/Tween/TweenUtil.cs)

## 현재 상태
- **프로젝트 비의존 공용 코드** — 의존은 Unity/DOTween/TMP뿐 (라이브러리 역동기화 대상)
- TapPress/TapRelease는 값을 파라미터로만 받음 — 탭 기본값(0.95/0.05)은 프로젝트 측 `GameConfigTable.TAP_SCALE`/`TAP_DURATION`(CSV 로드 시 채워지는 static)에 있고, 호출부에서 넘긴다
- Fade: CanvasGroup / Image / SpriteRenderer / TextMeshProUGUI (TMP는 무료 DOTween에 모듈이 없어 `DOTween.To`로 구현)
- Scale: `Scale`(임의 목표 스케일), `ScalePop`(0→1 OutBack, `.From(Vector3.zero)`), `PunchScale`, `TapPress`/`TapRelease`
- Rotate: `RotateLocal`(RotateMode 지정 가능, 기본 Fast — 상대 회전은 LocalAxisAdd 사용)
- Move: `Move`(월드) / `MoveAnchored`(RectTransform)
- Color: SpriteRenderer / Image / **Material**(2026-07-22 추가) — `SpriteRenderer.color`(표준 틴트)가 아니라 커스텀 셰이더의 `_Color` 프로퍼티를 직접 읽는 머테리얼(예: 몬스터/타워가 쓰는 글로우 셰이더)을 색상 트윈할 때 사용
- Float: **Material**(2026-07-22 추가) — 커스텀 셰이더의 임의 float 프로퍼티(`_GlowAmount` 등)를 프로퍼티명 문자열로 지정해 트윈

## 작업 내역

### 2026-07-14-0
- 개요: 신규 생성 (DOTween 공용 유틸 요청).
- 미검증: 에디터 미실행 상태 작성. 컴파일 확인 필요.

### 2026-07-14-1
- 개요: 프로젝트 비의존 공용화 — 설계 문서(Design/07_ui.html) 참조 주석 제거, TAP 상수 readonly→const 전환 후 TapPress(`_scale`,`_duration`)/TapRelease(`_duration`) 기본 파라미터로 개방 (기존 호출부 시그니처 호환 유지).

### 2026-07-14-2
- 개요: 탭 기본값 저장 위치를 Config.asset으로 이전 (사용자 선택).
- 수정: TAP_SCALE/TAP_DURATION const 삭제 → TapPress()/TapRelease() 무인자 오버로드가 `Config.Instance` 값 사용 + 명시값 오버로드 유지. 기본 파라미터 방식은 const가 사라져 불가능해 오버로드 2벌로 분리.

### 2026-07-14-3
- 개요: 탭 기본값 저장 위치 최종 확정 — **GameConfigTable(CSV + 클래스 static)** (사용자 지정). Config 참조/TweenUtil 자체 static/GameManager 주입 모두 제거.
- 수정: TweenUtil에서 TAP_SCALE/TAP_DURATION 및 무인자 TapPress()/TapRelease() 삭제 → 파라미터 버전만 유지. 값 보관은 `GameConfigTable.TAP_SCALE/TAP_DURATION`(테이블 생성자에서 CSV 값 로드)이 담당.
- 사용 예: `TweenUtil.TapPress(transform, GameConfigTable.TAP_SCALE, GameConfigTable.TAP_DURATION)`

### 2026-07-22-0
- 개요: 사용자 요청("HP 달때마다 Player색깔 변하는거... 머테리얼도 색이 서서히 변해야함") — [[TowerColorEffect]]가 몬스터/타워 공용 글로우 셰이더의 `_Color`를 트윈하기 위해 신규 오버로드 필요.
- 수정: `public static Tween Color(Material _target, Color _targetColor, float _duration) => _target.DOColor(_targetColor, _duration);` 추가.
- 검증: 컴파일 에러 0건, [[TowerColorEffect]] 2026-07-22-0에서 실제 사용 검증.

### 2026-07-22-3
- 개요: [[TowerColorEffect]]의 "HP 티어별 글로우 강도 변화 + 30% 이하 펄스 점멸" 구현에 필요한 범용 float 프로퍼티 트윈 오버로드 추가.
- 수정: `public static Tween Float(Material _target, string _propertyName, float _targetValue, float _duration) => _target.DOFloat(_targetValue, _propertyName, _duration);` 추가. DOTween `Material.DOFloat(endValue, propertyName, duration)` shortcut 그대로 래핑.
- 검증: 미검증(Unity MCP 미연결 세션, 컴파일 미확인) — [[TowerColorEffect]] 2026-07-22-3 참고.

### 2026-07-23-0
- 개요: [[DamageText]] 신규 구현("데미지 폰트도 넣어줘") — 월드 스페이스(3D) `TMPro.TextMeshPro`를 페이드아웃시켜야 하는데, 기존 `Fade(TextMeshProUGUI)` 오버로드는 UGUI 전용이라 3D TMP에는 못 씀.
- 수정: `public static Tween Fade(TMPro.TextMeshPro _target, float _targetAlpha, float _duration)` 추가 — UGUI 버전과 동일하게 `DOTween.To(() => _target.alpha, ...)` 패턴.
- 검증: 컴파일 에러 0건, [[DamageText]] 2026-07-23-0에서 실제 페이드아웃 동작 확인.

### 2026-07-23-1
- 개요: [[DamageTextManager]]의 치명타 카메라 셰이크/진동 지연 호출 구현("사격시스템 구현해줘") 도중 추가.
- 수정 1: `public static Tween ShakePosition(Transform _target, float _duration, float _strength, int _vibrato = 10)` 추가 — `Transform.DOShakePosition` 래핑, vibrato 매개변수는 사용자 피드백("카메라 쉐이크 조금더 빠르고")으로 나중에 추가(기본 10 유지, 호출부에서 30으로 override).
- 수정 2: `public static Tween DelayedCall(float _delay, TweenCallback _callback)` 추가 — `DOVirtual.DelayedCall` 래핑. 처음엔 [[DamageTextManager]]에서 `DG.Tweening.DOVirtual.DelayedCall`을 직접 호출했다가, 사용자 지적("우리 Tween 만들어둔거 있는데 왜 쌩으로 쓰냐")으로 이 헬퍼를 신설해 경유하도록 수정 — **DOTween 호출은 예외 없이 전부 TweenUtil에 모을 것**(코루틴/트윈 대상 없는 순수 지연 콜백도 포함).
- 검증: 컴파일 에러 0건, [[DamageTextManager]] 2026-07-23-2에서 실제 사용 확인.

### 2026-07-24-1
- 개요: [[ChainLightning]] 신규 구현("연쇄는 LineRenderer로 Glow하게") — LineRenderer는 alpha 단일 프로퍼티가 없어 기존 Fade 오버로드들로 못 씀.
- 수정: `public static Tween Fade(LineRenderer _target, float _targetAlpha, float _duration)` 추가 — `startColor`/`endColor`의 알파를 함께 트윈(DOTween.To 패턴, TMP Fade와 동일 이유로 전용 모듈 없어 커스텀 게터/세터).
- 검증: 컴파일 에러 0건, [[ChainLightning]] 2026-07-24-0에서 실제 페이드아웃 동작 확인.
