# ActorMonster

## 연관 클래스
- Actor — 베이스 클래스 (FactoryObject 계열)
- MonsterManager — MemoryPoolFactory로 생성/반납
- EnemyRecord — ColorHex를 SetColor로 적용

## 현재 상태
- 경로: Assets/Scripts/InGame/Actor/ActorMonster.cs
- 몬스터의 비주얼 GameObject 담당 (로직은 ECS 쪽, 위치 동기화는 VisualSyncSystem).
- `[SerializeField] Renderer m_Renderer` — 프리팹에서 연결 필요.
- `SetColor(Color)` — `m_Renderer.material.color` 변경 (material 인스턴스화 발생).
- `Open()`/`Close()` 오버라이드는 현재 base 호출만 하는 빈 구현.
- Entity ↔ ActorMonster 연결 구조와 전체 생명주기는 MonsterManager.md의 "동작 구조 (내부)" 섹션 참고.

## 작업 내역

### 2026-07-12-0
- 개요: 프로젝트 전체 스캔으로 기본 정보 문서 초기 생성 (코드 수정 없음)

---

## 2026-07-15-0

### 개요
D:\Unity\Job에서 머지 — Job 버전으로 교체.

### 수정 (함수 단위)
- 추가: `SetColor(string _colorHex)` — ColorUtility.TryParseHtmlString 파싱 후 SetColor(Color)
- 추가: `Open(EnemyRecord _record)` — 레코드 보관 + ColorHex 적용 + base Open()
- 추가: private 필드 `m_Record` (EnemyRecord)

### 미검증
컴파일 확인 필요.
