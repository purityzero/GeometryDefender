# Config

연관 클래스: SingletonScriptableObject, TweenUtil

## 개요
프로젝트 전역 설정용 ScriptableObject 싱글톤 (Glory 라이브러리). `Config.Instance`로 접근, Resources/Config.asset 자동 로드/생성 (에디터 메뉴 ScriptableObject/Config/Create).

## 현재 상태
- 서버: `Path`, `ServerType`(eSeverType)

---

## 2026-07-14-0

### 개요
플레이어 빌드 컴파일 에러 지뢰 제거.

### 파일
- Assets/Scripts/Glory/Config.cs

### 증상
`using UnityEditor` 와 `[MenuItem]` 메서드가 `#if UNITY_EDITOR` 가드 없이 존재 → 에디터에서는 정상이지만 플레이어 빌드 시 컴파일 에러.

### 수정

**using / Create()**
- 전: `using UnityEditor;` 가드 없음, `[MenuItem] public static void Create()` 가드 없음, 지역 변수명 `a`
- 후: 둘 다 `#if UNITY_EDITOR` 가드로 감쌈, 변수명 `createdConfig`로 변경 (단일 문자 금지 규칙)

### 미검증
에디터 미실행 상태 편집. 컴파일 확인 필요.

### 2026-07-14-1
- 개요: 트윈 기본값 필드 추가 (사용자 선택: TweenUtil의 const 대신 Config.asset에서 튜닝).
- 수정: `TapScale = 0.95f`, `TapDuration = 0.05f` 공개 필드 추가.

### 2026-07-14-2
- 개요: 트윈 기본값 필드 **제거(원복)** — 사용자 재선택으로 저장 위치가 GameConfigTable(CSV)로 최종 변경됨. Config는 서버 설정 전용으로 복귀.
