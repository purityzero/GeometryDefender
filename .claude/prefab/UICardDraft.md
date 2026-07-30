# UICardDraft (Assets/Resources/Prefabs/UI/UICardDraft.prefab)

연관 스크립트: 없음 (구성요소만)
중첩 프리팹: 없음
기획 근거: Assets/Design/07_ui.html 화면 3 (FIG-07-C · CARD DRAFT)

## 개요
레벨업 카드 드래프트 오버레이 프리팹. 루트 Canvas 없음(풀스트레치) — 씬 Canvas 아래 UIManager 생성 전제.
로드 경로: `Prefabs/UI/UICardDraft` / 프리팹 guid: 2a9d5c1f7e3b4a8dc0f6b2e4a8d13c57

## 계층 구조 (fileID 대역 9003000000000001XXX, 뒤 4자리 표기) — 2026-07-30 기준
```
UICardDraft (...1001)                — RectTransform 풀스트레치
├─ Image_Dim (...1011)               — 딤 배경 a0.9, RaycastTarget ON (뒤 클릭 차단)
├─ Text_Title (...1021)              — "LEVEL UP - CHOOSE ONE" 시안 30 볼드, y+260
├─ Group_Cards (...1031)             — 중앙 660×320, HorizontalLayoutGroup(spacing 14, MiddleCenter)
│  └─ Item_Card (...1041)            — 카드 템플릿 200×320, Image #12121C + Button (코드가 3장 복제)
│     ├─ Image_Icon (...1051)        — shape_star 시안 80×80 (상단) — 등급색/도형은 코드 교체
│     ├─ Text_Name (...1061)         — "Card Name" 볼드 22 #EBEBF5
│     └─ Text_Effect (...1071)       — "Effect" 20 #A0A0B8 (하단 영역)
├─ Btn_Reroll (...1081)              — 190×56, y-220 x0(2026-07-30, 구 x-110에서 중앙으로 이동 — Btn_Skip 삭제로 대칭 배치 근거가 사라짐) / Text_Reroll "REROLL (1)"
└─ Text_BuildInfo (...1121)          — "OFFENSE 0 / UTILITY 0 / DEFENSE 0" #606078, y-300
```
`Btn_Skip`(...1101, 자식 Text_Skip ...1111/1113 포함)은 2026-07-30에 스킵 기능 자체가 폐지되며 완전히 삭제됨 — 아래 changelog 참고.

## 설계 메모
- 등급별 외곽 글로우는 코드에서 Item_Card 배경색/머티리얼 교체로 처리 (UIGlowMat 활용 가능).
- 카드 3장은 고정이지만 템플릿 1장 + 복제 방식 (MetaTree Item_Node와 동일 패턴).

---

## 2026-07-30-0 — 스킵 기능 폐지에 따른 Btn_Skip 삭제

### 개요
사용자 요청("스킵자체는 없어져야할듯 대신 리롤을 좀 많이주는걸로 변경해줘 업그레이드하면") — [[CardManager]]/[[UICardDraft]](class)/[[MetaTreeRecord]] 2026-07-30-0과 세트. 메타 트리 M-403이 스킵 대신 리롤 다량 지급 노드로 바뀌면서, 화면에서도 Skip 버튼 자체가 필요 없어짐.

### 수정 (오브젝트 단위)
- `Btn_Skip`(GO ...1100, RectTransform ...1101, CanvasRenderer ...1102, Image ...1103, Button ...1104)과 자식 `Text_Skip`(GO ...1110, RectTransform ...1111, CanvasRenderer ...1112, TMP ...1113) 전체 삭제(YAML 블록 통째로 제거).
- 루트(...1001) RectTransform의 `m_Children`에서 `{fileID: ...1101}` 참조 제거.
- 루트(...1900) `UICardDraft` 컴포넌트의 `m_SkipButton`/`m_SkipText` 필드 라인 제거.
- `Btn_Reroll`(...1081)의 `m_AnchoredPosition.x`를 `-110` → `0`으로 변경 — 기존엔 Skip(+110)과 좌우 대칭 배치였는데, Skip이 사라지며 화면 중앙에 홀로 남게 배치.

### 검증
`grep`으로 fileID 중복 0건, Skip 관련 참조 전부 제거 확인. 컴파일/Play Mode는 [[UICardDraft]](class) 2026-07-30-0 참고.

---

## 2026-07-24-0 — 필드 배선 + Text_Title UIText 부착

### 개요
[[UICardDraft]](class) 전체 구현에 맞춰 실제 데이터 표시가 가능하도록 프리팹 배선. MCP 미연결, YAML 직접 편집.

### 수정 (오브젝트 단위)

**UICardDraft (루트, ...1900)**
- 후: `m_CardContainer: {fileID: ...1031}`(Group_Cards), `m_CardTemplate: {fileID: ...1041}`(Item_Card), `m_RerollButton: {fileID: ...1084}`, `m_RerollText: {fileID: ...1093}`, `m_SkipButton: {fileID: ...1104}`, `m_SkipText: {fileID: ...1113}`, `m_BuildInfoText: {fileID: ...1123}` 추가.

**Text_Title (...1020)**
- 전: TMP 텍스트만("LEVEL UP - CHOOSE ONE" 정적 baked 문구, 키 없음)
- 후: 신규 `UIText` 컴포넌트(fileID `9003000000000002001`, script guid `1a37630bea274644a85de3916ce19d91`) 부착, `m_Component` 목록에 4번째 항목으로 추가. `m_Text: {fileID: ...1023}`(기존 TMP), `m_Key: CardDraftTitle`.

### 검증
`grep -oE "^--- !u![0-9]+ &[0-9]+"` 로 파일 내 총 54블록, 중복 fileID 없음 확인.

### 미검증
Unity MCP 미연결, 컴파일/Play 확인 안 됨.

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
