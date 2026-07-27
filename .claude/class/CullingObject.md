# CullingObject

연관 클래스: ActorMonster(같은 오브젝트의 CullingObject를 직렬화 필드로 캐시), MonsterManager(활성 몬스터 목록을 순회하며 매 프레임 구동) — 2026-07-27-1부터. (Pooling.cs/Factory.cs가 이 역할을 대신하던 2026-07-27-0 구현은 되돌림, 아래 참고)

## 개요
뷰포트(카메라) 밖으로 나가면 `SetActive(false)`로 꺼주는 범용 컬링 컴포넌트. `Renderer`(월드 오브젝트) 또는 `RectTransform`(UI 요소) 둘 중 자기 자신에 붙어있는 쪽을 자동 판별해 그 경계로 뷰포트 안/밖을 판정한다. `UpdateLogic()`을 외부에서 호출해줘야 동작(자체 `Update()` 없음).

## 현재 상태
- 경로: Assets/Scripts/Glory/Optimization/CullingObject.cs
- `[SerializeField] private Renderer m_ObjectRenderer;` / `[SerializeField] private RectTransform m_RectTransform;`(2026-07-23) — 프리팹에서 미리 연결 가능, 비어있으면 `Awake()`에서 각각 `GetComponent<Renderer>()`/`GetComponent<RectTransform>()`로 폴백.
- `IsInCameraView`: `m_ObjectRenderer`가 있으면 `Bounds`, 없고 `m_RectTransform`이 있으면 `GetWorldCorners()`로 뷰포트 좌표 변환 후 화면 안/밖 판정.
- `mainCamera`/`isVisible` 등 나머지 필드는 이번 작업 범위 밖(기존 camelCase 네이밍 유지 — CODE.MD의 `m_` 접두사 규칙과 다르지만, 이번 변경과 무관한 기존 코드라 손대지 않음).
- 부착된 프리팹(2026-07-27): Assets/Resources/Prefabs/Monster/{Triangle,Square,Star,Pentagon,Diamond,Circle}.prefab 6개 — 전부 `m_ObjectRenderer`를 같은 오브젝트의 SpriteRenderer로 연결, `m_RectTransform`은 비움(월드 오브젝트).
- CullingObject.UpdateLogic()은 자기 자신을 상속(OnEnable/OnDisable 자동등록)이 아니라 `ActorMonster.UpdateCullingLogic()`(직렬화 참조로 캐시된 CullingObject를 호출) → `MonsterManager.UpdateCulling()`(매 프레임 활성 몬스터 전체 순회) 경로로 구동된다 — 이유: CullingObject가 자기 GameObject를 SetActive(false)하는 컴포넌트라, IUpdatable 자동등록 패턴을 썼다면 화면 밖으로 나가는 순간 OnDisable로 갱신 목록에서 이탈해 다시는 안 켜지는 데드락이 생기기 때문(그래서 이 클래스는 계속 순수 MonoBehaviour 유지, IUpdatable 미상속).
- **2026-07-27-1로 구동 주체가 바뀜**: 처음엔(2026-07-27-0) 이 구동 로직을 `MemoryPooling<T>`(여러 무관한 풀이 재사용하는 공용 클래스)에 넣었으나, 몬스터 전용 지식을 공용 라이브러리가 알게 되는 문제로 되돌리고 몬스터 쪽 코드로 옮김 — 상세는 [[ActorMonster]]/[[MonsterManager]]/[[Pooling]]/[[Factory]] 2026-07-27-1 항목 참고.

## 작업 내역

### 2026-07-27-2

#### 개요
qa-tester 에이전트가 실측(Play Mode)에서 발견한 버그 수정 — [client-issues.md 2026-07-27-0](../qa/client-issues.md#2026-07-27-0--cullingobject가-몬스터-6종에서-실질적으로-전혀-동작-안-함-awake에서-캐싱한-maincamera가-씬-전환-도중-파괴된-참조로-굳어버림) 참고. `mainCamera`가 `Awake()`에서 1회만 캐싱되는데 이 컴포넌트가 붙은 오브젝트는 풀링 재사용 대상이라, 씬 전환 중 카메라가 교체/파괴되면 캐시가 죽은 참조로 영구히 굳어 컬링이 조용히 멈추는 문제였음.

#### 파일
- Assets/Scripts/Glory/Optimization/CullingObject.cs

#### 수정 (함수 단위)
**UpdateLogic()**
- 전:
```csharp
public void UpdateLogic()
{
    if (mainCamera == null)
        return;
    ...
}
```
- 후:
```csharp
public void UpdateLogic()
{
    if (mainCamera == null)
        mainCamera = Camera.main;

    if (mainCamera == null)
        return;
    ...
}
```
파괴된 카메라 참조도 `== null` 오버로드로 true가 되므로, 가드에 걸릴 때마다 `Camera.main`으로 재조회 — 별도 이벤트 훅 없이 다음 프레임에 자연 복구됨.

#### 검증
Play Mode에서 정확히 재현 조건(캐시된 카메라가 `DestroyImmediate`로 파괴된 상태)을 직접 만들어 `UpdateLogic()` 호출 → `mainCamera`가 유효한 `Camera.main`("Main Camera")으로 재할당되고, 화면 밖 위치의 테스트 오브젝트가 정확히 `SetActive(false)` 처리됨을 확인. 콘솔 에러 0건.
(주의: 이 세션에서는 기존에 이미 알려진 별개의 미해결 버그 — `World.DefaultGameObjectInjectionWorld`가 씬 전환 중 null이 되는 문제(client-issues.md 2026-07-21-1/2026-07-23-0) — 가 재현되어 `MonsterManager.Init()`이 막혀 정상적인 몬스터 스폰 경로로는 재검증 못 함. 이 버그와는 무관하므로 CullingObject 단독 격리 테스트로 검증함.)

---

### 2026-07-23-0

#### 개요
신규 md 생성. 사용자 요청(신규 코드 규칙): "Awake/Start에서 GetComponent 대신, Unity 내장 컴포넌트는 왠만하면 멤버 변수로 선언해 Prefab에 연동". 기존 코드 전수 검사 중 이 클래스가 해당돼 수정.

#### 파일
- Assets/Scripts/Glory/Optimization/CullingObject.cs

#### 수정 (함수 단위)
**클래스 선언**
- 전: `private Renderer objectRenderer; private RectTransform rectTransform;`(private, GetComponent 전용)
- 후: `[SerializeField] private Renderer m_ObjectRenderer; [SerializeField] private RectTransform m_RectTransform;`(직렬화, CODE.MD 네이밍 규칙에 맞춰 `m_` 접두사로 리네임 — 기존 사용처가 없어 리네임에 따른 참조 유실 위험 없음)

**Awake()**
- 전: `objectRenderer = GetComponent<Renderer>(); rectTransform = GetComponent<RectTransform>();`(무조건 재조회)
- 후: 각각 `if (필드 == null) 필드 = GetComponent<T>();`로 폴백(이미 인스펙터에서 연결돼 있으면 재조회 안 함)

**IsInCameraView / Awake() 내부 null 체크**
- `objectRenderer`/`rectTransform` 참조를 전부 `m_ObjectRenderer`/`m_RectTransform`으로 치환(로직 동일).

#### 검증
현재 이 컴포넌트를 부착한 씬/프리팹이 없어 씬 데이터 갱신은 불필요. 필드가 비어있으면 기존과 동일하게 GetComponent로 폴백하므로 향후 부착 시에도 동작 변화 없음(미검증 — 실제 부착 사례가 생기면 Play Mode 확인 필요).

---

### 2026-07-27-0

#### 개요
인게임 몬스터 6종 프리팹에 실제로 부착(사용자 요청 "인게임에서 CullingObject 적용해줘" → 몬스터만으로 범위 확정). CullingObject.cs 자체는 수정하지 않음.

#### 파일
- Assets/Resources/Prefabs/Monster/Triangle.prefab
- Assets/Resources/Prefabs/Monster/Square.prefab
- Assets/Resources/Prefabs/Monster/Star.prefab
- Assets/Resources/Prefabs/Monster/Pentagon.prefab
- Assets/Resources/Prefabs/Monster/Diamond.prefab
- Assets/Resources/Prefabs/Monster/Circle.prefab

#### 수정
6개 프리팹 모두 동일 패턴 — 루트 GameObject의 `m_Component` 목록에 fileID `9001000000000000001`(CullingObject) 추가, 해당 fileID로 MonoBehaviour 블록 신설:
```yaml
--- !u!114 &9001000000000000001
MonoBehaviour:
  ...
  m_Script: {fileID: 11500000, guid: 45503f694daaf81468c57c5d86dfb963, type: 3}
  m_EditorClassIdentifier: Assembly-CSharp::CullingObject
  m_ObjectRenderer: {fileID: <같은 오브젝트의 SpriteRenderer fileID>}
  m_RectTransform: {fileID: 0}
```
프리팹별 상세 계층/fileID는 .claude/prefab/{Triangle,Square,Star,Pentagon,Diamond,Circle}.md 참고.

---

### 2026-07-27-1

#### 개요
사용자 지적("CullingObject를 Pooling에서 관리할게 아니라 MonsterManager에서 관리해야하는거 아니야?") — 2026-07-27-0에서 구동 로직을 `MemoryPooling<T>`(공용 클래스)에 넣었던 것을 자가 진단 후 재설계. 이 클래스(CullingObject.cs) 자체는 이번에도 수정하지 않음 — 구동 방식만 [[ActorMonster]]/[[MonsterManager]] 쪽으로 이동.

#### 파일
없음(이 클래스 자체는 무변경) — 관련 변경은 [[ActorMonster]], [[MonsterManager]], [[Pooling]], [[Factory]] 참고.

#### 검증
Unity 에디터 포커스 재부여로 실제 재컴파일 확인(`Tundra build success`, 에러 0건). **Play Mode 실측은 미검증** — qa-tester 에이전트로 확인 예정.

#### 검증
Unity MCP 도구가 이번 세션에 잡히지 않아 YAML 직접 편집으로 진행(PREFAB.MD "MCP 미연결" 경로). GUID는 CullingObject.cs.meta에서 직접 대조(`45503f694daaf81468c57c5d86dfb963`), fileID는 각 프리팹 내 기존 값과 겹치지 않는 대역 사용. **미검증** — 실제 Unity 에디터 컴파일/Play Mode 확인 못 함(MCP 미연결). `mcp__ide__getDiagnostics`로는 Pooling.cs C# 컴파일 에러 0건만 확인, 프리팹 YAML 파싱 정합성은 에디터에서 열어봐야 최종 확인됨.
