# LaserBeamVisual

연관 클래스: [[ActorPlayer]](소유/구동 주체), `LineRenderer`(x2, Core+Glow — 2026-07-27-1에서 잠깐 1개로 단순화했다가 2026-07-27-2에서 다시 2개로 복원됨), `TowerRecord.PrefabPath`(생성 경로 지정), [[TowerRecord]]

## 2026-07-27-2 — Core(흰색) 라인 복원 (2026-07-27-1 되돌림)

### 개요
2026-07-27-1에서 "하얀색 없으면 더 이쁘겠다"는 요청으로 Core를 제거했으나, 실제 렌더링을 본 사용자가 "다시 되돌려줘 저건 아니야.. 컬러는 너무 레이져가 아니야.."라고 재지적 — 색만 있는 단일 선은 "레이저"로 안 읽힌다는 판단. Core+Glow 2겹 구조를 그대로 복원(2026-07-27-0과 동일 구조). **회전 속도(90도/초, 2026-07-27-1에서 180→90 완화)는 이번 되돌림과 무관하게 유지** — 사용자가 "속도 좀더 느리게 하는건 맞는데"로 명시적으로 확인.

### 구조 복원
- `Mat_LaserBeam.mat`(흰색, `_GlowAmount`=2.5) 재생성 — 2026-07-27-1에서 삭제했던 것을 동일 스펙으로 복원(단, guid는 새로 발급됨, 기존 참조와 무관하므로 문제 없음).
- `Mat_LaserGlow.mat`의 `_GlowAmount`를 1→0.6으로 되돌림(코어와 같이 쓰이므로 다시 낮춰서 흰색에 안 묻히게).
- 프리팹 루트 `LaserBeam`에 `LineRenderer`(Core) 컴포넌트 재부착(width 0.05, sortingOrder 17, `Mat_LaserBeam.mat`), 자식 `GlowLine`은 그대로 유지.
- `LaserBeamVisual.cs`: `m_LineRenderer` 1필드 → `m_CoreLineRenderer`/`m_GlowLineRenderer` 2필드로 되돌림(2026-07-27-0과 동일 코드).

### 검증
Play Mode 스크린샷으로 흰 코어 + 연두색 글로우가 다시 정상 표시됨을 확인.

### 교훈
비주얼 디테일(글로우 유무, 코어 색상)은 스크린샷으로 실제 확인 전까지 "이쁘다/레이저답다" 판단이 여러 번 뒤집힐 수 있는 영역 — 되돌림 요청이 오면 즉시 이전 스펙으로 정확히 복원하고, 그 사이 바뀐 별개 요청(회전 속도 등)은 섞지 않고 유지할 것.

---

## 2026-07-27-1 — Core(흰색) 라인 제거, 단일 Glow 라인으로 단순화 + 회전 속도 추가 완화 (2026-07-27-2에서 되돌려짐, 아래는 당시 기록)

### 개요
2026-07-27-0에서 "하얀 코어 + 색 있는 글로우" 2겹 구조로 만들었으나, 사용자가 실제 렌더링을 보고 "연두색은 있어두 되는데 중간에 하얀색 없으면 더 이쁘겠다"고 재요청 — Core LineRenderer를 완전히 제거하고 Glow 라인 하나만 남기는 것으로 되돌림. 같은 요청 세션에서 "조금 더 천천히 돌면 좋겠다"도 함께 반영(`LASER_ROTATION_SPEED` 180→90, [[GameConfigRecord]] 참고).

### 구조 변경
- 프리팹: 루트 `LaserBeam`의 `LineRenderer` 컴포넌트 제거(Core 삭제), 자식 `GlowLine`의 `LineRenderer`(width 0.3, `Mat_LaserGlow.mat`)만 유지. `LaserBeamVisual.m_LineRenderer`가 이제 `GlowLine`의 `LineRenderer`를 직접 가리킴.
- `Mat_LaserBeam.mat`(흰색 코어용)은 더 이상 참조하는 곳이 없어 삭제.
- `Mat_LaserGlow.mat`의 `_GlowAmount`를 0.6→1(단독으로 쓰이니 존재감을 살짝 더 올림).
- `LaserBeamVisual.cs`: `m_CoreLineRenderer`/`m_GlowLineRenderer` 2필드 → `m_LineRenderer` 1필드로 단순화. `SetColor()`/`UpdateBeam()`/`SetBeamActive()`의 동작 자체(시그니처)는 동일 — [[ActorPlayer]] 쪽 호출부는 수정 불필요.

### 검증
Unity MCP Play Mode에서 `manage_camera` 스크린샷으로 실제 렌더링 확인 — 흰색 없이 연두색 단일 라인이 은은하게 번지는 형태로 정상 표시됨.

---

## 2026-07-27-0 — 신규 생성: Laser(#6) 무기 전용 시각 효과 (2026-07-27-1에서 구조 변경됨, 아래는 최초 버전 기록)

### 개요
사용자 요청("회전하면서 다수 공격하는 레이져", "레이저 같은경우는 glow효과도 있으면 좋고", 이후 "glow효과는 나오지 않고 있습니다... 안은 하얀색인데 겉이 레이져처럼 연두색 이런걸루 했으면 좋겠는데")로 신설. `ActorPlayer`가 Laser 무기 해금 시 `AddWeapon()`에서 자식으로 1개만 생성해 계속 재사용(Splash/Chain 이펙트처럼 여러 발 동시 재생이 필요 없어 풀링 대상 아님 — `MonoBehaviour` 직접 상속, `FactoryObject` 아님).

### 구조 — LineRenderer 2개(Core + Glow)
단일 LineRenderer로 시도했을 때 사용자가 "글로우가 안 보이고 그냥 하얗게만 보인다"고 지적 — 색상이 있는 얇은 선 하나로는 "하얀 코어 + 색 있는 외곽 글로우" 느낌을 낼 수 없어 2개로 분리:
- **Core**(루트 GameObject 자신의 LineRenderer): 얇음(width 0.05), 항상 흰색 고정, `Mat_LaserBeam.mat`(`Custom/GlowSpriteAdditive`, `_Color`=흰색, `_GlowAmount`=2.5 — 완전히 번쩍이도록 높게).
- **Glow**(자식 GameObject "GlowLine"): 넓음(width 0.3), 무기 고유색(런타임 `SetColor()`로 변경 가능), `Mat_LaserGlow.mat`(같은 셰이더, `_GlowAmount`=0.6 — 코어보다 낮게 눌러 흰색으로 안 뭉개지게).
- 두 LineRenderer 모두 `m_UseWorldSpace=1`, 동일 위치(원점→끝점)를 매 프레임 같이 갱신 — Core가 sortingOrder 17(위), Glow가 16(아래)로 겹쳐 그려짐.

### Custom/GlowSpriteAdditive 셰이더 + MaterialPropertyBlock — 이 프로젝트에서 실제로 확인된 발광 방식
`Assets/Resources/URP/GlowSpriteAdditive.shader`(Blend One One 가산 블렌드, `_Color.rgb * _GlowAmount * texAlpha * vertexAlpha`)는 몬스터/투사체/타워가 전부 쓰는 `GlowMat_*` 계열과 동일 셰이더(`Custom/GlowSpriteAdditive`, guid `6f5c48c11d91509cd463ce14a75e7875`) — 프로젝트 전역에서 이미 검증된 발광 파이프라인이라 재사용. **주의: 이 셰이더는 LineRenderer 정점 색상(`startColor`/`endColor`/`colorGradient`)의 RGB를 안 쓰고 알파만 곱한다** — 색상은 반드시 material의 `_Color`(MaterialPropertyBlock 경유)로 세팅해야 함. `SetColor(Color)`는 Glow 라인에만 적용(Core는 항상 흰색 고정).

### 이전 시행착오 (참고용, 원인 규명은 못함)
최초 버전은 `Mat_ChainLightning.mat`(`Sprites/Default`, 비가산 블렌드)을 그대로 재사용했으나 사용자가 "라인렌더에 glow효과가 전혀 없다"고 지적. `Custom/GlowSpriteAdditive` + 단일 라인(파란색 `#3d5afe`)으로 1차 교체했을 때도 "그냥 안은 하얀색"이라는 지적을 받음 — 정확한 원인(프로퍼티 블록 미적용/가산 블렌드 특성/얇은 선의 다운샘플링 등)은 규명하지 않고, 대신 사용자가 원하는 최종 형태(하얀 코어+색 있는 글로우 2겹 구조)로 바로 재설계해 해결.

### 공개 API
```csharp
public void SetColor(Color _color)       // Glow 라인 색(무기 고유색, TowerRecord.ColorHex)
public void UpdateBeam(Vector3 origin, float angleDegrees, float range)  // 매 프레임 위치 갱신
public void SetBeamActive(bool isActive) // gameObject.SetActive
```

### 검증
Unity MCP Play Mode에서 각도를 강제 고정(0도) 후 `manage_camera` 스크린샷으로 실제 렌더링 결과 육안 확인 — 흰색 코어 라인 주위로 연두색 글로우가 번지는 것을 확인(사용자 요청과 일치). 무기 쿨다운 게이지(`UIInGameHUD`)도 같은 색(`#44FF33`)으로 표시되는 것까지 확인.

### 관련 프리팹
- [LaserBeam.md](../prefab/LaserBeam.md)
