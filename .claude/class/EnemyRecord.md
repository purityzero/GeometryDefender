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

---

## 2026-07-21-0

### 개요
사용자 요청(Design 관점: "적군의 색을 조금 더 알록달록하게") — Normal Variant(5종) `ColorHex`를 종족별로 구분되는 색으로 변경. Elite(#ff00aa)/Boss(#ffd600)는 변경하지 않음(아래 근거 참고).

### 파일
- Assets/Resources/Table/EnemyTable.csv (ColorHex 컬럼만, 코드 변경 없음)

### 수정 (Id 1~5)

| Id | 종족 | 도형 | 전 | 후 |
|---|---|---|---|---|
| 1 | Normal | Triangle | #ff3355 | #ff3355 (유지) |
| 2 | Swift | Circle | #ff3355 | #ff9500 |
| 3 | Heavy | Square | #ff3355 | #7c4dff |
| 4 | Splitter | Diamond | #ff3355 | #29cc66 |
| 5 | Ranged | Pentagon | #ff3355 | #3d8bff |

### 설계 근거
- Assets/Design/03_enemy.html은 "SPECIES — 5종 ... 모두 적색 베이스"라고 명시 — Normal 티어 5종이 전부 동일한 빨강인 것은 버그가 아니라 원래 의도된 설계였음. 다만 이 상태는 게임 시작 후 2분간(Elite 미출현 구간) 화면의 모든 몬스터가 도형만 다르고 색은 완전히 동일해 "알록달록"과 반대되는 결과를 냄 — 사용자가 명시적으로 재검토를 요청해 이 부분만 수정.
- Elite(#ff00aa 마젠타)/Boss(#ffd600 골드)는 손대지 않음: 02_combat.html·03_enemy.html 모두 "엘리트=마젠타, 보스=별 도형+골드"를 변종(위험도) 등급을 한눈에 읽게 하는 의도된 신호로 명시하고 있어, 종별 색을 섞으면 이 신호가 흐려짐. Normal 티어는 항상 스폰되고(0:00~ 상시 20~100% 비중) 동시 등장 빈도가 가장 높아 색상 다양화의 체감 효과가 가장 큰 반면, 변종 등급 신호와 충돌하지 않는 유일한 티어라 이곳만 수정.
- 5색 선정: 기존 UI에서 이미 쓰이는 시안(#00e5ff, 타워)·마젠타(#ff00aa, Elite)·골드(#ffd600, Boss)와 겹치지 않는 고채도 색 5개(빨강/주황/보라/초록/파랑)를 색상환에 고르게 분산 배치. Normal(빨강)은 기존 값 그대로 유지(가장 흔한 기준 몬스터라는 상징성 보존, 최소 침습).

### 검증
CSV 파싱 로직(EnemyRecord.cs/EnemyTable) 자체는 변경 없는 순수 데이터 수정이라 리스크 낮음. 다만 실제 씬에서 몬스터 색상이 반영되는 화면 확인은 client-issues.md 2026-07-21-1 선행 버그로 InGameScene 자연 진입 자체가 막혀 있어 이번 세션에서 눈으로 확인은 못함 — MonsterManager.SpawnVisual()의 `ColorUtility.TryParseHtmlString(_record.ColorHex, ...)` 로직 자체는 이전부터 동작하던 경로라 데이터만 바뀌면 그대로 반영될 것으로 예상.
