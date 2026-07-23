# 미완료 작업

## 2026-07-24

### 개요
경험치(XP)/레벨업 시스템 + 카드 드래프트 시스템(전체 30장) 구현 완료. Unity MCP가 이번 세션 내내 미연결이라 전부 코드/YAML 직접 편집으로 진행했고, **컴파일도 실제 플레이도 한 번도 확인 못 했다.** 이전 2026-07-22 항목(Pierce/Splash/Homing/Chain 미구현, 카드 시스템 부재)은 이번 세션에서 전부 구현돼 아래 검증 대상으로 대체됨. 관련 문서: `.claude/design/xp-leveling.md`, `.claude/design/card-draft.md`, `.claude/class/XpManager.md`, `.claude/class/CardManager.md`, `.claude/class/CardRecord.md`, `.claude/class/OrbitalSystem.md`(ProjectileEffects/EnemySpeciesData/CardEffectState 포함), `.claude/class/TowerController.md`/`TowerHealth.md`/`MonsterManager.md`/`ProjectileManager.md`/`UICardDraft.md`/`UIInGameHUD.md`/`InGameScene.md`/`UIRunOver.md`/`RewardComponent.md`/`MoveSystem.md`의 2026-07-24 항목.

### 다음 세션 최우선 작업 — Unity 연결 후 검증
0. **완료**: 사용자가 보고한 "한글 깨짐" 현상 원인 확정 + 수정 완료(2026-07-23) — `DungGeunMo Bitmap` 폰트 에셋이 Dynamic 아틀라스(1024×1024) + Multi Atlas Textures 비활성화 상태라 아틀라스가 가득 차면 이후 새 글자가 영구히 깨지는 문제였음. `m_IsMultiAtlasTexturesEnabled`를 `1`로 변경해 해결, 실측 검증 완료. 상세는 [client-issues.md 2026-07-23-1](qa/client-issues.md) 참고.
1. **컴파일 확인 완료**(2026-07-23 QA 세션에서 확인, 에러 0건) — 특히 신규 ECS 컴포넌트/시스템(`ProjectileEffects`, `OrbitalSystem`, `EnemySpeciesData`, `ProjectileCollisionSystem`/`ProjectileMoveSystem`/`MoveSystem` 변경분)의 Burst 컴파일 문제 없음. `TowerController`의 `SceneSingleton<TowerController>` 전환도 타입 충돌 없음.
2. **Play Mode 핵심 루프 검증 — 부분 진전**: 2026-07-23 QA 세션에서 `World.DefaultGameObjectInjectionWorld`가 TitleScene→InGameScene 전환 후 null이 되는 기존 미해결 버그([client-issues.md 2026-07-23-0](qa/client-issues.md), 원조는 2026-07-21-1)에 막혔었으나, **같은 날 후속 세션(데미지 텍스트 기능 추가 중)에서는 TitleScene→Play→InGameScene 실제 흐름을 여러 차례 반복해도 이 블로커가 한 번도 재현되지 않고 몬스터 스폰/처치/런 종료까지 전부 정상 동작함** — 근본 원인 규명 없이 "우연히 안 걸림"일 수 있어 미해결로 남겨두지만, 다음 세션에서 재현 여부부터 다시 확인할 것(재현 안 되면 그 사이 있었던 Editor 재시작이 관련 있을 가능성).
   - **완료(2026-07-23)**: `Assets/Scripts/Glory/Scene/UpdatableBehaviour.cs:8`의 `OnEnable()`이 `BaseScene.Current.Register(this)`를 null 체크 없이 호출하던 문제 수정(`?.`로 변경, `OnDisable()`과 대칭 맞춤). World-null 블로커와 같은 뿌리인지는 확정 못했지만(별도 원인일 가능성 큼 — 아래 2026-07-23-1 참고), 최소한 이 지점의 크래시 위험은 제거됨.
   - **완료(2026-07-23) — 관련 있는 더 큰 원인 발견/수정**: `InGameScene` 매니저 접근을 `InGameScene.Current.xxx`로 중앙화하는 리팩토링(사용자 요청, "Manager가 너무 많지 않아?") 도중, `BaseScene.Current`(TitleScene/InGameScene 공유 슬롯)를 그대로 재사용하는 방식이 씬 전환마다 레이스를 일으켜 실제 NRE로 재현됨 — InGameScene이 자기 전용 독립 static을 갖도록 구조적으로 수정해 해결. 이 발견은 **"BaseScene.Current가 씬 전환 도중 예상보다 이르게 바뀔 수 있다"는 새로운 근거**라 World-null 블로커 원인 규명 시에도 참고할 가치가 있음 — 다만 World-null 자체는 별개 메커니즘(ECS World 생명주기)이라 이 수정으로 자동 해결되진 않을 것으로 예상. 상세는 [[InGameScene]] 2026-07-23-1, `project_shared_singleton_slot_race` 메모리 참고.
   - **해소된 것**: Febucci Text Animator NRE(타이틀 로고)는 Editor 재시작 후 재현 안 됨 — 이번엔 "에디터 세션 오염" 가설과 일치하게 해소됨(단, 근본 원인인 "Play 중 재컴파일 자체를 막는 예방"은 여전히 `Stop Playing And Recompile` 설정에 의존 — 설정이 풀리면 재발 가능).
   - **완료(2026-07-23)**: `MonsterManager.RecycleVisual()`의 `MissingReferenceException`은 근본 원인까지 확정하고 수정 완료 — ECS World가 씬 언로드와 별개 생명주기라 살아있는 몬스터/투사체/오비탈 엔티티가 세션 간 누수되던 버그였음(`OnDestroy()`에서 일괄 파괴하도록 수정). 실제 재현 시나리오(런 도중 이탈→재플레이→타워 사망까지)로 검증 완료, 콘솔 에러 0건. 상세는 [[MonsterManager]] 2026-07-23-2, [[ProjectileManager]] 2026-07-23-1, [client-issues.md 2026-07-23-2](qa/client-issues.md) 참고. **World-null 블로커(위 2번 항목)와는 별개 원인으로 판명** — World-null 자체는 여전히 미해결.
3. **씬/프리팹 YAML 배선 실제 반영 확인**: `InGameScene.unity`에 배치한 `XpManager`/`CardManager` 오브젝트, `UICardDraft.prefab`의 7개 필드 연결 + `Text_Title` 신규 UIText, `UIInGameHUD.prefab`의 `m_XpFillImage` — 전부 MCP `find_gameobjects`/`manage_components` 리소스 조회로 fileID가 실제 instance ID와 맞물리는지 재확인.
4. **카드 30장 중 특히 신규 서브시스템이 필요했던 것들 개별 테스트**: Pierce I/II(#105/#106), Splash I(#303), Chain Lightning(#304), Homing Missile(#305), Double Shot(#107), Triangle Hunter(#108), Shield Burst(#404), Berserker(#502), Orbital Ring(#503), Time Slow(#504), Vampire(#405), Phoenix(#406) — ECS 로직이 실제로 붙는지가 관건.

### 사용자 확인 대기 중 (결정 필요)
- **`UIInGameHUD.prefab` 외부 수정**: 이전에 "아이콘은 예전껄로 돌려놔"로 명시적으로 제거했던 `Icon_Hp`/`Icon_Timer`/`Icon_Kill`/`frame_capsule` 참조가, 이번 세션에 다시 파일을 열어보니 재등장해 있었다(내가 되돌린 적 없음 — 에디터에서 직접 편집/저장한 것으로 추정). 현재 상태(아이콘 있음) 그대로 둘지, 다시 지울지 사용자 답변 대기. 상세는 `.claude/class/UIInGameHUD.md` 2026-07-24-0, `.claude/prefab/UIInGameHUD.md` 2026-07-24-0 참고.

### 알려진 단순화 (버그 아님, 밸런스 조정 여지 — 검증 중 문제로 보이면 먼저 여기부터 확인)
- Pierce 관통 시 동일 프레임 재히트 방지 로직 없음.
- Orbital Ring은 오브 개별이 아니라 공용 0.5초 쿨다운.
- Homing은 물리 조향이 아니라 단순 lerp 회전.
- Berserker는 선형 커브만 지원.
- 근거는 전부 `.claude/design/card-draft.md` 상단 "2026-07-24 구현 완료" 절에 기록됨.
