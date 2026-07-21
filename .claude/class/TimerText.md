# TimerText

## 연관 클래스
- TimerManager (elapsedTime을 읽어옴)
- BaseScene, IUpdatable (Update 대신 구동)

## 개요
InGameScene.unity에 이미 손으로 배치돼 있던 실제 HUD(`Canvas/Top/Timer`)의 시간 텍스트를 매 프레임 갱신하는 컴포넌트. `TimerManager.Current.elapsedTime`을 "mm:ss"로 포맷해 `m_TimeText`에 대입한다.

## 현재 상태
- 경로: Assets/Scripts/InGame/TimerText.cs
- `[SerializeField] private TextMeshProUGUI m_TimeText;`
- `Start()`에서 `BaseScene.Current.Register(this)`, `OnDestroy()`에서 `BaseScene.Current?.Unregister(this)` — MonsterManager/SpawnManager/TimerManager와 동일한 패턴([[BaseScene]] 참고). 이 오브젝트는 씬 자체에 배치된 것(DontDestroyOnLoad로 캐싱되는 UI가 아님)이라 `Start()` 기반 등록으로 충분함([[UIInGameHUD]]의 Show()/Close() 케이스와 다름).
- `UpdateLogic()`: `TimerManager.Current`가 null이면 무시, 아니면 정수초 → `mm:ss` 변환해 텍스트 대입.
- 씬 배치: InGameScene.unity의 `Canvas/Top/Timer` 오브젝트(fileID 1134574031)에 컴포넌트로 부착(fileID 812340010), `m_TimeText`는 그 아래 `frame/Text (TMP)`의 TextMeshProUGUI(fileID 1239586912)를 참조.

## 작업 내역

### 2026-07-21-0

#### 개요
사용자 지적: InGameScene의 UI 시간을 갱신하는 이전 작업([[UIInGameHUD]] 2026-07-21-0)이 실제로는 씬에 이미 배치돼 있던 진짜 HUD(`Canvas/Top/Timer/Text (TMP)`, fileID 1239586910)를 확인하지 않고, 아무도 쓰지 않던 `UIInGameHUD.prefab`을 새로 띄우는 방식으로 만들어져 있었음 — 씬 전체 오브젝트 이름을 다시 훑어서 발견. 사용자가 "씬 안의 진짜 HUD"를 선택해 그쪽으로 다시 연결.

#### 되돌린 것
- `InGameScene.OnSetup()`의 `UIManager.instance.Get<UIInGameHUD>();` 호출 제거
- `UIInGameHUD.cs`를 원래의 빈 스텁(`public class UIInGameHUD : UIBase { }`)으로 되돌림
- `UIInGameHUD.prefab`의 `m_TimeText` 필드 참조 제거
- 상세 되돌림 근거는 [[UIInGameHUD]] 2026-07-21-1 참고.

#### 신규 파일
- Assets/Scripts/InGame/TimerText.cs (+.meta, guid a0f55e52c317c24401adf6d2901d9403 신규 발급)

#### 수정 (씬, 오브젝트 단위)
- Assets/Scenes/InGameScene.unity — `Canvas/Top/Timer`(1134574031)에 TimerText 컴포넌트(812340010) 추가, `m_TimeText: {fileID: 1239586912}`(Text (TMP)의 TextMeshProUGUI)로 연결

#### 미검증
컴파일/에디터 미실행 상태 편집. 실제 Play Mode에서 화면 상단 Timer 표시가 mm:ss로 매초 갱신되는지, 에디터 TimeScale을 올렸을 때 같이 빨라지는지 확인 필요.
