# RewardComponent (RewardData)

## 연관 클래스
- MonsterManager — 사망/도달 처리 시 이벤트 페이로드로 전달
- EnemyRecord — GoldReward, DamageToBase 원본 데이터

## 현재 상태
- 경로: Assets/Scripts/InGame/ECS/RewardComponent.cs
- 파일명은 RewardComponent지만 실제 struct 이름은 `RewardData` (IComponentData).
- 필드: `GoldReward` (int), `DamageToBase` (int), `IsBoss` (bool, 2026-07-22 추가).
- MonsterManager의 `OnMonsterDie` / `OnMonsterReachEnd` 이벤트 인자 타입으로도 그대로 사용됨.

## 작업 내역

### 2026-07-12-0
- 개요: 프로젝트 전체 스캔으로 기본 정보 문서 초기 생성 (코드 수정 없음)

---

## 2026-07-22-0

### 개요
[[UIRunOver]]의 `BossKills` 정산에 필요해 추가 — `OnMonsterDie` 이벤트로는 처치된 몬스터가 보스인지 구분할 방법이 아예 없었음. 상세는 `.claude/design/shard-acquisition.md` 참고.

### 파일
- Assets/Scripts/InGame/ECS/RewardComponent.cs
- Assets/Scripts/InGame/MonsterManager.cs

### 수정
- `RewardData`에 `public bool IsBoss;` 필드 추가.
- `MonsterManager.Spawn()`에서 `IsBoss = (_record.Variant == eEnemyVariant.Boss)`로 채움. `ProcessDeadMonsters()`에서 `IsBoss == true`면 신규 `bossKillCount`(ObservableVariable&lt;int&gt;, killCount와 동일 패턴) 증가.

### 검증
[[UIRunOver]] 2026-07-22-3 참고 — Play Mode에서 `bossKillCount` 값이 정산 공식에 정확히 반영되는 것 실측 확인.
