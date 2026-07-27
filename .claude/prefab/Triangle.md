# Triangle (Assets/Resources/Prefabs/Monster/Triangle.prefab)

연관 스크립트: [[ActorMonster]](루트 부착), [[CullingObject]](루트 부착, 2026-07-27 신규), FadeTweenEffect/TweenEffectPlayer(자식 TriangleGlow, 2026-07-27 신규)

## 개요
몬스터 타입 중 하나. 루트 GameObject에 Transform + SpriteRenderer + ActorMonster + CullingObject, 자식으로 halo 전용 오브젝트(TriangleGlow) 보유. `MonsterManager` → `MemoryPoolFactory<ActorMonster, eMonsterType>` 풀에서 Pop/Push된다.

## 계층 구조
```
Triangle (fileID: 6569387957203651539)
├─ Transform (fileID: 7664721616041180917)                       — m_LocalScale 1,1,1 (실제 크기는 EnemyTable.VisualSize가 스폰마다 덮어씀, MonsterManager.cs:249)
├─ SpriteRenderer (fileID: 5219979027789072714)                  — sprite guid dd0cc48f04347ef4ca5cc5eb95d0a665, material GlowMat_Triangle_Normal
├─ ActorMonster (fileID: 8711757341023546138)                    — m_Renderer → 5219979027789072714, m_GlowRenderer → 2059400926474417918(TriangleGlow의 SpriteRenderer, 2026-07-27-2), m_CullingObject → 9001000000000000001
├─ CullingObject (fileID: 9001000000000000001)                   — m_ObjectRenderer → 5219979027789072714, m_RectTransform 비움
└─ TriangleGlow (fileID: 7466931005421000146, 신규 2026-07-27-2)  — halo 자식, localScale 1,1,1(2026-07-27-3에서 1.4→1로 정정, 아래 참고)
   ├─ Transform (fileID: 1601280225283626729)
   ├─ SpriteRenderer (fileID: 2059400926474417918)                — 코어와 동일 sprite, sortingOrder 3(코어 0보다 위, 2026-07-27-3에서 -1→3으로 정정), material GlowMat_Triangle_Halo(guid 607e95684ddb98b4da2b43af90591fd6, 코어와 별도 에셋)
   ├─ FadeTweenEffect (fileID: 232166809432260673)                — Duration 3, Ease Linear, TargetAlpha 0
   └─ TweenEffectPlayer (fileID: 8787209944316450279)             — m_Effects → FadeTweenEffect, LoopCount -1, LoopType Yoyo
```

## 작업 내역

### 2026-07-27-0

#### 개요
사용자 요청("인게임에서 CullingObject 적용해줘") — 몬스터만 대상으로 확정. WayPoint의 자동 반경 계산상 몬스터가 항상 화면 밖에서 스폰되므로 컬링 실이득 있음을 확인 후 적용.

#### 파일
- Assets/Resources/Prefabs/Monster/Triangle.prefab

#### 수정 (오브젝트 단위)
**Triangle (루트)**
- 전: `m_Component` = [Transform, SpriteRenderer, ActorMonster] 3개
- 후: `m_Component`에 CullingObject(fileID 9001000000000000001) 추가, 4개
- 신규 MonoBehaviour 블록: `m_Script` guid `45503f694daaf81468c57c5d86dfb963`(CullingObject.cs.meta에서 확인), `m_ObjectRenderer: {fileID: 5219979027789072714}`, `m_RectTransform: {fileID: 0}`

#### 검증
Unity MCP 도구가 이번 세션에 잡히지 않아 YAML 직접 편집(PREFAB.MD "MCP 미연결" 경로)으로 진행 — **미검증**(Unity 에디터 컴파일/Play Mode 확인 못 함). `mcp__ide__getDiagnostics`는 C# 컴파일만 확인 가능하고 프리팹 YAML 정합성은 검증 범위 밖.

---

### 2026-07-27-1 — 구동 주체를 Pooling.cs에서 ActorMonster/MonsterManager로 이동

#### 개요
사용자 지적("CullingObject를 Pooling에서 관리할게 아니라 MonsterManager에서 관리해야하는거 아니야?") — `MemoryPooling<T>`(여러 무관한 풀에 재사용되는 공용 클래스)에 컬링 캐싱 로직을 얹었던 2026-07-27-0의 구현을 되돌리고, 몬스터 전용 지식은 몬스터 쪽 코드(`ActorMonster`/`MonsterManager`)에만 두도록 재설계. 상세는 [[ActorMonster]], [[MonsterManager]], [[Pooling]], [[Factory]] 2026-07-27 항목 참고.

#### 파일
- Assets/Resources/Prefabs/Monster/Triangle.prefab

#### 수정 (오브젝트 단위)
**ActorMonster (fileID 8711757341023546138)**
- 전: `m_Renderer`만 보유
- 후: `m_CullingObject: {fileID: 9001000000000000001}` 필드 추가 — 같은 오브젝트의 CullingObject 컴포넌트를 직렬화 참조로 연결(런타임 GetComponent 없이 캐싱).

#### 검증
포커스 재부여로 Unity 에디터 실제 재컴파일/재임포트 확인 완료 — `Tundra build success`, 이 프리팹 재임포트 시 Missing Script 등 파싱 에러 없음. **Play Mode 실측은 여전히 미검증.**

---

### 2026-07-27-2 — TitleScene 헥사곤 halo 방식 Glow 추가

#### 개요
사용자 요청("일반 몬스터에도 Glow효과 맞게끔 넣어주고" — TitleScene 헥사곤 코어+halo 2계층 방식을 지칭) — 상세 설계 근거는 [[ActorMonster]] 2026-07-27-2, `.claude/class/CullingObject.md`가 아니라 TitleScene.md 2026-07-22-6~8 참고.

#### 파일
- Assets/Resources/Prefabs/Monster/Triangle.prefab
- Assets/Resources/Mat/Enemy/GlowMat_Triangle_Halo.mat (신규, `GlowMat_Triangle_Normal.mat` 복제 후 `_GlowAmount` 1→2.5)

#### 수정 (오브젝트 단위)
Unity MCP `manage_prefabs`(open_prefab_stage) + `manage_gameobject`/`manage_components`로 진행(구조적 변경, PREFAB.MD MCP 연결 경로).
- 루트에 자식 `TriangleGlow` 신규 생성(위 계층 구조 참고).
- `ActorMonster.m_GlowRenderer` 필드를 `TriangleGlow`의 SpriteRenderer로 연결.

#### 검증
`save_prefab_stage` 후 YAML grep으로 `m_GlowRenderer`/`m_Father`/`m_Effects` fileID가 실제로 일치하는지 확인. 컴파일 에러 0건. **Play Mode 시각 확인은 사용자가 직접 진행 예정.**

---

### 2026-07-27-3 — halo 스케일/정렬순서 정정 (사용자가 InGameScene에서 직접 고친 기준 반영)

#### 개요
사용자가 InGameScene의 `ActorPlayer` halo(`HexagonGlow`)를 직접 수정 후 "몬스터들은 저거 참조해서 저런 방식으로 Glow나올 수 있도록 해줘" 지시 — 실제 `HexagonGlow`를 확인한 결과 애초 가정(1.4배 확대, 코어보다 아래)과 다르게 **거의 동일 크기(localScale ≈1) + 코어보다 위(sortingOrder 3 > 코어 0)**로 구성돼 있었음. 코어와 거의 같은 크기의 halo가 코어 위에 겹쳐 알파만 pulsing하며 "전체가 밝아졌다 옅어졌다" 하는 방식 — TitleScene Hexagon/HexagonGlow(halo가 더 크고 아래)와는 다른, 이 씬만의 별도 적용 방식.

#### 파일
- Assets/Resources/Prefabs/Monster/Triangle.prefab

#### 수정 (오브젝트 단위)
**TriangleGlow — Transform (fileID 1601280225283626729)**
- 전: `m_LocalScale: {x: 1.4, y: 1.4, z: 1}`
- 후: `m_LocalScale: {x: 1, y: 1, z: 1}`

**TriangleGlow — SpriteRenderer (fileID 2059400926474417918)**
- 전: `m_SortingOrder: -1`
- 후: `m_SortingOrder: 3`

머테리얼(`GlowMat_Triangle_Halo`, 도형별 전용 에셋)은 그대로 유지 — Tower처럼 여러 오브젝트가 공유하는 `GlowMat_TitleHexagonHalo`를 그대로 재사용하지 않은 이유는, 몬스터 6종은 서로 다른 스프라이트(도형)를 쓰므로 하나의 공유 머테리얼로는 다른 도형의 halo가 잘못된 모양(텍스처 불일치)으로 나오기 때문 — "저런 방식"은 스케일/정렬순서 등 구조적 기법만 재현하고, 텍스처가 필요한 머테리얼 자체는 도형별로 유지.

#### 검증
컴파일 에러 0건(에셋 재임포트, YAML 파싱 이상 없음). **Play Mode 시각 확인은 사용자가 직접 진행 예정.**
