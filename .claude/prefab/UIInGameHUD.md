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
└─ Panel_Synergy (...1061)           — 하단 스트레치 h170, y+40, VerticalLayoutGroup
   └─ Item_Synergy (...1071)         — 행 템플릿 h30 (코드가 4개로 복제 — 미구현, 아직 아무도 인스턴스화 안 함)
      ├─ Text_Label (...1073)        — "OFFENSE 0/5" #A0A0B8 (우측 180px 제외 스트레치)
      └─ Image_GaugeBG (...1077)     — 우측 160×10 #1A1A26
         └─ Image_GaugeFill (...1081) — Filled(Horizontal) 시안, fillAmount 0
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
