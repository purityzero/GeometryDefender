# Diamond (Assets/Resources/Prefabs/Monster/Diamond.prefab)

연관 스크립트: [[ActorMonster]](루트 부착), [[CullingObject]](루트 부착, 2026-07-27 신규), FadeTweenEffect/TweenEffectPlayer(자식 DiamondGlow, 2026-07-27 신규)

## 개요
몬스터 타입 중 하나. 루트 GameObject에 Transform + SpriteRenderer + ActorMonster + CullingObject, 자식으로 halo 전용 오브젝트(DiamondGlow) 보유. `MonsterManager` → `MemoryPoolFactory<ActorMonster, eMonsterType>` 풀에서 Pop/Push된다.

## 계층 구조
```
Diamond (fileID: 4061516438042263973)
├─ Transform (fileID: 3978401736774714502)                     — m_LocalScale 1,1,1 (실제 크기는 EnemyTable.VisualSize가 스폰마다 덮어씀)
├─ SpriteRenderer (fileID: 5040216242497316485)                — sprite guid 4181f9c255bc7cb42b0496069bb94b75, material GlowMat_Diamond_Normal
├─ ActorMonster (fileID: 7133491914644655348)                  — m_Renderer → 5040216242497316485, m_GlowRenderer → 7442844165583270274(DiamondGlow의 SpriteRenderer, 2026-07-27-2), m_CullingObject → 9001000000000000001
├─ CullingObject (fileID: 9001000000000000001)                 — m_ObjectRenderer → 5040216242497316485, m_RectTransform 비움
└─ DiamondGlow (fileID: 8031306640437663400, 신규 2026-07-27-2) — halo 자식, localScale 1,1,1(2026-07-27-3에서 1.4→1로 정정)
   ├─ Transform (fileID: 2012967459224556077)
   ├─ SpriteRenderer (fileID: 7442844165583270274)               — 코어와 동일 sprite, sortingOrder 3(코어 0보다 위, 2026-07-27-3에서 -1→3으로 정정), material GlowMat_Diamond_Halo(guid d74214640a8fe914284f7aecc0a222ab)
   ├─ FadeTweenEffect — Duration 3, Ease Linear, TargetAlpha 0
   └─ TweenEffectPlayer — m_Effects → FadeTweenEffect, LoopCount -1, LoopType Yoyo
```

## 작업 내역

### 2026-07-27-0

#### 개요
사용자 요청("인게임에서 CullingObject 적용해줘") — 몬스터만 대상으로 확정. WayPoint의 자동 반경 계산상 몬스터가 항상 화면 밖에서 스폰되므로 컬링 실이득 있음을 확인 후 적용.

#### 파일
- Assets/Resources/Prefabs/Monster/Diamond.prefab

#### 수정 (오브젝트 단위)
**Diamond (루트)**
- 전: `m_Component` = [Transform, SpriteRenderer, ActorMonster] 3개
- 후: `m_Component`에 CullingObject(fileID 9001000000000000001) 추가, 4개
- 신규 MonoBehaviour 블록: `m_Script` guid `45503f694daaf81468c57c5d86dfb963`(CullingObject.cs.meta에서 확인), `m_ObjectRenderer: {fileID: 5040216242497316485}`, `m_RectTransform: {fileID: 0}`

#### 검증
Unity MCP 도구가 이번 세션에 잡히지 않아 YAML 직접 편집(PREFAB.MD "MCP 미연결" 경로)으로 진행 — **미검증**(Unity 에디터 컴파일/Play Mode 확인 못 함). `mcp__ide__getDiagnostics`는 C# 컴파일만 확인 가능하고 프리팹 YAML 정합성은 검증 범위 밖.

---

### 2026-07-27-1 — 구동 주체를 Pooling.cs에서 ActorMonster/MonsterManager로 이동

#### 개요
사용자 지적("CullingObject를 Pooling에서 관리할게 아니라 MonsterManager에서 관리해야하는거 아니야?") — `MemoryPooling<T>`(여러 무관한 풀에 재사용되는 공용 클래스)에 컬링 캐싱 로직을 얹었던 2026-07-27-0의 구현을 되돌리고, 몬스터 전용 지식은 몬스터 쪽 코드(`ActorMonster`/`MonsterManager`)에만 두도록 재설계. 상세는 [[ActorMonster]], [[MonsterManager]], [[Pooling]], [[Factory]] 2026-07-27 항목 참고.

#### 파일
- Assets/Resources/Prefabs/Monster/Diamond.prefab

#### 수정 (오브젝트 단위)
**ActorMonster (fileID 7133491914644655348)**
- 전: `m_Renderer`만 보유
- 후: `m_CullingObject: {fileID: 9001000000000000001}` 필드 추가 — 같은 오브젝트의 CullingObject 컴포넌트를 직렬화 참조로 연결(런타임 GetComponent 없이 캐싱).

#### 검증
포커스 재부여로 Unity 에디터 실제 재컴파일/재임포트 확인 완료 — `Tundra build success`, 이 프리팹 재임포트 시 Missing Script 등 파싱 에러 없음. **Play Mode 실측은 여전히 미검증.**

---

### 2026-07-27-2 — TitleScene 헥사곤 halo 방식 Glow 추가 + 2026-07-27-3 스케일/정렬순서 정정
사용자 요청("일반 몬스터에도 Glow효과 맞게끔 넣어주고") — `DiamondGlow` 자식 추가, `ActorMonster.m_GlowRenderer` 연결(상세 근거는 [[ActorMonster]] 2026-07-27-2 참고). 이후 사용자가 InGameScene `ActorPlayer`의 halo를 직접 고친 기준(거의 동일 크기 + 코어보다 위)에 맞춰 `m_LocalScale` 1.4→1, `m_SortingOrder` -1→3 정정(상세 근거는 [[Triangle]](prefab) 2026-07-27-3 참고). 컴파일 에러 0건. **Play Mode 시각 확인은 사용자가 직접 진행 예정.**
