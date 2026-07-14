# EnemyRecord (eEnemyShape / EnemyTable)

## 연관 클래스
- Table, Record, TableManager (Glory)
- MonsterManager — Spawn 매개변수로 사용

## 현재 상태
- 경로: Assets/Scripts/Table/EnemyRecord.cs
- `eEnemyShape` enum: Cube, Sphere, Capsule.
- `EnemyRecord : Record` 필드: DisplayName, Shape, ColorHex, MaxHp(int), MoveSpeed(float), DamageToBase(int), GoldReward(int).
- `EnemyTable : Table<EnemyRecord>`.
- 데이터: Assets/Resources/Table/EnemyTable.csv (헤더: Id,DisplayName,Shape,ColorHex,MaxHp,MoveSpeed,DamageToBase,GoldReward)

## 작업 내역

### 2026-07-12-0
- 개요: 프로젝트 전체 스캔으로 기본 정보 문서 초기 생성 (코드 수정 없음)

---

## 2026-07-15-0

### 개요
D:\Unity\Job에서 머지 — Job 버전으로 교체. 03_enemy.html 기획 반영본.

### 수정
- 전: eEnemyShape { Cube, Sphere, Capsule } + 8필드 단순 레코드
- 후:
  - eEnemyShape { Triangle, Circle, Square, Diamond, Pentagon, Star }
  - eEnemySpecies { Normal, Swift, Heavy, Splitter, Ranged } / eEnemyVariant { Normal, Elite, Boss } 신설
  - 필드 확장: Species/Variant/VisualSize/XpReward/SplitCount/SplitChildId/FireRange/FireCooldown/PrefabPath
  - EnemyTable: shapeMap/speciesMap/variantMap 딕셔너리 + GetRecordById / GetRecordBySpeciesAndVariant
- EnemyTable.csv도 15행(5종족 × Normal/Elite/Boss)으로 교체.

### 미검증
컴파일/테이블 로드 확인 필요.
