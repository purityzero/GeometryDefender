# UIInGameHUD (Assets/Resources/Prefabs/UI/UIInGameHUD.prefab)

연관 스크립트: [[UIInGameHUD]] (루트 부착 — **2026-07-23-2부터 실사용 중**, m_HpText/m_TimeText/m_KillText 연결됨)
중첩 프리팹: 없음
기획 근거: Assets/Design/07_ui.html 화면 2 (FIG-07-B · INGAME HUD)

## 개요
인게임 HUD 프리팹. 루트 Canvas 없음(풀스트레치 RectTransform) — 씬 Canvas 아래 직접 배치. **2026-07-23-2부터 InGameScene.unity의 `Canvas` 직속 자식으로 실제 인스턴스가 배치돼 있고, 이게 유일한 실제 HUD다**(이전엔 씬에 손으로 배치된 별도 오브젝트가 진짜 HUD였고 이 prefab은 미사용 — 상세 경위는 [[UIInGameHUD]] 참고).
로드 경로: `Prefabs/UI/UIInGameHUD` / 프리팹 guid: 7f2c4e8a1d5b4c9fb6e0a3d7c1f58b62

## 계층 구조 (fileID 대역 9002000000000001XXX, 뒤 4자리 표기)
```
UIInGameHUD (...1001)                — RectTransform 풀스트레치, UIInGameHUD 컴포넌트(...1900) 부착
├─ Panel_Top (...1011)               — 상단 스트레치 h48, y-64
│  ├─ Pill_Hp (...1013)              — Image(frame_capsule) 170×44 (좌) / Text_Hp "100/100" 시안 22
│  │  └─ Image_HpFill (2026-07-24)   — Filled(Horizontal, Left) frame_capsule 재사용, 색 (1,0.25,0.35,0.55), fillAmount=currentHp/maxHp, sibling index 0(Icon_Hp/Text_Hp보다 뒤)
│  ├─ Pill_Time (...1021)            — 140×44 (중) / Text_Time "00:00" #A0A0B8
│  └─ Pill_Kill (...1029)            — 170×44 (우) / Text_Kill "0" #FF3355
├─ Image_XpGauge (...1041)           — 상단 스트레치 h6, y-120, 배경 #1A1A26
│  └─ Image_XpFill (...1045)         — Filled(Horizontal) shape_square 시안, fillAmount 0 (미구현 — 코드에서 갱신 안 함)
├─ Btn_Pause (...1051)               — 우상단 72×72, 투명 Image + Button / Text_Pause "II" — **2026-07-23-2부터 OnClick 연결됨**(UIInGameHUD.OnClickPauseButton() → UIPause 팝업)
└─ Panel_WeaponCooldown (...1061, 2026-07-27 Panel_Synergy에서 리네임/용도 변경) — 하단 스트레치 h170, y+40, **2026-07-30부터 ScrollRect**(구 VerticalLayoutGroup은 Content로 이동, 아래 참고)
   └─ Viewport (...1065, 2026-07-30 신규) — 풀스트레치, RectMask2D(UIMetaTree의 ScrollView와 동일 패턴 재사용)
      └─ Content (...1114, 2026-07-30 신규) — 상단 스트레치, VerticalLayoutGroup(구 Panel_WeaponCooldown 설정 그대로 이관) + ContentSizeFitter(Vertical=PreferredSize)
         └─ Item_WeaponCooldown (...1071, 구 Item_Synergy)  — 행 템플릿 h30, **비활성**(코드가 무기 수만큼 `ResUtil.Create`로 복제, [[UIInGameHUD]] 2026-07-27-0 참고)
            ├─ Text_Label (...1073)        — 무기 이름으로 런타임 갱신 (우측 180px 제외 스트레치)
            └─ Image_GaugeBG (...1077)     — 우측 160×10 #1A1A26
               └─ Image_GaugeFill (...1081) — Filled(Horizontal), 색은 무기 `ColorHex`로 런타임 갱신, fillAmount=무기 쿨다운 진행률
```

추가(2026-07-23-4, 루트 직속 자식으로 append):
```
├─ Text_Fps (...1090)                — 좌상단 anchor(0,1) pos(16,-16), "FPS: 60" #A0A0B8 16pt, UIFpsCounter 부착(m_FpsText 자기참조)
└─ Btn_Cheat (...1100)               — Btn_Pause 왼쪽(anchoredPosition -108,-140) 72×72, 투명 Image + Button / Text_Cheat "CHEAT" — OnClick → UIInGameHUD.OnClickCheatButton() → UICheatWindow 팝업
```

추가(2026-07-28-0, 루트 직속 자식으로 append):
```
└─ Text_Wave (...1110)               — 상단 중앙 anchor(0.5,1) pos(0,-150) size(200,30), "WAVE 1" #A0A0B8 18pt
```
(실제 좌표가 md 최초 기록 y=-16과 다르게 y=-150으로 되어있음을 2026-07-30 재확인 시 발견 — 다른 요소와 겹치지 않도록 이 세션 이전에 조정된 것으로 추정, 실제 파일 기준으로 정정)

추가(2026-07-30-0, 루트 직속 자식으로 append):
```
└─ Text_Level (...1120)              — 상단 중앙 anchor(0.5,1) pos(0,-190) size(200,30), "LV.1" #A0A0B8 18pt, Text_Wave 바로 아래
```

## 씬 배치 (2026-07-23-2부터)
- InGameScene.unity의 `Canvas`(fileID 655750134/RectTransform 655750138) 직속 자식. PrefabInstance fileID 1786891867, stripped RectTransform 1786891868.
- RectTransform 오버라이드는 `m_Name` 하나뿐 — 나머지는 prefab 기본값(풀스트레치) 그대로 상속.
- Image_XpGauge/Panel_Synergy는 여전히 시각적으로만 존재하고 코드로 갱신되는 곳이 없음(원래 기획서 스코프 중 미구현 부분 — HP/Timer/Kill/Pause만 이번에 실제로 연결됨).

## 설계 메모
- 디자인의 ❤/⏱/💀 이모지는 폰트 아틀라스 미포함 위험으로 제외 — 텍스트만.
- Pill 배경/아이콘: 2026-07-23-2에서 `frame_capsule.png` 배경 + `icon_hp/icon_timer/icon_kill.png` 아이콘으로 교체를 시도했으나, 2026-07-23-3에서 사용자 요청("아이콘은 예전껄로 돌려놔")으로 전부 원복 — Pill_Hp/Time/Kill은 다시 플랫 다크 컬러(`#12121C` 반투명, 스프라이트 없음)이고 아이콘 자식 오브젝트는 없다.
- XP/시너지 게이지는 Image Filled 타입 — 코드에서 `fillAmount`로 갱신할 예정이었으나 아직 미구현(이번 작업 범위 밖).
- Btn_Pause 좌/우 반전(왼손 모드)은 코드에서 anchoredPosition.x 부호 전환으로 처리 예정(미구현).
- **멀티 스프라이트 텍스처 참조 시 주의(참고용, 현재는 미사용)**: `frame_capsule.png`/`icon_*.png`류는 TextureImporter가 `spriteMode: 2`(Multiple)라, `m_Sprite`의 `fileID`는 관용적인 `21300000`이 아니라 각 텍스처 `.meta`의 `internalIDToNameTable`(classID 213) 값을 그대로 써야 한다 — 이번엔 되돌려져서 실제로 화면에 뜨는지 검증되지 않은 채 남음. 나중에 다시 이 방식으로 아이콘/스프라이트를 붙이게 되면 이 패턴을 재사용하되, 에디터에서 실제로 렌더링되는지 먼저 확인할 것.

---

## 2026-07-30-1 — Panel_WeaponCooldown을 ScrollView로 전환

### 개요
사용자 요청("InGameHud의 스킬 쿨타임쪽 스크롤뷰로 변경해야할듯") — 무기 슬롯이 메타 트리(M-405)로 최대 5개까지 늘어날 수 있게 되면서(2026-07-30-1/2 참고), 고정 높이(h170) 안에 `VerticalLayoutGroup`으로만 쌓던 기존 방식은 항목이 늘면 화면 밖으로 넘치는 문제가 있었음. **재사용 우선 원칙**에 따라 새로 만들지 않고, 이미 이 프로젝트에 있는 [[UIMetaTree]]의 ScrollView 구조(guid까지 확인 후 그대로 재사용)를 그대로 따름.

### 수정 (오브젝트 단위)
- `Panel_WeaponCooldown`(GO ...1060): 기존 `VerticalLayoutGroup` 컴포넌트(...1062) 제거, `ScrollRect`(...1063, `m_Content`=Content RT, `m_Viewport`=Viewport RT, `m_Vertical=1`/`m_Horizontal=0`) 추가. `m_Children`을 `Item_WeaponCooldown` 직접 참조 → `Viewport` 하나로 교체.
- 신규 **Viewport**(GO ...1064, RectTransform ...1065, RectMask2D ...1066) — 풀스트레치, `RectMask2D`만 사용(Image+Mask 조합을 처음에 만들었다가, [[UIMetaTree]]의 기존 ScrollView가 이미 `RectMask2D`(guid `3312d7739989d2b4e91e6319e9a96d76`)만으로 구현돼 있는 걸 확인하고 동일하게 맞춤 — 프로젝트 관례 우선).
- 신규 **Content**(GO ...1069, RectTransform ...1114, VerticalLayoutGroup ...1115, ContentSizeFitter ...1116) — 구 `Panel_WeaponCooldown`이 갖고 있던 `VerticalLayoutGroup` 설정(Padding 24/24/8/8, Spacing 8, ChildAlignment 1, ForceExpandWidth 1)을 그대로 이관 + `ContentSizeFitter`(HorizontalFit=Unconstrained, VerticalFit=PreferredSize) 신규 추가 — 항목 수만큼 Content 높이가 늘어나고 Viewport가 이를 잘라 스크롤.
- `Item_WeaponCooldown`(RectTransform ...1071)의 `m_Father`를 `Panel_WeaponCooldown`(...1061) → `Content`(...1114)로 변경(그 외 좌표/자식 구조 불변).
- **루트 `UIInGameHUD` 컴포넌트(...1900)의 `m_WeaponCooldownContainer`도 함께 갱신 필수**: `Panel_WeaponCooldown`(...1061) → `Content`(...1114). 이걸 놓치면 `UpdateWeaponCooldowns()`가 여전히 `Panel_WeaponCooldown` 밑에 직접 `ResUtil.Create`로 행을 생성해버려, 새로 생기는 무기 행이 스크롤 대상(Content) 밖에 놓이는 조용한 버그가 났을 것 — 코드는 그대로인데 프리팹 배선만 바꾸는 리팩터링에서 놓치기 쉬운 지점이라 기록.

### 검증
`grep`으로 fileID 중복 0건, 신규 오브젝트 간 부모/자식 참조 일관성 확인(Panel→Viewport→Content→Item). Play Mode 미검증 — 무기 5개 보유 시 스크롤이 실제로 동작하는지, 드래그가 게이지 위에서 정상적으로 잡히는지(RectMask2D 자체엔 레이캐스트 그래픽이 없어 빈 공간 드래그는 안 먹힐 수 있음 — Image_GaugeBG/Fill 위에서의 드래그로 확인 필요) 확인 필요.

---

## 2026-07-30-0 — Text_Level 신규 (유저 레벨 숫자 표시)

### 개요
[[UIInGameHUD]](class.md) 2026-07-30-1 참고. 사용자 요청("유저의 레벨 숫자도 표기했으면 좋겠음. InGameHud에").

### 수정 (오브젝트 단위)
루트(...1001) `m_Children`에 `{fileID: ...1121}` 추가. 신규 **Text_Level**(GO ...1120, RectTransform ...1121, CanvasRenderer ...1122, TextMeshProUGUI ...1123) — Text_Wave와 동일 스타일(anchor(0.5,1) pivot(0.5,1) size(200,30) fontSize18 색#A0A0B8), `anchoredPosition(0,-190)`로 Text_Wave(y=-150) 바로 아래 배치. 루트 컴포넌트(...1900)에 `m_LevelText: {fileID: ...1123}` 연결.

### 검증
`grep`으로 fileID 중복 0건 확인. Play Mode 미검증 — 실제 화면에서 다른 요소와 안 겹치는지, 레벨업 시 정상 갱신되는지 확인 필요.

---

## 2026-07-28-0 — Text_Wave 신규 (현재 웨이브 번호 표시)

### 개요
[[UIInGameHUD]](class.md) 2026-07-28-0 참고. Unity MCP 미연결로 YAML append 방식.

### 수정 (오브젝트 단위)
루트(...1001) `m_Children`에 `{fileID: 9002000000000001111}` 추가. **Text_Wave**(...1110) 신규: RectTransform(...1111)+CanvasRenderer(...1112)+TextMeshProUGUI(...1113), Text_Fps 패턴 그대로(단순 텍스트, Pill 배경 없음) 상단 중앙 배치. 루트 컴포넌트(...1900)에 `m_WaveText: {fileID: 9002000000000001113}` 연결.

### 검증
grep으로 파일 전체 fileID 중복 0건 확인(마커 문자열로 유일 컨텍스트 확보 후 append, 작업 완료 후 마커 제거).

### 미검증
Unity MCP 미연결, 컴파일/Play Mode 확인 안 됨 — Text_Wave가 Text_Fps/Panel_Top과 겹치지 않는지, 웨이브 전환 시 텍스트가 정확히 갱신되는지 확인 필요.

---

## 2026-07-27-0 — Panel_Synergy → Panel_WeaponCooldown 리네임/용도 변경 (Unity MCP 사용)

### 개요
사용자 요청("인게임 하단 비어있는 곳에... 무기 쿨타임... exp 차는거처럼") — 시너지 표시는 애초에 한 번도 실사용된 적 없는 미구현 기능이라 사용자 확정으로 폐기하고 같은 자리를 무기 쿨다운 게이지로 재활용. [[UIInGameHUD]](class.md) 2026-07-27-0 참고.

### 수정 (오브젝트 단위, Unity MCP `manage_gameobject`)
- **Panel_Synergy(...1061) → Panel_WeaponCooldown**: 이름만 변경, RectTransform(하단 스트레치 h170, y+40)/VerticalLayoutGroup(spacing 8, childControlHeight false)은 그대로 재사용.
- **Item_Synergy(...1071) → Item_WeaponCooldown**: 이름 변경 + `SetActive(false)`로 비활성화(원래도 "미구현이라 인스턴스화 안 함"이었지만 `activeSelf: true`로 남아있어 실제로는 플레이스홀더 텍스트("OFFENSE 0/5")가 그대로 노출되고 있었음 — 이번에 정식 템플릿으로 전환).
- **Text_Label**: 플레이스홀더 텍스트 "OFFENSE 0/5" → "Weapon"(런타임에 무기 이름으로 즉시 덮어써짐, 아무 의미 없는 placeholder).
- Image_GaugeBG/Image_GaugeFill 구조·좌표는 무변경 — 코드가 `row.transform.Find("Image_GaugeBG/Image_GaugeFill")` 경로로 직접 찾으므로 계층 이름을 그대로 유지해야 함.

### 검증
`UIInGameHUD` 컴포넌트의 `m_WeaponCooldownContainer`/`m_WeaponCooldownTemplate` 필드가 각각 새 이름의 오브젝트로 정상 연결됨을 리소스 조회로 확인. Play Mode 실측은 [[UIInGameHUD]](class.md) 2026-07-27-0 참고(미완료).

---

## 2026-07-23-4 — FPS 표시 + 치트 창 열기 버튼 추가

### 개요
[[UIInGameHUD]](class.md) 2026-07-23-4 참고. Unity MCP 미연결로 YAML 직접 편집(append 방식) + `[[UICheatWindow]]`(prefab.md) 신규 생성.

### 수정 (오브젝트 단위)
루트(...1001) `m_Children`에 `{fileID: ...1091}`, `{fileID: ...1101}` 2개 추가.
- **Text_Fps**(...1090) 신규: RectTransform+CanvasRenderer+TextMeshProUGUI+UIFpsCounter.
- **Btn_Cheat**(...1100) 신규: Btn_Pause 구조 복제(Image+Button+Text_Cheat 자식), OnClick Persistent Call → 루트(...1900) `OnClickCheatButton`.

### 검증
`grep`으로 파일 전체 fileID 중복 0건, dangling reference 0건(외부 sprite guid 참조 제외) 확인.

### 미검증
Unity MCP 미연결, 컴파일/Play Mode 확인 안 됨. Text_Fps가 다른 Pill과 겹치지 않는지, Btn_Cheat 클릭 시 실제로 UICheatWindow가 열리는지 에디터에서 확인 필요.

---

## 2026-07-24-0 — m_XpFillImage 배선 + 외부 수정 발견(미해결)

### 개요
[[xp-leveling]] 스펙 구현 — HUD XP 게이지 실배선. 상세 경위는 [[UIInGameHUD]](class) 2026-07-24-0 참고.

### 수정 (오브젝트 단위)
**UIInGameHUD (루트, ...1900)**
- 후: `m_XpFillImage: {fileID: 9002000000000001047}`(Image_XpFill의 Image 컴포넌트) 추가.

### ⚠️ 외부 수정 발견 — 미해결
파일을 다시 열었을 때 2026-07-23-3에서 제거했던 `Icon_Hp`/`Icon_Timer`/`Icon_Kill`/`frame_capsule` 참조가 다시 존재함을 발견(파일이 1543줄이 아니라 1773줄) — 이번 세션에서 되돌린 적 없음, 사용자가 에디터에서 직접 편집·저장한 것으로 추정. **이번 작업은 이 상태를 그대로 두고 `m_XpFillImage` fileID만 확인해 배선함** — 아이콘 유지/재제거 여부는 사용자 확인 필요(미해결).

### 검증
`grep -oE "^--- !u![0-9]+ &[0-9]+"` 로 InGameScene.unity/이 파일 양쪽 중복 fileID 없음 확인.

### 미검증
Unity MCP 미연결, 컴파일/Play 확인 안 됨. 아이콘 유지 여부 사용자 확인 대기.

---

## 2026-07-14-5

### 개요
신규 생성. 07_ui.html 화면 2 기준 인게임 HUD 프리팹 구성요소 제작.

### 파일
- Assets/Resources/Prefabs/UI/UIInGameHUD.prefab (+.meta)

### 미검증
에디터 미실행 YAML 직접 작성. 파싱/레이아웃/게이지 fillAmount 동작 확인 필요.

---

## 2026-07-15-2

### 개요
루트에 동명 컴포넌트(UIInGameHUD, UIBase 상속 빈 스텁) 부착 + UITable(Resources/Table/UITable.csv)에 경로 등록.

### 수정 (오브젝트 단위)

**UIInGameHUD (루트)**
- 전: RectTransform만
- 후: RectTransform + UIInGameHUD(MonoBehaviour, fileID 뒤 4자리 1900)

### 미검증
에디터에서 스크립트 연결(Missing 아님) 확인 필요.

---

## 2026-07-21-0 (2026-07-21-1에서 되돌림)

### 개요
사용자 요청: 인게임 시간 UI 갱신. UIInGameHUD(루트, fileID ...1900)의 `m_TimeText`를 Text_Time(TextMeshProUGUI, fileID 9002000000000001027)에 연결. 상세 로직은 [[UIInGameHUD]] 참고.

### 수정 (오브젝트 단위)

**UIInGameHUD (루트, ...1900)**
- 전: `m_Script`만 있고 직렬화 필드 없음
- 후: `m_TimeText: {fileID: 9002000000000001027}` 추가

---

## 2026-07-21-1

### 개요
사용자 지적으로 위 작업이 잘못 짚었다는 걸 확인 — 이 prefab은 실제 게임에 연결된 적이 없었고, 진짜 HUD는 InGameScene.unity에 이미 손으로 배치돼 있었음(Canvas/Top/Timer 등). 상세 경위는 [[UIInGameHUD]] 2026-07-21-1, 실제 연결처는 [[TimerText]] 참고.

### 수정 (오브젝트 단위)

**UIInGameHUD (루트, ...1900)**
- 전: `m_TimeText: {fileID: 9002000000000001027}`
- 후: 해당 줄 제거 (2026-07-21-0 이전 상태로 복귀)
