# ResUtil

연관 클래스: MemoryPooling(풀 생성이 ResUtil.Create 경유), Factory

## 개요
Resources 로드/생성 정적 유틸리티 (Glory 라이브러리). `Resources.Load`/`Instantiate` 직접 호출 대신 이걸 쓰는 게 프로젝트 규칙 (glory.md).

## 현재 상태
- 경로: Assets/Scripts/Glory/Resource/ResUtil.cs
- `LoadAsync(path)` / `Load<T>(path)` — Resources 로드 (실패 시 에러 로그 + null)
- `Create(path, parent, isInit)` / `Create<T>(path, parent, isInit)` — Resources 경로에서 로드 후 Instantiate. isInit=true면 Attach(로컬 트랜스폼 초기화), false면 Attach_Local(원본 로컬값 유지). Create&lt;T&gt;는 컴포넌트 없으면 에러 로그 + 파괴 + null
- `Create(prefabGO, parent)` / `Create<T>(prefabComponent, parent)` — **참조 기반** 생성 (2026-07-19). 프리팹 내부 템플릿처럼 Resources 경로가 없는 대상을 복제할 때 사용. 컴포넌트 참조 버전은 GetComponent 없이 타입 그대로 반환. null 프리팹 에러 로그, Attach로 로컬 초기화
- `Attach` / `Attach_Local` / `SetAllLayer` — 보조 헬퍼
- ~~`AddChild`~~ — 2026-07-19-1에 Create로 리네임/흡수 (사용자 확정 규칙: 생성 함수는 전부 Create 네이밍)

## 주의
- 경로 기반이든 참조 기반이든 인자 순서는 (소스, parent)로 동일.
- 참조 기반 Create는 원본의 로컬 트랜스폼을 초기화(Attach)함 — 원본 로컬값 유지가 필요하면 경로 기반의 `_isInit=false`(Attach_Local)처럼 별도 처리 필요(현재 참조 기반엔 해당 옵션 없음, 필요해지면 추가).

---

## 2026-07-19-0

### 개요
사용자 요청: UIMetaTree.SpawnNode의 `Instantiate(m_NodeTemplate, m_Content)`를 ResUtil로 대체하고 싶은데, 기존 AddChild는 GameObject 반환이라 GetComponent가 한 번 더 필요 → 컴포넌트 타입 그대로 반환하는 제네릭 오버로드 신규 추가.

### 파일
- Assets/Scripts/Glory/Resource/ResUtil.cs

### 수정
```csharp
// 추가 (기존 AddChild 아래)
public static T AddChild<T>(Transform _parent, T _prefab) where T : Component
{
    if (null == _prefab)
    {
        Debug.LogError("ResUtil::AddChild() null == prefab");
        return null;
    }

    T _obj = GameObject.Instantiate<T>(_prefab);
    Attach(_obj.transform, _parent);
    return _obj;
}
```
- 네이밍/null 비교 스타일(`null ==`, `_obj`)은 파일 내 기존 Create&lt;T&gt; 스타일을 따름.
- **원본 라이브러리(github.com/purityzero/library) 미반영** — 역동기화 필요.

### 미검증
에디터 미실행 상태 편집. 컴파일 확인 필요.

---

## 2026-07-19-1

### 개요
사용자 확정 규칙("생성 관련은 다 Create으로", 주요 규칙으로 선언 — .claude/rules/glory.md ResUtil 절에 반영): AddChild/AddChild&lt;T&gt;를 Create 오버로드로 리네임. 인자 순서도 경로 기반과 동일한 (소스, parent)로 통일. 레거시 AddChild는 호출처가 없어 시그니처 변경 안전, 본문도 Attach 재사용으로 정리(기존엔 Attach와 같은 로직을 중복 구현).

### 파일
- Assets/Scripts/Glory/Resource/ResUtil.cs

### 수정
- 전: `AddChild(Transform parent, GameObject prefab)` / `AddChild<T>(Transform _parent, T _prefab)`
- 후: `Create(GameObject _prefab, Transform _parent)` / `Create<T>(T _prefab, Transform _parent)` — 둘 다 null 가드 + Attach
- 호출처 갱신: UIMetaTree.SpawnNode, UIMetaTree.RefreshNodeList(헤더), ToggleButtonList.SetData 2곳(기존 직접 Instantiate도 이번에 Create로 전환)
- **원본 라이브러리(github.com/purityzero/library) 미반영** — ToggleButtonList.cs 변경분과 함께 역동기화 필요.

### 미검증
에디터 미실행 상태 편집. 컴파일 확인 필요.
