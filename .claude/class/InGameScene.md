# InGameScene

연관 클래스: MonsterManager, SpawnManager

## 개요
인게임 씬 진입점. 씬 배치 컴포넌트로 MonsterManager / SpawnManager를 직렬화 참조해 Start에서 Init 호출.

## 현재 상태
- `Start()`: m_MonsterManager.Init() → m_SpawnManager.Init()
- Update는 비어 있음

---

## 2026-07-15-0

### 개요
D:\Unity\Job (구 작업 폴더, 2026-06-09까지 작업)에서 머지로 신규 도입. 스크립트 guid도 Job 것 유지 (Job InGameScene.unity가 참조).

### 파일
- Assets/Scripts/InGame/InGameScene.cs (+.meta, Job에서 복사)

### 미검증
컴파일/씬 연결 확인 필요.

---

## 2026-07-20-0

### 개요
사용자 요청: InGameScene의 ActorPlayer를 TitleScene 중앙 헥사곤(Image_Hexagon/Glow_Image_Hexagon, 88.8×77.6px)과 화면상 같은 크기로. 스크립트 변경 없음, 씬 값만 수정.

### 파일
- Assets/Scenes/InGameScene.unity

### 수정 (오브젝트 단위)

**ActorPlayer (fileID 1165160029, Transform 1165160030)**
- 전: `m_LocalScale: {x: 0.75, y: 0.75, z: 1}`
- 후: `m_LocalScale: {x: 0.40625, y: 0.40625, z: 1}`

### 계산 근거
- 두 쪽 다 같은 스프라이트(shape_hexagon_0, 222×194px, PPU 100 → 월드 2.22×1.94유닛)
- 타이틀 캔버스 기준 해상도 720×1280, 헥사곤 UI 88.8×77.6px
- InGame 카메라 orthographic size 6.5 → 세로 13유닛 = 1280px → 1유닛 = 98.4615px
- scale = (88.8 ÷ (98.4615 × 2.22)) = 0.40625 (= 13/32, 세로 계산도 동일값)
- 88.8:77.6 == 222:194 (같은 비율)라 가로/세로 단일 스케일로 정확히 일치

### 미검증
에디터 미실행 상태 편집. 씬이 에디터에 열려 있었다면 리로드 후 실제 크기 비교 확인 필요.
