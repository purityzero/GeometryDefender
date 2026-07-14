# TitleHexagonEffect

## 연관 클래스
- (외부) DOTween

## 현재 상태
- 경로: Assets/Scripts/Title/TitleHexagonEffect.cs
- 타이틀 화면 육각형 이미지 페이드 연출: `Start()`에서 `HexagonImage.DOFade(0, Speed)`를 Yoyo 무한 루프.
- public 필드: `HexagonImage` (Image, 인스펙터 연결 필요), `Speed` (float, 기본 5).
- 빈 `Update()`가 남아 있음 (원래부터 있던 코드 — 요청 없이는 제거하지 않음).

## 작업 내역

### 2026-07-12-0
- 개요: 프로젝트 전체 스캔으로 기본 정보 문서 초기 생성 (코드 수정 없음)

### 2026-07-14-0
- 개요: TitleScene의 Glow_Image_Hexagon에서 이 컴포넌트를 제거하고 FadeTweenEffect + TweenEffectPlayer 조합으로 대체 (코드 수정 없음, 씬만 변경). **스크립트는 이제 사용처가 없음** — 삭제 여부는 사용자 판단 대기.
