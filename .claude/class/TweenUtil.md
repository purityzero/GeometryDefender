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
