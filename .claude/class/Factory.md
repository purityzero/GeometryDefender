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

## 작업 내역

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
