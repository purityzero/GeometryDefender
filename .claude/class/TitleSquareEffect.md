# TitleSquareEffect

## 연관 클래스
- SceneManager (Glory) — 씬 전환 중이면 이동 정지

## 현재 상태
- 경로: Assets/Scripts/Title/TitleSquareEffect.cs
- 타이틀 화면 배경 사각형 연출: 랜덤 시작 위치/방향/속도(1~5)/회전속도(±30~120)로 떠다니며 카메라(직교) 경계에서 반사(bounce).
- `Start()`에서 Camera.main 없으면 컴포넌트 비활성화. SpriteRenderer bounds로 오브젝트 반크기 계산 후 `SetRandomPosition()`으로 화면 내 랜덤 배치.
- `Update()`에서 `SceneManager.instance.IsSceneTransitioning == true`면 정지 → Move / Rotate / CheckBounce 순서로 처리.
- 이동 가능 영역 계산은 `GetMoveArea()`(Rect 반환, 카메라 크기 - 오브젝트 반크기)로 통일 — SetRandomPosition / CheckBounce 공용.
- 부착 대상: TitleScene의 `Game` 하위 Square ~ Square (6) 7개.

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
