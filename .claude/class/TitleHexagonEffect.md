# TitleHexagonEffect

## 연관 클래스
- (외부) DOTween

## 현재 상태
- 경로: Assets/Scripts/Title/TitleHexagonEffect.cs
- 타이틀 화면 육각형 이미지 페이드 연출: `Start()`에서 `HexagonImage.DOFade(0, Speed)`를 Yoyo 무한 루프.
- public 필드: `HexagonImage` (Image, 인스펙터 연결 필요), `Speed` (float, 기본 5).
- `Update()` 없음 (2026-07-21 제거, 아래 참고). 2026-07-14 기준과 동일하게 TitleScene.unity에서 여전히 사용처 없음(재확인 완료) — 삭제 여부는 여전히 사용자 판단 대기.

## 작업 내역

### 2026-07-12-0
- 개요: 프로젝트 전체 스캔으로 기본 정보 문서 초기 생성 (코드 수정 없음)

### 2026-07-14-0
- 개요: TitleScene의 Glow_Image_Hexagon에서 이 컴포넌트를 제거하고 FadeTweenEffect + TweenEffectPlayer 조합으로 대체 (코드 수정 없음, 씬만 변경). **스크립트는 이제 사용처가 없음** — 삭제 여부는 사용자 판단 대기.

### 2026-07-21-0

#### 개요
[[BaseScene]]/[[IUpdatable]] 도입 작업 중 사용자가 "InGame + Title 전부" 범위에 이 스크립트의 빈 Update() 제거를 명시적으로 포함 — 위 2026-07-14-0의 "요청 없이는 제거하지 않음" 유보 조건이 이번엔 충족됨.

#### 수정 (함수 단위)

**Update()**
- 전: `void Update() { }` (빈 구현, 페이드는 DOTween 자체 루프라 애초에 불필요했음)
- 후: 메서드 자체 삭제

#### 미검증
컴파일/에디터 미실행 상태 편집. 씬에 사용처가 없어 실동작 확인은 의미 없음(재부착 시에만 해당).
