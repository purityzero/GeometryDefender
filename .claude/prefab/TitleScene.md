# TitleScene (씬 — Assets/Scenes/TitleScene.unity)

연관 스크립트: TitleScene, TitleSquareEffect, TitleHexagonEffect

## 개요
프리팹이 아닌 씬 파일이지만, UI 계층 YAML 직접 편집 작업을 프리팹 규칙에 준해 여기 기록한다.

## 주요 계층 (파악된 부분만)
```
Game (Transform, fileID 1004255016)          — 배경 연출 루트 (월드)
├─ Square ~ Square (6)  ×7                    — SpriteRenderer + TitleSquareEffect
Btn_MetaTree (fileID 1927580510)              — RectTransform(160×30) + Button + Image
└─ H_LayoutGroup (fileID 1142883388)          — RectTransform + HorizontalLayoutGroup
   ├─ Icon_Slot (fileID 502973040)            — 레이아웃용 슬롯 (사용자가 에디터에서 생성)
   │  └─ Image (fileID 649836078)             — 10×10, y+4.5 오프셋, Image + UIFoldout(?) + RotateLoopEffect
   └─ (Text, fileID 847273900)                — 80×18.78
```
- 주의: Image에 `UnityEngine.Rendering.UI.UIFoldout`(RP Core 디버그 UI용 Toggle, fileID 649836082)이 붙어 있음 — 의도인지 불명, 오추가로 보임. 삭제는 사용자 확인 후.

---

## 2026-07-14-0

### 개요
Btn_MetaTree > H_LayoutGroup의 Image/Text **세로 중심선 어긋남** 수정.

### 파일
- Assets/Scenes/TitleScene.unity

### 증상 / 원인
HorizontalLayoutGroup의 `m_ChildAlignment: 0`(UpperLeft) — 높이가 다른 Image(10)와 Text(18.78)가 상단 기준 정렬되어 두 요소의 세로 중심이 서로 어긋남.

### 수정 (오브젝트 단위)

**H_LayoutGroup — HorizontalLayoutGroup (fileID 1142883390)**
- 전: m_ChildAlignment: 0 (UpperLeft)
- 후: m_ChildAlignment: 3 (MiddleLeft)
- 나머지(Spacing, ForceExpand, ChildControl)와 RectTransform은 원본 그대로 유지.

### 경위 (재발 방지)
처음에 "중앙 정렬"을 그룹을 버튼 중앙으로 옮기는 것으로 오해석해 RectTransform 스트레치 + MiddleCenter로 바꿨다가 사용자 지적으로 되돌림. **요소 간 정렬 요청은 어긋난 축(여기선 세로)만 고치고, 배치 위치는 건드리지 않는다.**

### 미검증
에디터 미실행 상태 YAML 직접 편집. Image/Text 세로 중심이 한 줄로 맞는지 확인 필요.

---

## 2026-07-14-1

### 개요
Icon_Slot > Image에 Y축 왕복 회전 연출(RotateLoopEffect) 부착.

### 파일
- Assets/Scenes/TitleScene.unity

### 수정 (오브젝트 단위)

**Image (fileID 649836078)**
- 전: RectTransform + CanvasRenderer + Image + UIFoldout
- 후: 위에 더해 `RotateLoopEffect`(fileID 900100001, script guid 3f8c2d94ab5e4f7a9c1d6e8b2a4c7f01) 컴포넌트 추가 — m_Component 목록 + MonoBehaviour 블록 삽입
- 필드 초기값(회전 2초/휴식 0.5초)은 이후 사용자가 에디터에서 0.3초/1초, Ease Linear, RotationValue (0,360,0)으로 튜닝.

### 미검증
플레이로 회전/휴식/역회전 반복 확인 필요.

---

## 2026-07-14-2

### 개요
씬 내 DOTween 적용 오브젝트를 전부 TweenEffect 컴포넌트 체계로 전환.

### 파일
- Assets/Scenes/TitleScene.unity

### 수정 (오브젝트 단위)

**Glow_Image_Hexagon (fileID 1206704447)**
- 전: TitleHexagonEffect(1206704451) — HexagonImage DOFade(0, Speed 3) 요요 무한
- 후: FadeTweenEffect(900100002: Duration 3, Ease Linear, TargetAlpha 0) + TweenEffectPlayer(900100003: LoopCount -1, LoopType Yoyo)

**Icon_Slot > Image (fileID 649836078)**
- 전: RotateLoopEffect(900100001) — Y±360 왕복, 회전 0.3s/휴식 1s (사용자가 UIFoldout은 사전에 직접 제거)
- 후: RotateTweenEffect ×2(900100004: +360 / 900100005: -360, 각 Duration 0.3, Delay 1, Linear, LocalAxisAdd) + TweenEffectPlayer(900100006: LoopCount -1, Restart)
- 동작 차이: 기존은 [회전→휴식] 순서, 전환 후는 [휴식→회전] 순서라 최초 시작 시 1초 대기 후 회전 시작 (사이클 자체는 동일)

**변환 제외**: Square ×7의 TitleSquareEffect는 DOTween이 아닌 매 프레임 이동/반사 로직이라 대상 아님.

**신규 meta guid**: FadeTweenEffect 5a1c8e2fd0b94b6c8e3a7d915c2f4a02 / RotateTweenEffect 6b2d9f30e1c05c7d9f4b8ea26d305b13 / TweenEffectPlayer 7c3ea041f2d16d8ea05c9fb37e416c24

### 미검증
플레이로 헥사곤 글로우 페이드 요요 + 아이콘 왕복 회전 확인 필요.

---

## 2026-07-14-3

### 개요
모바일 portrait(720×1280) 기준 해상도 대응 — 모든 CanvasScaler를 Scale With Screen Size로 전환.

### 파일
- Assets/Scenes/TitleScene.unity
- ProjectSettings/ProjectSettings.asset

### 증상 / 원인
Canvas 3개(메인 Canvas 655750134, SceneManager 하위 페이드 Canvas 1483428905, GlowCanvas 1739172949)가 전부 Constant Pixel Size(UiScaleMode 0) — 해상도가 바뀌면 UI 픽셀 크기가 고정이라 비율이 깨짐.

### 수정 (오브젝트 단위)

**CanvasScaler ×3 (공통)**
- 전: m_UiScaleMode: 0, m_ReferenceResolution: 800×600
- 후: m_UiScaleMode: 1 (ScaleWithScreenSize), m_ReferenceResolution: **720×1280**, MatchWidthOrHeight: 0 (Width 기준 — 세로 게임 표준: 가로 배치 고정, 세로 여유만 기기별 차이)
- 페이드 Canvas의 m_ScaleFactor 999(오입력으로 추정, 신모드에선 무시되는 값) → 1로 정리

**ProjectSettings**
- 전: defaultScreenOrientation: 4 (AutoRotation)
- 후: 0 (Portrait 고정)

### 참고
- 월드 배경(Square/TitleSquareEffect)은 런타임에 카메라 경계를 계산하므로 해상도 무관 자동 적응 — 수정 불필요.
- 전제: 작업 기준 Game 뷰가 세로 720×1280. 만약 1280×720(가로)로 작업했다면 ReferenceResolution을 뒤집어야 함.
- (기록 보충) 이 작업 직후 D: 드라이브가 일시 유실됐으나, 복구 후 씬/ProjectSettings 반영 상태 디스크 검증 완료.

### 미검증
Game 뷰에서 720×1280 / 1080×1920 / 1080×2340 등으로 바꿔가며 버튼 크기·배치 비율 유지 확인 필요.
