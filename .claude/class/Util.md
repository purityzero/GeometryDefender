# Util (EScene)

## 연관 클래스
- SceneManager (Glory) — `NextScene(EScene.XXX.ToString())` 형태로 사용
- TitleScene

## 현재 상태
- 경로: Assets/Scripts/Util.cs
- 현재 내용은 `EScene` enum 하나뿐: `TitleScene`, `InGameScene`.
- 씬 이름 문자열 하드코딩 대신 enum → ToString()으로 사용하는 용도.
- 참고: enum 네이밍 규칙(e 접두사)과 달리 `EScene`으로 되어 있음 (기존 코드 유지).

## 작업 내역

### 2026-07-12-0
- 개요: 프로젝트 전체 스캔으로 기본 정보 문서 초기 생성 (코드 수정 없음)
