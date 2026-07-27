# UICheatWindow (Assets/Resources/Prefabs/UI/UICheatWindow.prefab)

연관 스크립트: [[UICheatWindow]] (루트 부착)
중첩 프리팹: 없음

## 개요
`Assets/Editor/QA/*` 에디터 전용 툴들의 런타임(빌드 포함) 이식판 치트 창. 루트 Canvas 없음(풀스트레치) — `UIManager`가 PopupCanvas 밑에 생성. 로드 경로: `Prefabs/UI/UICheatWindow` / 프리팹 guid: `5078efff03d94f4cb119d8c6b2e0d1d3`.

fileID 대역: `9010000000000001XXX`(뒤 4자리 표기, 1001~1255 사용). 이 프리팹은 Unity MCP 미연결 상태에서 세션 스크래치패드의 PowerShell 생성 스크립트(`gen_cheatwindow.ps1`)로 만들어졌다 — 손으로 하나씩 fileID를 배정하는 대신, 자식을 먼저 만들고(각자 자기 fileID를 미리 예약) 부모를 나중에 emit하는 "reserve-then-finalize" 패턴으로 `m_Children` 목록을 항상 정확하게 채움. 생성 후 `grep`으로 중복 fileID/dangling reference 0건 확인 완료.

## 계층 구조 (뒤 4자리 표기, 정확한 값은 위 대역 기준)
```
UICheatWindow (...1001)                    — RectTransform 풀스트레치, UICheatWindow 컴포넌트(...1255)
├─ Image_BG (...1003)                      — 어두운 반투명 배경, RaycastTarget ON
├─ Text_Title (...1007)                    — "- CHEAT -"
├─ Btn_Close (...1013)                     — 우상단 72×72 "X" 버튼 → Close()
├─ ScrollView (...1017, ScrollRect)
│  └─ Viewport (RectMask2D)
│     └─ Content (VerticalLayoutGroup+ContentSizeFitter, 패딩 24/24/16/32, spacing14)
│        ├─ Text_Header "TIME SCALE"
│        ├─ Row (HorizontalLayoutGroup) — Btn_TimeScale_1x~5x (m_TimeScaleButtons[5])
│        ├─ Text_Header "current: 1.0x" (m_TimeScaleText — 런타임에 텍스트 갱신)
│        ├─ Text_Header "WAVE SKIP"
│        ├─ Row — Btn_QuickSkip_+10s/+30s/+60s (m_QuickSkipButtons[3])
│        ├─ Container_List (VLG+CSF) — Btn_WaveTemplate(비활성, m_WaveButtonTemplate) — 코드가 WaveTable 개수만큼 Instantiate
│        ├─ Text_Header "CRIT"
│        ├─ Row — Btn_CritChance / Btn_CritMultiplier
│        ├─ Text_Header "MONSTER SPAWN"
│        ├─ Row — Btn_Variant_Normal/Elite/Boss (m_VariantToggleButtons[3])
│        ├─ Row — Btn_SpawnCount_10/50/150/300 (m_SpawnCountButtons[4])
│        ├─ Text_Header "CARD APPLY"
│        ├─ ScrollView_Card (ScrollRect, 2026-07-27 vertical=true/horizontal=false로 수정 — 아래 참고)
│        │  └─ Viewport (RectMask2D)
│        │     └─ Container_List (VLG+CSF) — Row(비활성, m_CardRowTemplate) → Text_CardName + Btn_ApplyCard — 코드가 CardTable 개수만큼 Instantiate
│        ├─ Text_Header "# 메타트리" (2026-07-27 신설)
│        ├─ Row_MetaTreeTemplate (비활성, m_MetaTreeRowTemplate) → Text_Label + Btn_Unlock(자식 Text_Label="해금") — 코드가 MetaTreeTable 개수만큼 Instantiate (2026-07-27 신설)
│        └─ Row_ShardGrant — Btn_AddShard("Shard +1000", m_AddShardButton) (2026-07-27 신설)
```

## 설계 메모
- Slider/다중 상태 Toggle 컴포넌트는 프로젝트에 선례가 없어(grep 확인) 프리셋 버튼(시간배속 1x~5x, 스폰수 10/50/150/300)과 단순 색상토글 버튼(Normal/Elite/Boss)으로 대체 — 사용자 확정 사항([[UICheatWindow]] class.md 참고).
- 모든 버튼의 `m_OnClick.m_PersistentCalls.m_Calls`는 빈 배열(`[]`) — 인터랙션은 전부 `UICheatWindow.cs`의 `Show()`/`WireStaticButtons()`에서 코드로 `AddListener` 연결.
- 버튼 행(Row)은 전부 `HorizontalLayoutGroup`(ChildControlWidth/Height=1, ForceExpand=1)로 균등 분할 — 카드 행(Text+Button)도 동일 방식이라 텍스트/버튼 폭이 50:50으로 균등 분할됨(폭 비대칭 조정은 후속 작업 필요, 미검증 상태).
- Row/Container GameObject 이름이 전부 "Row"/"Container_List"로 동일(생성 스크립트가 이름을 파라미터화하지 않음) — 계층 탐색 시 부모-자식 관계와 내용(어떤 버튼이 들어있는지)으로 구분해야 함.

## 작업 내역

### 2026-07-27-0 — Variant 토글 시각 피드백 버그 + 카드 ScrollView 방향 버그 수정, 메타트리 치트 섹션 신설 (Unity MCP 사용)
사용자 리포트("치트쪽... 제대로 고르지도 못해", "ScrollView도 이상함", "메타트리도 치트 할 수 있게") — 이번엔 Unity MCP가 연결돼 있어 `open_prefab_stage`로 직접 열어 진단/수정(과거 YAML 직접 편집과 달리 실제 컴포넌트 속성을 조회하며 원인 특정).

- **Btn_Variant_Normal/Elite/Boss**: `Button.transition`을 `ColorTint`(1) → `None`(0)으로 변경. 코드(`SetVariantToggleVisual()`)가 `Image.color`를 직접 칠하는데, ColorTint가 상호작용마다 `colors.normalColor`(흰색)로 그래픽 색을 강제 재대입해 선택 상태가 시각적으로 안 보였음 — 상세 원인은 [[UICheatWindow]](class.md) 2026-07-27-6 참고.
- **ScrollView_Card**: `ScrollRect.horizontal=true, vertical=false` → `horizontal=false, vertical=true`. 카드 리스트가 `VerticalLayoutGroup`으로 세로로 쌓이는데 세로 스크롤이 꺼져있어 카드 대부분에 도달 불가능했음.
- **메타트리 섹션 신규 생성**: `manage_gameobject`(create)로 `Text_Header`("# 메타트리"), `Row_MetaTreeTemplate`(비활성, `Text_Label`+`Btn_Unlock`), `Row_ShardGrant`(`Btn_AddShard`)를 바깥 `Container_List`(`ScrollView`의 Content) 밑에 직접 생성 — Wave 목록과 동일하게 별도 내부 ScrollRect 없이 바깥 스크롤에 얹음(노드 14개 정도라 충분, 중첩 ScrollRect 추가는 버그 유발 위험만 커짐). 신규 버튼들도 `Button.transition=None`으로 생성(Variant 버그와 같은 이유 선제 방지).
- `UICheatWindow.cs`에 `m_AddShardButton`/`m_MetaTreeListContainer`/`m_MetaTreeRowTemplate` 필드 3개 wiring 완료(컴포넌트 instanceID 직접 대입, `manage_components.set_property`).

#### 검증
IDE 진단/콘솔 에러 0건. **Play Mode 확인 못함** — Play 진입 시도 중 기존에 알려진 "Play 중 재컴파일 누적 시 Text Animator/UI 초기화 영구 고장" 증상이 재현돼(타이틀 화면에 헥사곤 로고만 보이고 버튼/사각형 전부 미표시) 정상 테스트 불가 — Unity 에디터 재시작 후 재검증 필요.

### 2026-07-23-5 — Button guid 오류 수정
모든 Button 컴포넌트(20개)가 실제로는 `1a37630bea274644a85de3916ce19d91`(= `UIText`, `UnityEngine.UI.Button` 아님)로 붙어있던 버그 수정 — 올바른 guid `4e29b1a8efbd4b44bb3f3716e73f07ff`로 재생성. 상세 원인/증상은 [[UICheatWindow]](class.md) 2026-07-23-5 참고. `grep`으로 20개 전부 `EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Button` 재확인.

### 2026-07-23-4
신규 생성. 사용자 요청("지금 Tool에 있는 기능 전부 포함한 치트 창"). 상세 경위는 [[UICheatWindow]](class.md) 참고.

### 미검증
Unity MCP 미연결, 컴파일/Play Mode 확인 전혀 안 됨. 특히 ScrollView 영역 높이(제목/닫기버튼 아래 -90~-270px)가 실제 화면비에서 카드 30개+웨이브 5개를 포함한 전체 컨텐츠를 스크롤로 다 보여주는지, 버튼 균등분할 레이아웃이 보기에 괜찮은지 에디터에서 직접 확인 필요.
