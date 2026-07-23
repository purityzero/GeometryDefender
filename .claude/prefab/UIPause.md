# UIPause (Assets/Resources/Prefabs/UI/UIPause.prefab)

연관 스크립트: [[UIPause]](루트 부착 — **2026-07-23-1부터 실제 로직 구현됨**), [[UIText]](정적 라벨 로컬라이즈), [[UIInGameHUD]](Btn_Pause가 이 팝업을 여는 호출처)
중첩 프리팹: 없음
기획 근거: Assets/Design/07_ui.html 화면 6 (FIG-07-F · PAUSE MENU)

## 개요
일시정지 메뉴 오버레이 프리팹. 루트 Canvas 없음(풀스트레치) — 씬 Canvas 아래 UIManager 생성 전제.
로드 경로: `Prefabs/UI/UIPause` / 프리팹 guid: 4c8e2a6f9b1d4f7ea3c5e7b9d1a36f84

## 계층 구조 (fileID 대역 9005000000000001XXX/2XXX, 뒤 4자리 표기)
```
UIPause (...1001)                    — RectTransform 풀스트레치, UIPause 컴포넌트(...1900) 부착
├─ Image_Dim (...1011)               — 딤 배경 a0.85, RaycastTarget ON
├─ Text_Title (...1021)              — "PAUSED" 시안 36 볼드, 자간 10, y-160 — UIText(...2001, key: PauseTitle)
├─ Btn_Resume (...1031)              — 시안 배경 400×60, y+140, OnClick→OnClickResumeButton / Text_Resume(...1043) — UIText(...2002, key: PauseResumeButton)
├─ Btn_Restart (...1051)             — 다크 배경 400×60, y+64, OnClick→OnClickRestartButton / Text_Restart(...1063) — UIText(...2003, key: RunOverRestartButton, [[UIRunOver]] 키 재사용)
├─ Btn_MainMenu (...1071)            — 400×60, y-12, OnClick→OnClickMainMenuButton / Text_MainMenu(...1083) — UIText(...2004, key: RunOverMainMenuButton, [[UIRunOver]] 키 재사용)
├─ Btn_Sound (...1091)               — 400×60, y-88, OnClick→OnClickSoundButton / Text_Sound(...1103, "사운드: ON/OFF" — UIText 없음, 코드가 매 프레임 아니라 클릭/Show 시 조합해서 대입, m_SoundText 필드로 직접 참조)
├─ Btn_EnemyDamageText (2026-07-23 추가) — 400×60, y-164, OnClick→OnClickEnemyDamageTextButton / Text_EnemyDamageText("적군 데미지 표시: ON/OFF" — Btn_Sound와 동일 패턴, m_EnemyDamageTextText 필드로 직접 참조)
├─ Btn_AllyDamageText (2026-07-23 추가)  — 400×60, y-240, OnClick→OnClickAllyDamageTextButton / Text_AllyDamageText("아군 데미지 표시: ON/OFF" — 동일 패턴, m_AllyDamageTextText 필드로 직접 참조)
├─ Group_Build (...1111)             — Image #12121C 박스, 하단 y+80 h220 (좌우 70 여백). Text_BuildList(m_BuildText)는 2026-07-23부터 앱 버전 대신 "타겟팅: X / 획득한 카드: ..." 실제 빌드 정보 표시(UIPause.md 2026-07-23-3 참고).
└─ Text_Version (2026-07-23 추가)    — 화면 우하단 코너 footnote, fontSize 12 우측 정렬, `Application.version` 표시(Group_Build에서 밀려난 버전 텍스트의 새 자리). m_VersionText 필드로 연결.
   ├─ Text_BuildLabel (...1121)      — "CURRENT BUILD" #606078 — UIText(...2005, key: PauseBuildLabel)
   └─ Text_BuildList (...1131)       — "-" #A0A0B8 → Show() 시 `Application.version`으로 대입(m_BuildText 필드), UIText 없음(정적 라벨이 아니라 시스템 값)
```
루트 UIPause 컴포넌트 필드: `m_SoundText` → Text_Sound(...1103), `m_BuildText` → Text_BuildList(...1133).

## 설계 메모
- 사운드 토글은 코드에서 Text_Sound를 "사운드: ON"/"사운드: OFF"로 교체하는 방식(2026-07-23-1, `SettingsSoundLabel`+`SettingsOn`/`SettingsOff` 키 조합) — 처음 설계 메모의 "2-상태 토글형 버튼 패턴"대로 단일 Button + 텍스트 전환 방식 그대로 구현. `UIToggleButton`(GoOn/GoOff 자식 오브젝트로 아이콘까지 바꾸는 정식 토글 컴포넌트, [[UISetting]]이 사용 중)은 이번엔 안 씀 — 하이어라키를 더 늘리지 않고 기존 Btn_Sound 구조(플레인 Button+Text 1개)를 그대로 살리는 쪽을 택함.
- 🔇/🏠/▶ 이모지는 폰트 미포함 위험으로 제외.
- Restart/MainMenu 라벨은 [[UIRunOver]]의 동일 의미 버튼과 완전히 같은 문구/동작이라 새 StringTable 키를 만들지 않고 `RunOverRestartButton`/`RunOverMainMenuButton`을 재사용(로컬라이제이션 키 관리 원칙 — 동일 문구 새 키 금지).

---

## 2026-07-14-5

### 개요
신규 생성. 07_ui.html 화면 6 기준 일시정지 메뉴 프리팹 구성요소 제작.

### 파일
- Assets/Resources/Prefabs/UI/UIPause.prefab (+.meta)

### 미검증
에디터 미실행 YAML 직접 작성. 파싱/버튼 스택 배치 확인 필요.

---

## 2026-07-15-2

### 개요
루트에 동명 컴포넌트(UIPause, UIBase 상속 빈 스텁) 부착 + UITable(Resources/Table/UITable.csv)에 경로 등록.

### 수정 (오브젝트 단위)

**UIPause (루트)**
- 전: RectTransform만
- 후: RectTransform + UIPause(MonoBehaviour, fileID 뒤 4자리 1900)

### 미검증
에디터에서 스크립트 연결(Missing 아님) 확인 필요.

---

## 2026-07-23-1

### 개요
사용자 요청("UIPause 만들어줘, StringTable 다 참고해서 만들고") — 빈 스텁이던 [[UIPause]]를 실제 로직으로 구현하고, 정적 라벨 5개(Title/Resume/Restart/MainMenu/BuildLabel)에 [[UIText]]를 부착해 로컬라이즈. 상세는 [[UIPause]] 참고.

### 파일
- Assets/Resources/Prefabs/UI/UIPause.prefab
- Assets/Scripts/UI/UIPause.cs
- Assets/Resources/Table/StringTable.csv (PauseTitle/PauseResumeButton/PauseBuildLabel 3개 키 신규 추가, Id 53~55)

### 수정 (오브젝트 단위)
**Text_Title/Text_Resume/Text_Restart/Text_MainMenu/Text_BuildLabel**: 각각 UIText MonoBehaviour(...2001~2005) 추가, `m_Text`는 자기 자신의 TMP, `m_Key`는 위 "계층 구조" 참고.
**Btn_Resume/Btn_Restart/Btn_MainMenu/Btn_Sound**: `m_OnClick.m_PersistentCalls.m_Calls`가 비어있던 것 → 각각 UIPause의 대응 메서드 1건씩 연결.
**UIPause(루트, ...1900)**: `m_SoundText`→Text_Sound(...1103), `m_BuildText`→Text_BuildList(...1133) 필드 연결.

### 미검증
Unity MCP 미연결 상태라 YAML 직접 편집으로 진행 — 컴파일 에러 0건인지, 5개 버튼/라벨이 실제로 정상 동작(Resume=닫기+timeScale 복구, Restart/MainMenu=씬 전환, Sound=토글+텍스트 갱신)하는지, 언어 전환 시 UIText 5개가 전부 갱신되는지 확인 필요.

---

## 2026-07-23-2

### 개요
사용자 요청("UIPause에도 넣어줘") — [[UISetting]]에 추가한 적군/아군 데미지 텍스트 표시 토글을 이 프리팹에도 추가. 상세는 [[UIPause]] 2026-07-23-2 참고. Unity MCP 연결 상태라 `manage_prefabs`(open/save/close_prefab_stage) + `manage_gameobject`(duplicate) + `manage_components`(set_property)로 진행.

### 수정 (오브젝트 단위)
**Btn_Sound → duplicate**: `Btn_EnemyDamageText`(y=-164), `Btn_AllyDamageText`(y=-240) 생성. 자식 텍스트 `Text_Sound` clone을 각각 `Text_EnemyDamageText`/`Text_AllyDamageText`로 리네임.
**Btn_EnemyDamageText.Button.m_OnClick**: `OnClickEnemyDamageTextButton` 연결(m_Mode=1, m_CallState=2).
**Btn_AllyDamageText.Button.m_OnClick**: `OnClickAllyDamageTextButton` 연결.
**UIPause(루트)**: `m_EnemyDamageTextText`→Text_EnemyDamageText, `m_AllyDamageTextText`→Text_AllyDamageText 필드 연결.

### 검증
`save_prefab_stage` 후 실제 YAML grep으로 `m_OnClick`/`m_MethodName`/필드 연결 전부 재확인(리소스 조회는 UnityEvent 내부를 못 보여주므로 — PREFAB.MD 문서화된 원칙). Play Mode 실측 — `Btn_Pause`→일시정지 팝업에서 신규 두 행 정상 렌더링, 실제 버튼 클릭으로 토글 동작 확인(ON→OFF→ON). 콘솔 에러 0건.
