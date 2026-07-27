# Pooling (MemoryPooling)

## 연관 클래스
- MemoryPoolFactory (Factory.cs) — 타입별로 이 풀을 하나씩 보유
- ResUtil — Resources 로드/생성

## 현재 상태
- 경로: Assets/Scripts/Glory/Optimization/Pooling.cs (Glory 라이브러리)
- `MemoryPooling<T> where T : Component` — active/hide 두 리스트로 관리하는 단순 풀. CullingObject 등 특정 기능은 전혀 모르는 순수 제네릭 상태(2026-07-27-0에서 잠깐 CullingObject 캐싱을 얹었다가 같은 날 2026-07-27-1로 되돌림 — 아래 작업 내역 참고).
- `m_MaxCount`는 상한이 아니라 Prewarm 개수 — 풀 소진 시 Pop이 무제한 동적 생성 (grow-only).
- Push는 active 리스트에서 제거 성공 시에만 반납 (이중 반납 방어).
- Prewarm은 멱등 — 이미 오브젝트가 있으면 재호출 무시.

## 작업 내역

### 2026-07-12-0
- 개요: Prewarm 중복 호출 시 풀 오브젝트가 배수로 늘어나는 버그 예방 가드 추가
- 파일: Assets/Scripts/Glory/Optimization/Pooling.cs
- 증상(잠재): MonsterManager.Init() 등 상위 초기화가 두 번 불리면 풀 오브젝트가 정확히 2배 생성
- 원인: Prewarm()에 멱등 가드 없음 — CLAUDE.md의 "초기화 중복 호출 → 값이 정확히 N배" 패턴
- 수정 (Prewarm):
  - 전:
    ```csharp
    public void Prewarm()
    {
        for (int i = 0; i < m_MaxCount; ++i)
    ```
  - 후:
    ```csharp
    public void Prewarm()
    {
        // 중복 호출 시 풀 오브젝트가 배수로 늘어나는 것을 방지
        if (m_ActiveList.Count > 0 || m_HideList.Count > 0)
            return;

        for (int i = 0; i < m_MaxCount; ++i)
    ```
- 미검증: 에디터/플레이 테스트 전 (컴파일 확인 필요)
- 원본 저장소 반영 완료: github.com/purityzero/library 커밋 3c0e863 (Factory.cs, Pooling.cs, FactoryObject.cs 3개 파일 동기화)

---

## 2026-07-22-0

### 개요
사용자 리포트 — 씬 전환 시 콘솔에 `MissingReferenceException`(ActorMonster) 발생, `MemoryPooling.Clear()` → `MemoryPoolFactory.Clear()` → `MonsterManager.OnDestroy()` 경로.

### 증상
```
MissingReferenceException: The object of type 'ActorMonster' has been destroyed but you are still trying to access it.
UnityEngine.Component.get_gameObject ()
MemoryPooling`1[T].Clear () (at Assets/Scripts/Glory/Optimization/Pooling.cs:78)
MemoryPoolFactory`2[T,TEnum].Clear () (at Assets/Scripts/Glory/Partterns/Factory/Factory.cs:88)
MonsterManager.OnDestroy () (at Assets/Scripts/InGame/MonsterManager.cs:231)
```

### 원인
풀에 담긴 오브젝트(`ActorMonster` 등)는 전부 풀 생성 시 넘긴 `_parent`(`MonsterManager.m_PoolParent`)의 자식 — 이 부모는 InGameScene 소속이라 DontDestroyOnLoad가 아님. 씬 전환으로 InGameScene이 언로드되면 그 자식들(풀 오브젝트 전부)이 Unity에 의해 먼저 파괴되고, 그 직후 실행되는 `MonsterManager.OnDestroy()` → `Factory.Clear()` → `Pooling.Clear()`가 **이미 파괴된 참조**에 `.gameObject`로 접근하며 예외가 남(Unity Object의 "가짜 null" 상태에서 멤버 접근 시 예외를 던지는 동작).

### 수정 (함수 단위)

**Clear()**
- 전:
  ```csharp
  public void Clear()
  {
      foreach (T obj in m_ActiveList)
      {
          GameObject.Destroy(obj.gameObject);
      }
      foreach (T obj in m_HideList)
      {
          GameObject.Destroy(obj.gameObject);
      }

      m_ActiveList.Clear();
      m_HideList.Clear();
  }
  ```
- 후: 두 foreach 모두 `if (obj == null) continue;` 가드 추가(Unity 오버로드 `==`가 파괴된 오브젝트를 정확히 null로 판정) — 이미 씬 언로드로 사라진 오브젝트는 다시 Destroy할 필요 없이 건너뜀.

### 검증
Unity MCP 컴파일 확인(에러 0건). Play Mode 실측: InGameScene에서 몬스터 10마리 스폰(풀 active 상태로 채움) → `SceneManager.instance.NextScene("TitleScene")`으로 실제 씬 전환 → 콘솔 에러 0건(수정 전엔 동일 시나리오에서 `MissingReferenceException` 재현 확인).

---

## 2026-07-27-0 (도입 후 같은 날 되돌림 — 2026-07-27-1 참고)

### 개요
인게임 몬스터에 CullingObject(뷰포트 밖 SetActive(false)) 적용 요청. 몬스터는 WayPoint의 자동 반경 계산상 항상 화면 밖에서 스폰되어 컬링 실이득이 있음을 확인. 단, CullingObject 자신은 `UpdatableBehaviour` 등 `IUpdatable` 자동등록 패턴(OnEnable/OnDisable에서 Register/Unregister)을 상속하면 안 된다 — 화면 밖으로 나가 스스로 SetActive(false)하는 순간 OnDisable로 갱신 목록에서 영구 이탈해 다시 화면에 들어와도 안 켜지는 데드락이 생기기 때문. 대신 이미 매 프레임 호출되고 있던 `MemoryPooling<T>.UpdateLogic()`(기존엔 빈 가상 메서드)에서 구동하도록 구현.

### 파일
- Assets/Scripts/Glory/Optimization/Pooling.cs

### 수정 (함수 단위)

**필드 추가**
- 전: `m_ActiveList`, `m_HideList`만 존재
- 후: `private Dictionary<T, CullingObject> m_HashCullingObject = new Dictionary<T, CullingObject>();` 추가

**Pop()**
- 전: active 리스트에 추가 + SetActive(true) 후 반환
- 후: 그 직후 `if (obj.TryGetComponent(out CullingObject cullingObject) == true) m_HashCullingObject[obj] = cullingObject;` 추가 — CullingObject가 없는 풀 대상(UIToastMessage/DamageText/CritExplosion/SplashExplosion/ChainLightning 등)은 TryGetComponent가 false라 캐시에 안 들어가고 조용히 스킵됨(부작용 없음).

**Push()**
- 전: active 리스트 제거 성공 시 SetActive(false) + hide 리스트 추가
- 후: 같은 분기에 `m_HashCullingObject.Remove(_obj);` 추가.

**Clear()**
- 전: activeList/hideList만 Clear
- 후: `m_HashCullingObject.Clear();` 추가.

**UpdateLogic()**
- 전: 빈 가상 메서드(`{ }`)
- 후:
  ```csharp
  public virtual void UpdateLogic()
  {
      foreach (CullingObject cullingObject in m_HashCullingObject.Values)
      {
          cullingObject.UpdateLogic();
      }
  }
  ```

### 검증
- Unity MCP 도구가 이번 세션에 잡히지 않아(연결 안 됨) YAML 직접 편집 경로로 프리팹 6개(Triangle/Square/Star/Pentagon/Diamond/Circle)에 CullingObject 부착.
- `mcp__ide__getDiagnostics`로 Pooling.cs 확인 — 에러 0건(Hint성 메시지도 없음). 단 이건 IDE(Roslyn) 진단이며 Unity 에디터 자체 컴파일/Play Mode 실측은 못 함 — **미검증**으로 남김.

---

## 2026-07-27-1 — 컬링 결합 제거, 순수 제네릭 상태로 복원

### 개요
사용자 지적("CullingObject를 Pooling에서 관리할게 아니라 MonsterManager에서 관리해야하는거 아니야?") — `MemoryPooling<T>`는 `UIToastMessage`/`DamageText`/`CritExplosion`/`SplashExplosion`/`ChainLightning`/`ActorMonster` 6곳에서 재사용되는 공용 클래스인데, 그중 컬링이 필요한 건 몬스터뿐이었다. 공용 클래스에 특정 기능 로직을 얹기 전에 전체 사용처부터 확인했어야 하는 사례(루트 CLAUDE.md "재사용 우선 원칙"에 일반 규칙으로 추가됨). 몬스터 전용 지식은 [[ActorMonster]]/[[MonsterManager]] 쪽으로 이동.

### 파일
- Assets/Scripts/Glory/Optimization/Pooling.cs

### 수정 (함수 단위)
**클래스 선언**
- 전(2026-07-27-0): `private Dictionary<T, CullingObject> m_HashCullingObject = new Dictionary<T, CullingObject>();` 필드 존재
- 후: 해당 필드 제거 — 원래 상태로 복원

**Pop()**
- 전: 활성화 직후 `obj.TryGetComponent(out CullingObject cullingObject) == true`면 `m_HashCullingObject[obj] = cullingObject;`
- 후: 해당 블록 제거

**Push(T)**
- 전: 반납 성공 시 `m_HashCullingObject.Remove(_obj);` 호출
- 후: 해당 호출 제거

**Clear()**
- 전: `m_HashCullingObject.Clear();` 호출
- 후: 해당 호출 제거

**UpdateLogic()**
- 전: `m_HashCullingObject.Values`를 순회하며 각 CullingObject의 `.UpdateLogic()` 호출
- 후: 원래대로 빈 가상 메서드(`public virtual void UpdateLogic() { }`)로 복원

결과적으로 이 파일은 CullingObject를 전혀 모르는 순수 제네릭 풀링 클래스로 되돌아감(2026-07-27-0 이전과 100% 동일).

### 검증
Unity 에디터 포커스 재부여로 실제 재컴파일 확인 — 편집 도중 과도기 상태에서 `CS0103: m_HashCullingObject` 에러가 한 번 잡혔으나(순차 편집 중 Unity가 끼어들어 잡은 스냅샷), 최종 저장 상태 기준으로는 이후 두 차례 `Tundra build success`로 에러 0건 확인. **Play Mode 실측은 미검증.**
