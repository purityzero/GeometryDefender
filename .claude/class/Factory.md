# Factory (MemoryPoolFactory / IFactory / IMemoryPoolFactory)

## 연관 클래스
- MemoryPooling (Pooling.cs) — 실제 풀 구현체
- FactoryObject — Open()/Close() 생명주기 대상
- MonsterManager — ActorMonster 풀링에 사용

## 현재 상태
- 경로: Assets/Scripts/Glory/Partterns/Factory/Factory.cs (Glory 라이브러리)
- `MemoryPoolFactory<T, TEnum>` — enum 키별 MemoryPooling을 딕셔너리로 보유. (2026-07-21까지는 `<T1, TEnum1>`이었음 — 리팩토링으로 `IFactory`/`IMemoryPoolFactory` 인터페이스와 동일한 이름으로 통일, 아래 참고)
- Create: 풀에서 Pop 후 `Open()` 호출. Recycle: 풀 반납(Push) 성공 시에만 `Close()` 호출.
- 생성자에서 enum→Resources 경로 매핑을 받아 풀 구성. Prewarm/Clear는 전체 풀 일괄.
- `GetAllActive()`(2026-07-27) — `m_ObjectTypeDictionary.Keys` 그대로 반환. `Create()`로 내준 뒤 아직 `Recycle()` 안 된 오브젝트 전체 집합을 이미 이 딕셔너리가 추적 중이라(2026-07-22-0 참고) 별도 저장소 없이 재사용. 호출부(`MonsterManager`)가 매 프레임 활성 오브젝트를 순회해야 할 때 사용.

## 작업 내역

### 2026-07-27-0

#### 개요
사용자 지적("CullingObject를 Pooling에서 관리할게 아니라 MonsterManager에서 관리해야하는거 아니야?") — 몬스터 컬링 구동을 [[MonsterManager]]가 맡도록 재설계하며, "현재 활성화된 오브젝트 전체"를 얻을 방법이 필요해 추가. 상세 배경은 [[Pooling]]/[[ActorMonster]]/[[MonsterManager]] 2026-07-27 항목 참고.

#### 파일
- Assets/Scripts/Glory/Partterns/Factory/Factory.cs

#### 수정 (함수 단위)
**신규 `GetAllActive()`**
```csharp
public IEnumerable<T> GetAllActive()
{
    return m_ObjectTypeDictionary.Keys;
}
```
- 새 리스트/딕셔너리를 따로 만들지 않고, 이미 `Create()`/`Recycle()`이 갱신 중이던 `m_ObjectTypeDictionary`(2026-07-22-0 참고)를 그대로 노출 — 같은 "활성 오브젝트 집합"을 두 곳에서 중복 추적하지 않기 위함.

#### 검증
Unity 에디터 포커스 재부여로 실제 재컴파일 확인(`Tundra build success`, 에러 0건). **Play Mode 실측은 미검증.**

---

### 2026-07-22-0

#### 개요
사용자 지적("m_DicVisual FactoryPool 사용하면 되지않나?") — `MonsterManager`/`ProjectileManager`가 각자 `Dictionary<Entity, (Enum, T)>`를 따로 들고 있던 것을 보고, "Entity→어느 타입 풀에서 나왔는지" 역참조를 호출부가 아니라 팩토리 자신이 기억하도록 개선 요청. 팩토리는 이미 `Create(TEnum)` 시점에 어느 풀에서 꺼냈는지 알고 있으므로, 그 사실 자체를 내부에 저장해두면 호출부의 중복 Dictionary가 필요 없어짐(같은 "Entity→시각 오브젝트" 관계를 ECS `VisualObject` 컴포넌트와 별도 Dictionary 두 곳에서 중복 추적하던 문제, CLAUDE.md "여러 개념이 하나의 저장소를 공유" 계열 이슈의 변형).

#### 파일
- Assets/Scripts/Glory/Partterns/Factory/Factory.cs

#### 수정 (함수 단위)
**신규 필드**
- 추가: `private Dictionary<T, TEnum> m_ObjectTypeDictionary` — `Create()`가 내준 오브젝트가 어느 enum 타입(풀)에서 나왔는지 팩토리가 스스로 기억.

**Create(TEnum _type)**
- 후: `pool.Pop()` 성공 직후 `m_ObjectTypeDictionary[obj] = _type;` 한 줄 추가.

**Recycle** — 시그니처 변경
- 전: `public bool Recycle(TEnum _type, T _obj)` — 호출부가 타입을 알고 있어야 호출 가능.
- 후: `public bool Recycle(T _obj)` — `m_ObjectTypeDictionary`에서 타입을 자체 조회, 없으면(이 팩토리가 생성한 적 없는 오브젝트) 에러 로그 후 false. 조회 성공 시 이후 로직(풀 조회 → Push → Close)은 기존과 동일. 반납 성공 시 `m_ObjectTypeDictionary.Remove(_obj)`로 정리.
- `IMemoryPoolFactory<T, TEnum>` 인터페이스도 시그니처 함께 변경.

**Clear()**
- 추가: `m_ObjectTypeDictionary.Clear()` — 풀 오브젝트 파괴와 함께 추적 정보도 정리.

#### 영향받은 호출부
- `MonsterManager.RecycleVisual()`/`ProjectileManager.RecycleVisual()` — 둘 다 자체 `Dictionary<Entity, (Enum,Actor)>`를 완전히 제거하고, ECS `VisualObject` 컴포넌트(entity에 이미 붙어있는 시각 Transform 참조)에서 `GetComponent<T>()`로 Actor를 직접 얻어 `Recycle(actor)` 호출로 단순화. 상세는 [[MonsterManager]]/[[ProjectileManager]] 2026-07-22 항목 참고.

#### 검증
Unity MCP `refresh_unity`(force+compile) → `read_console` 에러/경고 0건.

---

### 2026-07-12-0
- 개요: Recycle의 Close 호출 순서 수정 + null 방어 추가
- 파일: Assets/Scripts/Glory/Partterns/Factory/Factory.cs
- 증상(잠재): 이중 반납이나 풀 미소속 오브젝트를 넘기면 Push 실패에도 Close 부작용(isAlive=false 등)이 이미 적용됨. `_obj`가 null이면 NRE.
- 원인: `_obj.Close()`를 Push 성공 확인 전에 호출
- 수정 (Recycle):
  - 전:
    ```csharp
    if (m_MemoryPoolDictionary.TryGetValue(_type, out MemoryPooling<T1> pool) == false)
    {
        Debug.LogError($"[MemoryPoolFactory] 등록되지 않은 타입: {_type}");
        return false;
    }

    _obj.Close();
    return pool.Push(_obj);
    ```
  - 후:
    ```csharp
    if (_obj == null)
    {
        Debug.LogError($"[MemoryPoolFactory] Recycle 실패 — null 오브젝트: {_type}");
        return false;
    }

    if (m_MemoryPoolDictionary.TryGetValue(_type, out MemoryPooling<T1> pool) == false)
    {
        Debug.LogError($"[MemoryPoolFactory] 등록되지 않은 타입: {_type}");
        return false;
    }

    // 풀 반납이 실제로 성공한 경우에만 Close — 이중 반납/미소속 오브젝트에 부작용 방지
    if (pool.Push(_obj) == false)
        return false;

    _obj.Close();
    return true;
    ```
- 트레이드오프: Close가 SetActive(false) 이후에 불림 — Close에서 코루틴 시작 등 활성 상태가 필요한 작업을 하면 안 됨 (현재 FactoryObject.Close는 플래그만 변경하므로 영향 없음)
- 미검증: 에디터/플레이 테스트 전 (컴파일 확인 필요)
- 원본 저장소 반영 완료: github.com/purityzero/library 커밋 3c0e863 (Factory.cs, Pooling.cs, FactoryObject.cs 3개 파일 동기화 — 저장소 버전이 구버전 API여서 단순 패치 대신 프로젝트 버전 전체로 교체)

---

## 2026-07-21-0

### 개요
사용자 요청(리팩토링 조사 항목 #4) — `MemoryPoolFactory<T1, TEnum1>`의 제네릭 이름이 구현하는 인터페이스(`IFactory<T, TEnum>`/`IMemoryPoolFactory<T, TEnum>`)와 달라 혼란을 준다는 지적. `T2` 등 구분해야 할 다른 제네릭이 없어 숫자 접미사에 의미가 없었음.

### 파일
- Assets/Scripts/Glory/Partterns/Factory/Factory.cs

### 수정
- `public class MemoryPoolFactory<T1, TEnum1> : IMemoryPoolFactory<T1, TEnum1>` → `public class MemoryPoolFactory<T, TEnum> : IMemoryPoolFactory<T, TEnum>` (본문 전체의 `T1`/`TEnum1` 사용처도 함께 치환, 로직 변경 없음)

### 검증
Unity MCP 컴파일 확인, 에러 0건.
