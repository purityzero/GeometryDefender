# GameManager

## 연관 클래스
- MonoSingleton (Glory)
- TableManager (Glory)

## 현재 상태
- 경로: Assets/Scripts/GameManager.cs
- `MonoSingleton<GameManager>` 상속.
- `Awake()`에서 `TableManager.instance.init()` 호출 — 게임 진입 시 테이블 로드 트리거가 유일한 역할.

## 작업 내역

### 2026-07-29-0 — 사운드 마스터 볼륨 기본값 조정
사용자 피드백("전체적인 볼륨이 너무 커") — `Awake()`에 `SoundManager.instance.SetCategoryVolume(eSoundCategory.Master, 0.5f)` 추가. [[SoundManager]](Glory) 자체의 기본값(1.0)은 건드리지 않고 이 프로젝트의 부트스트랩 지점에서만 낮춤(Glory는 프로젝트 무관 기본값 유지, 실제 볼륨 취향은 프로젝트가 결정).
검증: 컴파일 에러 0건. Play Mode — `GetCategoryVolume(Master)`가 0.5 반환, 실제 재생 중인 BGM의 `AudioSource.volume`도 0.5로 정상 반영됨을 확인.

---

### 2026-07-12-0
- 개요: 프로젝트 전체 스캔으로 기본 정보 문서 초기 생성 (코드 수정 없음)

---

## 2026-07-15-4

### 개요
TitleScene에 GameManager 오브젝트 배치 (코드 수정 없음) — 이전까지 어떤 씬에도 없고 instance 접근 코드도 없어 Awake의 TableManager.init()이 실행된 적 없음.
