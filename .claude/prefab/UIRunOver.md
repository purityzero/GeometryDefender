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

---

## 2026-07-15-2

### 개요
루트에 동명 컴포넌트(UIRunOver, UIBase 상속 빈 스텁) 부착 + UITable(Resources/Table/UITable.csv)에 경로 등록.

### 수정 (오브젝트 단위)

**UIRunOver (루트)**
- 전: RectTransform만
- 후: RectTransform + UIRunOver(MonoBehaviour, fileID 뒤 4자리 1900)

### 미검증
에디터에서 스크립트 연결(Missing 아님) 확인 필요.

---

## 2026-07-22-0

### 개요
사용자 요청("UIRunOver도 만들어줘") — [[UIRunOver]](../class/UIRunOver.md)가 실제 로직을 갖게 되면서, 이 프리팹의 텍스트 필드/버튼을 그 컴포넌트에 실제로 연결. Unity MCP `manage_prefabs`(open_prefab_stage)로 편집.

### 수정 (오브젝트 단위)

**UIRunOver (루트, fileID ...1900 컴포넌트)** — 직렬화 필드 5개 연결
| 필드 | 연결 대상 (fileID 뒤 4자리) |
|---|---|
| `m_ScoreText` | Text_Score(...1043) |
| `m_BestText` | Text_Best(...1053) |
| `m_StatsValueText` | Text_StatsValue(...1083) |
| `m_ShardsEarnedText` | Text_ShardsEarned(...1113) |
| `m_ShardsTotalText` | Text_ShardsTotal(...1123) |

**버튼 3개** — `m_OnClick.m_PersistentCalls`에 UIRunOver 루트(...1900)를 대상으로 하는 Persistent Call 추가
| 버튼 | 연결된 메서드 |
|---|---|
| Btn_MetaTree(...1134) | `OnClickMetaTree` |
| Btn_Restart(...1154) | `OnClickRestart` |
| Btn_MainMenu(...1174) | `OnClickMainMenu` |

### 검증
`manage_prefabs.save_prefab_stage` 후 YAML을 직접 grep해 5개 필드 fileID와 3개 버튼의 `m_Target`/`m_MethodName`이 의도대로 저장된 것 확인. Play Mode에서 `Btn_MetaTree.onClick.Invoke()`를 실제로 호출해 `UIMetaTree`가 열리는 것까지 실측(상세는 [[UIRunOver]](../class/UIRunOver.md) 2026-07-22-0 참고).

### 참고 — MCP로 컴포넌트 참조 값을 넣을 때
`manage_components.set_property`의 object 참조 값은 **런타임 instance ID**(정수)만 받고, 프리팹 YAML의 fileID는 그대로 못 씀 — 이 문서 상단 계층 표의 fileID는 YAML을 읽을 때 대조용이고, 실제 MCP 호출 시에는 `find_gameobjects`/컴포넌트 리소스로 조회한 instance ID를 써야 한다. UnityEvent(`m_OnClick`) 같은 필드는 `{"m_PersistentCalls": {"m_Calls": [{"m_Target": <instanceID>, "m_MethodName": "...", "m_Mode": 1, "m_CallState": 2}]}}` 형태로 통째로 넘기면 정상 반영됨(직접 실험으로 확인, 공식 문서화된 스키마는 아님).
