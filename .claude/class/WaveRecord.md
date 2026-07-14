# WaveRecord (WaveTable)

## 연관 클래스
- Table, Record, TableManager (Glory)
- WaveSpawnRecord — 웨이브별 스폰 순서 상세

## 현재 상태
- 경로: Assets/Scripts/Table/WaveRecord.cs
- 필드: NormalCount(int), SwiftCount(int), HeavyCount(int), SpawnInterval(float), ClearBonus(int).
- `WaveTable : Table<WaveRecord>`.
- 데이터: Assets/Resources/Table/WaveTable.csv
- 아직 웨이브 진행 로직(스포너)은 없음 — 테이블만 준비된 상태.

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
