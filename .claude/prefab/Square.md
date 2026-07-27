# Square (Assets/Resources/Prefabs/Monster/Square.prefab)

연관 스크립트: [[ActorMonster]](루트 부착), [[CullingObject]](루트 부착, 2026-07-27 신규), FadeTweenEffect/TweenEffectPlayer(자식 SquareGlow, 2026-07-27 신규)

## 개요
몬스터 타입 중 하나. 루트 GameObject에 Transform + SpriteRenderer + ActorMonster + CullingObject, 자식으로 halo 전용 오브젝트(SquareGlow) 보유. `MonsterManager` → `MemoryPoolFactory<ActorMonster, eMonsterType>` 풀에서 Pop/Push된다.

## 계층 구조
```
Square (fileID: 2976980498768928354)
├─ Transform (fileID: 1758506301540457091)                     — m_LocalScale 1,1,1 (실제 크기는 EnemyTable.VisualSize가 스폰마다 덮어씀)
├─ SpriteRenderer (fileID: 2683836546428861909)                — sprite guid ae08509d7989b134daf81d9e1575673e, material GlowMat_Square_Normal
├─ ActorMonster (fileID: 3611853868197571574)                  — m_Renderer → 2683836546428861909, m_GlowRenderer → 5361535452116357381(SquareGlow의 SpriteRenderer, 2026-07-27-2), m_CullingObject → 9001000000000000001
├─ CullingObject (fileID: 9001000000000000001)                 — m_ObjectRenderer → 2683836546428861909, m_RectTransform 비움
└─ SquareGlow (fileID: 7648482173055035313, 신규 2026-07-27-2) — halo 자식, localScale 1,1,1(2026-07-27-3에서 1.4→1로 정정)
   ├─ Transform (fileID: 4164450919384093213)
   ├─ SpriteRenderer (fileID: 5361535452116357381)              — 코어와 동일 sprite, sortingOrder 3(코어 0보다 위, 2026-07-27-3에서 -1→3으로 정정), material GlowMat_Square_Halo(guid e3bf7fa73b8a4414ea89508f50e15161)
   ├─ FadeTweenEffect (fileID: 5a1c8e2fd0b94b6c8e3a7d915c2f4a02 스크립트 사용)  — Duration 3, Ease Linear, TargetAlpha 0
   └─ TweenEffectPlayer                                          — m_Effects → FadeTweenEffect, LoopCount -1, LoopType Yoyo
```

## 2026-07-27-3 — halo 스케일/정렬순서 정정
사용자가 InGameScene `ActorPlayer`의 halo(`HexagonGlow`)를 직접 고친 기준(거의 동일 크기 + 코어보다 위)을 따라 `SquareGlow`도 동일하게 정정 — 상세 근거는 [[Triangle]](prefab) 2026-07-27-3 참고. `m_LocalScale` 1.4→1, `m_SortingOrder` -1→3. 컴파일 에러 0건.

## 작업 내역

### 2026-07-27-0

#### 개요
사용자 요청("인게임에서 CullingObject 적용해줘") — 몬스터만 대상으로 확정. WayPoint의 자동 반경 계산상 몬스터가 항상 화면 밖에서 스폰되므로 컬링 실이득 있음을 확인 후 적용.

#### 파일
- Assets/Resources/Prefabs/Monster/Square.prefab

#### 수정 (오브젝트 단위)
**Square (루트)**
- 전: `m_Component` = [Transform, SpriteRenderer, ActorMonster] 3개
- 후: `m_Component`에 CullingObject(fileID 9001000000000000001) 추가, 4개
- 신규 MonoBehaviour 블록: `m_Script` guid `45503f694daaf81468c57c5d86dfb963`(CullingObject.cs.meta에서 확인), `m_ObjectRenderer: {fileID: 2683836546428861909}`, `m_RectTransform: {fileID: 0}`

#### 검증
Unity MCP 도구가 이번 세션에 잡히지 않아 YAML 직접 편집(PREFAB.MD "MCP 미연결" 경로)으로 진행 — **미검증**(Unity 에디터 컴파일/Play Mode 확인 못 함). `mcp__ide__getDiagnostics`는 C# 컴파일만 확인 가능하고 프리팹 YAML 정합성은 검증 범위 밖.

---

### 2026-07-27-1 — 구동 주체를 Pooling.cs에서 ActorMonster/MonsterManager로 이동

#### 개요
사용자 지적("CullingObject를 Pooling에서 관리할게 아니라 MonsterManager에서 관리해야하는거 아니야?") — `MemoryPooling<T>`(여러 무관한 풀에 재사용되는 공용 클래스)에 컬링 캐싱 로직을 얹었던 2026-07-27-0의 구현을 되돌리고, 몬스터 전용 지식은 몬스터 쪽 코드(`ActorMonster`/`MonsterManager`)에만 두도록 재설계. 상세는 [[ActorMonster]], [[MonsterManager]], [[Pooling]], [[Factory]] 2026-07-27 항목 참고.

#### 파일
- Assets/Resources/Prefabs/Monster/Square.prefab

#### 수정 (오브젝트 단위)
**ActorMonster (fileID 3611853868197571574)**
- 전: `m_Renderer`만 보유
- 후: `m_CullingObject: {fileID: 9001000000000000001}` 필드 추가 — 같은 오브젝트의 CullingObject 컴포넌트를 직렬화 참조로 연결(런타임 GetComponent 없이 캐싱).

#### 검증
포커스 재부여로 Unity 에디터 실제 재컴파일/재임포트 확인 완료 — `Tundra build success`, 이 프리팹 재임포트 시 Missing Script 등 파싱 에러 없음. **Play Mode 실측은 여전히 미검증.**
