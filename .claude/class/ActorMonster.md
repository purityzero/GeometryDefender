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

---

## 2026-07-22-0

### 개요
사용자 요청("적군에도 머테리얼 적용해주고") — 몬스터 프리팹 6종(Triangle/Circle/Square/Diamond/Pentagon/Star) 전부 `m_Renderer.material`이 **프로젝트에 존재하지 않는(guid `a97c105638bdf8b4a8650670310a4cd3`) 깨진 참조**였던 것을 발견/수정. 코드(ActorMonster.cs) 변경은 없음 — 프리팹 데이터만 수정.

### 파일
- Assets/Resources/Prefabs/Monster/{Triangle,Circle,Square,Diamond,Pentagon,Star}.prefab

### 원인
언제부터인지 불명(과거 머지 과정에서 유실된 것으로 추정) — 6개 프리팹 모두 SpriteRenderer의 `m_Materials[0]`이 실존하지 않는 guid를 가리키고 있었음. `Assets/Resources/Mat/Enemy/`엔 이미 `GlowMat_{도형}_{Normal|Elite}` 11종(Star는 Boss 1종)이 정상적으로 준비돼 있었는데 프리팹이 이걸 안 쓰고 있었던 것.

### 수정
6개 프리팹 각각 `m_Materials[0]`을 해당 도형의 `GlowMat_{도형}_Normal`(Star는 `GlowMat_Star_Boss`, Star Shape은 EnemyTable상 전부 Boss Variant라 이거 하나뿐)로 교체. Normal/Elite 두 머테리얼은 `_GlowAmount`(2로 동일) 등 셰이더 파라미터가 완전히 같고 `_Color`만 다른데, 그 `_Color`는 런타임에 `ActorMonster.SetColor()`가 매 스폰마다 `EnemyRecord.ColorHex`로 덮어쓰므로(같은 프리팹을 Normal/Elite/Boss 모든 Variant가 공유) 어느 쪽을 골라도 최종 렌더링에 차이 없음 — Normal로 통일.

### 검증
Unity MCP 컴파일 확인(에러 0건). Play Mode 실측 — 6개 도형 전부 스폰 후 `SpriteRenderer.sharedMaterial.name`이 각각 `GlowMat_Triangle_Normal`/`GlowMat_Circle_Normal`/`GlowMat_Square_Normal`/`GlowMat_Diamond_Normal`/`GlowMat_Pentagon_Normal`/`GlowMat_Star_Boss`로 정상 반영(런타임엔 `(Instance)` 접미사 붙음 — `SetColor()`가 `.material` 접근으로 자동 인스턴스화하기 때문, 기존 동작 그대로).

---

## 2026-07-22-1

### 개요
사용자 지적("적군 InGameScene에서 적군 색상 물빠진색으로 나오는거") — 진짜 원인은 Bloom/Tonemapping이 아니라 **색공간(sRGB vs Linear) 미변환**이었음.

### 원인
`ColorUtility.TryParseHtmlString(hex, out Color)`는 hex를 sRGB 기준으로 해석한 `Color`를 돌려주는데, 이 프로젝트는 Linear 색공간(URP 기본)이라 이 값을 그대로 `material.color`(=커스텀 Glow 셰이더의 `_Color`)에 넣으면 실제 화면 출력 시 감마 보정이 어긋나 색이 원래보다 밝고 채도 낮게(예: `#7c4dff`가 연보라 `#B995FF`로) 보인다. Bloom을 완전히 꺼봐도(threshold/intensity 조정, Tonemapping 추가 등 다 시도) 워시아웃이 그대로여서 Bloom이 원인이 아님을 픽셀 단위로 직접 확인 후 색공간 문제로 특정(`Camera.Render()`+`ReadPixels`로 실제 픽셀 hex를 여러 지점에서 비교).

### 수정 (함수 단위)
- **전**: `SetColor(Color _color) { m_Renderer.material.color = _color; }`
- **후**: `m_Renderer.material.color = _color.linear;` — `.linear`로 변환해서 넣어야 화면에 원래 hex 그대로 보임.

### 검증
Play Mode에서 Normal/Swift/Heavy/Ranged 4종 동시 스폰 후 `Camera.Render()+ReadPixels`로 각 픽셀의 실제 hex를 확인 — 4종 전부 EnemyTable의 `ColorHex`와 거의 정확히 일치(`ff3355→FF355A`, `ff9500→FF9900`, `7c4dff→8050FF`, `3d8bff→3F91FF`, 미세한 차이는 8비트 양자화 수준). 스크린샷으로도 정사각형이 더 이상 연보라가 아니라 선명한 보라로 보이는 것 확인. 콘솔 에러 0건.

### 참고
`ActorProjectile.SetColor()`도 동일 버그 패턴이라 같은 수정 적용(2026-07-22, [[ProjectileManager]] 테이블화 작업과 함께). `TowerColorEffect`의 색상 필드들은 hex 파싱이 아니라 인스펙터/코드에서 `new Color(r,g,b)`로 직접 수치를 넣은 것이라 이 버그와 무관(그쪽은 이미 육안으로 맞을 때까지 튜닝된 값이라 손대지 않음).
