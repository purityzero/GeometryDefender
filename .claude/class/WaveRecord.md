# WaveRecord (WaveTable)

## 연관 클래스
- Table, Record, TableManager (Glory)
- WaveSpawnRecord — 웨이브별 스폰 순서 상세

## 현재 상태 (2026-07-28 기준 정정)
- 경로: Assets/Scripts/Table/WaveRecord.cs
- 필드: StartTime(int), Duration(int, 2026-07-28 신설 — 아래 참고), NormalWeight/SwiftWeight/HeavyWeight/SplitterWeight/RangedWeight(int, 종족별 가중치), EliteChance(float).
- `WaveTable : Table<WaveRecord>` — `GetActivePhase(int _elapsedSeconds)`(경과초 기준 현재 페이즈 조회), `GetFinalPhaseStartTime()`(2026-07-22 추가, 마지막 페이즈 **시작** 시각 — [[DifficultyManager]].GetInfiniteStepCount()의 Infinite 배율 증가 기준점 전용), `GetFinalPhaseEndTime()`(2026-07-28 추가, 마지막 페이즈 **종료** 시각 = StartTime+Duration — [[DifficultyManager]]의 난이도 클리어 판정 전용).
- 데이터: Assets/Resources/Table/WaveTable.csv (Id,StartTime,Duration,NormalWeight,SwiftWeight,HeavyWeight,SplitterWeight,RangedWeight,EliteChance — 5행, 전 행 Duration=120, 마지막 행 StartTime=480→종료 600)
- 웨이브 진행 로직(스포너)은 [[SpawnManager]]가 담당.

## 2026-07-28-0 — Duration 컬럼 신설 + GetFinalPhaseEndTime() 추가
사용자 요청("5웨이브를 가면 바로 끝내지말고 5웨이브가 끝나야 게임 종료되는걸로") — 기존엔 `DifficultyManager`가 `GetFinalPhaseStartTime()`(480, Wave5 **시작** 시각)만으로 클리어 판정을 해서 Wave5가 시작하자마자 런이 종료됐음. Wave5가 실제로 다 진행된 뒤 끝나도록, 각 웨이브에 `Duration`(지속 시간, 초) 필드를 추가하고 마지막 웨이브의 종료 시각(`StartTime+Duration`)을 새로 계산하는 `GetFinalPhaseEndTime()`을 신설.
- CSV: 전 행에 `Duration=120`(기존 웨이브 간격과 동일하게 통일, 사용자 확정) 추가 — Wave5 종료 시각은 480+120=600초.
- `GetFinalPhaseStartTime()`은 그대로 유지 — [[DifficultyManager]].GetInfiniteStepCount()(Infinite 난이도 배율 증가 계산)는 `08_balance.html` 공식(`t=480`부터 2분마다 증가)이 시작 시각 기준이라 변경 대상이 아님, 클리어 판정(UpdateLogic)만 종료 시각으로 교체.
- 검증: `mcp__ide__getDiagnostics` 컴파일 에러 0건. Play Mode 미검증(Unity MCP 미연결) — 실제 600초 경과 시 클리어되는지, 480~600초 구간엔 클리어되지 않는지 확인 필요.

## 작업 내역

### 2026-07-12-0
- 개요: 프로젝트 전체 스캔으로 기본 정보 문서 초기 생성 (코드 수정 없음)

---

## 2026-07-15-0

### 개요
D:\Unity\Job에서 머지 — 웨이브 방식이 "카운트 기반"에서 "시간 페이즈 + 종족 가중치"로 변경.

### 수정
- 전: NormalCount/SwiftCount/HeavyCount/SpawnInterval/ClearBonus
- 후: StartTime + 종족별 Weight 5종 + EliteChance, `WaveTable.GetActivePhase(경과초)` 추가
- WaveTable.csv도 페이즈 5행으로 교체.

---

## 2026-07-22-0

### 개요
[[DifficultyManager]] 구현 중 "난이도 클리어 = 마지막 웨이브 도달 시점까지 생존" 판정에 필요해 추가. 상세는 `.claude/design/difficulty-progression.md` 참고.

### 파일
- Assets/Scripts/Table/WaveRecord.cs

### 수정 (함수 단위)
**신규 `WaveTable.GetFinalPhaseStartTime()`**
```csharp
public int GetFinalPhaseStartTime()
{
    return list[list.Count - 1].StartTime;
}
```

### 검증
[[DifficultyManager]] 2026-07-22-0 참고 — Play Mode에서 이 값(480)을 기준으로 한 클리어 판정 실측 확인됨.
