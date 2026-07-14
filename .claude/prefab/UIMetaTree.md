# UIMetaTree (Assets/Resources/Prefabs/UI/UIMetaTree.prefab)

연관 스크립트: UIMetaTree(루트, UIBase 상속 빈 스텁), UIAssetBox
중첩 프리팹: 없음
기획 근거: Assets/Design/07_ui.html 화면 5 (FIG-07-E · META TREE), 05_meta.html (노드/비용 데이터)

## 개요
메타 트리 화면 UI 프리팹. 사용자 요청으로 **구성요소(계층/컴포넌트)만** 제작 — 노드 목록 채우기/해금 로직 스크립트는 미작성.
로드 경로: `Prefabs/UI/UIMetaTree` (ResUtil/UIManager 경유 Resources 로드 전제)
프리팹 guid: 5d7a3f1c9b2e4d6fa8c0e5b7d3f19a46

## 계층 구조
```
UIMetaTree (fileID ...1000)                    — RectTransform(풀스트레치) + UIMetaTree(빈 스텁, ...1900) — 씬 Canvas 아래에 UIManager가 생성하는 전제
├─ Image_BG (...1010)                          — Image #0A0A0F 풀스트레치, RaycastTarget ON (배경 클릭 차단)
├─ Panel_Top (...1020)                         — RectTransform (상단 스트레치, h80, y-64 세이프에어리어)
│  ├─ Btn_Back (...1030)                       — 투명 Image(RaycastTarget ON) + Button (타겟=자기 Image)
│  │  └─ Text_Back (...1035)                   — TMP "< BACK" #A0A0B8
│  └─ AssetBox_Shard (...1040)                 — Image(시안 10% 배경) + UIAssetBox(m_CurrencyType=1 Shard, m_AmountText→...1053)
│     ├─ Image_Icon (...1045)                  — shape_diamond 시안 24×24
│     └─ Text_Amount (...1050, TMP comp 1053)  — TMP "0" 시안 볼드, 우측 정렬
└─ ScrollView (...1060)                        — ScrollRect(세로만, Elastic, Content→...1081, Viewport→...1071)
   └─ Viewport (...1070)                       — RectMask2D
      └─ Content (...1080)                     — VerticalLayoutGroup(패딩 24/24/8/24, 간격 10, 폭 스트레치) + ContentSizeFitter(세로 Preferred)
         ├─ Text_BranchHeader (...1090)        — TMP "STARTING POWER" 시안, h40 — 줄기 헤더 템플릿
         └─ Item_Node (...1100)                — Image #12121C 배경(RaycastTarget ON) + Button(타겟=배경) , h72 — 노드 아이템 템플릿
            ├─ Image_Icon (...1110)            — shape_hexagon 시안 28×28
            ├─ Text_Name (...1120)             — TMP "M-101 HP +10" #EBEBF5
            ├─ Group_Cost (...1130)            — 잠김 상태 표시 (우측)
            │  ├─ Image_CostIcon (...1132)     — shape_diamond 시안 16×16
            │  └─ Text_Cost (...1136)          — TMP "20" #FFD600 우측 정렬
            └─ Image_Unlocked (...1140)        — shape_diamond 그린(#00FF88) 24×24, **기본 비활성** (해금 상태 표시)
```
- fileID 대역: 9001000000000001000~1143 (위 표기는 뒤 4자리)

## 참조 GUID (실파일 대조 완료)
- shape_diamond: 4181f9c255bc7cb42b0496069bb94b75 (sprite fileID -5073088034143875061)
- shape_hexagon: 19f6ee037f06fd44392d7663600da747 (sprite fileID -6796216251724203947)
- UIAssetBox.cs: 2d9d50f8f6badac479e1b20a585a1ae3
- TMP 폰트: LiberationSans SDF 8f586378b4e144a9851e7b34d9b748ee
- ScrollRect/ContentSizeFitter/RectMask2D guid는 uGUI 패키지 캐시에서 대조

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
