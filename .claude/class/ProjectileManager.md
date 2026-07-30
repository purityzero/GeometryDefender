# ProjectileManager

## 2026-07-30-3 — Orbital Ring 오브에 주황 Glow + 주황↔노랑 색상 Tween
사용자 요청("오비탈 링도 주황색으로 Glow효과, Tween효과 빨강-주황으로" → "빨강을 빼고 노랑으로") — [[ActorPlayer]] 2026-07-30-4(Frost Orb Turret)와 동일한 `Material._GlowAmount`/`DOColor` Yoyo 루프 방식을 재사용. 색상 트윈은 최종적으로 주황(1,0.55,0) ↔ 노랑(1,0.9,0.1).

### 파일
- Assets/Scripts/InGame/ProjectileManager.cs
- Assets/Scripts/InGame/Actor/ActorProjectile.cs

### 수정
- `SpawnVisual(Entity, ProjectileRecord, float)`: 반환형 `void`→`ActorProjectile`(생성된 인스턴스 반환, 실패 시 null). 기존 `Fire()` 호출부는 반환값을 그냥 버리므로 무수정 호환.
- 신규 `ApplyOrbitalGlowEffect(ActorProjectile)`: 주황(`1,0.55,0`) 기본색 설정 후 `TweenUtil.Color(material, 빨강.linear, ORBITAL_RING_COLOR_TWEEN_DURATION).SetLoops(-1, Yoyo)` + `TweenUtil.Float(material, "_GlowAmount", ORBITAL_RING_GLOW_MAX, ORBITAL_RING_GLOW_PULSE_DURATION).SetLoops(-1, Yoyo)`. `SpawnOrbitals()`에서 각 오브 생성 직후 호출.
- **풀링 오염 주의**: Orbital Ring의 오브는 Frost Orb Turret 비주얼(비영구, `ResUtil.Create` 1회성)과 달리 `MemoryPoolFactory`로 풀링되는 인스턴스라, 무한 루프 Tween을 건 채로 반납되면 다음에 그 오브젝트를 재사용하는 **전혀 다른 투사체**가 엉뚱하게 계속 반짝이게 된다. `ActorProjectile.Close()`(반납 시 항상 호출)에 `DOTween.Kill(material)` + `_GlowAmount`를 기본값(1)으로 복귀시키는 정리 코드를 추가해 방지.
- 신규 GameConfigTable 상수: `ORBITAL_RING_GLOW_MIN`(1)/`MAX`(2.5)/`GLOW_PULSE_DURATION`(1초)/`COLOR_TWEEN_DURATION`(1.5초) — Frost Orb Turret의 `ORBITAL_SLOW_*`와 별개 상수(서로 다른 기능이라 분리).

### 검증
컴파일 확인 필요. Play Mode 미검증 — Orbital Ring 오브가 주황~빨강으로 반짝이는지, 만료/재사용 후 다른 투사체가 반짝임을 물려받지 않는지 확인 필요.

---

## 2026-07-30-2 — SpawnOrbitals/SpawnVisual에 시각 크기 배율 파라미터 추가
사용자 요청("오비탈링도... 좀 살짝 더 크게") — `SpawnOrbitals(..., float _visualScaleMultiplier = 1f)`/`SpawnVisual(Entity, ProjectileRecord, float _visualScaleMultiplier = 1f)` 둘 다 선택적 배율 파라미터 신설(기본값 1이라 기존 호출부는 무수정 호환). `visualScale` 계산식 끝에 배율을 곱함. [[CardManager]] 2026-07-30-4에서 Orbital Ring 카드 호출 시 1.3 전달.

---

## 2026-07-30-1 — Mortar 전용 투사체 타입 신설
[[ActorPlayer]] 2026-07-30-3(신규 무기 Mortar #8) 참고. `eProjectileType`에 `Mortar` 추가(2026-07-29-0에서 실측으로 확인된 함정과 동일 — enum에 값이 없으면 해당 CSV 행 전체가 조용히 로드 실패하므로 반드시 함께 추가). `ProjectileTable.csv` Id=7(청동색 #C77B3D, Size 0.35로 다른 투사체보다 크게, TrailDuration 0.3으로 "묵직한 포탄" 느낌) 신설, `TowerTable.csv` Mortar 행의 `ProjectileId=7`로 연결.

### 검증
컴파일 확인 필요. Play Mode 미검증 — `ProjectileTable.GetRecordById(7)`가 정상 조회되는지(enum 누락 시 조용히 null이 되는 함정 재확인).

---

## 2026-07-30-0 — Orbital Ring이 화면에서 안 움직이던 버그 수정 (ProjectileVisualSyncSystem 쿼리 누락)

### 개요
사용자 피드백("오비탈 링 버그있음 움직이도 않고 뭘 하는건지 모르겠음"). 원인: `ProjectileVisualSyncSystem`이 `ProjectileTag`를 가진 엔티티만 쿼리해서 시각 Transform에 위치를 복사하는데, `SpawnOrbitals()`가 만드는 엔티티는 `OrbitalTag`만 갖고 `ProjectileTag`는 없다. `OrbitalSystem` 자체는 `LocalTransform.Position`을 매 프레임 정확히 회전 궤도로 갱신하고 있었지만, 그 값을 실제 화면의 `ActorProjectile` GameObject로 복사해주는 시스템이 없어 시각 오브젝트가 스폰 위치(Center)에 정지된 채로 보였던 것 — 데이터/로직은 정상, 시각 동기화만 누락된 케이스.

### 파일
- Assets/Scripts/InGame/ECS/ProjectileVisualSyncSystem.cs

### 수정
`OnCreate()`의 쿼리를 `GetEntityQuery(LocalTransform, ProjectileTag)`(AND) → `EntityQueryDesc { All = [LocalTransform], Any = [ProjectileTag, OrbitalTag] }`로 변경 — 두 태그 중 하나만 있어도 동기화 대상에 포함되도록 완화.

### 검증
컴파일 확인 필요. Play Mode 미검증 — Orbital Ring 카드 획득 후 실제로 타워 주위를 도는지, 몬스터 접촉 시 데미지 틱이 들어가는지 확인 필요.

---

## 2026-07-29-0 — Archer 전용 투사체 타입(Rapid) 신설 (색상 불일치 수정)
사용자 요청("래피드 무기는 색상 변경해야할듯?") 조사 중 발견 — `TowerTable.csv`의 Archer(Id=1) `ColorHex`(무기 쿨다운 게이지 색)를 바꿔도, 실제 날아가는 투사체 색은 `ProjectileId=1`(Basic)을 CentralTower와 공유하고 있어 그대로 시안색(`#00e5ff`)이었다 — 게이지 색과 실제 총알 색이 어긋나는 불일치. `ProjectileTable.csv`에 Archer 전용 행(Id=6, Type=Rapid, ColorHex=#FF5E3A, Size=0.18/TrailDuration=0.15 — 다른 투사체보다 작고 트레일이 짧아 "가볍고 빠른" 연사 느낌) 신설, `TowerTable.csv` Archer 행의 `ProjectileId`를 1→6으로 변경. `ProjectileRecord.cs`의 `eProjectileType` enum에 `Rapid` 값 추가 필요(CSV의 `Type` 컬럼이 이 enum으로 파싱되는데, 정의에 없는 문자열이면 **해당 행 자체가 통째로 로드 실패**하는 걸 실측으로 확인 — 에러 로그도 없이 조용히 스킵됨, CLAUDE.md 데이터 레이어 버그 유형 (1)과 유사한 함정).

### 검증
Play Mode(TitleScene→Play→Normal 실클릭) — `ProjectileTable.list.Count=6` 확인, `TowerTable.GetRecordById(1).ProjectileId=6` → `ProjectileTable.GetRecordById(6)`가 정상 조회됨(`ColorHex=#FF5E3A`, `Type=Rapid`) 확인. 처음엔 enum 값 추가를 빠뜨려 6번 레코드가 통째로 `null`이 되는 걸 실측으로 발견 → enum 추가 후 재확인해 해결. 콘솔 에러 0건.

## 2026-07-27-2 — 효과 아이콘 오버레이 호출부 제거 (2026-07-23-4 되돌림)
사용자 요청("아이콘 오버레이 없애줘") — [[ActorProjectile]] 2026-07-27-1과 세트. `SpawnVisual()`에서 `actorProjectile.SetEffectIcons(...)` 호출 삭제, 이제 이 메서드 안에서 `_cardEffects`를 전혀 안 쓰게 되어 매개변수 자체도 제거(`SpawnVisual(Entity, ProjectileRecord)`로 시그니처 축소). `Fire()`의 호출부도 `SpawnVisual(entity, record, _cardEffects)` → `SpawnVisual(entity, record)`로 인자 축소. `Fire()`가 `m_EntityManager.AddComponentData(entity, _cardEffects)`로 ECS 쪽에 카드 효과를 붙이는 부분은 그대로 유지(관통/스플래시/체인/호밍 실제 게임 로직은 무관 — 시각 아이콘만 제거).

검증: 컴파일 에러 0건. Play Mode에서 CentralTower 자동 발사 중 콘솔 에러 0건 확인.

## 2026-07-24-0 — 카드 드래프트 시스템용 확장
[[card-draft]] 스펙 구현. `Fire()` 시그니처에 `ProjectileEffects _cardEffects = default` 파라미터 추가 — 발사 엔티티에 `ProjectileEffects`(Pierce/SplashRadius/ChainJumps·Radius/IsHoming/HomingTarget) 컴포넌트를 그대로 부착, `ProjectileStats.Pierce = record.Pierce + _cardEffects.Pierce`(테이블 기본값 + 카드 누적).

신규 `SpawnOrbitals(Vector2 _center, int _count, int _damage, float _radius, float _orbitDistance)`(public) — Orbital Ring(#503) 카드용. `_count`개 엔티티를 `OrbitalTag`+`ProjectileStats`+`OrbitalData`(균등 분배된 `AngleOffset`)+`LocalTransform`으로 생성, 시각화는 기존 `SpawnVisual`(Basic `ProjectileRecord`)을 그대로 재사용. 이동/데미지는 `ProjectileMoveSystem`이 아니라 신규 `OrbitalSystem`이 전담(태그로 분리되어 있어 서로 간섭 없음).

`ProjectileCollisionSystem`/`ProjectileMoveSystem` 쿼리에 `RefRW/RefRO<ProjectileEffects>`가 필수 컴포넌트로 추가됨 — `ProjectileTag`를 가진 모든 엔티티는 `Fire()`를 거쳐 생성되므로 항상 `ProjectileEffects`도 함께 붙는다(누락되는 경로 없음, 쿼리 매칭 불일치 없음).

### 미검증
Unity MCP 미연결, 컴파일/Play 확인 안 됨 — Burst 컴파일 오류 가능성이 가장 높은 지점(신규 ECS 컴포넌트/시스템).


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

## 2026-07-28-0 — ProjectileRecord.Alpha 신설 (투사체 색상 다운톤)
사용자 지적("무기 알파값 적용 안된거 같아 ActorProjectile alpha 조정하는게 없는데?") — [[TowerRecord]]/[[ActorPlayer]] 2026-07-28-0에서 무기 색(TowerTable.ColorHex, 무기 쿨다운 게이지/Laser 비주얼용)에만 Alpha를 적용했는데, 실제로 화면에 날아가는 투사체 스프라이트 색은 별개 테이블(`ProjectileTable.ColorHex`, `SpawnVisual()`이 소비)이라 놓치고 있었던 지점 — 사용자가 발견.
- `ProjectileRecord`에 `Alpha`(float) 컬럼 신설, `ProjectileTable.csv` 5행 전부 `Alpha=0.4`(TowerTable과 동일 다운톤 값, "무기 전체적으로 alpha를 다운톤 시켜줘" 요청 연장선).
- `SpawnVisual(Entity, ProjectileRecord)` — `ColorUtility.TryParseHtmlString()` 결과의 `.a`를 `_record.Alpha`로 덮어쓴 뒤 `actorProjectile.SetColor()` 호출(기존엔 항상 알파 1).
- 검증: `mcp__ide__getDiagnostics` 컴파일 에러 0건(기존 스타일 힌트만 존재, 이번 변경과 무관). Play Mode 미검증(Unity MCP 미연결).

## 2026-07-22 업데이트 — ProjectileTable 도입
사용자 요청("무기(Projectile)도 테이블로 관리할 수 있게 해줘 — 크기, 데미지, 타입, 모형 등")로 하드코딩돼 있던 투사체 스펙(반지름 상수, 프리팹 경로 상수)을 `Assets/Resources/Table/ProjectileTable.csv`로 이전.
- 컬럼: `Id,Type(eProjectileType),ColorHex,Alpha(2026-07-28 신설),Size,TrailDuration,DamageMultiplier,Pierce,SplashRadius,ChainJumps,ChainRadius,PrefabPath`. Splash/Chain 관련 컬럼은 값만 채워두고 실제 로직은 아직 없음(`ProjectileCollisionSystem`이 Pierce=0 고정 처리만 함) — [[TowerController]]의 "이번 패스 범위" 참고, 5종 데이터는 다 있지만 동작은 Basic만 구현됨.
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
- `ProjectileVisualSyncSystem`: `VisualSyncSystem`을 `ProjectileTag` 대상으로 그대로 복제(2026-07-30부터 `OrbitalTag`도 Any 조건으로 포함 — 아래 2026-07-30-0 참고, 안 그러면 Orbital 엔티티가 시각적으로 안 움직임).
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

## 2026-07-23-0 — IUpdatable 등록 중앙화
사용자 요청("IUpdatable 인터페이스로 만들지 말고, UIBase 등등 최상위 클래스에서 등록") — 상세 배경은 [[SceneSingleton]] 2026-07-23-0 참고. 클래스 선언 `SceneSingleton<ProjectileManager>, IUpdatable` → `SceneSingleton<ProjectileManager>`(IUpdatable 제거), `Start()`(Register만 하던 것) 삭제, `UpdateLogic()` → `public override`, `OnDestroy()`의 수동 Unregister 호출 제거(`base.OnDestroy()`가 대신 처리, World 생존 확인 후 `m_ExpiredQuery` Dispose + 팩토리 Clear 로직은 그대로 유지). 미검증(컴파일/Play Mode 확인 필요).

## 2026-07-23-1 — 살아있는 투사체/오비탈 엔티티가 씬 전환 시 누수되던 버그 수정

### 개요
[[MonsterManager]] 2026-07-23-2와 동일한 버그가 여기도 있었음 — `World.DefaultGameObjectInjectionWorld`는 씬 언로드와 별개 생명주기라, `OnDestroy()`가 `m_ExpiredQuery`(만료된 것만)만 Dispose하고 **아직 만료되지 않은 투사체/오비탈 엔티티는 정리하지 않아** 다음 플레이 세션까지 좀비로 남을 수 있었음.

### 파일
- Assets/Scripts/InGame/ProjectileManager.cs

### 수정 (함수 단위)
**OnDestroy()**
- 전: World 생존 확인 후 `m_ExpiredQuery.Dispose();`만 수행.
- 후: 같은 블록 안에서 `ProjectileTag`/`OrbitalTag` 각각으로 새 쿼리를 만들어 `m_EntityManager.DestroyEntity(...)`로 살아있는 것까지 포함해 전부 파괴한 뒤 각 쿼리를 Dispose, 그 다음 `m_ExpiredQuery.Dispose()`.

### 검증
[[MonsterManager]] 2026-07-23-2에 기록된 실제 재현 테스트(런 도중 씬 전환 → 재플레이)에서 `ProjectileTag` 쿼리 카운트를 함께 확인 — 전환 후 0으로 정리됨. 콘솔 에러 0건.

### 관련 클래스
- [MonsterManager.md](../class/MonsterManager.md) 2026-07-23-2 — 동일 버그 패턴, 같이 발견/수정

### 2026-07-23-2 — 데미지 텍스트용 크리티컬 정보 전달
사용자 요청("데미지 폰트도 넣어줘") — 상세는 [[ProjectileStats]] 2026-07-23-0 참고. `Fire(...)`에 `bool _isCrit = false` 매개변수 추가, `ProjectileStats.IsCrit`에 그대로 설정. 검증: 컴파일 에러 0건, Play Mode 실측(타워 발사 → 몬스터 피격 시 데미지 텍스트 표시) 확인.

### 2026-07-23-3 — SceneSingleton → UpdatableBehaviour 전환(싱글톤 난립 정리)
사용자 지적("Manager가 너무 많지 않아?") — 개별 `.Current` 폐지, `InGameScene.Current.projectileManager`로 접근하도록 통일. `TowerController.cs`가 `InGameScene.Current.projectileManager.Fire(...)`로 호출하도록 변경. 상세 설계/버그/검증은 [[InGameScene]] 2026-07-23-1 참고.

### 2026-07-24-1 — const 전부 GameConfigTable로 이관
[[GameConfigRecord]] 2026-07-24-0 참고. `POOL_SIZE`/`PREFAB_NATIVE_DIAMETER` 제거 → `GameConfigTable.PROJECTILE_POOL_SIZE`/`PROJECTILE_PREFAB_NATIVE_DIAMETER` 참조. 같은 스윕으로 `ProjectileMoveSystem.HOMING_TURN_RATE`도 `GameConfigTable.PROJECTILE_HOMING_TURN_RATE`로 이관(별도 md 없이 이 문서의 "ECS 시스템 개요" 관례에 따라 여기 기록).
검증: 컴파일 에러 0건. Play Mode 재검증 미완료.

### 2026-07-23-4 — 투사체 다중 효과 아이콘 전달
사용자 요청("사격시스템 구현해줘") — 상세는 [[ActorProjectile]] 2026-07-23-0 참고.
- **Fire(...)**: `SpawnVisual(entity, record)` → `SpawnVisual(entity, record, _cardEffects)`로 카드 효과 구조체를 같이 넘기도록 변경.
- **SpawnVisual(Entity, ProjectileRecord, ProjectileEffects = default)**: 매개변수 추가(기본값 있어 `SpawnOrbitals()`의 기존 호출은 무수정으로 호환 — 오비탈은 카드 효과 대상이 아니라 항상 아이콘 전부 꺼짐). 끝에 `actorProjectile.SetEffectIcons(Pierce>0, SplashRadius>0f, ChainJumps>0, IsHoming)` 호출 추가.
- 검증: 컴파일 에러 0건, Play Mode 실측(4효과 동시 부여 시 아이콘 4개 동시 표시) 확인.
