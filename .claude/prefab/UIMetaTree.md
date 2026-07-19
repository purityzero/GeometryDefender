# UIMetaTree (Assets/Resources/Prefabs/UI/UIMetaTree.prefab)

연관 스크립트: UIMetaTree(루트, 줄기탭+노드 해금 로직), UIAssetBox, [ToggleButtonList](../class/ToggleButtonList.md), [UIToggleButton](../class/UIToggleButton.md), [MetaTreeNodeItem](../class/MetaTreeNodeItem.md)(UIToggleButton 상속)
중첩 프리팹: 없음
기획 근거: Assets/Design/07_ui.html 화면 5 (FIG-07-E · META TREE), 05_meta.html (노드/비용 데이터 + 구현 노트 콜아웃)

## 개요
메타 트리 화면 UI 프리팹. 최초엔 계층/컴포넌트만 제작(로직 없음)했으나, 2026-07-18에 줄기 탭 전환 + 노드 잠김/해금 토글 로직까지 구현 완료.
로드 경로: `Prefabs/UI/UIMetaTree` (ResUtil/UIManager 경유 Resources 로드 전제)
프리팹 guid: 5d7a3f1c9b2e4d6fa8c0e5b7d3f19a46

## 계층 구조
```
UIMetaTree (fileID ...1000)                    — RectTransform(풀스트레치) + UIMetaTree(...1900) — 씬 Canvas 아래에 UIManager가 생성하는 전제
├─ Image_BG (...1010)                          — Image #0A0A0F 풀스트레치, RaycastTarget ON (배경 클릭 차단)
├─ Panel_Top (...1020)                         — RectTransform (상단 스트레치, h80, y-64 세이프에어리어)
│  ├─ Btn_Back (...1030)                       — 투명 Image(RaycastTarget ON) + Button (타겟=자기 Image, OnClick→UIMetaTree.Close 프리팹 고정 연결)
│  │  └─ Text_Back (...1035)                   — TMP "< BACK" #A0A0B8
│  └─ AssetBox_Shard (...1040)                 — Image(시안 10% 배경) + UIAssetBox(m_CurrencyType=1 Shard, m_AmountText→...1053)
│     ├─ Image_Icon (...1045)                  — shape_diamond 시안 24×24
│     └─ Text_Amount (...1050, TMP comp 1053)  — TMP "0" 시안 볼드, 우측 정렬
├─ Panel_Tabs (fileID ...2000, 신규)            — RectTransform(상단 스트레치, h56, y-152) + HorizontalLayoutGroup(패딩24/24, 간격8, 자식폭균등) + ToggleButtonList(라디오모드, KeepOneSelected, m_ToggleButtonPrefab→Item_Tab, m_ToggleListId="MetaTreeBranch" — 2026-07-18-2, UIMetaTree.cs 하드코딩 상수 대신 여기서 관리)
│  └─ Item_Tab (...2010)                       — 템플릿, **기본 비활성**. RectTransform + Image(반투명 배경, RaycastTarget) + Button(타겟=자기Image) + UIToggleButton(GoOn→Text_On, GoOff→Text_Off)
│     ├─ Text_On (...2020, TMP comp 2023)      — TMP 시안 볼드 16pt 중앙정렬, 기본 비활성 — 선택된 탭
│     └─ Text_Off (...2030, TMP comp 2033)     — TMP #A0A0B8 16pt 중앙정렬, 기본 활성 — 비선택 탭
└─ ScrollView (...1060)                        — ScrollRect(세로만, Elastic, Content→...1081, Viewport→...1071). RectTransform 재계산: anchoredPosition(0,-92)/sizeDelta(0,-264) (Panel_Tabs 삽입분 반영, 2026-07-18)
   └─ Viewport (...1070)                       — RectMask2D
      └─ Content (...1080)                     — VerticalLayoutGroup(패딩 24/24/8/24, 간격 10, 폭 스트레치) + ContentSizeFitter(세로 Preferred)
         ├─ Text_BranchHeader (...1090)        — TMP "STARTING POWER" 시안, h40 — 줄기 헤더 템플릿, **기본 비활성**. 런타임에 최초 1회만 Instantiate(UIMetaTree.m_HeaderText로 보관·재사용) — 줄기 전환마다 Destroy/재생성하지 않음(2026-07-18-9)
         └─ Item_Node (...1100)                — Image #12121C 배경(RaycastTarget ON) + Button(타겟=배경) + MetaTreeNodeItem(...1106, UIToggleButton 상속, GoOn→Image_Unlocked, GoOff→Group_Cost), h72 — 노드 아이템 템플릿, **기본 비활성**(2026-07-18)
            ├─ Image_Icon (...1110)            — shape_hexagon 시안 28×28 (MetaTreeNodeItem.m_IconImage)
            ├─ Text_Name (...1120)             — TMP "M-101 HP +10" #EBEBF5 (MetaTreeNodeItem.m_NameText)
            ├─ Group_Cost (...1130)            — 잠김 상태 표시 (우측) = MetaTreeNodeItem(상속받은 UIToggleButton)의 GoOff
            │  ├─ Image_CostIcon (...1132)     — shape_diamond 시안 16×16
            │  └─ Text_Cost (...1136)          — TMP "20" #FFD600 우측 정렬 (MetaTreeNodeItem.m_CostText)
            └─ Image_Unlocked (...1140)        — 24×24, **기본 비활성** — MetaTreeNodeItem(상속받은 UIToggleButton)의 GoOn (해금 상태 표시). 스프라이트 `Resources/Image/UI/unlock_open_128.png`(guid 6d1792c3ca2ca4d4b84de135783558c5), 색상 흰색(원본) — **사용자가 에디터에서 직접 교체함**(원래 제가 넣은 shape_diamond 그린 placeholder에서 변경, 2026-07-18). 앞으로 이 오브젝트를 다시 편집할 땐 이 스프라이트를 유지할 것.
```
- fileID 대역: 9001000000000001000~1143 (기존, 뒤 4자리 표기) + 9001000000000002000~2033 (2026-07-18 신규, Panel_Tabs/Item_Tab/Text_On/Text_Off)
- Item_Node의 컴포넌트는 **MetaTreeNodeItem 하나뿐**(fileID ...1106) — `MetaTreeNodeItem : UIToggleButton` 상속 구조라 베이스 필드(m_SelectButton/m_GoOn/m_GoOff/m_ImageOn/m_ImageOff/m_TextOn/m_TextOff/m_LockObject)와 파생 필드(m_NameText/m_CostText/m_IconImage)가 한 컴포넌트 YAML 블록에 같이 직렬화됨. m_SelectButton→...1104(기존 Button), m_GoOn→...1140(Image_Unlocked), m_GoOff→...1130(Group_Cost), m_ImageOn/m_ImageOff/m_TextOn/m_TextOff/m_LockObject는 미사용(None). m_NameText→...1123(Text_Name TMP), m_CostText→...1139(Text_Cost TMP), m_IconImage→...1113(Image_Icon Image).
- **주의(2026-07-18-1 이력)**: 처음엔 Item_Node에 UIToggleButton(...1105)과 MetaTreeNodeItem(...1106)을 형제 컴포넌트 두 개로 붙였다가, 사용자 지적으로 MetaTreeNodeItem이 UIToggleButton을 상속하도록 리팩터링하면서 ...1105는 삭제하고 그 필드를 ...1106에 병합함. 혹시 옛 기록이나 다른 md에 ...1105가 남아있으면 이 문서가 최신 기준.
- guid는 Assets/Scripts/UI/MetaTreeNodeItem.cs.meta 참고(에디터가 라이브 임포트해 자동 생성한 값, 상속 변경으로도 guid는 안 바뀜 — 같은 파일).

## 참조 GUID (실파일 대조 완료)
- shape_diamond: 4181f9c255bc7cb42b0496069bb94b75 (sprite fileID -5073088034143875061)
- shape_hexagon: 19f6ee037f06fd44392d7663600da747 (sprite fileID -6796216251724203947)
- UIAssetBox.cs: 2d9d50f8f6badac479e1b20a585a1ae3
- TMP 폰트: LiberationSans SDF 8f586378b4e144a9851e7b34d9b748ee
- ScrollRect/ContentSizeFitter/RectMask2D guid는 uGUI 패키지 캐시에서 대조
- (2026-07-18 신규) UIToggleButton.cs: aed685d4d3d86a0478f7384b45135df5
- (2026-07-18 신규) ToggleButtonList.cs: 7ec7c95845f0e6e42bfbee443770e4e0
- (2026-07-18 신규) HorizontalLayoutGroup: 30649d3a9faa99c48a7b1166b86bf2a0

## UIMetaTree(...1900) 직렬화 필드 (2026-07-18 신규)
- m_BranchTabs → Panel_Tabs의 ToggleButtonList(...2003)
- m_Content → Content RectTransform(...1081)
- m_BranchHeaderTemplate → Text_BranchHeader(...1090)
- m_NodeTemplate → Item_Node의 UIToggleButton(...1105)
- ~~m_AssetBoxShard → AssetBox_Shard의 UIAssetBox(...1044)~~ — 2026-07-19-0에 제거(UIAssetBox가 옵저버로 자동 갱신, 스크립트 필드도 삭제됨)

## 설계 메모
- 노드 상태 전환 규칙(코드에서): 잠김=Group_Cost ON / 해금=Image_Unlocked ON + Group_Cost OFF. 배경/아이콘 색은 코드로 교체.
- 디자인의 "✓" 체크 글리프는 LiberationSans 기본 아틀라스에 없을 수 있어 그린 다이아 아이콘으로 대체.
- 노드명 구분자 "·"(U+00B7)도 같은 이유로 미사용 (공백 구분).
- **자체 Canvas 없음** (사용자 지시) — 렌더링되려면 부모가 Canvas 계층이어야 함. 현재 UIManager.Get은 UIManager 자신의 transform 아래에 생성하므로, UIManager가 Canvas 아래에 있거나 생성 후 Canvas로 옮기는 처리가 별도로 필요.

---

## 2026-07-14-4

### 개요
신규 생성. 07_ui.html 화면 5 기준 메타 트리 UI 프리팹 구성요소 제작 (스크립트/데이터 연동 제외).

### 파일
- Assets/Resources/Prefabs/UI/UIMetaTree.prefab (+.meta)
- Assets/Resources/Prefabs.meta, Assets/Resources/Prefabs/UI.meta (폴더 신규)

### 미검증
에디터 미실행 상태 YAML 직접 작성. 파싱/레이아웃/스크롤 동작, UIAssetBox 참조 연결, 세이프에어리어 여백 확인 필요.

---

## 2026-07-14-5

### 개요
루트의 Canvas/CanvasScaler/GraphicRaycaster 제거 — 씬의 UIManager가 씬 Canvas 아래에 생성하는 구조로 변경 (사용자 지시).

### 파일
- Assets/Resources/Prefabs/UI/UIMetaTree.prefab

### 수정 (오브젝트 단위)

**UIMetaTree (루트, fileID ...1000)**
- 전: RectTransform(anchor 0,0~0,0) + Canvas(SortingOrder 200) + CanvasScaler(720×1280) + GraphicRaycaster (사용자가 에디터에서 3개 컴포넌트를 비활성화해 둔 상태였음)
- 후: RectTransform만 유지, 앵커 풀스트레치(0,0~1,1, sizeDelta 0)로 변경 — 부모 Canvas에 꽉 참

### 미검증
부모 Canvas 아래 생성 시 풀스크린 표시 여부 확인 필요.

---

## 2026-07-15-2

### 개요
루트에 동명 컴포넌트(UIMetaTree, UIBase 상속 빈 스텁) 부착 + UITable(Resources/Table/UITable.csv)에 경로 등록.

### 수정 (오브젝트 단위)

**UIMetaTree (루트)**
- 전: RectTransform만
- 후: RectTransform + UIMetaTree(MonoBehaviour, fileID 뒤 4자리 1900)

### 미검증
에디터에서 스크립트 연결(Missing 아님) 확인 필요.

---

## 2026-07-18-0

### 개요
사용자 요청("Toggle을 이용해서 UIMetaTree 만들어줘" → "둘 다"로 범위 확정: 줄기탭 + 노드 잠김/해금 둘 다 Toggle 적용)에 따라 Panel_Tabs 신규 추가 + Item_Node에 UIToggleButton 컴포넌트 추가 + 템플릿 3종(Text_BranchHeader/Item_Node/Item_Tab) 기본 비활성화. 스크립트 쪽 구현은 .claude/class/UIMetaTree.md 참고.

### 파일
- Assets/Resources/Prefabs/UI/UIMetaTree.prefab

### 수정 (오브젝트 단위)

**Item_Node (...1100)**
- 전: m_IsActive 1, 컴포넌트 4개(RT/CanvasRenderer/Image/Button)
- 후: m_IsActive 0(런타임 Instantiate 템플릿으로 전환), UIToggleButton(...1105) 컴포넌트 추가(GoOn→Image_Unlocked, GoOff→Group_Cost)

**Text_BranchHeader (...1090)**
- 전: m_IsActive 1
- 후: m_IsActive 0(런타임 Instantiate 템플릿으로 전환)

**ScrollView (...1061, RectTransform)**
- 전: anchoredPosition(0,-60), sizeDelta(0,-200)
- 후: anchoredPosition(0,-92), sizeDelta(0,-264) — Panel_Tabs(h56) + 여백 삽입분 반영

**UIMetaTree 루트 RectTransform(...1001)**
- m_Children에 Panel_Tabs(...2001) 삽입 (Panel_Top과 ScrollView 사이)

**Panel_Tabs (...2000, 신규)**
- RectTransform(상단 스트레치, h56, y-152) + HorizontalLayoutGroup + ToggleButtonList(라디오모드, KeepOneSelected)
- 자식: Item_Tab(...2010, 템플릿, 기본 비활성) — Text_On(...2020)/Text_Off(...2030) 두 TMP 라벨을 GoOn/GoOff로 전환. 라벨 문자열/색상은 코드(UIMetaTree.SetupBranchTabs)에서 런타임에 UIToggleButton.textOn/textOff 접근자로 세팅(프리팹엔 "TAB" placeholder) — 2026-07-18-1에서 GetComponentsInChildren 순회 방식을 폐기하고 접근자 방식으로 교체(아래 참고).

**UIMetaTree(...1900, MonoBehaviour)**
- 직렬화 필드 5개 신규 연결 (m_BranchTabs/m_Content/m_BranchHeaderTemplate/m_NodeTemplate/m_AssetBoxShard) — 상세는 위 "UIMetaTree 직렬화 필드" 섹션 참고

### 미검증
에디터 미실행 상태 YAML 직접 편집. 컴파일, 스크립트 참조 연결(missing 아님), ScrollView 재계산 좌표의 실제 픽셀 결과, HorizontalLayoutGroup 4탭 균등 배치, 템플릿 비활성화가 VerticalLayoutGroup/ContentSizeFitter에 미치는 영향 — 전부 에디터 확인 필요.

---

## 2026-07-18-1

### 개요
`UIMetaTree.cs`가 `transform.Find("문자열")`로 자식을 찾던 걸 사용자가 지적 — Item_Node에 참조 보관용 컴포넌트(MetaTreeNodeItem)를 추가해 직렬화 필드로 대체. 탭 쪽은 프리팹 변경 없이 UIToggleButton.textOn/textOff 접근자 추가만으로 해결(코드 쪽 .claude/class/UIToggleButton.md, UIMetaTree.md 참고).

### 파일
- Assets/Resources/Prefabs/UI/UIMetaTree.prefab

### 수정 (오브젝트 단위)

**Item_Node (...1100)**
- 전: 컴포넌트 5개(RT/CanvasRenderer/Image/Button/UIToggleButton)
- 후: MetaTreeNodeItem(...1106) 컴포넌트 추가 — m_NameText→...1123(Text_Name TMP), m_CostText→...1139(Text_Cost TMP), m_IconImage→...1113(Image_Icon Image)
- m_Component 목록에 `{fileID: 9001000000000001106}` 추가

### 미검증
컴파일, MetaTreeNodeItem 스크립트 참조 연결(missing 아님) 확인 필요.

---

## 2026-07-18-2

### 개요
사용자 지적: Item_Node에 UIToggleButton(...1105)과 MetaTreeNodeItem(...1106)을 형제 컴포넌트로 같이 붙일 이유가 없다, MetaTreeNodeItem이 UIToggleButton을 상속받아야 한다. 맞는 지적이라 반영 — 상세는 [MetaTreeNodeItem.md](../class/MetaTreeNodeItem.md) 참고.

### 파일
- Assets/Resources/Prefabs/UI/UIMetaTree.prefab

### 수정 (오브젝트 단위)

**Item_Node (...1100)**
- 전: 컴포넌트 6개(RT/CanvasRenderer/Image/Button/UIToggleButton(...1105)/MetaTreeNodeItem(...1106))
- 후: 컴포넌트 5개(RT/CanvasRenderer/Image/Button/MetaTreeNodeItem(...1106)) — UIToggleButton(...1105) 블록 삭제, 그 8개 필드(m_SelectButton/m_GoOn/m_GoOff/m_ImageOn/m_ImageOff/m_TextOn/m_TextOff/m_LockObject)를 ...1106(MetaTreeNodeItem) 블록에 병합
- m_Component 목록에서 `{fileID: 9001000000000001105}` 제거

**UIMetaTree(...1900, MonoBehaviour)**
- m_NodeTemplate: `{fileID: 9001000000000001105}` → `{fileID: 9001000000000001106}` (더 이상 별도 UIToggleButton 컴포넌트가 없으므로 MetaTreeNodeItem 컴포넌트를 직접 가리킴)

### 미검증
컴파일, 프리팹 컴포넌트 연결(missing 아님) 확인 필요.

---

## 2026-07-19-0

### 개요
UIAssetBox가 재화 옵저버로 자동 갱신되도록 바뀌면서(.claude/class/UIAssetBox.md 2026-07-19-0) UIMetaTree 스크립트의 m_AssetBoxShard 필드가 삭제됨 — 프리팹의 대응 직렬화 라인도 제거.

### 파일
- Assets/Resources/Prefabs/UI/UIMetaTree.prefab

### 수정 (오브젝트 단위)

**UIMetaTree(...1900, MonoBehaviour)**
- 전: `m_AssetBoxShard: {fileID: 9001000000000001044}` 포함
- 후: 해당 라인 제거 (AssetBox_Shard 오브젝트/UIAssetBox 컴포넌트 자체는 유지 — m_CurrencyType=Shard 직렬화 값으로 스스로 옵저버 등록)

### 미검증
에디터 미실행 상태 편집. 파싱/재화 차감 시 표시 갱신 확인 필요.
