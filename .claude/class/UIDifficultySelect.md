# UIDifficultySelect

연관 클래스: [[UIToggleButton]](Glory, 각 난이도 항목의 잠금/클릭 처리), `UIPopup`(부모), `DifficultyManager`(`eDifficultyLevel`/`SelectedDifficulty`), `PlayerManager`(`IsDifficultyUnlocked`), `TitleScene`(`OnClickPlayButton()`에서 호출), `SceneManager`(난이도 확정 후 InGameScene 전환), `StringTable`("ToastDifficultyLocked")

## 개요
Title의 Play 버튼을 누르면 바로 InGameScene으로 가지 않고, 이 팝업에서 난이도(Normal/Hard/Hell/Infinite)를 먼저 고르게 한다. 순차 언락 구조([[difficulty-progression]] 스펙)를 반영해 아직 해금 안 된 난이도는 잠금 아이콘 표시 + 클릭 불가.

## 경로
- 스크립트: `Assets/Scripts/UI/UIDifficultySelect.cs`
- 프리팹: `Assets/Resources/Prefabs/UI/UIDifficultySelect.prefab`
- 테이블: `UITable.csv`(Id=6, Popup), `StringTable.csv`(Id=3, `ToastDifficultyLocked`)

## 구조
```csharp
public class UIDifficultySelect : UIPopup
{
    [SerializeField] private UIToggleButton m_NormalItem;
    [SerializeField] private UIToggleButton m_HardItem;
    [SerializeField] private UIToggleButton m_HellItem;
    [SerializeField] private UIToggleButton m_InfiniteItem;
    ...
}
```
- 별도의 `DifficultyButtonItem` 서브클래스를 만들지 않고 **Glory의 `UIToggleButton`을 그대로 필드 타입으로 사용** — 각 항목 라벨("NORMAL"/"HARD"/...)이 정적이라(런타임에 안 바뀜) `MetaTreeNodeItem`처럼 이름을 코드로 세팅해줄 필요가 없어, 서브클래스 자체가 불필요했음(재사용 우선/단순함 원칙).
- `Show()`에서 4개 항목 전부 `SetupItem()` 호출 → `PlayerManager.instance.IsDifficultyUnlocked(_level)` 조회 → `UIToggleButton.SetData(true, callback)`(항상 on 상태로 표시 — 토글이 아니라 "선택 즉시 진행"이라 on/off 왕복 개념이 없음) + `SetLock(isUnlocked == false)`.
- 클릭 시(`OnClickDifficulty`): 잠긴 상태면(이론상 `SetLock`이 이미 버튼을 `interactable=false`로 막아서 여기까지 안 오지만, [[UIMetaTree]].OnClickNode()와 동일하게 방어적으로 재확인) 토스트 표시 후 종료. 언락 상태면 `DifficultyManager.SelectedDifficulty = _level;` 세팅 후 `SceneManager.instance.NextScene(EScene.InGameScene.ToString());`.

## 프리팹 계층 구조
```
UIDifficultySelect (RectTransform 풀스트레치, UIDifficultySelect)
├── Image_BG (풀스트레치, 배경색 (0.039,0.039,0.058,1) — [[UIMetaTree]]와 동일)
├── Panel_Top (상단 바, anchor top-stretch, height 80, y=-64 — UIMetaTree의 Panel_Top과 동일 레이아웃)
│   └── Btn_Back (좌측, anchor(0,0.5), pos(24,0), size(140,56) — UIMetaTree.Btn_Back과 동일)
│       ├── Image(투명, alpha=0, raycastTarget — 클릭 판정 전용 "투명 클릭 영역")
│       ├── Button(targetGraphic=위 Image, onClick → UIDifficultySelect.Close() Persistent Call)
│       └── Text_Back ("< BACK", 회색 #a0a0b8, fontSize 24, 좌측 정렬 — UIMetaTree.Text_Back과 동일)
├── Text_Title ("SELECT DIFFICULTY", y=-170)
├── Item_Normal (Button+UIToggleButton+Image, 배경색 cyan #00e5ff, y=-260)
│   ├── Text_Name ("NORMAL", m_GoOn 겸용 — 항상 표시)
│   ├── Off (빈 오브젝트, m_GoOff 겸용 — 실제로 안 쓰임, SetToggle의 null 참조 방지용)
│   └── Image_Lock (m_LockObject, 잠금 아이콘 — 아래 참고)
├── Item_Hard (동일 구조, 노란색 #ffd600, y=-360)
├── Item_Hell (동일 구조, 빨간색 #ff3355, y=-460)
└── Item_Infinite (동일 구조, 마젠타 #ff00aa, y=-560)
```

### Back 버튼 — 처음엔 자체 방식(우상단 ✕ 닫기)으로 만들었다가 전면 교체
사용자 지적("MetaTree와 똑같은 UI로 Back버튼 MetaTree처럼 구현해야지")으로, 최초 구현(우상단 `Btn_Close` + "✕" 텍스트, 임의 배치)을 전부 걷어내고 **`UIMetaTree.prefab`의 `Panel_Top`/`Btn_Back`/`Text_Back` 구조를 그대로 복제**했다 — 좌표(anchor/anchoredPosition/sizeDelta), 색상(#a0a0b8), 폰트 크기(24), 클릭 메커니즘(투명 Image 히트 영역 + `Close()` Persistent Call)까지 전부 동일. 새 UI를 만들 때 프로젝트에 이미 확립된 화면 진입/이탈 패턴(여기선 Popup의 Back 버튼)이 있으면 그 구조를 그대로 재사용해야 하고, 임의로 다른 디자인(다른 위치/다른 라벨/다른 메커니즘)을 새로 만들면 안 된다 — 이번 건으로 확정된 교훈.

## 잠금 아이콘 (중요 — 기존 에셋 재사용)
- **`Assets/Resources/Image/UI/unlock_open_128.png`**(guid `6d1792c3ca2ca4d4b84de135783558c5`)를 재사용 — 프로젝트에 이미 있던 잠금 관련 유일한 이미지 에셋(사용자 지시로 확인 후 반영, 2026-07-22). 처음엔 `TMP "🔒 LOCKED"` 텍스트로 임시 구현했다가, 사용자가 "Unlock 이미지 있어서 사용하라고 했는데 안 했다"고 지적해 이 스프라이트로 교체.
- 각 Item의 `Image_Lock`(원래 `Text_Lock`이라는 TMP 오브젝트였던 것을 컴포넌트 교체 — TMP 제거 + Image 추가 + 이름 변경)에 `m_Sprite`로 연결. 오른쪽 정렬(anchor 1,0.5), 크기 32×40.
- `UIToggleButton.SetLock(bool)`이 이미 `m_LockObject.SetActive(_isLocked)` + 버튼 `interactable` 토글을 전부 처리 — 이 프리팹은 그 `m_LockObject` 슬롯에 위 아이콘 오브젝트를 연결하기만 하면 됨(로직 재사용, 신규 코드 없음).

## 씬 연동
`TitleScene.OnClickPlayButton()`: `SceneManager.instance.NextScene(...)` 직접 호출 → `UIManager.instance.Get<UIDifficultySelect>();`로 변경(2026-07-22). `TitleScene.unity`(씬 파일)는 건드리지 않고 `TitleScene.cs`(스크립트)만 수정 — 사용자 승인 하에 진행([[TitleScene]] 참고).

## 프리팹 제작 방식 (참고용 — 향후 유사 작업 시)
Unity MCP가 연결된 상태에서 완전히 새로운 프리팹을 헤드리스로 제작한 절차:
1. 임시 씬(`Assets/_ScratchUIBuild.unity`, 작업 후 삭제) 생성 → 루트 GameObject 1개 생성 → `manage_prefabs.create_from_gameobject`로 프리팹화.
2. `manage_prefabs.modify_contents`(헤드리스, 열린 스테이지 없이)로 `create_child`(배열 지원 — 같은 부모의 형제 여러 개를 한 번에 생성 가능)와 `component_properties`(대상은 인스턴스ID 대신 **이름 또는 계층 경로 문자열**, 예: `"Item_Normal/Text_Name"`)를 사용해 전체 구조와 레이아웃/텍스트를 구성.
3. 컴포넌트 간 상호 참조(Button.targetGraphic, UIToggleButton의 각 슬롯, 스크립트의 필드)는 헤드리스 모드로 불가 — `open_prefab_stage`로 열어 `find_gameobjects`/리소스 조회로 **살아있는 인스턴스ID**를 얻은 뒤 `manage_components.set_property`로 연결, `save_prefab_stage` → `close_prefab_stage`.
4. `Button`에 `Image`를 추가하면 Unity가 targetGraphic을 자동으로 채워줌(수동 연결 불필요했음). 반대로 `Button`/커스텀 스크립트만 단독으로 `components_to_add`하면 `Transform`(RectTransform 아님)으로 생성됨 — 같은 오브젝트에 `Image`/TMP 등 RectTransform을 요구하는 컴포넌트를 추가해야 자동 업그레이드됨.
5. 오브젝트 리네임은 `manage_gameobject.modify`가 비활성(inactive) 오브젝트를 못 찾는 경우가 있어, 그럴 땐 `execute_code`로 `EditorUtility.InstanceIDToObject(id).name = "..."` 직접 호출.

## 검증 (2026-07-22, Play Mode)
Title→Btn_Play 실제 흐름:
- 팝업 정상 표시, Normal/Hard(이전 세션에 미리 해금된 상태)는 `interactable=true`, Hell/Infinite는 `false` + 잠금 아이콘(`unlock_open_128_0`) 표시 확인.
- Normal 버튼 실제 클릭(`Button.onClick.Invoke()`) → `DifficultyManager.SelectedDifficulty=Normal` 설정 → InGameScene 전환 → `DifficultyManager.Current.currentDifficulty=Normal` 확인.
- **Back 버튼 교체 후 재검증**: `Btn_Back.onClick.Invoke()` → 팝업 `activeInHierarchy=false`(정상 닫힘) 확인. 이후 Btn_Play로 재오픈 → Normal 클릭 → InGameScene 전환까지 재확인(교체가 기존 흐름을 깨지 않았음).
- 콘솔 에러 0건.
