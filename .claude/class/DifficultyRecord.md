# DifficultyRecord / DifficultyTable

연관 클래스: Record, Table, TableManager, [[DifficultyManager]](유일한 소비처)
기획 근거: Assets/Design/08_balance.html "난이도 진행 (Normal → Hard → Hell → Infinite)"

## 2026-07-30-1 — Normal DifficultyMultiplier 2차 하향 (1차 조정 검증 후 훨씬 과감하게)
`design-issues.md` 2026-07-30-1 검증 결과(1차 조정 0.8→0.65 후에도 평균 +6.1%/최고 +7.6%에 그침, 600초 미달) 후속 — 사용자 확인("크로스오버만 더 과감하게") 후 Normal `DifficultyMultiplier` 0.65→**0.4**(원본 1.0 대비 총 60% 완화). 이 값은 크로스오버 시점과 크로스오버 이후 백로그 증가 기울기 양쪽에 곱해지는 가장 레버리지 큰 손잡이라 가장 크게 움직였다. [[TowerRecord]] 2026-07-30-4, [[GameConfigRecord]] 2026-07-30-6과 함께 한 세트. Hard(1.3)/Hell(1.6)/Infinite는 그대로 유지.

### 검증
컴파일 불필요(CSV 값 변경), `refresh_unity`(assets, if_dirty) 후 콘솔 에러 0건. Play Mode 재검증 필요 — 이번엔 오히려 600초를 넘겨 "너무 쉬움"으로 나올 가능성도 있음.

---

## 2026-07-30-0 — Normal DifficultyMultiplier 추가 하향 (design-issues.md 2026-07-30-0 후속)
`design-issues.md` 2026-07-30-0 QA 결론(완전 신규 유저 상태로 Normal 7회 연속 시도, 전부 600초 미클리어·최고 324초, Hard가 단 한 번도 자연 해금 안 됨) 원인 분석 — 스폰레이트 공식의 `DifficultyMultiplier`가 그대로 크로스오버 시점과 이후 백로그 증가 기울기 양쪽에 곱해지므로 가장 직접적인 손잡이. Normal `DifficultyMultiplier` 0.8→**0.65**(추가 약 19% 완화, 2026-07-27 1.0→0.8 조정과 동일한 "Normal 전용 손잡이" 패턴). Hard(1.3)/Hell(1.6)/Infinite는 그대로 유지 — Normal이 먼저 정상 궤도에 올라야 그 다음 사다리 재검토가 의미 있다는 게 이번 QA의 결론이라, 상위 난이도는 손대지 않음. [[TowerRecord]] 2026-07-30-3, [[GameConfigRecord]] 2026-07-30-5와 함께 한 세트로 조정.

### 검증
컴파일 불필요(CSV 값 변경), `refresh_unity`(assets, if_dirty) 후 콘솔 에러 0건. Play Mode 재검증 필요 — 다음 QA에서 Normal이 실제로 600초에 도달하는지, Hard가 자연 해금되는지 확인.

---

## 개요
난이도(Normal/Hard/Hell/Infinite)별 배율/순차 언락 체인 데이터 테이블. CSV: `Resources/Table/DifficultyTable.csv`.
2026-07-22, 사용자 요청("난이도에 관한 것들을 다 테이블에서 관리")으로 신설 — 이전엔 `DifficultyManager.cs`에 switch문 3개 + 상수 2개로 하드코딩되어 있었음.

## 현재 상태
```csharp
public class DifficultyRecord : Record
{
    public string DisplayName;
    public eDifficultyLevel Level;          // "Normal"/"Hard"/"Hell"/"Infinite" 문자열 → 리플렉션으로 enum 자동 매핑 (EnemyRecord.Species와 동일 패턴)
    public float DifficultyMultiplier;      // 스폰/적HP 배율 (Infinite는 시작값 — 여기에 스텝 증가분이 더해짐)
    public float ShardMultiplier;           // Shards 배율 (동일)
    public float InfiniteStepSeconds;       // Infinite 전용, 스텝 간격(초). Normal/Hard/Hell은 0 = 스텝 증가 없음 센티널
    public float InfiniteStepAmount;        // Infinite 전용, 스텝당 증가량. Normal/Hard/Hell은 0
    public int NextId;                      // 다음 난이도 레코드의 Id. 0 = 체인 끝 — MetaTreeRecord.PrereqId(선행 Id, 0=없음)의 반대 방향 대칭 패턴
}

public class DifficultyTable : Table<DifficultyRecord>
{
    public DifficultyRecord GetRecordById(int _id);
    public DifficultyRecord GetRecordByLevel(eDifficultyLevel _level);
    public eDifficultyLevel? GetNextLevel(eDifficultyLevel _level);  // NextId 체인을 따라가 다음 레벨 반환, 체인 끝이면 null
}
```

## CSV 데이터
```
Id,DisplayName,Level,DifficultyMultiplier,ShardMultiplier,InfiniteStepSeconds,InfiniteStepAmount,NextId
1,Normal,Normal,1.0,1.0,0,0,2
2,Hard,Hard,1.3,1.5,0,0,3
3,Hell,Hell,1.6,2.5,0,0,4
4,Infinite,Infinite,1.6,2.5,120,0.30,0
```

(2026-07-28: `InfiniteStepAmount` 0.10→0.30, 사용자 요청 "+0.10이 아니라 +0.30이 되게끔 해줘" — [[DifficultyManager]] 2026-07-28-0 참고. 참고로 위 예시의 `Normal` 행 `DifficultyMultiplier=1.0`은 이후 다른 세션에서 0.8로 밸런스 조정됐으나 이 문서에는 반영 안 돼 있었음 — 이번 변경과 무관해 손대지 않았고, 실제 값은 CSV 원본을 신뢰할 것.)

## 설계 판단 근거 (design-planner 스펙에서 확정)
- `Level` 필드로 enum을 직접 두는 이유: 4개 값이 고정이고 레코드와 1:1 대응이라 `EnemyRecord.Species`(1:N)보다는 `TowerRecord.GetRecordById()`에 가까움 — 다만 `DifficultyManager.currentDifficulty`가 이미 enum 값을 들고 있어 Id 변환 없이 바로 조회 가능해야 하므로 `Level` 필드 + `GetRecordByLevel()` 채택.
- `NextId`를 Id 순서 암묵 추론("다음 행=Id+1")으로 대체하지 않고 명시 필드로 둔 이유: `MetaTreeRecord.PrereqId`도 명시 필드를 쓰는 프로젝트 관례이고, Id는 "파일 내 고유값" 이상의 의미가 없다는 게 다른 테이블들의 공통 전제 — 순서 의존은 나중에 행 재배열/삽입 시 조용히 깨질 수 있음.
- `InfiniteStepSeconds`/`InfiniteStepAmount`를 모든 행에 컬럼으로 두고 Normal/Hard/Hell은 0으로 채운 이유: `EnemyRecord.SplitCount`/`SplitChildId`가 Splitter 종에만 의미 있고 나머지는 0인 것과 동일 패턴(사각 테이블 유지, 레코드별 서브클래스 안 만듦) — `DifficultyManager.GetInfiniteStepCount()`가 `InfiniteStepSeconds <= 0`이면 0을 반환하도록 하면 별도 분기 없이 전체 난이도가 동일 계산식을 탐.
- 클리어 조건(마지막 웨이브 도달 시각)은 이 테이블에 중복 저장하지 않음 — `WaveTable.GetFinalPhaseStartTime()`을 그대로 참조(웨이브 테이블이 바뀌어도 자동 반영, 데이터 중복 방지). 클리어 조건 자체(생존 판정 로직)도 난이도별 분기가 없는 공통 로직이라 이 테이블 범위 밖.

## 주의
- CSV `Level` 컬럼 문자열은 `eDifficultyLevel` enum 이름과 정확히 일치해야 함(대소문자 무시, 철자 다르면 `Enum.Parse` 예외 → `LoadCsvTable`의 try-catch가 전체 파싱을 통째로 삼켜 4행 전부 빈 리스트가 될 수 있음 — 다른 enum 필드 테이블과 동일한 리스크).
- `Id`(1~4)와 `eDifficultyLevel` enum 값(0~3, Normal=0 등)은 서로 다른 체계 — Id로 enum 값이나 순서를 암묵 추론하지 말 것(`Level`/`NextId` 필드로만 판단).
- `DifficultyManager`가 `GetDifficultyMultiplier()`/`GetShardMultiplier()`를 핫패스(매 프레임/매 스폰)에서 호출하므로, `GetRecordByLevel()`(내부 `List.Find()`)을 매번 다시 타지 않도록 `Init()` 시점에 결과를 캐싱해서 사용해야 함(`DifficultyManager.m_CurrentRecord`).

## 작업 내역

### 2026-07-22-0
- 개요: 신규 생성 — `DifficultyManager.cs`의 하드코딩 switch문 3개(`GetDifficultyMultiplier`/`GetShardMultiplier`/`GetNextDifficulty`) + 상수 2개(`INFINITE_STEP_SECONDS`/`INFINITE_STEP_AMOUNT`)를 이 테이블로 대체.
- 파일: Assets/Scripts/Table/DifficultyRecord.cs(신규), Assets/Resources/Table/DifficultyTable.csv(신규), Assets/Scripts/Glory/Table/TableManager.cs(로드/등록 3줄)
- 검증: [[DifficultyManager]] 2026-07-22-1 참고 — Play Mode에서 테이블 로드/배율 계산/언락 체인 전부 리팩토링 전과 동일한 값으로 동작하는 것 확인, 컴파일/콘솔 에러 0건.
