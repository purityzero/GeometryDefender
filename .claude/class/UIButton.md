# UIButton

## 연관 클래스
- Button (UnityEngine.UI, 부모)
- BaseScene (`PlaySfx` 호출 대상, [[BaseScene]] 참고)

## 개요
`Button`을 상속한 확장 클래스 — 클릭 시 지정된 SoundTable 키로 SFX를 재생한다([[PREFAB]] "확장 클래스 관례" 참고, 신규 UI 버튼은 표준 `Button` 대신 이걸 사용).

## 현재 상태
- 경로: Assets/Scripts/UI/UIButton.cs
- `[SerializeField] private string m_ClickSoundKey = "ButtonClick";` — 기본값은 공용 클릭음 키. 빈 문자열로 비우면 소리 없음(특수 버튼용).
- `Awake()`에서 `onClick.AddListener(OnClickPlaySound)` 등록 → 클릭마다 `BaseScene.Current?.PlaySfx(m_ClickSoundKey);` 호출.

## 작업 내역

### 2026-07-29-0 — 신규 생성
사용자 요청("UIButton이라고 만들어서, 거기에 효과음 추가할 수 있게 string 값 넣을 수 있는거 만들어줘 Button 상속 받아서 만들어도 됨"). 최초엔 전역 레이캐스트 리스너(UIClickSoundPlayer)로 접근했으나 사용자가 반려("이거는 좀 아닌거 같아") — 실제 재생 로직은 [[BaseScene]].PlaySfx()로 옮기고, 이 클래스는 사운드 키 보관 + 클릭 트리거 역할만 한다.

### 2026-07-29-1 — 기존 버튼 전체에 적용 (스크립트 참조 guid 교체)
사용자 요청("실제 버튼들한테 씌워줄래?"). MCP 미사용(사용자 지시 유지) 상태라 [[PREFAB]] "YAML 직접 편집 vs Unity MCP" 경로를 따름 — 이 경우는 "새 컴포넌트 부착"이 아니라 **이미 있는 `Button` 컴포넌트의 스크립트 참조(guid)만 `UIButton`으로 교체**하는 것이라, PREFAB.MD의 "확장 클래스는 필드 구조가 베이스와 대부분 동일하므로 guid만 교체하면 나머지 직렬화 필드는 그대로 재사용 가능" 원칙에 정확히 해당 — `m_Component` 목록 변경이나 fileID 신설 없이 각 Button 블록의 `m_Script` 줄만 치환.

- 프로젝트 UI 프리팹(`Assets/Resources/Prefabs/UI/*.prefab`) 전체 + `TitleScene.unity`에서 `UnityEngine.UI.Button`의 실제 script guid(`4e29b1a8efbd4b44bb3f3716e73f07ff`, `UISetting.prefab`에서 실측 확인)를 grep으로 전수 조사 — 총 10개 파일, 57개 Button 컴포넌트 발견(UICardDraft 3 / UICheatWindow 22 / UIDifficultySelect 5 / UIErrorWindow 1 / UIInGameHUD 2 / UIMetaTree 3 / UIPause 6 / UIRunOver 3 / UISetting 8 / TitleScene.unity 4). `InGameScene.unity`는 0건(씬에 직접 배치된 버튼 없음, 전부 프리팹 인스턴스로만 존재).
- `UIButton.cs`는 이번 세션에 신규 생성한 파일이라 `.meta`가 없었으나, **백그라운드에서 Unity 에디터가 이미 열려 있어 파일 생성 시점에 자동으로 `.meta`(guid: `de176b997a6bbb5479a5903510920639`)를 생성**해둔 상태였음 — 직접 새 guid를 만들지 않고 이 실제 값을 그대로 사용(추측 금지 원칙).
- 57개 파일 각각에서 `guid: 4e29b1a8efbd4b44bb3f3716e73f07ff` → `guid: de176b997a6bbb5479a5903510920639`로 전량 치환. `m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Button`(missing-script 표시용 폴백 텍스트, 스크립트가 정상 resolve되면 무시됨)는 최소 변경 원칙에 따라 그대로 둠.
- 검증: 치환 전/후 각 파일의 guid 카운트가 정확히 일치(0/57 → 57/0)함을 grep으로 확인.

### 미검증
컴파일/Play Mode 미실행 — 사용자 지시("MCP 연결하지말고 나 불러")에 따라 MCP 자동 검증 없이 직접 확인 대기 중. 특히 (1) 모든 버튼이 실제로 클릭음을 내는지, (2) 기존에 `Button`에 연결돼 있던 `m_OnClick` Persistent Call/`m_TargetGraphic` 등 직렬화 데이터가 guid 교체 후에도 인스펙터에서 그대로 보이는지 실제 에디터에서 확인 필요.
