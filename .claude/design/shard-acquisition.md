# 샤드 획득(Geometry Shards 정산) 구현 스펙

## 구현 완료 (2026-07-22)
스펙대로 구현 완료 — 상세는 [[UIRunOver]] 2026-07-22-3, [[PlayerManager]] 2026-07-22-1, [[MonsterManager]] 2026-07-22-3, [[RewardComponent]] 2026-07-22-0 참고. `shardMultiplier`는 [[difficulty-progression]] 구현 완료로 실제 값이 연결됨(더 이상 1.0 고정 아님). 남은 미결정 사항(PlayerPrefs vs 파일 저장, Score 공식)은 아래 그대로 유지 — 이번 작업 범위에 포함 안 함.

## 출처 문서
- `Assets/Design/05_meta.html` — 핵심 문서. "메타 자원: Geometry Shards" 섹션에 정산 공식, SaveData/RunRecord 스펙, 세이브 정책(JSON 파일, PlayerPrefs 미사용) 명시.
- `Assets/Design/08_balance.html` — "경제 흐름 (Shards)" 섹션: 숙련도별 예상 샤드량(초보 28 / 중급 68 / 숙련 112). "난이도 진행 (Normal → Hard → Hell → Infinite)" 섹션(2026-07-22 사용자 확정 — 기존 "난이도 옵션 (MVP 이후)"를 대체): 난이도별 Shards 배율(NORMAL×1.0/HARD×1.5/HELL×2.5) + Infinite 전용 `shardMultiplier(t) = 2.5 + floor((t-480)/120) × 0.10` 공식. 난이도 시스템 자체의 구현 스펙은 별도 문서 [[difficulty-progression]] 참고 — 이 문서(샤드 획득)는 그 시스템이 계산해주는 배율값을 소비하기만 한다.
- `Assets/Design/03_enemy.html` — 보스 처치 시 "카드 선택 1회 보너스 + Shards +10" (05_meta.html의 `bossKills×10` 항과 일치).
- `Assets/Design/01_concept.html` — "타워 HP 0 → 점수 기록 + 샤드 획득 → 메타 화면 이동" 개요 흐름.
- `Assets/Design/07_ui.html` — "화면 4: 런 종료" 목업. SCORE/Survival/Kills/Boss Kills/Cards + "💎 SHARDS EARNED +52 / Total: 312" 박스 레이아웃 확인.

## 개요
런이 끝나면(중앙 타워 HP 0) 생존 시간·킬 수·보스 처치 수를 기반으로 Shards를 정산해 플레이어의 영구 재화에 더한다. Shards는 런 사이에 영구 보존되며 메타 트리 노드 해금에 쓰인다(소비 쪽은 이미 구현됨).

## 데이터 스키마
문서(05_meta.html)의 코드 스니펫 그대로:
```csharp
shardsEarned = floor(survivalSec / 10) + floor(killCount / 50) + bossKills × 10
```
예시(문서 그대로):
- 5분 생존, 200킬, 보스 1처치 → 30 + 4 + 10 = 44
- 10분 생존, 800킬, 보스 2처치 → 60 + 16 + 20 = 96
- 15분 생존, 1500킬, 보스 3처치 → 90 + 30 + 30 = 150

`RunRecord`/`PlayerData`/`AssetData`는 이미 `Assets/Scripts/PlayerManager.cs`에 구현되어 있음(아래 "이미 있는 것" 참고) — 새 데이터 클래스 불필요.

## 트리거 시점
`Assets/Scripts/UI/UIRunOver.cs`의 `Show()` — 이미 이 메서드가 정확히 이 시점(런 종료 화면 표시)에 호출되고 있고, `killCount`/`survivalSeconds`를 모아 `RunRecord`를 만든 뒤 `PlayerManager.instance.AddRunRecord(runRecord)`까지 호출하는 코드가 **이미 존재**한다. 다만 `int shardsEarned = 0;`으로 하드코딩되어 있고, 계산된 값을 실제로 재화에 더하는 호출이 없다 — 정확히 이 두 줄(계산 + 적립)만 빠져있는 상태.

## 공식 / 로직
이미 있는 `Show()`의 구조를 그대로 따르되:
```csharp
int baseShards = Mathf.FloorToInt(runRecord.SurvivalSeconds / 10f)
    + (runRecord.KillCount / 50)
    + (runRecord.BossKills * 10);

float shardMultiplier = DifficultyManager.Current.GetShardMultiplier(runRecord.SurvivalSeconds); // 아래 참고
int shardsEarned = Mathf.RoundToInt(baseShards * shardMultiplier);

PlayerManager.instance.AddCurrency(eCurrencyType.Shard, shardsEarned);
```
`runRecord.KillCount`/`BossKills`/`SurvivalSeconds`가 만들어진 **이후**, `AddRunRecord` 호출 **이후**(정산은 기록과 별개 책임이므로 순서 의존 없음, 다만 코드 가독성상 함께 묶는 게 자연스러움)에 넣는다.

**`shardMultiplier` 계산은 이 문서의 책임이 아니다** — [[difficulty-progression]] 스펙에서 정의하는 난이도 시스템(현재 난이도 + Infinite 진행 시간)이 제공해야 하는 값이다. 난이도 시스템이 아직 없는 상태에서 샤드 획득만 먼저 구현한다면, 이 곱셈 부분은 `shardMultiplier = 1.0f` 고정(NORMAL 취급)으로 우선 넣고 난이도 시스템이 붙을 때 교체 — 두 스펙은 이 지점에서만 접점이 있고 서로 독립적으로 구현 가능하다.

## 기존 구현과의 접점

### 이미 있는 것 (재사용)
- `PlayerManager.cs`(`Assets/Scripts/PlayerManager.cs`) — `eCurrencyType.Shard`, `AssetData.Shards`, `GetCurrencyAmount()`, `GetCurrencyObservable()`, `SpendCurrency()`가 이미 구현됨. **`AddCurrency`류 메서드만 없음** — `SpendCurrency`와 대칭되는 이름으로 신규 추가 필요.
- `TimerManager.Current.elapsedTime`(float) — `survivalSec`으로 그대로 사용 중(`UIRunOver.cs`에서 이미 사용).
- `MonsterManager.Current.killCount`(`ObservableVariable<int>`) — `killCount`로 그대로 사용 중.
- `UIRunOver.cs` — `Show()`에서 `RunRecord` 생성, `PlayerManager.instance.AddRunRecord()` 호출, UI 텍스트(`m_ShardsEarnedText`/`m_ShardsTotalText`) 갱신까지 전부 이미 구현됨. **`shardsEarned` 계산 두 줄만 비어있음.**
- `UIMetaTree.cs` — 샤드 **소비** 쪽은 완결돼 있음(`PlayerManager.instance.SpendCurrency(eCurrencyType.Shard, record.Cost)`). 이번 스펙과 직접 연동 지점 없음(참고용).
- `eEnemyVariant.Boss`(`EnemyRecord.cs`) — 몬스터 레코드에 이미 Boss 구분 필드가 있음.

### 새로 필요한 것
1. **`PlayerManager.AddCurrency(eCurrencyType _currencyType, long _amount)`** — `SpendCurrency`와 대칭 구조로 신규 추가. `eCurrencyType.Shard` 케이스에서 `m_AssetData.Shards += (int)_amount; m_ShardsObservable.Value = m_AssetData.Shards; Save();` (SpendCurrency의 반대 방향, switch 구조 그대로 재사용).
2. **보스 처치 수 집계** — 현재 `RewardData`(`Assets/Scripts/InGame/ECS/RewardComponent.cs`)에 `IsBoss` 같은 필드가 없어서 `OnMonsterDie` 이벤트 핸들러(`UIRunOver.Show()`)가 보스 처치를 구분할 방법이 아예 없다. `killCount`와 대칭되는 패턴으로:
   - `RewardData`에 `public bool IsBoss;` 필드 추가.
   - `MonsterManager.Spawn()`에서 `IsBoss = (_record.Variant == eEnemyVariant.Boss)`로 채움.
   - `MonsterManager`에 `killCount`와 동일한 패턴으로 `public ObservableVariable<int> bossKillCount { get; } = new ObservableVariable<int>(0);` 추가, `ProcessDeadMonsters()`에서 `if (rewards[i].IsBoss == true) bossKillCount.Value++;`.
   - `UIRunOver.Show()`의 `BossKills = 0`(하드코딩) → `MonsterManager.Current.bossKillCount.Value`로 교체.
3. **`UIRunOver.Show()` 수정** — 위 "공식 / 로직" 섹션의 두 줄 추가 + `BossKills` 필드 교체.

### 충돌 가능 지점 (판단 필요, 아래 섹션 참고)
- 05_meta.html은 "PlayerPrefs는 사용하지 않음(Android 강제 종료 시 손실 위험)"이라고 명시하며 `Application.persistentDataPath/save.json` 직접 파일 저장을 요구하지만, **현재 `PlayerManager`는 이미 `PlayerPrefs.SetString()`으로 저장하고 있다**(JSON 문자열을 PlayerPrefs 값으로 직렬화 — 파일 직접 저장 아님). 이번 "샤드 획득" 기능은 이 기존 저장 방식을 그대로 타므로, 문서와 실제 구현이 이미 어긋나 있는 지점을 그대로 물려받는다.

## 문서에 없어서 확인이 필요한 부분
1. **PlayerPrefs vs JSON 파일 직접 저장** — 위 "충돌 가능 지점" 참고. 이번 "샤드 획득"만 별도로 파일 저장으로 바꿀지, 기존 `PlayerManager` 저장 방식(PlayerPrefs)을 그대로 따를지는 `PlayerManager` 전체의 저장 전략을 바꾸는 더 큰 결정이라 이 스펙 범위를 벗어난다. **권장(최소 침습): 이번 작업은 기존 PlayerPrefs 방식을 그대로 따르고, 파일 저장 마이그레이션은 별도 작업으로 분리.** 다만 결정은 사용자 확인 필요.
2. **`Score` 필드의 완전한 공식 미반영** — 05_meta.html의 실제 스코어 공식은 `score = killCount×10 + bossKills×500 + survivalSec×5 + cardsObtained²×20`이지만, 현재 `UIRunOver.cs`는 `Score = killCount`(주석에 "임시 지표")로 되어 있다. **이건 "샤드 획득"이 아니라 "점수 계산" 기능이라 이번 스펙 범위 밖**으로 판단했다 — 별도 스펙/작업으로 분리할지 확인 필요(이번 작업에서 보너스로 같이 고칠지도 결정 필요).

(난이도별 Shards 배율은 2026-07-22에 사용자가 확정 — 더 이상 미기재 항목 아님. 상세는 [[difficulty-progression]] 참고.)

## 참고
- 연관 클래스: `.claude/class/MonsterManager.md`(killCount 패턴 참고), `PlayerManager.cs`/`UIRunOver.cs`(위 파일들엔 아직 `.claude/class/*.md` 없음 — 이번 구현 진행 시 신규 생성 대상).
- 연관 스펙: [[difficulty-progression]] — Infinite 모드의 `shardMultiplier(t)`를 이 문서가 소비함.
