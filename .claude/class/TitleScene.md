# TitleScene

## 연관 클래스
- SceneManager (Glory)
- EScene (Util.cs)
- BaseScene(부모, Glory)

## 현재 상태
- 경로: Assets/Scripts/Title/TitleScene.cs
- `OnClickPlayButton()` — 2026-07-22부터 바로 씬 전환하지 않고 `UIManager.instance.Get<UIDifficultySelect>();`로 난이도 선택 팝업을 먼저 띄움([[UIDifficultySelect]] 참고). 실제 `SceneManager.instance.NextScene(EScene.InGameScene.ToString())` 호출은 그 팝업에서 난이도를 고른 뒤 실행됨.
- `BaseScene`을 상속(2026-07-21). `OnSetup()`(protected override)에 주석 "하지마라"만 있음 — 예전에 `TableManager.instance.init()`을 여기서 호출했다가 GameManager.Awake()의 호출과 중복돼 되돌린 이력이 있음(2026-06-07 커밋). **이 메서드에 초기화 로직을 추가하지 말 것.**
- **주의(사용자 지시)**: `TitleScene.unity`(씬 파일)는 이 프로젝트에서 사용자가 직접 수동으로 편집 중이라 건드리지 말 것 — 단, `TitleScene.cs`(스크립트)는 사용자 승인 하에 수정 가능(2026-07-22 확인됨). 둘을 혼동하지 말 것.

## 작업 내역

### 2026-07-12-0
- 개요: 프로젝트 전체 스캔으로 기본 정보 문서 초기 생성 (코드 수정 없음)

---

## 2026-07-15-4

### 개요
OnClickMetatreeButton 단순화 — UIManager.Get<T>() 신설 오버로드 사용.

### 수정 (함수 단위)

**OnClickMetatreeButton()**
- 전: `UIManager.instance.Get<UIMetaTree>(TableManager.instance.GetTable<UITable>().GetRecordByName("UIMetaTree").PrefabPath);` (테이블 미로드 시 NRE)
- 후: `UIManager.instance.Get<UIMetaTree>();` (내부에서 UITable 조회, 실패 시 로그 + null)

---

## 2026-07-21-0

### 개요
사용자 요청: InGameScene/TitleScene이 공통 BaseScene을 상속받도록 구조 변경. 상세 설계는 [[BaseScene]] 참고.

### 파일
- Assets/Scripts/Title/TitleScene.cs

### 수정 (함수 단위)

**클래스 선언**
- 전: `public class TitleScene : MonoBehaviour`
- 후: `public class TitleScene : BaseScene`

**Start() → OnSetup()**
- 전: `private void Start() { //하지마라 }`
- 후: `protected override void OnSetup() { //하지마라 }` (내용 동일, 실행 시점도 동일하게 Start 단계 — BaseScene.Start()가 대신 호출)

**using 정리**
- `using UnityEngine;` 제거 — MonoBehaviour를 더 이상 직접 상속하지 않고(BaseScene 경유), 파일 내 UnityEngine 타입 직접 참조가 없어 이 변경으로 인해 미사용이 됨.

### 미검증
컴파일/에디터 미실행 상태 편집. 실제 Play Mode로 버튼 클릭 흐름 확인 필요.

---

## 2026-07-22-1

### 개요
사용자 요청("UISetting 만들어서 언어 변경할 수 있게 만들어줘") — 이전엔 빈 구현이던 `OnClickSettingsButton()`을 실제로 연결. `Btn_Settings`(TitleScene.unity)는 이미 이 메서드에 Persistent Call로 연결돼 있었음(2026-07-15 이전부터 존재 — 씬을 직접 만지지 않고도 스크립트만 채우면 되는 상태였음).

### 파일
- Assets/Scripts/Title/TitleScene.cs

### 수정
```csharp
// 전
public void OnClickSettingsButton()
{
    
}

// 후
public void OnClickSettingsButton()
{
    UIManager.instance.Get<UISetting>();
}
```
`OnClickPlayButton()`/`OnClickMetatreeButton()`과 동일 패턴.

### 검증 (2026-07-22, Play Mode)
`Btn_Settings.onClick.Invoke()` 실제 호출 → [[UISetting]] 팝업이 정상적으로 열리는 것 확인. 콘솔 에러 0건.

---

## 2026-07-23-0

### 개요
사용자 요청("TitleScene 버튼들도 StringTable 적용해줘") — TitleScene.unity에 하드코딩돼 있던 4개 버튼/라벨 텍스트("PLAY", "META TREE", " Settings", "How to Play")를 [[UIText]]로 교체. **씬 파일(TitleScene.unity) 자체를 수정하는 작업이라, 위 "건드리지 말 것" 지시에 따라 작업 전 사용자에게 재확인 — "네, 씬 파일 수정 허용 (권장)"으로 이번 건에 한해 명시적 승인받고 진행.** 이 승인은 이번 작업 한정이며, 문서 상단의 기본 원칙("씬 파일 건드리지 말 것")은 여전히 유효 — 다음에 씬 파일을 다시 손대야 할 일이 생기면 재확인할 것.

### 대상 및 처리
씬 전체에서 `TextMeshProUGUI` 6개 인스턴스 중:
- `Text_MetaTree`(META TREE) → [[UIText]] 부착, key=`RunOverMetaTreeButton`(RunOver 화면과 동일 문구라 기존 키 재사용, 신규 키 안 만듦)
- `Btn_Play` 자식 텍스트(PLAY) → key=`TitlePlayButton`(신규)
- `Btn_Settings` 자식 텍스트(" Settings") → key=`TitleSettingsButton`(세션 초반 다른 작업에서 만들어졌으나 그때는 아무 데도 연결 안 된 상태였음 — 이번에 최초로 실제 연결)
- `Btn_HowToPlay` 자식 텍스트(How to Play) → key=`TitleHowToPlayButton`(신규)
- `Text_Count`(리터럴 "0") → 스킵. 프로젝트 전체 grep 결과 어떤 코드도 참조하지 않는 죽은 UI(CLAUDE.md "죽은 UI는 키 스킵" 원칙)
- `Text_Title`("GEOMETRY\nDEFENDER") → 스킵. 게임 로고/브랜드명이라 로컬라이제이션 범위 밖으로 판단(사용자 요청 범위는 "버튼들"이지 로고가 아님)

### 파일
- Assets/Scenes/TitleScene.unity (UIText 컴포넌트 4개 부착 — Unity MCP `find_gameobjects`로 라이브 instance ID 확인 후 `manage_components`로 부착/설정, `manage_scene` action=save로 저장)
- Assets/Resources/Table/StringTable.csv (Id 51 `TitlePlayButton`, Id 52 `TitleHowToPlayButton` 추가 — `TitleSettingsButton`/`RunOverMetaTreeButton`은 기존 키 재사용)

### 부수 발견 및 수정
이 작업 도중 TableManager 부트스트랩 순서 버그를 발견해 별도로 수정함 — 상세는 [[TableManager]] 2026-07-23-0 참고(TitleScene 자신의 씬 오브젝트에 UIText를 붙이는 것 자체가 이 버그를 처음 표면화시킴).

### 검증 (2026-07-23, Play Mode)
[[TableManager]] 수정 후 재검증 — 콘솔 에러/경고 0건. 4개 텍스트 전부 `TextMeshProUGUI.text` 직접 읽기로 정상 렌더링 확인("META TREE"/"PLAY"/"Settings"/"How to Play", `StringTable.CurrentLanguage`=English 상태). `PlayerManager.instance.SetLanguage(eLanguage.Korean)` 호출로 4곳 전부 실시간 한국어 전환("메타 트리"/"플레이"/"설정"/"게임 방법") 확인. 스크린샷 대신 TMP 텍스트 직접 읽기로 검증(2026-07-22 확립된 스크린샷 플레이키니스 회피 관례 적용).

---

## 2026-07-24-0

### 개요
사용자가 실제 재현한 `UpdatableBehaviour.OnEnable()` NRE — 상세 원인/수정은 [[InGameScene]] 2026-07-24-0 참고(TitleScene도 동일 이유로 동일 수정 적용).

### 파일
- Assets/Scripts/Title/TitleScene.cs

### 수정
- 클래스 선언에 `[DefaultExecutionOrder(-1000)]` 추가.
- 2026-07-21-0에서 "미사용이라 제거"했던 `using UnityEngine;`을 이 attribute 때문에 다시 추가 — 위 "현재 상태"의 관련 서술은 이제 낡은 기록이니 참고 시 이 항목을 우선할 것.

### 미검증
[[InGameScene]] 2026-07-24-0 참고.
