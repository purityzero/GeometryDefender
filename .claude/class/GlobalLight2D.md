# GlobalLight2D

## 연관 클래스
- (Unity) Light2D (URP 2D)

## 현재 상태
- 경로: Assets/Scripts/GlobalLight2D.cs
- 수동 static 중복 방지 패턴 (MonoSingleton 미사용).
- 중복 인스턴스 발견 시 Light2D를 먼저 꺼서 OnEnable 경고를 막은 뒤 `Destroy(gameObject)`.
- 유일 인스턴스는 `DontDestroyOnLoad` 처리 — 씬 전환 간 전역 2D 라이트 유지 용도.

## 작업 내역

### 2026-07-12-0
- 개요: 프로젝트 전체 스캔으로 기본 정보 문서 초기 생성 (코드 수정 없음)
