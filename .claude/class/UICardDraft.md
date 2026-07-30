# UICardDraft

연관 클래스: [[UIPopup]](부모), UIManager, UITable, `CardManager`(카드 롤링/적용 위임), `XpManager`(pendingLevelUps 소비), `StringTable`, `ResUtil`(템플릿 복제)

## 개요
UICardDraft.prefab 루트에 부착되는 화면 컴포넌트. **2026-07-24부터 전체 구현 완료**([[card-draft]] 스펙) — 레벨업 시 열려 카드 3장 중 1장을 선택하게 하는 드래프트 팝업.

## 현재 상태 (2026-07-30)
- `public class UICardDraft : UIPopup`
- 필드: `m_CardContainer`(RectTransform), `m_CardTemplate`(GameObject, `Item_Card` 템플릿), `m_RerollButton`/`m_RerollText`, `m_BuildInfoText`. (`m_SkipButton`/`m_SkipText`는 2026-07-30에 스킵 기능 폐지로 제거됨 — 아래 changelog 참고)
- `Awake()`: Reroll 버튼 리스너를 1회만 등록.
- `Show()`/`Close()`(override): `Time.timeScale` 0/1 전환(`UIPause`와 동일 패턴).
- `RollAndDisplay()`: `CardManager.Current.RollCards()` → `DisplayCards()`/`RefreshButtons()`/`RefreshBuildInfo()`.
- `DisplayCards()`: 기존 클론 파괴 → 템플릿 임시 활성화 → 카드 수만큼 `ResUtil.Create(m_CardTemplate, m_CardContainer)`로 복제(UIMetaTree와 동일 "활성화→클론→비활성화" 패턴) → `SetupCard()`.
- `SetupCard()`: `Text_Name`/`Text_Effect`에 `StringTable.GetString(record.NameKey/EffectKey)`, `Image_Icon`을 카테고리별 색으로 틴트, 카드 `Button.onClick`에 클로저로 `_record` 캡처해 등록(런타임 AddListener — 매번 다른 카드가 들어가므로 프리팹 `m_PersistentCalls`는 사용 못함).
- `OnClickCard`/`OnClickReroll` 구현.
- `AdvanceOrClose()`: `XpManager.Current.ConsumePendingLevelUp()` 소비 후 남아있으면 재롤링(연속 레벨업), 없으면 `Close()`.
- `RefreshButtons()`/`RefreshBuildInfo()`: 전부 StringTable 경유(`CardDraftRerollButton`/`CardDraftNoCards`/`CardCategoryOffense`~`Special`).

## 프리팹 배선 (2026-07-24)
루트 `UICardDraft` MonoBehaviour(fileID `9003000000000001900`)에 필드 연결. `Text_Title`(fileID `9003000000000001020`)에 신규 `UIText` 컴포넌트(fileID `9003000000000001900+`... 상세는 `.claude/prefab/UICardDraft.md` 참고) 추가해 `CardDraftTitle` 키 연결.

## 미검증
Unity MCP 미연결, YAML/코드 직접 편집 — 컴파일/Play 확인 안 됨.

---

## 2026-07-30-0 — 스킵 기능 폐지 (대신 리롤 다량 지급 노드로 대체)

### 개요
사용자 피드백("메타 트리 스킵기능이 오히려 스킵함으로서 안좋아지는거 같음") → "스킵자체는 없어져야할듯 대신 리롤을 좀 많이주는걸로 변경해줘 업그레이드하면"으로 방향 확정. [[CardManager]]/[[MetaTreeRecord]] 2026-07-30-0과 세트 — M-403(구 Skip Token)이 `RerollCount +3` 노드로 교체됨.

### 파일
- Assets/Scripts/UI/UICardDraft.cs
- Assets/Resources/Prefabs/UI/UICardDraft.prefab (프리팹 변경 상세는 [prefab.md](../prefab/UICardDraft.md) 2026-07-30-0 참고)

### 수정 (함수 단위)
- **필드**: `m_SkipButton`/`m_SkipText` 제거.
- **`Awake()`**: `m_SkipButton.onClick.AddListener(OnClickSkip);` 제거.
- **`OnClickSkip()`**: 메서드 전체 삭제.
- **`RefreshButtons()`**: `canSkip`/`m_SkipButton.gameObject.SetActive(...)`/`m_SkipText.SetText(...)` 3줄 제거.

### 검증
컴파일 확인 필요. Play Mode 미검증 — Reroll 버튼이 중앙에 잘 배치되는지, 스킵 없이도 드래프트 흐름이 정상인지 확인 필요.

---

## 2026-07-15-2

### 개요
신규 생성 (빈 스텁). 같은 이름의 프리팹 루트에 부착 (guid는 .claude/prefab/UICardDraft.md 참고).

### 파일
- Assets/Scripts/UI/UICardDraft.cs (+.meta)

### 미검증
컴파일/프리팹 스크립트 연결 확인 필요.

---

## 2026-07-22-0

### 개요
[[UIPopup]] 신설(사용자 요청 — 팝업 공용 베이스 + 뒤로가기 + 씬 전환 정리)에 맞춰 상속 전환. 상세는 [[UIPopup]] 2026-07-22-0 참고.

### 파일
- Assets/Scripts/UI/UICardDraft.cs

### 수정
- `public class UICardDraft : UIBase` → `public class UICardDraft : UIPopup`

### 미검증
빈 스텁이라 컴파일 확인 외 별도 동작 검증 대상 없음(에러 0건 확인).

---

## 2026-07-24-0 — 게임오버 후 정지가 풀리던 버그 수정 (Time.timeScale → SetPaused)

### 개요
사용자 버그 리포트("죽었을때, RunOver나오면서 뒤에 적들은 멈춰야하는데 전혀 멈추질 않음") — 근본 원인과 최종 설계는 [[InGameScene]] 2026-07-24-1에 상세 기록. 요약: `Close()`가 `Time.timeScale = 1f`를 무조건 실행하던 것이 원인 — 레벨업으로 이 팝업이 열려있던 중 타워가 죽어도, 이후 카드 선택/스킵으로 팝업이 닫히면 정지가 풀렸다. 처음엔 `TowerController.isDead`로 `Close()`를 가드하는 방식으로 고쳤으나, 사용자 지시("TimeScale 건드는건 위험해, QA때만 건드는걸로 하자")로 **`Time.timeScale` 자체를 프로덕션 일시정지에 안 쓰는 방향**으로 재설계.

### 수정
- `Show()`: `Time.timeScale = 0f;` → `InGameScene.Current?.SetPaused(true);`
- `Close()`: `Time.timeScale = 1f;` → `InGameScene.Current?.SetPaused(false);` — 게임오버 상태(`InGameScene.m_isGameOver`)면 이 호출로도 정지가 안 풀림(내부적으로 OR 처리, [[InGameScene]] 참고) — 이 클래스는 더 이상 "게임오버인지"를 직접 알 필요가 없다.

### 검증
컴파일 에러 0건. Play Mode 실측 — 사망 후 `Show()`→`Close()`를 반복해도 몬스터 ECS 위치/`TimerManager.elapsedTime`이 그대로 유지(3초 간격 샘플링 완전 일치) 확인. 생존 중(회귀 테스트)에는 `Close()` 시 정상적으로 정지가 풀리고 다시 진행되는 것도 확인.

### 관련 클래스
- [[InGameScene]] 2026-07-24-1 — `SetPaused`/`ApplyFreezeState` 설계 전체, 재현 시나리오 상세
- [[BaseScene]] 2026-07-24-0 — `isPaused` 게이트
- [[UIPause]] 2026-07-24-0 — 동일 버그/수정

---

## 2026-07-29-0 — 오픈 로그 추가 (QA 즉시 감지용)

### 개요
사용자 제안: "UICardDraft 떳을때 Logger.Log("[UICardDraft] Open"); 하나 만들어두고, 그거 확인해서 즉각 반응 하게 하면 어떨지?" — qa-tester 에이전트가 이 팝업이 열렸는지 매 폴링마다 `find_gameobjects`로 화면을 뒤지는 대신, `read_console`로 이 로그 한 줄만 확인해도 즉시 감지할 수 있게 됨(레벨업 팝업을 방치하면 `InGameScene.SetPaused` 정지 상태가 계속 유지돼 관찰이 왜곡되는 문제, [[InGameScene]] 2026-07-29-0 참고). `Logger.Debug`는 이 프로젝트 `Logger` 클래스에 없는 메서드라(`Log`/`Error`만 존재) `Logger.Log`로 대체.

### 파일
- Assets/Scripts/UI/UICardDraft.cs

### 수정 (함수 단위)
**`Show()`**
- 전: `base.Show(); InGameScene.Current?.SetPaused(true); RollAndDisplay();`
- 후: `base.Show();` 다음 줄에 `Logger.Log("[UICardDraft] Open");` 추가(그 뒤로 기존 로직 동일).

연속 레벨업으로 `AdvanceOrClose()` → `RollAndDisplay()`가 재호출될 때는 `Show()`가 다시 안 불려 로그도 다시 안 찍힌다 — 팝업이 "처음 열리는" 시점 감지가 목적이라 의도된 동작(재롤링 카드 갱신은 화면이 이미 열려있다는 걸 QA가 이미 아는 상태이므로 별도 신호 불필요).

### 검증
`refresh_unity(compile: request)` 컴파일 에러 0건. Play Mode 실측(로그 실제 출력 확인)은 별도로 안 함 — 단순 로그 추가라 회귀 위험 낮음.

### 관련
- `.claude/agents/qa-tester.md` — 이 로그를 활용하도록 QA 절차에 반영
