# TowerRecord (eTargetingType / TowerTable)

## 연관 클래스
- Table, Record, TableManager (Glory)

## 현재 상태
- 경로: Assets/Scripts/Table/TowerRecord.cs
- `eTargetingType` enum: First, Strongest.
- `TowerRecord : Record` 필드: DisplayName, ColorHex, Cost(int), Damage(int), AttackInterval(float), Range(float), SplashRadius(float), ProjectileSpeed(float), DefaultTargeting(eTargetingType).
- `TowerTable : Table<TowerRecord>`.
- 데이터: Assets/Resources/Table/TowerTable.csv
- 아직 타워 구현 클래스는 없음 — 테이블만 준비된 상태.

## 작업 내역

### 2026-07-12-0
- 개요: 프로젝트 전체 스캔으로 기본 정보 문서 초기 생성 (코드 수정 없음)
