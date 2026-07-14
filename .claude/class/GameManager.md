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

---

## 2026-07-15-4

### 개요
TitleScene에 GameManager 오브젝트 배치 (코드 수정 없음) — 이전까지 어떤 씬에도 없고 instance 접근 코드도 없어 Awake의 TableManager.init()이 실행된 적 없음.
