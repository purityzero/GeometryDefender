# OrbitalSystem / ProjectileEffects / EnemySpeciesData / CardEffectState

[[card-draft]] 스펙 구현 과정에서 신설된 소규모 ECS 컴포넌트/시스템 4종. 개별 문서를 나누지 않고(기존 프로젝트도 소규모 컴포넌트는 관련 매니저 문서에 합치는 관례 — [[ProjectileManager]] 참고) 이 문서 하나로 묶어 기록.

## ProjectileEffects (IComponentData)
경로: Assets/Scripts/InGame/ECS/ProjectileEffects.cs
필드: `Pierce`(int), `SplashRadius`(float), `ChainJumps`(int), `ChainRadius`(float), `IsHoming`(bool), `HomingTarget`(Entity).
`ProjectileManager.Fire()`가 매 발사마다 부착. `ProjectileCollisionSystem`/`ProjectileMoveSystem`이 소비. 같은 파일에 `OrbitalTag`(빈 태그)와 `OrbitalData`(Center/Radius/AngularSpeed/AngleOffset/DamageCooldownTimer)도 함께 정의.

## OrbitalSystem (ISystem)
경로: Assets/Scripts/InGame/ECS/OrbitalSystem.cs
`[UpdateInGroup(typeof(SimulationSystemGroup))][UpdateAfter(typeof(ProjectileMoveSystem))]`. `OrbitalTag` 붙은 엔티티를 `angle = AngleOffset + SystemAPI.Time.ElapsedTime × AngularSpeed`로 매 프레임 회전 배치, 몬스터와 겹치면 `GameConfigTable.ORBITAL_DAMAGE_TICK_INTERVAL`(기본 0.5초, 2026-07-24 const→GameConfigTable 이관, [[GameConfigRecord]] 2026-07-24-0 참고) 쿨다운으로 데미지. 만료 없음(카드 보유 중 상시 유지). Orbital Ring(#503) 카드 전담.

## EnemySpeciesData (IComponentData)
경로: Assets/Scripts/InGame/ECS/EnemySpeciesData.cs
필드: `Species`(eEnemySpecies) 1개. `MonsterManager.Spawn()`에서 부착, `TowerController.Fire()`가 타겟 조회해 Triangle Hunter(#108) 배율 판정.

## CardEffectState (정적 클래스, MonoBehaviour 아님)
경로: Assets/Scripts/InGame/CardEffectState.cs
`public static float TimeSlowMultiplier = 1f;` + `Reset()`. ECS `ISystem`(구조체)이 `CardManager`(MonoBehaviour)를 직접 참조할 수 없어 만든 매개 클래스 — Time Slow(#504) 카드가 `CardManager.ApplyCardEffect()`에서 이 값을 곱하고, `MoveSystem.OnUpdate()`가 매 프레임 읽어 이동속도에 곱연산([[MoveSystem]] 2026-07-24-0 참고).

## 미검증
Unity MCP 미연결, 컴파일/Play 확인 안 됨 — 신규 ECS 컴포넌트/시스템 4종 전부 Burst 컴파일 오류 가능성이 가장 높은 지점.
