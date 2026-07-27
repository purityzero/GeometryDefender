# ProjectileMoveSystem

연관 클래스: `ProjectileMotion`/`ProjectileEffects`/`ProjectileTag`/`ProjectileExpiredTag`(ECS 컴포넌트), `MonsterTag`/`DeadTag`/`ReachedEndTag`(ECS 태그), `ActorPlayer`(발사 시 `HomingTarget` 최초 대입), `ProjectileCollisionSystem`(이 시스템 다음 순서로 충돌 판정), `GameConfigRecord`(`PROJECTILE_HOMING_TURN_RATE`)

## 개요
매 프레임 투사체를 이동시키는 ECS `ISystem`. Homing Missile(#305) 카드가 적용된 투사체는 `ProjectileEffects.HomingTarget`을 향해 매 프레임 조금씩 방향을 트는(lerp) 방식으로 유도 이동을 구현한다.

## 경로
Assets/Scripts/InGame/ECS/ProjectileMoveSystem.cs

## 현재 상태
- `OnUpdate()`: `MonsterTag`(`DeadTag`/`ReachedEndTag` 제외) 조건으로 생존 몬스터 엔티티/위치를 `NativeList`로 한 번 수집한 뒤, `ProjectileTag`(`ProjectileExpiredTag` 제외) 투사체를 순회하며 이동 처리.
- 호밍 로직: `IsHoming == true`인 투사체는 `HomingTarget`이 여전히 유효한지(`SystemAPI.Exists` + `LocalTransform` 보유) 확인 → 무효화됐으면(목표가 죽거나 기지 도달로 파괴됨) `FindClosestMonster()`로 수집해둔 생존 몬스터 목록 중 투사체와 가장 가까운 엔티티를 새 `HomingTarget`으로 재대입 → 유효한 타겟이 있으면 그 방향으로 `PROJECTILE_HOMING_TURN_RATE × deltaTime` 비율만큼 `math.lerp`.
- 생존 몬스터가 하나도 없으면 재조준 실패(`Entity.Null`) → 직전 방향으로 직진(기존 동작과 동일하게 자연 감쇠).
- `NativeList<Entity>`/`NativeList<float3>`는 `Allocator.Temp` — `ProjectileCollisionSystem`과 동일 컨벤션으로 `OnUpdate()` 끝에서 명시적 `Dispose()`.

## 작업 내역

### 2026-07-27-0 — 호밍 미사일 타겟 소실 시 재조준 신규 구현

#### 개요
사용자 요청("호밍미사일은 내 타겟이 없어지면 자동으로 다른 타겟으로 다시 진로를 바꿔") — 기존엔 `HomingTarget`이 파괴되면(`SystemAPI.Exists`가 false) 조준 로직 자체를 건너뛰고 마지막 방향으로 그냥 직진하기만 했음.

#### 수정 (함수 단위)
**OnUpdate()**
- 전: `RefRO<ProjectileEffects>`로 조회, `HomingTarget` 무효 시 그냥 직진.
- 후: `RefRW<ProjectileEffects>`로 조회(재대입을 위해 쓰기 필요), 무효 시 사전에 수집해둔 생존 몬스터 목록에서 `FindClosestMonster()`로 가장 가까운 엔티티를 찾아 `effects.ValueRW.HomingTarget`에 재대입 후 계속 유도.

**신규 `FindClosestMonster(float3, NativeList<Entity>, NativeList<float3>)`**
- `ClosestTargetingStrategy`(발사 시점 최초 타겟 선정 로직)와 동일한 발상의 최단거리 탐색이나, 사거리 제한 없이(투사체는 이미 사거리 밖으로 나가 날아가는 중이라 원거리 재조준도 허용) 전체 생존 몬스터 중 최근접만 반환.

#### 설계 판단 — 원래 타겟팅 전략(Weakest 등) 유지 vs 단순 최근접 재조준
`ProjectileMoveSystem`은 순수 ECS `ISystem`이라 발사 당시 무기가 어떤 `ITargetingStrategy`를 썼는지(MonoBehaviour 쪽 `ActorPlayer.TowerWeapon`) 알 방법이 없다(구조적으로 분리됨). 사용자 요청도 "다른 타겟으로"라고만 했지 특정 전략을 명시하지 않아, 재조준은 항상 최근접 몬스터로 단순화 — 원래 전략을 그대로 이으려면 무기별 전략을 ECS 컴포넌트로 별도 직렬화해야 해서 이번 요청 범위를 크게 벗어난다고 판단.

#### 검증
IDE 진단 컴파일 에러 0건. Play Mode 실측 미완료 — Unity 에디터가 이 세션 내내 재컴파일 누적으로 인한 Text Animator 고장 상태라 정상 플레이 테스트 자체가 막혀있음([[UICheatWindow]] 2026-07-27-7 참고). 다음 세션에서 에디터 재시작 후: 호밍 카드 장착 → 발사 → 타겟 처치 → 투사체가 다른 생존 몬스터로 방향을 트는지 확인 필요.

---

### 2026-07-27-1 — 버그 수정: 타겟이 죽어도 재조준이 안 됨 (Exists()만으로는 부족)

#### 개요
사용자 실측 리포트("근데 안되던데?", "직선으로 계속 나가게 하지 말고") — 2026-07-27-0에서 구현한 재조준이 실제로는 작동 안 함.

#### 원인
몬스터는 죽어도 엔티티가 즉시 파괴되지 않는다. `HealthSystem`은 HP가 0 이하가 되면 커맨드 버퍼로 `DeadTag`만 붙이고, 실제 `EntityManager.DestroyEntity()`는 `MonsterManager.ProcessDeadMonsters()`(ECS 시스템이 아니라 `BaseScene.Update()`가 구동하는 일반 MonoBehaviour 폴링, [[MonsterManager]] 참고)에서 뒤늦게, ECS `SimulationSystemGroup`과는 별개 타이밍에 처리된다. 즉 "죽은 직후 ~ 실제 파괴되기 전" 사이 구간이 존재하는데, 이전 코드는 `SystemAPI.Exists(HomingTarget)`(엔티티가 완전히 사라졌는지)만 확인했기 때문에 이 구간 동안은 계속 "유효한 타겟"으로 오판 — 투사체가 이미 죽은(더 이상 갱신되지 않는) 몬스터의 마지막 위치를 향해 계속 직진하는 것처럼 보였다.

#### 수정 (함수 단위)
**OnUpdate() — hasValidTarget 판정**
- 전: `SystemAPI.Exists(target) && SystemAPI.HasComponent<LocalTransform>(target)`
- 후: 위 조건에 `&& SystemAPI.HasComponent<DeadTag>(target) == false && SystemAPI.HasComponent<ReachedEndTag>(target) == false` 추가 — 파괴를 기다리지 않고 `DeadTag`/`ReachedEndTag`가 붙는 즉시(다음 프레임, 커맨드 버퍼 playback 직후) 무효 판정 → 재조준.

#### 검증
IDE 진단 컴파일 에러 0건. **격리 ECS 테스트로 검증 완료** — InGameScene 전체 흐름 대신, `execute_code`로 TitleScene에서 몬스터 2마리(A/B)+호밍 투사체 엔티티를 직접 생성(A를 HomingTarget으로 지정한 뒤 A에 `DeadTag`만 붙이고 파괴는 안 함, 실제 게임의 "죽었지만 아직 안 파괴된" 구간을 그대로 재현) → 다음 틱 조회에서 `ProjectileEffects.HomingTarget`이 정확히 살아있는 B로 재대입됨을 확인(재조준 로직 확정 동작). 콘솔 에러 0건.

---

### 2026-07-27-2 — 호밍 미사일 전용 최대 생존시간 신설 (거리 기반 소멸이 안 먹히는 경우 대응)

#### 개요
사용자 리포트("호밍은 거리를 가서 삭제가 안되고, 20~30초 뒤에 사라지게 했으면 좋겠어"). 기존 소멸 판정(`travelDistance = distance(현재위치, 스폰위치)`)은 스폰 지점 기준 **직선 변위**만 본다 — 호밍 미사일은 재조준을 포함해 계속 방향을 트므로, 스폰 지점 근처를 맴돌며 누적 비행 시간과 무관하게 이 변위가 영영 `MaxDistance`를 못 넘을 수 있다(특히 HomingPod의 `Range=5.5`처럼 짧은 사거리 무기는 타워 근처를 맴돌기 쉬워 이 문제가 더 잘 드러남). 그 결과 호밍 미사일만 다른 투사체와 달리 소멸이 안 되고 계속 남아있는 것처럼 보였다.

#### 파일
- Assets/Scripts/InGame/ECS/ProjectileMotion.cs
- Assets/Scripts/Table/GameConfigRecord.cs
- Assets/Resources/Table/GameConfigTable.csv
- Assets/Scripts/InGame/ECS/ProjectileMoveSystem.cs

#### 수정 (함수 단위)
**`ProjectileMotion` 구조체**: `public float ElapsedTime;` 필드 추가(기본값 0, 매 프레임 누적).
**`GameConfigRecord.cs`**: `PROJECTILE_HOMING_MAX_LIFETIME`(기본 25f) 추가, CSV `ProjectileHomingMaxLifetime` 행(Id 55) 로드.
**`OnUpdate()` 소멸 판정**
- 전: `travelDistance >= MaxDistance`만으로 판정.
- 후: `motion.ValueRW.ElapsedTime += deltaTime;`로 누적 후, `isExpiredByDistance`(기존과 동일) `|| isExpiredByHomingLifetime`(`IsHoming == true && ElapsedTime >= PROJECTILE_HOMING_MAX_LIFETIME`) 중 하나라도 참이면 만료 — 논-호밍 투사체는 기존 거리 판정만 그대로 적용(동작 변화 없음).

#### 검증
IDE 진단 컴파일 에러 0건. **격리 ECS 테스트로 검증 완료** — 위와 같은 방식으로 `MaxDistance=999`(거리 판정으로는 절대 안 걸리게) 설정한 호밍 투사체를 만든 뒤, 약 25초 경과 후 재조회 → `ElapsedTime≈25.0`, `ProjectileExpiredTag` 정상 부착 확인(거리와 무관하게 시간 기준으로만 만료됨을 순수하게 격리 검증).
