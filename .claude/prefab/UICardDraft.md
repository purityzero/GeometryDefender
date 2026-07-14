# UICardDraft (Assets/Resources/Prefabs/UI/UICardDraft.prefab)

연관 스크립트: 없음 (구성요소만)
중첩 프리팹: 없음
기획 근거: Assets/Design/07_ui.html 화면 3 (FIG-07-C · CARD DRAFT)

## 개요
레벨업 카드 드래프트 오버레이 프리팹. 루트 Canvas 없음(풀스트레치) — 씬 Canvas 아래 UIManager 생성 전제.
로드 경로: `Prefabs/UI/UICardDraft` / 프리팹 guid: 2a9d5c1f7e3b4a8dc0f6b2e4a8d13c57

## 계층 구조 (fileID 대역 9003000000000001XXX, 뒤 4자리 표기)
```
UICardDraft (...1001)                — RectTransform 풀스트레치
├─ Image_Dim (...1011)               — 딤 배경 a0.9, RaycastTarget ON (뒤 클릭 차단)
├─ Text_Title (...1021)              — "LEVEL UP - CHOOSE ONE" 시안 30 볼드, y+260
├─ Group_Cards (...1031)             — 중앙 660×320, HorizontalLayoutGroup(spacing 14, MiddleCenter)
│  └─ Item_Card (...1041)            — 카드 템플릿 200×320, Image #12121C + Button (코드가 3장 복제)
│     ├─ Image_Icon (...1051)        — shape_star 시안 80×80 (상단) — 등급색/도형은 코드 교체
│     ├─ Text_Name (...1061)         — "Card Name" 볼드 22 #EBEBF5
│     └─ Text_Effect (...1071)       — "Effect" 20 #A0A0B8 (하단 영역)
├─ Btn_Reroll (...1081)              — 190×56, y-220 x-110 / Text_Reroll "REROLL (1)"
├─ Btn_Skip (...1101)                — 190×56, y-220 x+110 / Text_Skip "SKIP (+5)"
└─ Text_BuildInfo (...1121)          — "OFFENSE 0 / UTILITY 0 / DEFENSE 0" #606078, y-300
```

## 설계 메모
- 등급별 외곽 글로우는 코드에서 Item_Card 배경색/머티리얼 교체로 처리 (UIGlowMat 활용 가능).
- 카드 3장은 고정이지만 템플릿 1장 + 복제 방식 (MetaTree Item_Node와 동일 패턴).

---

## 2026-07-14-5

### 개요
신규 생성. 07_ui.html 화면 3 기준 카드 드래프트 프리팹 구성요소 제작.

### 파일
- Assets/Resources/Prefabs/UI/UICardDraft.prefab (+.meta)

### 미검증
에디터 미실행 YAML 직접 작성. 파싱/HLG 카드 배치 확인 필요.

---

## 2026-07-15-2

### 개요
루트에 동명 컴포넌트(UICardDraft, UIBase 상속 빈 스텁) 부착 + UITable(Resources/Table/UITable.csv)에 경로 등록.

### 수정 (오브젝트 단위)

**UICardDraft (루트)**
- 전: RectTransform만
- 후: RectTransform + UICardDraft(MonoBehaviour, fileID 뒤 4자리 1900)

### 미검증
에디터에서 스크립트 연결(Missing 아님) 확인 필요.
