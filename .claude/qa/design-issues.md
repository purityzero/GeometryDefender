# QA — Design 이슈 (기획/밸런싱)

`qa-tester` 에이전트가 자동 플레이테스트에서 관찰한 **기획적 판단이 필요한 사항**을 기록한다. 코드가 의도대로 동작하지만 수치/난이도/페이싱이 기획 의도와 다르게 느껴지는 경우 — 구현 자체가 잘못된 버그는 [client-issues.md](./client-issues.md)로.

밸런스 판단 기준은 [Assets/Design/08_balance.html](../../Assets/Design/08_balance.html)의 목표 곡선(평균 10~12분 생존, 10분 시점 DPS-처리량 교차)을 참고한다. 여기 기록은 **결론이 아니라 관찰** — 실제 수치 조정은 기획 검토 후 별도로 진행.

형식: 날짜, 관찰 내용, 근거(영상 타임스탬프/콘솔 로그/테이블 수치), 관련 테이블·기획 문서, 제안(있다면).

---

## 2026-07-27-0 — Normal 난이도 3판 전부 60초 안팎에 타워 사망 (목표 10~12분 대비 큰 격차)

### 개요
카드 306/307("최강 타겟팅"/"최속 타겟팅") 제거 QA 겸 초반 밸런스 관찰. TitleScene→`Btn_Play`→`Item_Normal`(난이도: Normal)→InGameScene 실제 클릭 경로로 독립된 3판을 진행. 매 레벨업 카드 드래프트마다 최고 등급(Legendary>Epic>Rare>Common) 카드를 선택. `execute_code`로 `TimerManager.elapsedTime`/`ActorPlayer.currentHp`/`XpManager.currentLevel`/`MonsterManager.killCount`를 매 폴링마다 직접 조회(영상 대신 수치로 확인).

### 관찰 (수치)
| 회차 | 사망 시점 | 사망 시 레벨 | 킬 수 | 직전 체크포인트 |
|---|---|---|---|---|
| 1 | ~44.9초 | 3 | 29 | - |
| 2 | t=49.7초까지 HP 100/100 유지 → t=64.8초 HP 0/100 확인 (그 사이 약 15초 안에 급격히 소진) | 5 | 56 | 49.7초까지 무피해 |
| 3 | ~47.9초 | 4 | 34 | t=33.5초까지 HP 100/100 |

세 판 모두 **60~65초 이내 타워 사망**. [08_balance.html](../../Assets/Design/08_balance.html) 목표(평균 10~12분=600~720초 생존)의 약 1/10 수준.

### 특이 패턴 (회차 2)
회차 2는 t=49.7초까지 HP 100/100(무피해)이었다가, 다음 폴링(t=64.8초)에 이미 HP 0으로 게임오버 상태였다 — 서서히 깎인 게 아니라 짧은 구간(약 15초) 안에 전체 체력이 소진된 것으로 보인다. 이 판에서 레벨3에 Legendary "Phoenix"(#406, ReviveOnce, 부활 시 HP 50%) 카드까지 뽑았는데도 사망까지 이어졌다 — 코드 확인 결과 `ActorPlayer.TakeDamage()`의 부활 로직 자체는 정상(1회성이라 재사용 안 되는 게 의도된 설계, [ActorPlayer.md](../class/ActorPlayer.md) 참고)이라 **부활 카드의 버그는 아님**, 부활 이후에도 두 번째로 치명타를 맞았다는 뜻. 정확히 몇 마리/어떤 웨이브가 이 급격한 피해를 냈는지는 이번 조사에서 프레임 단위로는 못 짚었다 — 다만 세 판 모두 일관되게 1분 안팎에 끝난 것을 보면 우연한 이상치는 아닌 것으로 보인다.

### 제안 (결론 아님, 기획 검토용)
- SpawnManager의 페이즈/스폰 램프(초반 유예 시간, 웨이브당 스폰량)가 Normal 기준으로 너무 이르게/많이 강해지는 것은 아닌지 확인 필요.
- 혹은 반대로, 이번 세션에서 다룬 카드 풀 자체(방어 카드 비중, MaxHp 카드의 절대치 등)가 초반 생존력을 못 받쳐주는 것일 수도 있음 — 3판 다 등급 최우선으로만 골라서 방어 카테고리를 매번 못 뽑았을 가능성 있음(예: 회차 1은 Weapon/Utility 위주). 방어 카테고리를 의도적으로 우선한 회차와 비교가 필요.
- 회차 2의 "무피해 구간 → 급격한 전멸" 패턴이 재현되는지, 재현된다면 특정 웨이브/몬스터 조합(엘리트 동시 등장 등)이 원인인지 별도 확인 권장.

### 관련 테이블/문서
- [Assets/Design/08_balance.html](../../Assets/Design/08_balance.html)
- Assets/Resources/Table/WaveTable.csv, WaveSpawnTable.csv (미확인 — 다음 조사 대상)

### 후속 조치 (2026-07-27, 코디네이터가 직접 원인 규명 + 1차 조정)
qa-tester 리포트를 넘겨받아 코드/테이블을 직접 분석. WaveTable Phase1(0~120초)은 NormalWeight=100(전부 기본 Normal, Elite 없음)이라 스폰 구성 문제는 아니었음 — 원인은 기본 타워(DPS≈26.25)의 킬레이트(≈1.31/s, Normal HP20 기준)가 스폰레이트 공식을 약 26~29초 지점부터 못 따라잡기 시작하는데, 단일 타겟 + 웨이포인트 경로상 사거리(구 5.0) 체류 시간이 겨우 ~3.3초라 그 뒤로 밀린 몬스터가 죽지 못하고 그대로 기지에 도달 → 데미지 누적이 눈덩이처럼 불어나는 구조. 3판 다 기획 문서 자체의 레벨업 벤치마크와 비슷한 속도로 성장했음에도 죽었다는 점에서, "카드/레벨이 느려서"가 아니라 이 구조적 병목이 원인으로 확정.

**1차 조정**: `TowerTable.csv` CentralTower Range 5.0→7.0(사거리 체류 시간 ~3.3초→~6.4초), `GameConfigTable.csv` TowerMaxHp 100→150·SpawnRampGraceSeconds 15→30. 상세는 [TowerRecord.md](../class/TowerRecord.md) 2026-07-27-2, [GameConfigRecord.md](../class/GameConfigRecord.md) 2026-07-27-3 참고. **정밀 튜닝(정확히 10~12분)은 아님** — 다음 QA에서 실제 생존 시간 재측정 필요.

### 2차 조정 (2026-07-27) — Normal 난이도 배율 추가 완화
사용자 요청("노말 난이도 조금 더 하향시켜줘"). 1차 조정(Range/TowerMaxHp/GraceSeconds)은 전 난이도 공통 적용이라, 이번엔 **Normal 전용 손잡이**로 `DifficultyTable.csv`의 `DifficultyMultiplier`(스폰레이트·적 HP·DamageToBase에 전부 곱해지는 배율)를 1.0→0.8로 낮췄다. 이 배율은 `SpawnManager`/`MonsterManager` 양쪽에서 곱해지므로 스폰 속도와 적 스탯이 동시에 20% 완화된다.
- 계산: 스폰레이트가 킬레이트(1.31/s)를 앞지르는 시점이 배율 0.8 적용 시 약 27.7초(램프 기준) → 그레이스(30초) 포함 실제 약 57.7초 지점으로 밀림(1차 조정 후 44초 대비 추가로 약 14초 더 유예).
- Hard(1.3)/Hell(1.6)/Infinite는 절대값 그대로 유지 — Normal만 낮아졌으므로 난이도 간 격차는 오히려 더 커짐(의도된 것으로 판단, Normal은 온보딩 난이도이므로).
- **정밀 튜닝 아님** — 다음 QA에서 실제 생존 시간 재측정 필요.

---

## 2026-07-27-1 — "게임이 정신 사납다" 진단 중 관찰: 다중 무기 빌드에서 레벨업 카드 드래프트가 지나치게 자주 뜸

### 개요
[client-issues.md 2026-07-27-5](./client-issues.md)(투사체 색상 충돌) 진단을 위해 무기 5종(중앙 타워+Archer+Mage+ChainCoil+HomingPod) 전부 장착 + Double Shot/Pierce/Splash/Chain/Homing 카드를 전부 적용한 상태로 자연 스폰 기반 전투를 관찰하는 과정에서, **킬레이트가 극단적으로 빨라지면서(무기 5개가 각자 독립 쿨다운으로 동시에 발사) 레벨업 카드 드래프트 팝업이 몇 초에 한 번꼴로 반복해서 뜨는 것**을 확인. `execute_code` 폴링 기준 약 15초 사이에 draft 팝업이 2회 이상 발생하는 구간 존재(HP 135/170·킬86 → HP 101/170·킬125 사이, 약 22초 동안 최소 1회 이상). 드래프트 화면은 게임을 일시정지시키므로, "일시정지→3장 중 선택→재개"가 짧은 간격으로 반복되면 그 자체로 흐름이 끊기고 산만한 느낌을 준다 — 사용자가 지목한 "투사체" 자체와는 별개 원인이지만, 같은 "정신 사납다" 체감에 함께 기여할 가능성이 있어 참고로 남긴다.

### 근거
- `qa_20260727_232301.mp4` 녹화 구간에서 카드 드래프트 프레임이 전체 23프레임 중 다수 비중을 차지(약 0.53초 분량 영상에도 드래프트 화면이 2회 이상 포착됨 — 실제 게임 시간으로는 훨씬 잦은 빈도).
- 킬 카운트 진행이 매우 빠름: 00:41에 29킬, 01:00에 48킬, 01:30에 86킬, 01:52에 125킬, 02:22에 204킬 — 이 페이스면 레벨업(정확한 킬 임계값은 미확인, XpManager 참고 필요)이 지속적으로 자주 발생.

### 제안 (결론 아님, 기획 검토용)
- 이 관찰은 "무기 5개 + 스택 카드"라는 극단적 빌드 조건에서 나온 것이라, 일반적인 진행 페이스(레벨업 3~5회 이내로 무기 1~2개만 있는 초중반)에서도 동일하게 문제인지는 별도 확인 필요.
- 문제로 판단되면: 레벨업 킬 임계값을 무기 개수/DPS에 비례해 동적으로 늘리거나, 드래프트를 매번 멈추지 않고 짧은 토스트+백그라운드 자동 선택(예: 일정 레벨 이상부터는 리롤 없이 자동 최상위 등급 선택) 같은 완화책을 검토할 수 있음 — 다만 이건 기획 의도(항상 신중하게 선택하게 하고 싶은지, 템포를 우선할 것인지)에 달려있어 QA 판단 범위 밖.

### 관련 클래스/테이블
- XpManager(레벨업 임계값 로직, 이번 조사에서 코드까지는 확인 안 함)
- [[CardManager]], [[ActorPlayer]] (무기 독립 쿨다운 구조)

---

## 2026-07-29-0 — 메타 트리 전부 해금 상태에서도 Normal이 114~176초에 사망 (2026-07-27-0 후속, 개선됐지만 목표엔 여전히 크게 미달)

### 개요
사용자 요청("게임데이터 리셋하고... 노말/하드/헬 깨는기준으로 니가 스스로 락풀어서 업그레이드 하고 플레이 좀 해봐") — 이번 세션에 변경된 시스템(투사체 스윕 충돌, 무기 6종 재조정, 카드 시스템 대개편, 메타 트리 21노드 확장, WaveTable 세분화) 통합 QA. `PlayerPrefs`(`PlayerData`/`OptionData`/`AssetData`) 삭제로 세이브 리셋 후, `PlayerManager.instance.UnlockMetaNode()`로 `MetaTreeTable`의 21개 노드 전부 해금(StartingPower MaxHp+60/DamagePercent+60%/RangePercent+25%/AttackSpeedPercent+10% 전부 포함, 최댓값 상태). TitleScene→`Btn_Play`→`Item_Normal` 실제 클릭 경로로 Normal 난이도를 독립 3회 시도, 매번 5배속(`Time.timeScale=5`).

### 관찰 (3회 독립 시도, 카드 드래프트 선택 전략만 다르게)
| 회차 | 카드 선택 전략 | 사망 시점 | 사망 시 maxHp | weaponCount |
|---|---|---|---|---|
| 1 | 항상 드래프트 첫 번째 카드 선택(전략 없음) | 175.8초 | 230 (MaxHp 카드 획득) | 미확인 |
| 2 | Offense/Speed 카테고리 우선 선택 | 114.2초 | 210 (증가 없음) | 1 (끝까지 증가 없음) |
| 3 | Weapon > Offense/Speed > Defense 우선순위로 재선택 | 119.9초 | 210 (증가 없음) | 1 (6회 레벨업 동안 Weapon 카드 단 한 번도 안 뜸) |

세 회차 모두 [08_balance.html](../../Assets/Design/08_balance.html) 목표(600~720초)의 **약 20~30% 수준**에서 사망. 2026-07-27-0의 1·2차 조정(Range 5→7, TowerMaxHp 100→150, SpawnRampGraceSeconds 15→30, Normal DifficultyMultiplier 1.0→0.8) 이후 사망 시점이 44~65초 → 114~176초로 약 2~3배 개선됐으나, 메타 트리를 **전부** 해금한 최상급 상태에서도 여전히 목표에 크게 못 미친다.

### 근거 — 스폰레이트 vs 킬레이트 재계산 (수치로 재확인)
`SpawnManager.UpdatePhaseSpawn()`: `spawnRate(t) = SpawnBaseRate(1.0) × (1 + max(0, t-30)/60)^1.3 × DifficultyMultiplier`. Normal=0.8배.
`TowerTable.csv` CentralTower: Damage=10, AttackInterval=0.4s → 메타 전부 해금 시 Damage×1.6(DamagePercent+60%)=16, AttackInterval÷1.1(AttackSpeedPercent+10%)≈0.364s → 유효 DPS≈43.96. `EnemyTable.csv` Normal(Id=1) MaxHp=20 → 킬당 약 0.455초 필요(단일 타겟, `DefaultTargeting=Closest`).
`spawnInterval = 킬소요시간`을 만족하는 t를 역산하면 **t≈100.5초** — 실제 사망 시점(114~120초, 회차 2·3)과 거의 정확히 일치. 즉 메타 트리를 전부 해금해 얻는 딜/공속 보너스를 반영해도, 단일 무기(중앙 타워)만으로는 스폰레이트를 100초 전후로 따라잡지 못하게 되는 구조적 한계가 그대로 남아있다.

콘솔 `[ActorPlayer] TakeDamage` 로그(회차 3)에서 `amount:5`(EnemyTable Normal의 DamageToBase=5)가 연속으로 찍힘 — 기지에 도달한 몬스터가 거의 전부 기본 Normal 종족이고, 죽지 못한 몬스터가 끊김 없이 계속 기지에 도달하고 있음을 뒷받침.

### 신규 관찰 — Weapon 카테고리 카드(추가 무기 해금)가 핵심 완화 수단인데 등장 확률이 낮음(수치 계산)
`CardTable.csv`의 `Category=Weapon` 카드(601~605, Archer/Mage/ChainCoil/HomingPod/LaserSpinner 해금)는 전부 `Rarity=Epic`. 회차 3에서 Weapon 카테고리를 최우선으로 고르도록 로직을 짰음에도, 사망까지 6회 연속 레벨업 드래프트(회당 3장) 중 **단 한 번도 Weapon 카드가 후보 3장에 포함되지 않았다**(weaponCount가 시작~사망까지 계속 1).

`CardManager.cs`의 `RARITY_WEIGHTS`(Common 60/Rare 25/Epic 12/Legendary 3, 100 만점) 기준 카드 한 슬롯이 Epic일 확률은 12%. `CardTable.csv` 전체 Epic 카드는 16장(Defense 2/Offense 2/Utility 7/Weapon 5)인데 균등 추첨이라면 그중 Weapon은 5/16≈31.3% — 즉 슬롯 하나가 정확히 Weapon 카드일 확률은 0.12×0.313≈3.75%, 드래프트 3장 기준 "이번 드래프트에 Weapon이 한 장이라도 포함될 확률"은 약 1-(1-0.0375)³≈10.8%. 이 값대로면 **6회 연속 드래프트에서 전부 못 뽑을 확률이 약 (1-0.108)⁶≈50%**로, 이번 회차 3의 결과(0/6)는 통계적으로 딱히 이례적인 불운도 아니다 — 즉 "두 번째 무기를 100초 안에 못 구할 확률"이 설계상 이미 상당히 높다는 뜻.

참고로 `GameConfigTable.csv`의 `PityThreshold=5`(연속 5회 Epic 이상 미획득 시 다음 드래프트 1장을 Epic/Legendary로 강제)로 인한 **레어도 천장 시스템은 이미 존재**하지만, 이 천장은 등급만 보장할 뿐 **카테고리(Weapon)까지 보장하지는 않는다** — 천장이 발동해도 위 계산대로 31.3% 확률로만 Weapon이 걸린다. 회차 3의 경우 6회 레벨업 중 몇 번째부터 천장이 발동했는지까지는 이번 조사에서 로그로 확인하지 않았음(추가 확인 필요 시 `CardManager.m_PitySinceEpic` 폴링 권장).

### 제안 (결론 아님, 기획 검토용)
- 레어도 천장(`PityThreshold`)과 별개로, "N회 레벨업 안에 Weapon 카테고리 미보유 시 다음 드래프트에 Weapon 1장 강제 포함" 같은 카테고리 전용 보장(또는 최소 Weapon 등장 가중치 상향)을 검토할 것 — 현재는 등급 천장만 있고 카테고리 천장이 없어, 등급 천장이 발동해도 Weapon이 아닌 다른 카테고리(Utility 7장 등)로 소모될 수 있음.
- 또는 단일 무기 자체의 초반 DPS(Damage/AttackInterval)나 스폰 커브(`SpawnRateExponent=1.3`, `SpawnRampGraceSeconds=30`)를 추가로 조정해, 두 번째 무기 없이도 100초보다 더 오래 버틸 수 있게 하는 방향도 가능 — 다만 2026-07-27-0에서 이미 두 차례 조정한 값이라 추가로 낮추면 Hard/Hell과의 난이도 격차가 더 벌어질 수 있음(현재 Hard=1.3배, Hell=1.6배는 이번 세션에서 그대로 유지, Normal 전용 배율만 손댐).
- 카드 선택 전략에 따라 사망 시점이 크게 달라지는 것(114초 vs 176초) 자체가, 초반 생존이 "본인이 원하는 카테고리를 못 뽑을 리스크"에 크게 좌우된다는 뜻 — 드래프트 카테고리 밸런스(가중치) 자체를 검토 대상으로 삼을 만하다.

### 검증 못한 부분
- 세 판 모두 클리어(600초 도달)에 실패해 `DifficultyManager.OnCleared`→`UnlockNextDifficulty()` 자동 언락 체인은 **런타임으로 검증 못함**(정적 코드 확인상 `DifficultyManager.cs`의 클리어 판정·언락 호출 자체는 정상적으로 연결되어 있음).
- Hard/Hell은 `PlayerManager.instance.UnlockDifficulty()`로 강제 해금 후 구조적 동작(씬 로드, `DifficultyMultiplier` 1.3/1.6 정상 적용, 카드 드래프트 정상 동작, 에러 0건)만 짧게(약 20~35초) 확인 — Normal의 패턴상 더 일찍 죽을 것이 명백해 보여 전체 600초 완주 시도는 하지 않음.

### 관련 클래스/테이블
- [SpawnManager.cs](../../Assets/Scripts/InGame/SpawnManager.cs), [ActorPlayer.cs](../../Assets/Scripts/InGame/Actor/ActorPlayer.cs)
- Assets/Resources/Table/{WaveTable,DifficultyTable,EnemyTable,TowerTable,CardTable,MetaTreeTable,GameConfigTable}.csv
- 이전 조사: 본 파일 2026-07-27-0(1차/2차 조정 이력)

### 참고 — 이번 QA 진행 중 발견한 환경 이슈(코드 버그 아님)
- Text Animator(Febucci) 핫 리로드 NRE(`TypewriterComponent`/`TextAnimatorComponentBase`)가 이번 세션 첫 2회 Play 진입 시 재현됨 — 기존에 알려진 이슈([[QARecorder]] 관련 문서 및 사용자 메모리 `project_febucci_hotreload_bug` 참고). Stop→Play 2회 반복 후에는 재현 없이 이후 세션 전부 클린했음(이번 세션 git status상 Play 중 재컴파일 이력이 있던 에디터 세션이라는 설명과 일치). 새 버그 아님, 기록만 남김.
- `watch:watch` 스킬로 녹화 영상을 직접 눈으로 리뷰하려 했으나, 이 머신에 Python 인터프리터가 설치돼 있지 않아(`python`/`python3`/`py` 전부 PATH에 없음) 스킬 실행 자체가 불가능했음 — 대신 콘솔의 `[ActorPlayer] TakeDamage` 로그와 스폰레이트/킬레이트 수식 역산으로 원인을 정량 검증(영상 없이도 결론 신뢰도는 충분하다고 판단). 영상 리뷰가 필요하면 Python 설치 후 재시도 필요. 녹화 파일 자체는 `QA_Recordings/`에 3개(`qa_20260729_160256.mp4`, `qa_20260729_161111.mp4`, `qa_20260729_161805.mp4`) 보존됨.

---
