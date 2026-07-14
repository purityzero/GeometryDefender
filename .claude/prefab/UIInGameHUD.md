# UIInGameHUD (Assets/Resources/Prefabs/UI/UIInGameHUD.prefab)

연관 스크립트: 없음 (구성요소만 — 화면 제어 스크립트 미작성)
중첩 프리팹: 없음
기획 근거: Assets/Design/07_ui.html 화면 2 (FIG-07-B · INGAME HUD)

## 개요
인게임 HUD 프리팹. 루트 Canvas 없음(풀스트레치 RectTransform) — 씬 Canvas 아래 UIManager 생성 전제.
로드 경로: `Prefabs/UI/UIInGameHUD` / 프리팹 guid: 7f2c4e8a1d5b4c9fb6e0a3d7c1f58b62

## 계층 구조 (fileID 대역 9002000000000001XXX, 뒤 4자리 표기)
```
UIInGameHUD (...1001)                — RectTransform 풀스트레치
├─ Panel_Top (...1011)               — 상단 스트레치 h48, y-64
│  ├─ Pill_Hp (...1013)              — Image 다크 필 170×44 (좌) / Text_Hp "100/100" 시안 22
│  ├─ Pill_Time (...1021)            — 140×44 (중) / Text_Time "00:00" #A0A0B8
│  └─ Pill_Kill (...1029)            — 170×44 (우) / Text_Kill "0" #FF3355
├─ Image_XpGauge (...1041)           — 상단 스트레치 h6, y-120, 배경 #1A1A26
│  └─ Image_XpFill (...1045)         — Filled(Horizontal) shape_square 시안, fillAmount 0 (코드가 갱신)
├─ Btn_Pause (...1051)               — 우상단 72×72, 투명 Image + Button / Text_Pause "II"
└─ Panel_Synergy (...1061)           — 하단 스트레치 h170, y+40, VerticalLayoutGroup
   └─ Item_Synergy (...1071)         — 행 템플릿 h30 (코드가 4개로 복제)
      ├─ Text_Label (...1073)        — "OFFENSE 0/5" #A0A0B8 (우측 180px 제외 스트레치)
      └─ Image_GaugeBG (...1077)     — 우측 160×10 #1A1A26
         └─ Image_GaugeFill (...1081) — Filled(Horizontal) 시안, fillAmount 0
```

## 설계 메모
- 디자인의 ❤/⏱/💀 이모지는 폰트 아틀라스 미포함 위험으로 제외 — 텍스트만.
- XP/시너지 게이지는 Image Filled 타입 — 코드에서 `fillAmount`로 갱신.
- Btn_Pause 좌/우 반전(왼손 모드)은 코드에서 anchoredPosition.x 부호 전환으로 처리.

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
