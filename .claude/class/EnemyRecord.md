# EnemyRecord (eEnemyShape / EnemyTable)

## 연관 클래스
- Table, Record, TableManager (Glory)
- MonsterManager — Spawn 매개변수로 사용, VisualSize를 실제 ActorMonster.transform.localScale에 적용(2026-07-21)

## 현재 상태 (2026-07-21 코드/데이터 기준으로 정정 — 아래 "현재 상태"가 실제와 다르던 부분을 실제 코드 기준으로 갱신)
- 경로: Assets/Scripts/Table/EnemyRecord.cs
- `eEnemyShape` enum: Triangle, Circle, Square, Diamond, Pentagon, Star.
- `eEnemySpecies` enum: Normal, Swift, Heavy, Splitter, Ranged. `eEnemyVariant` enum: Normal, Elite, Boss.
- `EnemyRecord : Record` 필드: DisplayName, Species, Variant, Shape, ColorHex, MaxHp(int), MoveSpeed(float), **VisualSize(float)**, DamageToBase(int), XpReward(int), GoldReward(int), SplitCount(int), SplitChildId(int), FireRange(float), FireCooldown(float), PrefabPath(string).
- `EnemyTable : Table<EnemyRecord>` — shapeMap/speciesMap/variantMap 딕셔너리, GetRecordById / GetRecordBySpeciesAndVariant.
- 데이터: Assets/Resources/Table/EnemyTable.csv (헤더: Id,DisplayName,Species,Variant,Shape,ColorHex,MaxHp,MoveSpeed,VisualSize,DamageToBase,XpReward,GoldReward,SplitCount,SplitChildId,FireRange,FireCooldown,PrefabPath)

### VisualSize — 몬스터 화면 크기 (2026-07-21 실적용)
- `MonsterManager.SpawnVisual()`에서 `actorMonster.transform.localScale = Vector3.one * _record.VisualSize;`로 매 스폰마다 적용 — 몬스터 프리팹 자체의 baked scale은 더 이상 유효하지 않음(전부 1,1,1로 초기화됨, 실제 크기는 항상 이 값이 결정).
- **기준점**: `1.0`이 아니라 `0.40625`가 "플레이어와 동일 크기" 기준값이다 — InGameScene의 ActorPlayer 실제 씬 스케일(Assets/Scenes/InGameScene.unity, [[InGameScene]] 참고)과 동일 수치. 플레이어와 몬스터가 같은 스프라이트 계열(사각 캔버스에 도형을 그린 아이콘 세트, 전부 스케일 1에서 대략 2.0~2.22 유닛 크기)을 쓰기 때문에 도형 종류가 달라도 이 값 하나로 동일 체감 크기가 나온다.
- Normal 티어(Variant=Normal) 5종 중 **Triangle(종족 Normal) = 0.40625로 플레이어와 정확히 동일** — 사용자 요청("일반몹은 플레이어와 같은 크기 기준으로")의 앵커. 나머지 4종(Swift/Heavy/Splitter/Ranged)은 기존 데이터에 이미 있던 종족별 상대 비율(Swift 0.8x, Heavy 1.6x, Splitter 1.2x, Ranged 1.0x — 각각 Normal-Triangle 대비)을 유지한 채 이 앵커에 맞춰 재계산(스웨트/헤비 등 아키타입별 크기 차이를 보존하기 위함 — Heavy가 크고 느리게, Swift가 작고 빠르게 보이는 기존 설계 의도를 그대로 살림).
- Elite = 같은 종족 Normal-tier 값의 ×1.3, Boss(전부 Shape=Star) = ×3.0 — 기존 데이터의 배수 구조를 그대로 유지(이미 종족별 상대 크기를 보존하는 일관된 설계였다고 판단, 2026-07-21 재검토 후 유지 결정). 절대값만 새 앵커 기준으로 재계산.

| Variant | Normal(Triangle) | Swift(Circle) | Heavy(Square) | Splitter(Diamond) | Ranged(Pentagon) |
|---|---|---|---|---|---|
| Normal | 0.40625 | 0.325 | 0.65 | 0.4875 | 0.40625 |
| Elite | 0.528125 | 0.4225 | 0.845 | 0.63375 | 0.528125 |
| Boss | 1.21875 | 0.975 | 1.95 | 1.4625 | 1.21875 |

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
