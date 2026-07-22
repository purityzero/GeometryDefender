# UIRunOver

연관 클래스: [[UIPopup]](부모, 2026-07-22부터 — 이전엔 UIBase 직접 상속), UIManager, UITable, TowerHealth(`OnDie` 이벤트로 트리거), MonsterManager(`killCount`), TimerManager(`elapsedTime`), PlayerManager(`AddRunRecord`/`playerData`/`GetCurrencyAmount`), InGameScene(`OnDie` 구독 배선), UIMetaTree, SceneManager

## 개요
런 종료 정산 화면. `TowerHealth.OnDie` → `InGameScene.OnTowerDie()` → `UIManager.instance.Get<UIRunOver>()`로 열린다. `Show()`를 오버라이드해 자체적으로 현재 런의 통계를 모아 `PlayerManager.AddRunRecord()`로 저장하고, 프리팹의 각 텍스트를 채운다. 상세 계층/필드 fileID는 [[UIRunOver (prefab)]](../prefab/UIRunOver.md) 참고.

## 현재 상태
- 경로: Assets/Scripts/UI/UIRunOver.cs
```csharp
public class UIRunOver : UIPopup
{
    [SerializeField] private TextMeshProUGUI m_ScoreText;
    [SerializeField] private TextMeshProUGUI m_BestText;
    [SerializeField] private TextMeshProUGUI m_StatsValueText;
    [SerializeField] private TextMeshProUGUI m_ShardsEarnedText;
    [SerializeField] private TextMeshProUGUI m_ShardsTotalText;

    public override void Show()
    {
        base.Show();
        // MonsterManager.Current.killCount / TimerManager.Current.elapsedTime로 RunRecord 생성
        // → PlayerManager.instance.AddRunRecord(...) → 텍스트 채우기
    }

    public void OnClickMetaTree() { UIManager.instance.Get<UIMetaTree>(); }
    public void OnClickRestart() { Time.timeScale = 1f; SceneManager.instance.NextScene(EScene.InGameScene.ToString()); }
    public void OnClickMainMenu() { Time.timeScale = 1f; SceneManager.instance.NextScene(EScene.TitleScene.ToString()); }
}
```
(전체 로직은 실제 파일 참고 — 위는 구조 요약)
- 데이터 흐름: `Show()`에서 `MonsterManager.Current.killCount.Value`/`TimerManager.Current.elapsedTime`을 읽어 `RunRecord`를 만들고 `PlayerManager.instance.AddRunRecord(_runRecord)` 호출(내부에서 BestScore 갱신 + PlayerPrefs 저장까지 됨) → 그 결과로 텍스트 5개 채움.
- 버튼 3개(`Btn_MetaTree`/`Btn_Restart`/`Btn_MainMenu`)는 프리팹에서 각각 `OnClickMetaTree`/`OnClickRestart`/`OnClickMainMenu`를 Persistent Call로 직접 호출(코드에서 `AddListener` 안 함, TitleScene의 버튼들과 동일 컨벤션 — CODE.MD "버튼 핸들러: OnClick+파스칼" 규칙).
- **게임 일시정지**: 실제 트리거는 [[InGameScene]]의 `OnTowerDie()`에서 `Time.timeScale = 0f;`(아래 "작업 내역" 참고) — `Time.deltaTime` 기반 로직(ECS 몬스터 이동, `SpawnManager`/`TimerManager`의 타이머)은 전부 멈추지만, uGUI 클릭/이벤트 처리는 `Time.timeScale`과 무관하게 계속 동작하므로 이 화면과 버튼은 정상 작동한다. `OnClickRestart`/`OnClickMainMenu`가 씬 전환 직전에 `Time.timeScale = 1f`로 복구(안 하면 다음 씬도 멈춘 채로 시작함) — `OnClickMetaTree`는 복구하지 않음(런이 끝난 화면 위에 계속 떠 있는 것뿐이라 배속을 되돌릴 이유가 없음).

## 설계 판단 — 알려진 단순화 (후속 검토 필요)
- **Score/CardsObtained가 실제 값이 아님**: 이 프로젝트엔 아직 (a) 킬 수와 별개인 "점수" 계산식 (b) 카드 시스템 자체가 없음. 그래서 `Score`는 `killCount`를 그대로 대입한 임시 지표, `CardsObtained`는 0 고정. 이 두 시스템이 실제로 생기면 `UIRunOver.Show()`의 해당 대입부만 교체하면 됨(주석으로 표시해둠).
- ~~BossKills/Shards Earned 미구현~~ → 2026-07-22-3에서 반영 완료(아래 참고). `BossKills`는 `MonsterManager.bossKillCount`, "Shards Earned"는 `05_meta.html` 공식 × `DifficultyManager.GetShardMultiplier()`로 실제 계산됨.
- UI가 `PlayerManager.instance.AddRunRecord(...)`를 직접 호출 — 별도 서비스/컨트롤러 계층 없이 UI가 데이터 저장을 직접 트리거하는 기존 프로젝트 컨벤션([[UIMetaTree]].OnClickNode()가 `PlayerManager.instance.SpendCurrency`/`UnlockMetaNode`를 직접 부르는 것과 동일 패턴)을 그대로 따름.

## 작업 내역

### 2026-07-22-3

#### 개요
`.claude/design/shard-acquisition.md` 스펙 구현 완료 — 하드코딩돼 있던 `BossKills=0`/`shardsEarned=0`을 실제 값으로 채움. 연관: [[MonsterManager]](`bossKillCount`), [[PlayerManager]](`AddCurrency`), [[DifficultyManager]](`GetShardMultiplier`).

#### 파일
- Assets/Scripts/UI/UIRunOver.cs

#### 수정 (함수 단위)
**Show()**
- 전: `int shardsEarned = 0;`, `RunRecord.BossKills = 0`
- 후:
  - `bossKillCount`를 `MonsterManager.Current.bossKillCount.Value`에서 읽어 `RunRecord.BossKills`에 대입.
  - `baseShards = floor(SurvivalSeconds/10) + (KillCount/50) + (BossKills×10)`(05_meta.html 공식) 계산 → `DifficultyManager.Current.GetShardMultiplier()`를 곱해 `shardsEarned` 산출.
  - `PlayerManager.instance.AddCurrency(eCurrencyType.Shard, shardsEarned)` 호출(실제 재화 적립 — 이전엔 이 호출 자체가 없었음).

#### 검증 (2026-07-22, Play Mode)
Title→Btn_Play→InGame 실제 흐름. 리플렉션으로 `TimerManager.elapsedTime=300`, `MonsterManager.killCount=200`, `bossKillCount=1`로 맞춘 뒤 `UIManager.instance.Get<UIRunOver>()` 호출 →
- `+44`/`Total: 44` 텍스트 표시, `PlayerManager.GetCurrencyAmount(Shard)=44` — `05_meta.html` 예시("5분 생존, 200킬, 보스 1처치 → 44")와 정확히 일치.
- `PlayerPrefs`의 `AssetData` 키에 `{"Shards":44}`로 실제 저장되는 것 확인.
- 콘솔 에러 0건.

---

### 2026-07-22-4

#### 개요
[[MetaTreeRecord]] 2026-07-22-0의 일부 — "Metatree 업그레이드가 스탯에 반영 안 됨" 버그 수정 중, 샤드 정산에도 메타 트리(ECONOMY 줄기 `ShardPercent`, 예: M-303 Shard Bonus)가 빠져있던 것을 함께 반영.

#### 파일
- Assets/Scripts/UI/UIRunOver.cs

#### 수정 (함수 단위)
**Show()**
- 전: `shardsEarned = round(baseShards × difficultyShardMultiplier)` — 난이도 배율만 반영.
- 후: `MetaTreeTable.GetTotalEffectValue(eMetaEffectType.ShardPercent, ...)`로 해금된 퍼센트 합산 → `metaShardMultiplier = 1 + (합산%/100)` 추가 계산, `shardsEarned = round(baseShards × difficultyShardMultiplier × metaShardMultiplier)`.

#### 검증
컴파일 확인(에러 0건). Shard Bonus 노드(M-303)를 실제로 해금한 상태에서의 End-to-End 재계산은 별도로 하지 않음(로직은 [[MetaTreeRecord]].GetTotalEffectValue()와 동일 패턴이라 그쪽 검증으로 갈음) — 필요 시 추가 확인 권장.

---

### 2026-07-22-2

#### 개요
사용자 요청("UIMetaTree, UIToastMessage, UIRunOver등 (Popup)... UIPopup을 따로 상속받아서") — [[UIPopup]] 신설에 맞춰 상속 전환 + UITable UIType 변경. 상세는 [[UIPopup]] 2026-07-22-0 참고.

#### 파일
- Assets/Scripts/UI/UIRunOver.cs
- Assets/Resources/Table/UITable.csv

#### 수정
- `public class UIRunOver : UIBase` → `public class UIRunOver : UIPopup` (기존 `Show()`가 이미 `base.Show()`를 부르고 있어서 이 한 줄 외엔 코드 변경 없음)
- UITable.csv: `UIType` `Normal` → `Popup` (PopupCanvas로 이동)

#### 검증
[[UIPopup]] 2026-07-22-0 참고 — `Btn_MetaTree` 클릭으로 위에 `UIMetaTree`를 열어도 `UIRunOver`가 가려지지 않고(오히려 MetaTree가 정상적으로 그 위에 그려짐), 뒤로가기 1회로 `UIMetaTree`만 닫히고 `UIRunOver`는 유지되는 것, `CloseAllPopups()`로 최종 정리되는 것까지 Play Mode 실측.

---

### 2026-07-22-0

#### 개요
사용자 요청("UIRunOver도 만들어줘") — 빈 스텁이던 컴포넌트를 실제로 구현하고 기존에 이미 완성돼 있던 프리팹(계층/텍스트/버튼)에 연결. [[TowerHealth]] "미구현 범위"에서 후속 작업으로 남겨뒀던 항목.

#### 파일
- Assets/Scripts/UI/UIRunOver.cs
- Assets/Scripts/InGame/InGameScene.cs (`TowerHealth.OnDie` 구독 배선)
- Assets/Resources/Prefabs/UI/UIRunOver.prefab (필드 5개 + 버튼 3개 OnClick 연결)

#### 수정 (함수 단위)
**InGameScene.OnSetup()**
- 후: `m_TowerHealth.OnDie += OnTowerDie;` 한 줄 추가

**InGameScene.OnTowerDie() (신규)**
```csharp
private void OnTowerDie()
{
    UIManager.instance.Get<UIRunOver>();
}
```

**UIRunOver** — 위 "현재 상태" 코드 참고 (전: 빈 클래스 → 후: Show 오버라이드 + 버튼 핸들러 3개)

#### 검증
Unity MCP `manage_prefabs`(open_prefab_stage)로 프리팹 편집 — `manage_components.set_property`로 5개 TMP 필드와 3개 Button의 `m_OnClick.m_PersistentCalls`를 연결, 저장 후 YAML에서 `m_Target`/`m_MethodName`이 의도대로(`OnClickMetaTree`/`OnClickRestart`/`OnClickMainMenu`, 전부 UIRunOver 루트 컴포넌트 fileID 9004000000000001900을 가리킴) 반영된 것 직접 확인.

Play Mode 실측(InGameScene 직접 Play + `execute_code`로 `TableManager.init()`/`MonsterManager.Init()`/`TowerHealth.Init(100)` 수동 호출, `OnDie` 구독은 리플렉션으로 재연결 — client-issues.md 2026-07-21-1 선행 버그로 `InGameScene.OnSetup()` 자연 흐름이 막혀있어 우회):
- `TowerHealth.TakeDamage(1000)` → `currentHp.Value=0` → `UIRunOver` 인스턴스 생성 + `activeInHierarchy=true` 확인.
- 텍스트 5개 전부 정상 반영: Score/Best "0", Stats "00:11\n0\n0\n0"(생존 11초, 킬 0), Shards "+0"/"Total: 0".
- `PlayerManager.playerData.RecentRuns.Count`가 1로 증가(중복 저장 없음 — `OnDie`가 정확히 1회만 발동하는 `TowerHealth.TakeDamage`의 가드 덕분).
- **실제 UnityEvent 경로까지 검증**: 리플렉션이 아니라 실제 씬에 생성된 `Btn_MetaTree`의 `Button.onClick.Invoke()`를 호출 → `UIMetaTree`가 실제로 열리는 것까지 확인(= 프리팹에 저장한 Persistent Call이 런타임에 정상 작동함을 간접 검증).
- 콘솔 에러 0건.
- `Btn_Restart`/`Btn_MainMenu`(씬 전환 트리거)는 세션 복잡도상 실제 클릭 검증은 안 함 — `OnClickRestart`/`OnClickMainMenu`가 각각 `SceneManager.instance.NextScene(EScene.InGameScene/TitleScene.ToString())`을 호출하는 것은 코드 리뷰로만 확인(TitleScene.OnClickPlayButton과 동일 API 사용).

---

### 2026-07-22-1

#### 개요
사용자 요청("RunOver가 뜨면 뒤에 게임은 멈춰야 할꺼 같아") — `OnDie` 시 게임을 일시정지. 이어서 "UIRunOver도 안 뜨고 작동도 안 할 것" 우려에 실측으로 답함(아래 검증 참고).

#### 파일
- Assets/Scripts/InGame/InGameScene.cs
- Assets/Scripts/UI/UIRunOver.cs

#### 수정 (함수 단위)
**InGameScene.OnTowerDie()**
- 전: `UIManager.instance.Get<UIRunOver>();`만 있었음
- 후: `Time.timeScale = 0f;`를 그 앞에 추가

**UIRunOver.OnClickRestart() / OnClickMainMenu()**
- 전: `SceneManager.instance.NextScene(...)`만 호출
- 후: 맨 앞에 `Time.timeScale = 1f;` 추가(안 하면 재시작/메인메뉴로 가도 다음 씬이 멈춘 채로 시작함). `OnClickMetaTree()`는 복구 안 함(런 종료 화면 위에 계속 있는 것뿐이라 배속을 되돌릴 이유가 없음).

#### 설계 근거
`SpawnManager`(스폰 타이머)/`TimerManager`(elapsedTime)/ECS `MoveSystem`(몬스터 이동) 전부 `Time.deltaTime`을 직접 참조하므로, `Time.timeScale = 0f` 한 줄로 셋 다 동시에 멈춘다 — 개별 시스템에 "일시정지 상태" 플래그를 추가하는 것보다 훨씬 단순(재사용 우선/단순함 원칙).

#### 검증 (Play Mode 실측)
- `Time.timeScale=0`으로 만든 뒤 몬스터를 새로 스폰해 위치 기록 → 실시간 2초 대기 후 재조회 → 좌표 완전히 동일(이동 정지 확인).
- **사용자 우려("UIRunOver도 안 뜨고 작동도 안 할 것") 검증**: `timeScale=0`인 상태에서 `UIRunOver.activeInHierarchy=true`(정상 표시), `Btn_MetaTree.onClick.Invoke()` 호출 시 실제로 `UIMetaTree`가 열림(`activeInHierarchy=true`) — uGUI 이벤트/버튼 처리는 `Time.timeScale`과 무관하게 정상 동작함을 실측으로 확인. 걱정할 필요 없는 것으로 결론.
- 컴파일 에러 0건, 콘솔 에러 0건.
