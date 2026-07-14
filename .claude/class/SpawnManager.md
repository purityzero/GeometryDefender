# SpawnManager

연관 클래스: InGameScene, MonsterManager, WaveTable, WaveSpawnTable

## 개요
스폰 로직 담당 예정 클래스 — 현재는 빈 `Init()`만 있는 스텁. Job 프로젝트에서 WaveTable(페이즈 가중치)/WaveSpawnTable(보스 스폰 시각) 스키마까지만 잡고 소비 로직은 미구현 상태였음.

## 현재 상태
- `Init()`: 빈 구현

---

## 2026-07-15-0

### 개요
D:\Unity\Job에서 머지로 신규 도입 (스텁).

### 파일
- Assets/Scripts/InGame/SpawnManager.cs (+.meta, Job에서 복사)

### TODO
- WaveTable.GetActivePhase / WaveSpawnTable.GetBossEventAtTime 를 사용한 실제 스폰 루프 구현.
