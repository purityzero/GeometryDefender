# UIPause (Assets/Resources/Prefabs/UI/UIPause.prefab)

연관 스크립트: 없음 (구성요소만)
중첩 프리팹: 없음
기획 근거: Assets/Design/07_ui.html 화면 6 (FIG-07-F · PAUSE MENU)

## 개요
일시정지 메뉴 오버레이 프리팹. 루트 Canvas 없음(풀스트레치) — 씬 Canvas 아래 UIManager 생성 전제.
로드 경로: `Prefabs/UI/UIPause` / 프리팹 guid: 4c8e2a6f9b1d4f7ea3c5e7b9d1a36f84

## 계층 구조 (fileID 대역 9005000000000001XXX, 뒤 4자리 표기)
```
UIPause (...1001)                    — RectTransform 풀스트레치
├─ Image_Dim (...1011)               — 딤 배경 a0.85, RaycastTarget ON
├─ Text_Title (...1021)              — "PAUSED" 시안 36 볼드, 자간 10, y-160
├─ Btn_Resume (...1031)              — 시안 배경 400×60, y+140 / Text_Resume "RESUME" (다크 텍스트)
├─ Btn_Restart (...1051)             — 다크 배경 400×60, y+64 / Text_Restart "RESTART"
├─ Btn_MainMenu (...1071)            — 400×60, y-12 / Text_MainMenu "MAIN MENU"
├─ Btn_Sound (...1091)               — 400×60, y-88 / Text_Sound "SOUND ON"
└─ Group_Build (...1111)             — Image #12121C 박스, 하단 y+80 h220 (좌우 70 여백)
   ├─ Text_BuildLabel (...1121)      — "CURRENT BUILD" #606078
   └─ Text_BuildList (...1131)       — 멀티라인 카드 목록 "-" #A0A0B8 (코드가 세팅)
```

## 설계 메모
- 사운드 토글은 코드에서 Text_Sound를 "SOUND ON"/"SOUND OFF"로 교체하는 방식 전제 (2-상태 토글형 버튼 패턴 — 단일 Button + 표시 전환).
- 🔇/🏠/▶ 이모지는 폰트 미포함 위험으로 제외.

---

## 2026-07-14-5

### 개요
신규 생성. 07_ui.html 화면 6 기준 일시정지 메뉴 프리팹 구성요소 제작.

### 파일
- Assets/Resources/Prefabs/UI/UIPause.prefab (+.meta)

### 미검증
에디터 미실행 YAML 직접 작성. 파싱/버튼 스택 배치 확인 필요.
