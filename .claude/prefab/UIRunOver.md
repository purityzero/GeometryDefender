# UIRunOver (Assets/Resources/Prefabs/UI/UIRunOver.prefab)

연관 스크립트: 없음 (구성요소만)
중첩 프리팹: 없음
기획 근거: Assets/Design/07_ui.html 화면 4 (FIG-07-D · RUN OVER SCREEN)

## 개요
런 종료 정산 화면 프리팹. 루트 Canvas 없음(풀스트레치) — 씬 Canvas 아래 UIManager 생성 전제.
로드 경로: `Prefabs/UI/UIRunOver` / 프리팹 guid: 6e1b8f4a3c7d4e2fa9b5d0c8e2f47a91

## 계층 구조 (fileID 대역 9004000000000001XXX, 뒤 4자리 표기)
```
UIRunOver (...1001)                  — RectTransform 풀스트레치
├─ Image_BG (...1011)                — #0A0A0F a0.95, RaycastTarget ON
├─ Text_Title (...1021)              — "- RUN OVER -" #FF3355 28, 자간 8, y-140
├─ Text_ScoreLabel (...1031)         — "SCORE" #606078 18, y-215
├─ Text_Score (...1041)              — "0" 시안 64 볼드, y-250
├─ Text_Best (...1051)               — "Best: 0" #606078 18, y-340
├─ Group_Stats (...1061)             — 좌우 48 여백, y-400 h150
│  ├─ Text_StatsLabel (...1071)      — 좌측 열 멀티라인 "Survival\nKills\nBoss Kills\nCards" #A0A0B8
│  └─ Text_StatsValue (...1081)      — 우측 열 멀티라인 "00:00\n0\n0\n0" #EBEBF5 우측 정렬
├─ Group_Shards (...1091)            — Image 시안 8% 박스, y-580 h130
│  ├─ Text_ShardsLabel (...1101)     — "SHARDS EARNED" #606078
│  ├─ Text_ShardsEarned (...1111)    — "+0" 시안 36 볼드
│  └─ Text_ShardsTotal (...1121)     — "Total: 0" #606078
├─ Btn_MetaTree (...1131)            — 시안 배경 + Button, 하단 y+200 h64 / Text_MetaTree "META TREE" (다크 텍스트)
├─ Btn_Restart (...1151)             — 다크 배경, y+128 h56 / Text_Restart "RESTART"
└─ Btn_MainMenu (...1171)            — 다크 배경, y+60 h56 / Text_MainMenu "MAIN MENU"
```

## 설계 메모
- 스탯 4행은 개별 행 오브젝트 대신 **좌/우 2개의 멀티라인 TMP 열**로 구성 (양쪽 fontSize 20 / lineSpacing 12 동일 — 행 정렬 유지). 코드는 값 열의 m_text를 "값1\n값2\n값3\n값4"로 한번에 세팅.
- ⭐/📊/💎 이모지는 폰트 미포함 위험으로 제외.

---

## 2026-07-14-5

### 개요
신규 생성. 07_ui.html 화면 4 기준 런 종료 화면 프리팹 구성요소 제작.

### 파일
- Assets/Resources/Prefabs/UI/UIRunOver.prefab (+.meta)

### 미검증
에디터 미실행 YAML 직접 작성. 멀티라인 스탯 열 줄맞춤, 버튼 배치 확인 필요.
