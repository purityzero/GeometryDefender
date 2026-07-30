# ProjectileCollisionSystem

## 2026-07-30-0 — Pierce가 같은 대상을 반복 타격해 새 대상을 못 맞히던 버그 수정
사용자 보고("관통1이 관통을 안하고 관통2가 관통 1번만 하는듯"). 원인: 관통으로 살아남은 투사체가 다음 프레임에 다시 hitIndex 탐색을 할 때, 스윕 판정 시작점(`projectilePreviousPosition`)이 방금 맞은 대상 바로 옆이라 **같은 대상을 또 hitIndex로 잡아버려**, Pierce 스택이 새로운 대상이 아니라 이미 맞은 대상을 반복 타격하는 데 소모되고 있었다. 예: Pierce=1이면 1번째 대상을 맞히고 관통 스택을 소모해 살아남지만, 바로 다음 프레임에 "새 대상"이 아니라 "방금 그 대상"을 다시 맞혀 그대로 소멸 — 결과적으로 관통이 전혀 안 되는 것처럼 보임.

### 파일
- Assets/Scripts/InGame/ECS/ProjectileEffects.cs
- Assets/Scripts/InGame/ECS/ProjectileCollisionSystem.cs

### 수정
- `ProjectileEffects`에 `public Entity LastHitEntity;` 신설(기본값 `Entity.Null`, 명시적 초기화 불필요).
- `hitIndex` 탐색 루프 최상단에 `if (monsterEntities[i] == effects.ValueRO.LastHitEntity) continue;` 추가 — 마지막으로 맞힌 대상은 건너뜀.
- Pierce로 살아남는 분기(`effects.ValueRO.Pierce > 0`)에서 `effects.ValueRW.LastHitEntity = monsterEntities[hitIndex];`를 함께 기록.

### 검증
컴파일 확인 필요. Play Mode 미검증 — Pierce I만 보유 시 2마리, Pierce I+II(선행조건상 항상 같이 있음) 보유 시 4마리를 실제로 관통하는지 확인 필요.

---

## 연관 클래스
- ProjectileStats, ProjectileEffects, DamageRequest, MonsterTag, CombatRadius

## 현재 상태
- 경로: Assets/Scripts/InGame/ECS/ProjectileCollisionSystem.cs
- `ISystem`(unmanaged), `SimulationSystemGroup`, `[UpdateAfter(typeof(ProjectileMoveSystem))]`.
- naive O(N×M) 원형 거리 판정(몬스터/투사체 수가 적은 현재 규모에선 충분, 규모 커지면 Spatial Hash Grid로 교체 예정).
- 명중 시 `DamageRequest` 추가 → Chain Lightning(#304)/Splash I(#303) 카드 효과 처리(체인/스플래시는 배타적) → Pierce 스택 있으면 계속 날아감, 없으면 `ProjectileExpiredTag` 부착.

## 작업 내역

### 2026-07-23-0
- 개요: 사용자 요청("데미지 폰트도 넣어줘") — 데미지 텍스트가 치명타 스타일을 표시하려면 `DamageRequest`에 크리티컬 여부가 실려야 함.
- 파일: Assets/Scripts/InGame/ECS/ProjectileCollisionSystem.cs
- 수정 (함수 단위)
  - **OnUpdate()**: 명중/체인/스플래시 3곳의 `DamageRequest` 생성 코드 전부에 `IsCrit = stats.ValueRO.IsCrit` 추가 — 같은 발사체의 크리티컬 여부를 체인/스플래시로 튄 피해에도 동일하게 적용.
- 검증: 컴파일 에러 0건, Play Mode 실측(치명타 시 노란 텍스트, 일반 시 흰 텍스트 확인) — [[DamageRequest]] 2026-07-23-0, [[ProjectileStats]] 참고.

### 2026-07-24-0
- 개요: 사용자 요청("연쇄 데미지 이펙트나 이런것도 좀 넣어줘.. 뭐 연쇄는지 폭발하는지 알수가 없어") — Splash/Chain 분기에 시각 이펙트 트리거 추가. 데미지 계산/타겟 판정 로직은 전혀 건드리지 않음(시각만 추가, [[ChainLightning]] "설계 근거" 참고 — 체인 판정 기준점을 "진짜 순차 호핑"으로 바꿀지 검토했으나 게임플레이 부작용 우려로 되돌리고 순수 시각화만 추가).
- 파일: Assets/Scripts/InGame/ECS/ProjectileCollisionSystem.cs
- 수정 (함수 단위)
  - **OnUpdate()**: 명중 직후 `DamageTextManager damageTextManager = (InGameScene.Current != null) ? InGameScene.Current.damageTextManager : null;` 지역 변수 추가([[HealthSystem]] 2026-07-23-1과 동일 이중 null 체크 패턴).
  - **Chain 분기**: 명중 지점부터 시작하는 `List<Vector3> chainPoints`를 루프 중 함께 누적, 실제로 1체 이상 튀었으면(`chainPoints.Count > 1`) `damageTextManager?.ShowChainLightning(chainPoints)` 호출.
  - **Splash 분기**: 데미지 적용 루프 뒤에 `damageTextManager?.ShowSplashExplosion(splashPosition)` 호출(다른 대상에 실제로 맞았는지와 무관하게, Splash 카드 보유 상태에서 명중할 때마다 항상 재생 — "터졌다"는 시각 피드백 자체가 목적).
- 검증: 컴파일 에러 0건. Play Mode 실측 — `TowerController.SetSplash()`/`SetChain()`으로 강제 적용 후 자동 전투 수 초 진행, 콘솔 에러 0건. 상세는 [[SplashExplosion]]/[[ChainLightning]] 참고.

### 2026-07-29-0 — 터널링 버그 수정: 스냅샷 판정 → 스윕(선분) 판정 + QA 5회 재검증

#### 개요
사용자 보고("5배속으로 테스트하면 타워가 6분(게임시간 30분 상당)만에 죽는데, 1배속으로는 60분 넘게 안 죽는다") 조사 결과, 이 시스템의 명중 판정이 원인으로 확인됨. 기존엔 "현재 프레임 위치가 몬스터 반경 안인지"만 매 프레임 스냅샷으로 검사했는데, 배속이 오르면 프레임당 이동거리가 커져 투사체가 몬스터를 그냥 지나쳐도(터널링) 어느 프레임에도 반경 안에 걸리지 않아 명중 판정이 누락됐다. 1배속에선 거의 안 보이던 문제가 5배속에서 급격히 심해져 명중률이 떨어지고, 몬스터가 덜 죽어 타워가 game-time 기준으로도 더 빨리 죽었던 것으로 결론.

#### 파일
- Assets/Scripts/InGame/ECS/ProjectileCollisionSystem.cs
- Assets/Scripts/InGame/ECS/ProjectileMoveSystem.cs (`PreviousPosition` 기록, [[ProjectileMoveSystem]] 2026-07-29-0 참고)
- Assets/Scripts/InGame/ECS/ProjectileMotion.cs (`PreviousPosition` 필드 추가)

#### 수정 (함수 단위)
**신규 `ClosestPointOnSegment(float3 _segmentStart, float3 _segmentEnd, float3 _point)`**: 직전 위치→현재 위치 선분에서 몬스터 중심에 가장 가까운 지점을 구하는 순수 기하 함수.

**OnUpdate() 명중 판정**
- 전: `math.distancesq(projectilePosition, monsterPositions[i]) <= hitDistance * hitDistance` (현재 위치 스냅샷만 검사)
- 후: `ClosestPointOnSegment(projectilePreviousPosition, projectilePosition, monsterPositions[i])`로 이번 프레임 이동 구간 전체를 스윕 판정 — 프레임당 이동거리가 반지름보다 커져도(고배속) 사이 구간을 지나치면서 몬스터를 스친 것을 잡아낸다.

#### 검증 (Play Mode, QA 5회 반복)
Unity MCP로 TitleScene→Btn_Play(실제 클릭 이벤트)→Item_Normal→InGameScene 진입 후 5배속(`Time.timeScale=5`)에서 Play Mode 진입~종료를 5회 반복(그중 1회는 세션 초반 Febucci Text Animator 핫 리로드 NRE가 재현돼 즉시 Stop→재진입, 별도 카운트 안 함). 매회 다음을 재현:
- `MoveData` 없는 순수 정지 몬스터(HP=100)를 원격 좌표(9000,9000)에 직접 생성 + 몬스터 앞 1유닛 지점에서 Speed=60(1프레임에 몬스터를 확실히 지나치는 속도)으로 발사되는 합성 투사체를 생성해 "한 프레임에 순간이동하듯 통과"하는 터널링 상황을 재현.
- 5회 전부 몬스터 HP가 투사체 Damage만큼 정확히 감소(100→63/59/47/71/33, 각각 Damage 37/41/53/29/67과 정확히 일치)하고 투사체가 명중 후 정상 소멸함을 확인 — 수정 전이라면 스냅샷 판정상 "현재 위치가 몬스터를 지나쳐 반경 밖"이라 완전히 미스했을 상황.
- 실 게임플레이(5배속, ChainCoil 무기 추가 상태)에서도 매회 킬카운트가 정상 상승(17~56)하고 타워 HP는 5회 전부 무손상(150/150) 유지, 콘솔 에러 0건.
- 부수 발견: `UICardDraft`(레벨업 카드 드래프트)가 열려있는 동안 `InGameScene.ApplyFreezeState()`가 `SimulationSystemGroup.Enabled=false`로 **ECS 시스템 전체(이 시스템 포함)를 완전히 정지**시킨다는 걸 처음 테스트에서 확인(합성 투사체가 전혀 안 움직여서 발견) — 버그 아님, 의도된 동작. QA 시 이 정지 상태를 놓치면 관찰이 왜곡되므로 매 폴링마다 드래프트를 확인/해소해야 함(이미 `.claude/agents/qa-tester.md`에 반영된 절차).

#### 관련
- [[ProjectileMoveSystem]] 2026-07-29-0 — `PreviousPosition` 기록 측 수정
- `.claude/qa/client-issues.md` — 이번 세션 QA 요약
