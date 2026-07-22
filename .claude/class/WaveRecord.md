# WaveRecord (WaveTable)

## 연관 클래스
- Table, Record, TableManager (Glory)
- WaveSpawnRecord — 웨이브별 스폰 순서 상세

## 현재 상태 (2026-07-22 기준 정정 — 아래 "작업 내역"과 실제 코드가 그동안 어긋나 있던 것을 바로잡음)
- 경로: Assets/Scripts/Table/WaveRecord.cs
- 필드: StartTime(int), NormalWeight/SwiftWeight/HeavyWeight/SplitterWeight/RangedWeight(int, 종족별 가중치), EliteChance(float).
- `WaveTable : Table<WaveRecord>` — `GetActivePhase(int _elapsedSeconds)`(경과초 기준 현재 페이즈 조회), `GetFinalPhaseStartTime()`(2026-07-22 추가, 마지막 페이즈 시작 시각 — [[DifficultyManager]]의 난이도 클리어 판정에 사용).
- 데이터: Assets/Resources/Table/WaveTable.csv (Id,StartTime,NormalWeight,SwiftWeight,HeavyWeight,SplitterWeight,RangedWeight,EliteChance — 5행, 마지막 행 StartTime=480)
- 웨이브 진행 로직(스포너)은 [[SpawnManager]]가 담당.

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
