# GameManager

## 연관 클래스
- MonoSingleton (Glory)
- TableManager (Glory)

## 현재 상태
- 경로: Assets/Scripts/GameManager.cs
- `MonoSingleton<GameManager>` 상속.
- `Awake()`에서 `TableManager.instance.init()` 호출 — 게임 진입 시 테이블 로드 트리거가 유일한 역할.

## 작업 내역

### 2026-07-12-0
- 개요: 프로젝트 전체 스캔으로 기본 정보 문서 초기 생성 (코드 수정 없음)
