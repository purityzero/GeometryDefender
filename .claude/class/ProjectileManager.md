# ProjectileManager

연관 클래스: `MonsterManager`(거의 동일한 구조 — Entity 스폰/풀 관리/만료 처리 패턴을 그대로 대칭 복제), `TowerController`(호출 주체), `ActorProjectile`/`Actor`/`FactoryObject`(시각 오브젝트), `MemoryPoolFactory<T,TEnum>`/[[Factory]](타입별 풀링 — 2026-07-22부터 `Create()` 시점에 타입을 스스로 기억해 `Recycle(T)` 한 인자로 반납 가능), `ProjectileRecord`/`ProjectileTable`/`eProjectileType`(데이터), `ProjectileTag`/`ProjectileStats`/`ProjectileMotion`/`ProjectileExpiredTag`(ECS 컴포넌트), `ProjectileMoveSystem`/`ProjectileCollisionSystem`/`ProjectileVisualSyncSystem`(ECS 시스템), `VisualObject`(ECS↔Transform 동기화, Entity→시각 오브젝트 조회의 유일한 소스 — 2026-07-22-3부터 별도 Dictionary 없이 이것만으로 반납 처리)

## 2026-07-22-4 — Basic 미사일 크기 축소 + MemoryPool 동작 검증
사용자 요청("미사일도 조금 작게 만들어주고, memoryPool 정상작동 하는지도 체크해봐") — `ProjectileTable.csv`의 Basic(Id=1) `Size`를 `0.3` → `0.22`로 축소(현재 실제로 발사되는 유일한 타입이라 이것만 조정, 나머지 4종은 미사용 placeholder라 유지).

### 검증 (Play Mode, 실제 Title→Btn_Play→InGame 흐름)
리플렉션으로 `m_ProjectileFactory`의 `m_MemoryPoolDictionary`/`m_ObjectTypeDictionary`, 각 `MemoryPooling`의 `m_ActiveList`/`m_HideList`를 직접 조회:
- `TowerController.Fire()` 직접 호출 직후: Basic pool `active=2 hidden=18`, `m_ObjectTypeDictionary` 카운트도 2로 정확히 일치(팩토리가 타입을 스스로 추적하는 게 실측 확인됨, [[Factory]] 2026-07-22-0).
- 2초 후(투사체 소멸 후): `active=0 hidden=20`, `m_ObjectTypeDictionary`도 0으로 복귀 — 반납 시 누수 없음.
- 5배속으로 몇 초 방치해 킬카운트가 143까지 자연 상승한 상태에서도 active/hidden 합이 항상 20(POOL_SIZE)으로 유지 — 풀이 커지거나 줄지 않고 정상 순환.
- 축소된 크기 반영 확인: 신규 발사된 투사체의 `ProjectileStats.Radius=0.22`, 시각 `localScale=(0.20,0.20,0.20)`(`(0.22×2)/2.22`) — 테이블 값이 ECS 스탯과 비주얼 스케일 양쪽에 정확히 반영됨.

### 검증 중 발견한 별개 이슈 (이번 수정 범위 밖, 수정 안 함)
- **TableManager 미초기화 관측 — 원인 특정됨, 버그 아니라 테스트 방법론 문제**: 검증 초반 한 차례 `TableManager.m_isInitialized=False`, `m_TableDictionary` 비어있어 모든 매니저의 `Init()`이 조용히 실패한 상태를 관측했으나, 이는 실제 버그가 아니라 **Play를 InGameScene이 열린 상태(또는 TitleScene을 거치지 않은 상태)로 시작**해서 생긴 현상이었다(사용자 확인). `GameManager.Awake()` → `TableManager.instance.init()` 부트스트랩 체인은 TitleScene에서만 자연스럽게 실행되고, InGameScene 단독 진입 경로에는 이를 트리거하는 코드가 없다(이미 [[MonsterManager]] md 2026-07-19-0에 "InGame 단독 플레이 시 테이블 미로드"로 기록돼 있던 것과 동일한 근본 원인). **재발 방지**: Play Mode 검증을 시작할 때는 항상 에디터의 활성 씬이 TitleScene인지 먼저 확인하고, 아니면 TitleScene을 먼저 열고 나서 Play해야 한다 — 이번 세션 memory에 [[feedback_qa_playmode_scene_check]]로 기록.
- **Text Animator(Febucci) NRE 스팸 — 원인 특정 및 조치 완료(2026-07-22, 후속 조사)**: `Editor.log` 직접 분석 + Play 중 재컴파일 재현으로 확정. TitleScene의 `Text_Title` 오브젝트(`TextAnimator_TMP`+`TypewriterComponent`, OnEnable 자동재생)가 **Play Mode 도중 스크립트를 재컴파일(hot reload)하면 영구히 깨짐** — Febucci 패키지 내부 `initialized`(bool)는 리로드 후에도 true로 남지만 실제 `_wrapper` 객체는 재구성 안 되고 null이 되어 이후 매 프레임 `Animate()`가 NRE. Toast/UIToastMessage와는 무관(직접 재현 시도했지만 재현 안 됐음), 원인은 오직 "Play 중 재컴파일" 상황. 우리 코드가 아닌 서드파티 패키지 내부 문제라 코드는 안 건드리고, Unity 에디터 설정(`Script Changes While Playing` → `Stop Playing And Recompile`, `EditorPrefs.ScriptChangesDuringPlayOptions=2`, 사용자 승인 후 적용)으로 재발 방지 — 재컴파일 전 자동으로 Play가 멈춰 `Application.isPlaying==false`가 되면서 Febucci 자체 가드가 정상 동작함. 상세는 memory `project_febucci_hotreload_bug` 참고.

## 2026-07-22-3 — m_DicVisual 자체를 제거 (아래 2026-07-22-2 struct 교체를 대체)
사용자 지적("m_DicVisual FactoryPool 사용하면 되지않나?")으로 재검토 — 결론은 [[Factory]] 2026-07-22-0 참고(`MemoryPoolFactory`가 `Create()` 시점에 타입을 스스로 기억하도록 개선, `Recycle(TEnum,T)` → `Recycle(T)`로 시그니처 단순화). 그 결과 `m_DicVisual`(바로 아래 항목에서 struct로 바꿨던 것) 자체가 통째로 필요 없어져 제거.
- 제거: `private Dictionary<Entity, ProjectileVisualEntry> m_DicVisual` 필드 및 `ProjectileVisualEntry` struct 전체.
- `SpawnVisual()`: `m_DicVisual[_entity] = ...` 한 줄만 삭제.
- `RecycleVisual()`: `m_EntityManager.GetComponentObject<VisualObject>(_entity).transform.GetComponent<ActorProjectile>()`로 Actor를 직접 얻어 `m_ProjectileFactory.Recycle(actorProjectile)` 호출로 대체 — ECS `VisualObject`(entity→시각 Transform)가 이미 있는 정보라 별도 역참조 테이블이 불필요했음(entity→시각 오브젝트 관계를 두 곳에서 중복 추적하던 셈).
- 검증: `refresh_unity`(force+compile) → `read_console` 에러/경고 0건.

## 2026-07-22-2 — 튜플 → struct 교체 (2026-07-22-3에서 구조 자체가 제거됨, 과정 기록용으로 유지)
사용자가 `(eProjectileType type, ActorProjectile actor)` 튜플 사용을 지적("정말 싫어해서")하여 `private struct ProjectileVisualEntry { public eProjectileType Type; public ActorProjectile Actor; }`로 교체.
- class 대신 struct 선택 이유: 필드 2개(enum+참조)뿐인 순수 데이터, `Dictionary<Entity, T>` 값으로 인라인 저장되어 class보다 힙 할당이 없음 — 동작/생명주기 없는 값 홀더라 struct가 더 적합.
- `m_DicVisual[_entity] = new ProjectileVisualEntry { Type = ..., Actor = ... }`, `RecycleVisual()`의 `visual.type`/`visual.actor` → `visual.Type`/`visual.Actor`로 변경.

## 2026-07-22 업데이트 — ProjectileTable 도입
사용자 요청("무기(Projectile)도 테이블로 관리할 수 있게 해줘 — 크기, 데미지, 타입, 모형 등")로 하드코딩돼 있던 투사체 스펙(반지름 상수, 프리팹 경로 상수)을 `Assets/Resources/Table/ProjectileTable.csv`로 이전.
- 컬럼: `Id,Type(eProjectileType),ColorHex,Size,TrailDuration,DamageMultiplier,Pierce,SplashRadius,ChainJumps,ChainRadius,PrefabPath`. Splash/Chain 관련 컬럼은 값만 채워두고 실제 로직은 아직 없음(`ProjectileCollisionSystem`이 Pierce=0 고정 처리만 함) — [[TowerController]]의 "이번 패스 범위" 참고, 5종 데이터는 다 있지만 동작은 Basic만 구현됨.
- `TowerRecord.ProjectileId`(신규 필드)로 어느 타워가 어느 투사체 레코드를 쓸지 지정 — 카드 시스템이 나중에 이 값을 갈아끼울 확장 지점(`ITargetingStrategy`와 동일 설계 사상).
- `Fire()` 시그니처 변경: `_radius`(float 직접 전달) → `_projectileId`(int, 테이블 조회)로 교체. 데미지는 `_baseDamage × record.DamageMultiplier`로 계산(현재 전부 1.0 — 카드 시스템 확장 지점).
- 풀링을 `MemoryPooling<ActorProjectile>`(단일 풀) → `MemoryPoolFactory<ActorProjectile, eProjectileType>`(타입별 풀, `MonsterManager`와 동일 패턴)로 교체 — 지금은 5종 레코드가 전부 같은 `Prefabs/Projectile/Basic.prefab`을 가리키지만, 나중에 타입별 실제 프리팹이 생겨도 코드 변경 없이 바로 대응 가능.
- 시각 스케일은 `record.Size`(반지름)에서 역산(`(Size×2)/PREFAB_NATIVE_DIAMETER`), 색상은 `record.ColorHex`를 `ActorProjectile.SetColor()`로 적용(2026-07-22 색공간 수정 포함, [[ActorMonster]] 참고).

## 개요
`SceneSingleton` + `IUpdatable`(`MonsterManager`와 동일 패턴). 투사체 ECS 엔티티 스폰(`Fire`), `MemoryPoolFactory<ActorProjectile, eProjectileType>` 기반 타입별 시각 오브젝트 풀 관리, `ProjectileExpiredTag` 붙은 엔티티의 매 프레임 정리(반납+`DestroyEntity`)를 담당.

## 경로
Assets/Scripts/InGame/ProjectileManager.cs

## ECS 시스템 개요 (Assets/Scripts/InGame/ECS/)
- `ProjectileMoveSystem`(`ISystem`, `MoveSystem`과 동일 스타일): `Direction×Speed×deltaTime`만큼 이동, `SpawnPosition`에서 `MaxDistance` 넘으면 `ProjectileExpiredTag` 부여.
- `ProjectileCollisionSystem`(`ISystem`, `HealthSystem`과 동일 스타일): naive O(N×M) 원형 거리 판정(`ProjectileStats.Radius + CombatRadius.Value`). 명중 시 대상의 기존 `DamageRequest` 버퍼에 Add(새 데미지 파이프라인 안 만들고 몬스터 쪽 기존 시스템 재사용) + 투사체에 `ProjectileExpiredTag` 부여. **Spatial Hash Grid 최적화는 안 함**(기획서도 "후반부before" 최적화로 명시, 지금 규모에 naive로 충분 — 후속 작업).
- `ProjectileVisualSyncSystem`: `VisualSyncSystem`을 `ProjectileTag` 대상으로 그대로 복제.
- `CombatRadius`(`IComponentData`): 몬스터의 충돌 반경. `MonsterManager.Spawn()`에서 `EnemyRecord.VisualSize * 0.5f`로 근사 계산해 추가(2026-07-22, 투사체 충돌용으로 신설 — 정확한 스프라이트 바운즈 대신 근사치).

## 흐름
- `Init()`: `ProjectileTable`의 각 레코드로 enum→경로 맵을 만들어 `MemoryPoolFactory<ActorProjectile, eProjectileType>(pathMap, 20, m_PoolParent)` 구성/Prewarm + `ProjectileExpiredTag` 쿼리 준비.
- `Fire(Vector2 from, to, damage, speed, range, projectileId)`: `ProjectileTable.GetRecordById()`로 레코드 조회 → 방향 계산 → Entity 생성(`ProjectileTag`/`ProjectileStats`/`ProjectileMotion`/`LocalTransform`) → `SpawnVisual()`에서 풀에서 `ActorProjectile` 하나 꺼내 `VisualObject`로 연결.
- `UpdateLogic()`: 매 프레임 `ProjectileExpiredTag` 쿼리 처리 — `RecycleVisual()`(entity의 `VisualObject.transform`에서 `ActorProjectile`을 직접 얻어 `m_ProjectileFactory.Recycle(actor)` 호출, 2026-07-22-3) + `DestroyEntity`(`MonsterManager.ProcessDeadMonsters`와 대칭 구조).

## 신규 에셋
- `Assets/Resources/Prefabs/Projectile/Basic.prefab` — `Triangle.prefab`과 동일 구조(Transform+SpriteRenderer+`ActorProjectile`).
- `Assets/Resources/Mat/GlowMat_ProjectileBasic.mat`(`Shader Graphs/Glow`, `_Color=#00e5ff`, `_GlowAmount=1`, `_MainTexture=shape_circle.png`) — **전용 에셋, 다른 오브젝트와 공유 안 함**(이전에 겪은 "공유 머테리얼이 트윈 도중 값에 오염되는 사고" 재발 방지 원칙 적용, [[GlowAmountTweenEffect]] 참고).

## 검증 (2026-07-22, Play Mode)
- `TowerController.Fire()`(리플렉션으로 수동 호출) 직후 `ProjectileTag` 쿼리 카운트가 증가하는 것 확인.
- 자동 전투 루프에서 `MonsterManager.killCount`가 지속 상승 — 투사체 생성→이동→충돌→데미지→몬스터 사망까지 전체 파이프라인이 실제로 도는 것 확인.
- 콘솔 에러 0건.

## 미검증
- 투사체가 실제로 "타워→적 방향"으로 정확히 날아가는 궤적을 스크린샷 한 프레임으로 육안 캡처하지는 못함(발사~명중까지가 매우 짧아 스크린샷 타이밍상 못 잡음) — 대신 `killCount` 지속 상승으로 종단 간(end-to-end) 동작을 간접 확인.
