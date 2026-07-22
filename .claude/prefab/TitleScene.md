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

---

## 2026-07-15-3

### 개요
UIManager 아래 UICanvas / PopupCanvas 2개 Canvas 신설 (UI 프리팹을 얹을 부모 Canvas).

### 파일
- Assets/Scenes/TitleScene.unity

### 수정 (오브젝트 단위)

**UIManager (fileID 1078791267, Transform 1078791268)**
- 전: m_Children 없음
- 후: 자식 2개 추가 — UICanvas(RectTransform 900100011), PopupCanvas(900100016)

**UICanvas (신규, GO 900100010 / RectTransform 900100011 / Canvas 900100012 / CanvasScaler 900100013 / GraphicRaycaster 900100014)**
- Canvas: ScreenSpaceOverlay, SortingOrder **200**
- CanvasScaler: ScaleWithScreenSize 720×1280, MatchWidth 0 (씬 표준과 동일)

**PopupCanvas (신규, GO 900100015 / RectTransform 900100016 / Canvas 900100017 / CanvasScaler 900100018 / GraphicRaycaster 900100019)**
- 동일 구성, SortingOrder **300**

### SortingOrder 배치 근거
GlowCanvas 100 < 메인 Canvas 101 < UICanvas 200 < PopupCanvas 300 < SceneManager 페이드 Canvas 9999 — 일반 UI 위에 팝업, 그 위에 씬 전환 페이드.

### 참고
- UIManager는 MonoSingleton이라 DontDestroyOnLoad — 두 Canvas도 씬 전환 시 함께 유지됨.
- 현재 UIManager.Get은 UIManager transform 바로 아래에 생성함 — 이 Canvas들을 실제 부모로 쓰려면 Get에 UICanvas/PopupCanvas 분기(m_UIDictinary/m_UIPopupDictinary 대응) 연결 코드가 별도로 필요 (미구현, 요청 시 진행).

### 미검증
에디터에서 씬 파싱/계층 표시, Canvas 스케일 적용 확인 필요.

---

## 2026-07-15-4

### 개요
GameManager 오브젝트 신설 — TableManager.init() 호출 주체가 어느 씬에도 없어 UITable 등 전 테이블이 미로드 상태였음 (Btn_MetaTree 클릭 시 NRE의 근본 원인).

### 파일
- Assets/Scenes/TitleScene.unity

### 수정 (오브젝트 단위)

**GameManager (신규 루트, GO 900100020 / Transform 900100021 / MonoBehaviour 900100022)**
- GameManager 컴포넌트(guid 03f66d2cd8ce0e148a67d8e2fb05cbda) 부착 — Awake에서 base.Awake(DontDestroyOnLoad) + TableManager.instance.init()
- SceneRoots에 등록

### 검증 결과 (같은 건으로 확인한 것)
- Btn_MetaTree(GO 1927580510) onClick → TitleScene(240471114).OnClickMetatreeButton 연결 정상
- ResUtil 경로("Prefabs/UI/UIMetaTree") / 루트 UIMetaTree 컴포넌트 매칭 정상
- GO 1164101812의 Btn_MetaTree(Button 비활성)와 GO 551002954는 레거시로 보임 — 정리는 사용자 판단 대기

### 미검증
플레이로 타이틀 → META TREE 버튼 → UIMetaTree 표시(UICanvas 하위 생성) 확인 필요. InGameScene 직접 플레이 시엔 GameManager가 없어 테이블 미로드 — 필요 시 InGameScene에도 추가.

---

## 2026-07-22-5

### 개요
사용자 지적("GlowCanvas를 나눌 필요가 없어보이는데 크로스체크해서 필요없으면 합쳐줘, Glow효과는 있어야해") — GlowCanvas(Screen Space Camera, sortingOrder 100)와 메인 Canvas(Screen Space Overlay, sortingOrder 101) 2개로 나뉘어 있던 구조를 Canvas 1개로 병합.

### 크로스체크 결과 (병합 전 조사)
- GlowCanvas는 단순 중복이 아니라 의도된 구조였음: Screen Space Overlay는 카메라 Bloom 포스트프로세싱이 끝난 **뒤**에 별도로 합성되므로 Bloom을 못 받는다. GlowCanvas(Screen Space Camera)에 `Glow_Image_Hexagon`(FadeTweenEffect로 알파 pulsing)/`Glow_Btn_Play`(정적, onClick 없음 — 순수 시각용) 딱 2개만 담아 Bloom 기반 halo를 만들고, 그 위에 메인 Canvas의 선명한 `Image_Hexagon`/`Btn_Play`(실제 클릭 핸들러 보유)가 Bloom 영향 없이 덧그려지는 "크리스프 레이어 + Bloom 레이어" 이중 구조였음.
- `Btn_MetaTree`/`Btn_Settings`(GlowCanvas 안): 진짜 죽은 잔여물이었음 — `Btn_MetaTree`는 Image alpha 0.141(거의 안 보임)+raycastTarget false+Button 컴포넌트 없음, `Btn_Settings`는 Image/Graphic 컴포넌트 자체가 없음(아무것도 렌더링 안 함). 둘 다 삭제해도 무해.
- 두 셰이더(`Shader Graphs/Glow`, `Custom/GlowUI`) 그래프/소스를 직접 열어 확인 — 둘 다 `BaseColor = _Color × _GlowAmount` **곱셈만** 하는 셰이더로, halo의 부드러운 번짐 자체는 셰이더가 아니라 URP Bloom 포스트프로세싱이 만드는 것임을 확인. 즉 Overlay Canvas에 이 셰이더를 그대로 옮겨도 Bloom을 안 받으면 halo는 재현 불가 — Screen Space Camera 전환이 필수라는 결론.
- 사용자가 "완전히 하나로 합치기" + "프로젝트의 Glow 셰이더로 진짜 병합"을 선택 — 알파를 pulsing시키면 아이콘 자체가 깜빡이므로, 대신 `_GlowAmount`(알파와 무관, 밝기만 변화)를 pulsing시켜 아이콘은 항상 불투명하게 유지하면서 halo 밝기만 숨쉬게 하는 방식 채택.

### 신규 파일
- `Assets/Scripts/Glory/Tween/GlowAmountTweenEffect.cs` — `ColorTweenEffect`/`FadeTweenEffect`와 동일한 `TweenEffectBase` 파생 패턴. `Image`→`SpriteRenderer` 순으로 탐색해 `TweenUtil.Float(material, "_GlowAmount", targetValue, duration)` 호출. `TweenUtil.Float`는 이미 [[TweenUtil]]에 있던 걸 재사용.

### 수정 (오브젝트 단위)

**Canvas (fileID 655750134, Canvas 컴포넌트 655750137)**
- 전: `m_RenderMode: 0`(Screen Space Overlay)
- 후: `m_RenderMode: 1`(Screen Space Camera), `worldCamera`는 기존과 동일한 Main Camera 참조 그대로(변경 없음), `planeDistance: 100`(GlowCanvas와 동일값, 변경 없음)

**Image_Hexagon (Canvas/Top 하위)**
- 전: `Image.material` = Default UI Material(UI/Default 셰이더), 애니메이션 컴포넌트 없음
- 후: `Image.material` = `GlowMat_Tower.mat`(`Shader Graphs/Glow`, 기존 `Glow_Image_Hexagon`이 쓰던 것과 동일 에셋 재사용) + `GlowAmountTweenEffect`(Duration 3, Ease Linear, TargetGlowAmount 0.4) + `TweenEffectPlayer`(LoopCount -1, LoopType Yoyo) 신규 부착 — 기존 `Glow_Image_Hexagon`의 FadeTweenEffect(Duration 3, Linear, Yoyo) 타이밍을 그대로 계승하되 대상만 알파→GlowAmount로 전환.

**Btn_Play (Canvas/Middle/V_LayoutGroup 하위)**
- 전: `Image.material` = Default UI Material
- 후: `Image.material` = `UIGlowMat.mat`(`Custom/GlowUI`, 기존 `Glow_Btn_Play`가 쓰던 것과 동일 에셋 재사용). `Glow_Btn_Play`는 애니메이션이 없는 정적 glow였으므로 추가 트윈 컴포넌트 없이 머테리얼 교체만으로 동일 룩 재현.
- onClick(`TitleScene.OnClickPlayButton`)은 그대로 유지 — 확인 완료(아래 검증 참고).

**GlowCanvas 전체 삭제**
- `Glow_Image_Hexagon`, `Glow_Btn_Play`, 죽은 잔여물 `Btn_MetaTree`/`Btn_Settings` 전부 포함해 루트 오브젝트째 삭제.

### 검증 (Play Mode 실측, `manage_camera` 스크린샷 + 프로퍼티 값 교차 확인)
- 컴파일/콘솔 에러 0건.
- 1200px 스크린샷: 타이틀 텍스트("GEOMETRY DEFENDER") 선명, 헥사곤/Play 버튼 모두 자연스러운 halo 확인 — 다른 UI(MetaTree/Settings/HowToPlay)에 의도치 않은 Bloom 번짐 없음. (480px 저해상도로 볼 땐 압축 아티팩트로 텍스트가 깨져 보였으나 고해상도 확인 결과 실제 렌더링은 정상 — 저해상도 스크린샷만으로 판단 시 오판 위험 있음, 기록해둠.)
- `_GlowAmount`를 연속 샘플링(0.41→0.55)해 실제로 pulsing 중임을 확인, 동시에 `Image.color.a`는 항상 1로 고정 — 의도한 대로 "아이콘은 항상 불투명, halo 밝기만 숨쉼" 동작 확인.
- `Btn_Play.onClick`이 여전히 `TitleScene.OnClickPlayButton` 1건 유지하는 것을 확인 후 실제 `Invoke()`로 클릭 시뮬레이션 → `InGameScene`으로 정상 전환, 콘솔 에러 0건.

### 남은 참고
배경의 은은한 동심원 물결 패턴은 이번 변경과 무관한 기존 월드 배경 연출로 판단(정사각형 오브젝트들의 Bloom이 겹쳐 생기는 것으로 추정) — 병합 전후 비교 스크린샷을 남기지 않아 100% 단정은 아니지만, Canvas 렌더 모드 변경과는 관련 없는 영역(World Space)이라 범위 밖으로 판단하고 손대지 않음.

---

## 2026-07-22-6

### 개요
사용자가 에디터에서 직접 확인 후 "Image_Hexagon이 Glow효과를 못 받는다", "색깔도 변했다"고 강하게 지적 — 2026-07-22-5의 실제 버그 2건을 확인, 사용자 제안("헥사곤을 UI에 두지 말고 Game/World Space 쪽으로 옮겨서 적용해도 됨")을 받아들여 구조를 변경.

### 발견한 버그 (2026-07-22-5의 원인)
1. **색이 바뀐 진짜 원인**: `Shader Graphs/Glow`/`Custom/GlowUI` 셰이더 둘 다 `BaseColor = _Color × _GlowAmount`를 계산할 때 쓰는 `_Color`는 **머테리얼 에셋에 박제된 값**이지, `Image.color`(컴포넌트의 vertex color)가 아니다. `GlowMat_Tower.mat`/`UIGlowMat.mat`을 그대로 재사용했더니 그 에셋들에 이미 저장돼 있던 `_Color`(각각 `(0,1,0.888)`, `(0,0.644,1)`)가 그대로 나와, `Image_Hexagon`/`Btn_Play`의 원래 색(`(0,1,0.95)`, `(0,1,1)`)과 달라 보였다.
2. **공유 에셋 오염 사고**: `GlowAmountTweenEffect`가 `Image_Hexagon`에서 `GlowMat_Tower.mat`(InGameScene 타워와 공유하는 에셋)을 직접 트윈하다가, Play Mode 종료 시점의 트윈 중간값(`_GlowAmount≈0.4`)이 **에셋 파일에 영구 저장**되어 InGameScene 타워의 밝기까지 오염시켰다. `git diff`로 발견, `git checkout`으로 복구(상세 경위는 [[GlowAmountTweenEffect]] 참고).

### 사용자 제안 반영 — 헥사곤을 UI에서 World Space로 이동
Screen Space Camera Canvas + 커스텀 셰이더 조합에서 반복적으로 문제가 생기자, 사용자가 "헥사곤은 UI에 둘 필요 없다"고 제안 — World Space SpriteRenderer(기존 Square/Tower와 동일 기법)로 옮겨 UI 셰이더 특유의 함정(색상 무시, PerRendererData 텍스처 미지원) 자체를 회피.

### 수정 (오브젝트 단위)

**Canvas/Top/Image_Hexagon (UI) — 완전 삭제**
- 기존 UI Image + GlowMat_Tower + GlowAmountTweenEffect/TweenEffectPlayer 전부 제거.

**Game/Hexagon (신규, World Space)**
- `SpriteRenderer`(sprite=`shape_hexagon.png`, sortingOrder=1 — Square들의 sortingOrder 0보다 위) + 전용 신규 머테리얼 `GlowMat_TitleHexagon.mat`(`Shader Graphs/Glow`, `_Color=(0,1,0.9503546,1)`=원래 Image_Hexagon 색 그대로, `_GlowAmount=1`, `_MainTexture=shape_hexagon.png` 명시 할당 — 안 하면 텍스처 슬롯이 비어 사각형 전체가 칠해짐) + `GlowAmountTweenEffect`(Duration 3, Linear, Target 0.4) + `TweenEffectPlayer`(LoopCount -1, Yoyo).
- 위치: Play Mode에서 기존 `Image_Hexagon.transform.position`(월드 좌표 `(0, 0.3, 90)`, Screen Space Camera의 planeDistance 때문에 Z=90이었을 뿐 X/Y만 유효)을 실측해 `(0, 0.3, 0)`으로 배치. 크기는 `GetWorldCorners()`로 실측한 `0.9 × 0.79` 월드 유닛에 맞춰 스프라이트 네이티브 바운즈(`2.22 × 1.94`, scale 1 기준)와 비교해 `localScale (0.406, 0.406, 1)`로 역산.

**Canvas/Middle/V_LayoutGroup/Btn_Play**
- `Image.material`을 `UIGlowMat.mat`(공유, `_Color=(0,0.644,1)`) 대신 전용 신규 `UIGlowMat_TitlePlay.mat`(`Custom/GlowUI`, `_Color=(0,1,1,1)`=원래 Btn_Play 색 그대로, `_GlowAmount=1`)로 교체 — UI 쓰임이라 `_MainTex`는 `[PerRendererData]`라 자동으로 Btn_Play의 스프라이트를 물어오므로 별도 텍스처 할당 불필요.

### 검증 (Play Mode 실측)
- 컴파일/콘솔 에러 0건.
- 머테리얼 값 직접 조회: 헥사곤 `_Color=(0,1,0.95)`(원래 색과 정확히 일치), Play 버튼 `_Color=(0,1,1)`(원래 색과 정확히 일치) — 색상 불일치 버그 해소 확인.
- 900px 스크린샷: 타이틀 텍스트/헥사곤/Play 버튼 모두 halo 정상, 다른 UI 요소 번짐 없음.
- `Btn_Play.onClick.Invoke()`로 클릭 시뮬레이션 → InGameScene 정상 전환, 콘솔 에러 0건.
- **오염됐던 `GlowMat_Tower.mat`을 `git checkout`으로 원복**하고 `_GlowAmount=1`/`_Color=(0,1,0.888)` 정상 복귀 확인(InGameScene 타워는 이번 변경과 무관해졌으므로 재오염 위험 없음 — Image_Hexagon이 더 이상 이 에셋을 참조하지 않음).

### 신규 파일
- `Assets/Resources/Mat/GlowMat_TitleHexagon.mat`, `Assets/Resources/Mat/UIGlowMat_TitlePlay.mat` — 둘 다 이 타이틀 화면 전용, 다른 씬/오브젝트와 공유 금지.
- `.claude/class/GlowAmountTweenEffect.md` — 공유 머테리얼 오염 사고 경위 및 재발 방지 원칙 기록.

### 교훈 (재발 방지)
- 커스텀 셰이더 머테리얼을 다른 오브젝트에 "재사용"할 때는 그 머테리얼의 `_Color` 등 값이 **Image/SpriteRenderer의 컴포넌트 색과 별개로 에셋에 박제**돼 있을 수 있다는 것부터 셰이더 그래프/소스를 직접 열어 확인해야 한다 — 겉보기 재사용이 실제로는 다른 색을 가져오는 함정이 될 수 있다.
- 런타임에 `material.SetFloat`/tween으로 값을 바꾸는 컴포넌트는 **반드시 그 오브젝트 전용 머테리얼 인스턴스/에셋**에만 붙여야 한다. 공유 에셋에 붙이면 Play Mode 종료 시점 값이 에셋 파일에 영구 저장되는 사고로 이어진다(GameObject/컴포넌트 변경과 달리 에셋 데이터 변경은 Play Mode 종료 후에도 되돌아가지 않음).

---

## 2026-07-22-7

### 개요
사용자가 2026-07-22-6 결과물을 보고 "글로우가 없어졌다가 생겼다가 하는 게 아니라 색상이 물빠졌다 쨍해졌다 하는 거다"라고 재지적 — 스크린샷 1장짜리 검증으로는 pulsing 사이클 전체를 못 봤던 것을 인정하고, 실제로 pulsing 주기 내내(고점/저점 모두) 스크린샷을 찍어 재검증한 결과 **사용자 지적이 정확했음을 확인**.

### 원인
2026-07-22-6에서 `Hexagon`(코어) 자체의 `_GlowAmount`를 0.4~1로 pulsing시켰는데, 이 셰이더는 `BaseColor = _Color × _GlowAmount`라 코어의 **채우기 색 자체**가 밝기 변화를 그대로 받는다 — 알파를 안 건드려서 "사라지진 않지만", 대신 코어 색이 통째로 흐려졌다 진해졌다 하는 것으로 나타났다. 원래 GlowCanvas 2중 구조(선명한 고정 코어 + 그 뒤에 깔린 별도의 pulsing 레이어)가 왜 필요했는지를 재확인한 셈 — 코어 자체를 pulsing시키면 안 되고, **코어와 별개인 halo 레이어만** pulsing해야 한다.

### 수정 (오브젝트 단위)

**Game/Hexagon (코어)**
- `GlowAmountTweenEffect`/`TweenEffectPlayer` 제거 — 이제 `_GlowAmount`는 항상 1로 고정(펄싱 없음), `sortingOrder`를 1→**2**로 올려 halo보다 항상 위에 그려지도록 함.

**Game/HexagonGlow (신규, halo 전용)**
- `Hexagon`과 같은 위치(0, 0.3, 0), 같은 스프라이트(`shape_hexagon.png`)지만 **더 큰 스케일**(`0.568` = 코어 `0.406`의 1.4배 — 코어보다 커야 가장자리가 삐져나와 halo 링이 보임), `sortingOrder=1`(코어보다 아래).
- 전용 신규 머테리얼 `GlowMat_TitleHexagonHalo.mat`(`Shader Graphs/Glow`, `_Color`/`_MainTexture`는 코어와 동일, `_GlowAmount`는 1로 고정 — 이쪽은 안 건드림).
- **`GlowAmountTweenEffect`가 아니라 기존 `FadeTweenEffect`** 재사용(Duration 3, Ease Linear, TargetAlpha 0) + `TweenEffectPlayer`(LoopCount -1, Yoyo) — 원래 `Glow_Image_Hexagon`의 알파 페이드 방식 그대로. 이제는 코어가 별개 오브젝트라 알파가 0에 가까워져도 "사라지는" 것처럼 안 보이고, 코어 가장자리 밖으로 삐져나온 halo 링만 옅어졌다 진해졌다 한다.

### 검증 (Play Mode, pulsing 주기 전체를 스크린샷으로 추적)
- `Thread.Sleep`으로 실제 시간을 흘려보내며 halo alpha를 0.99 → 0.58 → 0.29까지 여러 지점에서 샘플링, 각 지점마다 스크린샷 비교.
- **코어(`Hexagon`) 색은 3개 스크린샷 전부에서 `_Color=(0,1,0.95)`, `_GlowAmount=1`로 정확히 동일** — 물빠짐 현상 완전히 해소 확인.
- halo만 스크린샷상 크기/밝기가 눈에 띄게 오르내리는 것을 육안으로 확인(고점: 넓고 밝은 halo / 저점: 좁고 옅은 halo) — 의도한 "숨쉬는 halo" 그대로.
- `Btn_Play.onClick.Invoke()` 클릭 시뮬레이션 재확인 → InGameScene 정상 전환, 콘솔 에러 0건.
- 작업 후 `git status`로 `GlowMat_Tower.mat`/폰트 에셋 등 의도치 않은 변경 없음 확인.

### 신규 파일
- `Assets/Resources/Mat/GlowMat_TitleHexagonHalo.mat` — halo 전용, 코어와 별도 에셋(공유 금지 원칙 유지).

### 교훈 추가
- **pulsing 검증은 반드시 한 주기 전체(고점~저점)를 스크린샷으로 비교해야 한다** — 스크린샷 1~2장(우연히 비슷한 위상)만으로 "정상"이라 판단하면 실제로는 눈에 띄는 변화를 놓칠 수 있다. `Thread.Sleep`으로 실제 시간을 흘려보내고 중간중간 값을 샘플링해 사이클의 여러 지점을 의도적으로 잡아야 한다.
- 이번 사고 자체가 "알파를 페이드하면 사라지는 것처럼 보인다"는 이전 문제를 피하려다, "그럼 밝기를 바꾸자"로 갔다가 **코어와 halo를 분리하지 않은 채** 코어 자체의 밝기를 바꿔서 새로운 문제(색 워시아웃)를 만든 사례 — 원본 설계가 "왜 2개 레이어로 나눠져 있었는지"의 이유(코어는 고정, halo만 변화)를 끝까지 존중해야 했다.

---

## 2026-07-22-8

### 개요
2026-07-22-7 수정본도 사용자가 실제 에디터에서 확인 후 "제대로 검증 안 했다", "은은하게 빛나는 효과인데 그걸 인지 못한다"고 재차 지적. 사용자가 직접 `QA_Recordings/qa_20260721_165455.mp4`를 지목하며 "그 안의 헥사곤이 빛나는 부분만 참고하라"고 지시 — 실제 레퍼런스 영상과 대조해 재검증.

### 레퍼런스 확보 방법 (ffmpeg/python 미설치 환경)
이 환경엔 ffmpeg/python이 없어(`/watch` 스킬 setup 스크립트도 python3 없어 실행 불가) 일반적인 프레임 추출이 불가능했다. 대신 **Unity 자체의 `VideoPlayer` 컴포넌트로 QA mp4를 직접 재생**해 프레임을 캡처하는 방법을 사용:
- Play Mode에서 임시 GameObject + `VideoPlayer`(RenderTexture 타겟, `url = "file:///절대경로"`) 생성.
- 프레임 정확한 탐색은 `videoPlayer.frame = N`(비동기 seek, 몇 번의 execute_code 호출에 걸쳐 완료 대기)으로 안정적으로 됨 — 단, 아주 짧은 클립(2~3초)에서는 이 seek이 실패하고 `frame=-1`에 머무는 경우가 있어, 그럴 땐 `Play()` 후 다음 호출에서 바로 `Pause()`(왕복 지연 자체를 재생 시간으로 활용)하거나 `playbackSpeed`를 낮춰(`0.05` 등) 왕복 지연 동안 아주 조금만 재생되게 하는 방식으로 우회.
- `RenderTexture.active` + `Texture2D.ReadPixels` + `EncodeToPNG`로 스크래치 폴더에 PNG 저장 → `Read` 도구로 확인.

### 레퍼런스 관찰 (`qa_20260721_165455.mp4`, 2.5초 클립)
- t=0.4s / t=0.93s: 헥사곤 주변에 **꽤 크고 뚜렷한** 초록빛이 도는 시안 halo가 "DEFENDER" 텍스트 근처까지 넓게 번짐 — 지금까지 제가 만든 halo보다 훨씬 크고 진함.
- t=2.5s(클립 끝): halo가 거의 사라짐(옅음) — FadeTweenEffect(3초 주기)가 실제로 크게 오르내리는 것과 일치.
- Play 버튼: 미세한 rim glow만 있고 헥사곤만큼 강하지 않음 — 사용자도 "빨간 네모 말고 헥사곤 빛나는 부분만 참고하라"고 특정.

### 진짜 원인 — 오래된 instance ID 재사용으로 잘못된 부모/스케일
비교 작업 중 `HexagonGlow`를 실제로 열어보니 **`Game`이 아니라 `Hexagon`(코어)의 자식으로 붙어 있었고, 로컬 스케일도 지정한 `0.568`이 아니라 `1.1`로 저장**돼 있었다. 코어 `Hexagon`도 머테리얼이 `GlowMat_TitleHexagon`이 아니라 기본 `Sprites-Default`로 되돌아가 있었다. 원인은 **여러 차례의 Play/Stop 사이클과 대화 턴을 거치며 한참 전에 캐싱해둔 `Game`의 instance ID(`76486`)를 나중에 재사용**한 것 — 그 사이 domain reload/scene 재로드로 instance ID가 재할당되어, 실제로는 다른 오브젝트(이번엔 우연히 `Hexagon` 자신)를 부모로 잘못 지정하게 됐다. 이래서 halo가 코어에 완전히 가려지거나 어중간한 크기로 나와 레퍼런스와 다르게 보였던 것.

### 수정
1. `Hexagon`/`HexagonGlow` 둘 다 삭제 후, **바로 직전에 새로 조회한 fresh instance ID**로 한 번에 연속 재생성(중간에 Play/Stop이나 긴 지연 없이) — 매 단계 직후 `mcpforunity://scene/gameobject/{id}/components` 리소스로 `parentInstanceID`/`localScale`/`material`을 즉시 재확인.
2. 레퍼런스와 맞추기 위해 `GlowMat_TitleHexagonHalo.mat`의 `_GlowAmount`를 `1` → **`2.5`**로 상향(코어는 여전히 `1` 고정, 안 건드림).

### 검증
- 재생성 직후 리소스 읽기로 `Hexagon`(parent=`Game` fileID 확인, material=`GlowMat_TitleHexagon`, sortingOrder=2)과 `HexagonGlow`(parent=`Game`, material=`GlowMat_TitleHexagonHalo`, sortingOrder=1, `FadeTweenEffect`+`TweenEffectPlayer` 정상)를 각각 명시적으로 확인 후 저장.
- 저장된 `TitleScene.unity` 파일을 직접 grep해 `HexagonGlow`의 `m_Father`가 `Game`의 Transform fileID(`1004255016`)와 정확히 일치하는지 재확인(리소스 API만 믿지 않고 디스크 파일까지 대조).
- Play Mode 스크린샷(halo alpha 0.40 / 0.76 두 지점)에서 레퍼런스와 비슷한 크기·밝기의 halo 확인, 코어 색은 두 지점 모두 `(0,1,0.95)`/`glowAmount=1` 고정 재확인.
- `Btn_Play.onClick.Invoke()` 클릭 시뮬레이션 → InGameScene 정상 전환, 콘솔 에러 0건.
- `git status`로 폰트 에셋 등 의도치 않은 변경 없음 재확인(재발 방지 위해 매번 습관화).

### 교훈 추가
- **Unity MCP의 instance ID는 Play/Stop 사이클이나 대화 턴을 넘겨서 재사용하면 안 된다** — domain reload/scene 재로드로 재할당될 수 있어, 오래전에 얻은 ID로 `manage_gameobject`/`manage_components`를 호출하면 엉뚱한 오브젝트에 적용될 수 있다(이번엔 부모 지정이 잘못됨). **오브젝트를 생성/수정하기 직전에 항상 fresh하게 `find_gameobjects`로 ID를 다시 조회**하고, 생성/수정 직후에는 반드시 리소스 API로 실제 적용된 값(부모, 스케일, 머테리얼 등)을 재확인해야 한다 — 응답 메시지의 "success"만 믿으면 안 된다.
- ffmpeg/python이 없는 환경에서도 **Unity 자체 `VideoPlayer`로 로컬 mp4를 재생해 프레임을 캡처**하는 방법이 유효하다 — 별도 도구 설치 없이 프로젝트 내 QA 녹화 영상을 프레임 단위로 참고할 수 있다.
