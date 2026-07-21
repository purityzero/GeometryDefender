# Pooling (MemoryPooling)

## 연관 클래스
- MemoryPoolFactory (Factory.cs) — 타입별로 이 풀을 하나씩 보유
- ResUtil — Resources 로드/생성

## 현재 상태
- 경로: Assets/Scripts/Glory/Optimization/Pooling.cs (Glory 라이브러리)
- `MemoryPooling<T> where T : Component` — active/hide 두 리스트로 관리하는 단순 풀.
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
