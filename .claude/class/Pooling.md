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
