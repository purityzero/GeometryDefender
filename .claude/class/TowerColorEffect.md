# TowerColorEffect

## 연관 클래스
- TowerHealth (`Current.currentHp` — `ObservableVariable<int>` 구독 대상)
- [[TweenUtil]] (`Color(Material, Color, float)` — 2026-07-22 신규 오버로드로 이 클래스 때문에 추가됨)
- BaseScene, IUpdatable (Current 준비될 때까지만 재시도용 — [[ObservableIntText]]와 동일한 패턴)
- ActorPlayer(InGameScene.unity) — 부착 대상, `GlowMat_Tower` 머테리얼을 쓰는 SpriteRenderer 보유

## 개요
타워(플레이어) HP 비율에 따라 스프라이트 머테리얼 색을 3단계로 서서히 바꾸는 연출 컴포넌트. `Assets/Design/02_combat.html`의 "타워 HP 시각 표현" 스펙(100~70% Cyan / 70~30% Dim Cyan / 30~0% Red) 기준.

## 현재 상태
- 경로: Assets/Scripts/InGame/TowerColorEffect.cs
```csharp
public class TowerColorEffect : MonoBehaviour, IUpdatable
{
    private const float COLOR_TWEEN_DURATION = 0.5f;
    private const float MID_HP_RATIO = 0.7f;
    private const float LOW_HP_RATIO = 0.3f;

    [SerializeField] private SpriteRenderer m_SpriteRenderer;
    [SerializeField] private Color m_HighHpColor = new Color(0f, 0.8980392f, 1f);       // #00E5FF
    [SerializeField] private Color m_MidHpColor = new Color(0f, 0.7372549f, 0.83137256f); // #00BCD4
    [SerializeField] private Color m_LowHpColor = new Color(1f, 0.2f, 0.33333334f);       // #FF3355

    // ObservableIntText와 동일한 "Current 준비될 때까지 폴링 → 구독 성공하면 이벤트로만" 패턴
    public void UpdateLogic() { ... }
    private void OnHpChanged(int _oldValue, int _newValue)
    {
        float hpRatio = (float)_newValue / TowerHealth.Current.maxHp;
        Color targetColor = GetColorForRatio(hpRatio);
        TweenUtil.Color(m_SpriteRenderer.material, targetColor, COLOR_TWEEN_DURATION);
    }
}
```
- `TowerHealth.currentHp`(ObservableVariable&lt;int&gt;)를 구독 — HP가 바뀔 때마다 비율을 계산해 해당 구간 색으로 **`m_SpriteRenderer.material`을 DOTween으로 트윈**(스냅 아님, 0.5초).
- 색상 3종은 `[SerializeField]`로 노출 — 디자인 튜닝은 인스펙터에서 가능(코드 재수정 불필요).
- 씬 배치: InGameScene.unity의 ActorPlayer 오브젝트에 부착, `m_SpriteRenderer`는 같은 오브젝트의 SpriteRenderer(= `GlowMat_Tower` 머테리얼 사용)를 참조.

## 설계 근거
- **`SpriteRenderer.color`가 아니라 `.material.color`를 트윈하는 이유**: 이 프로젝트의 몬스터/타워 스프라이트는 커스텀 글로우 셰이더(`_Color`, `_GlowAmount` 프로퍼티)를 쓰고, 실제 표시 색은 셰이더의 `_Color`가 결정한다(`SpriteRenderer.color`는 흰색 그대로 안 건드림 — [[ActorMonster]].SetColor()가 이미 `m_Renderer.material.color`를 쓰는 것과 동일 컨벤션). 기존 `TweenUtil.Color(SpriteRenderer,...)`는 `SpriteRenderer.color`를 트윈해서 이 셰이더엔 안 먹혀 — Material 전용 오버로드를 새로 추가.
- **텍스트가 아니라서 [[ObservableIntText]]를 상속하지 않음**: 그 베이스는 `TMPro.TextMeshProUGUI` 대상으로 설계돼 있어 색상 연출엔 안 맞음. 대신 "Current 준비될 때까지 폴링, 구독 성공하면 이벤트로 전환"이라는 핵심 아이디어만 그대로 복제 — 굳이 공용 베이스로 추상화하지 않음(현재 이 패턴을 쓰는 곳이 텍스트 2곳 + 이 색상 효과 1곳뿐이라 무리하게 합칠 필요 없다고 판단, 필요해지면 그때 추출).
- **경계값(70%/30%) 처리는 `<=`**: CODE.MD 규칙("숫자 비교엔 == 금지, 범위 비교 사용")에 맞춰 `hpRatio <= 0.3f` / `hpRatio <= 0.7f`로 계산.

## 작업 내역

### 2026-07-22-0

#### 개요
사용자 요청("기획에서 보고 HP달때마다 Player색깔 변하는거 연출 적용해줘, 머테리얼도 색이 서서히 변해야함") — 02_combat.html 스펙대로 신규 구현.

#### 신규 파일
- Assets/Scripts/InGame/TowerColorEffect.cs (Unity MCP `manage_script`로 생성, guid 자동 발급)

#### 연관 수정
- [[TweenUtil]] — `Color(Material, Color, float)` 오버로드 신규 추가(위 참고)
- Assets/Scenes/InGameScene.unity — ActorPlayer에 `TowerColorEffect` 컴포넌트 부착 + `m_SpriteRenderer` 필드를 같은 오브젝트의 SpriteRenderer로 연결

#### 검증 (Play Mode 실측, `Time.timeScale=5`로 진행)
- `TowerHealth.Init(100)` → `TakeDamage(40)`(100→60, Mid 티어 진입) → 잠시 대기 후 색이 Mid(`#00BCD4`, RGB 0/0.737/0.831)로 정확히 도달.
- 이어서 `TakeDamage(35)`(60→25, Low 티어 진입) → 트윈 시작 시점 색이 직전 Mid 색에서 출발(스냅 아님을 코드 경로로 확인 — `DOColor()`는 현재 값에서 목표 값으로 보간) → 완료 후 Low(`#FF3355`, RGB 1/0.2/0.333)에 정확히 도달.
- 컴파일 에러 0건, 콘솔 에러 0건.
- **미검증**: 정확한 프레임 단위 중간값(트윈이 진짜로 프레임마다 보간되는 모습) 캡처는 못함 — MCP 왕복 지연이 트윈 지속시간(0.5초, 5배속이면 0.1초)보다 길어 중간 프레임을 정확히 잡기 어려움. 대신 `TweenUtil.Color`가 `Material.DOColor()`(DOTween 표준 보간 API)를 그대로 호출하는 코드이므로 "서서히 변한다"는 설계상 보장됨.
- **⚠️ 위 검증은 불충분했음** — `material.color` 프로퍼티 값만 코드로 읽어서 "의도한 값과 일치"를 확인했을 뿐, 실제 화면에 어떻게 보이는지 스크린샷으로 확인하지 않았다. 사용자가 "머테리얼 변화도 없고, 그냥 색만 어두어지는데, 제대로 검증이 안됬는데?"라고 지적해서 재검증 진행 — 아래 2026-07-22-1 참고. 값이 맞다는 것과 화면에 의도대로 보인다는 것은 다른 문제였다.

---

## 2026-07-22-1

### 개요
사용자 지적("머테리얼 변화도 없고, 그냥 색만 어두어지는데") + "적군도 색이 이상해졌어" — 실제 스크린샷(`manage_camera` screenshot)으로 재검증하다가 서로 다른 두 가지 근본 원인을 발견, 둘 다 수정.

### 원인 1 — 글로우 셰이더의 `_Color × _GlowAmount` 증폭 (몬스터+타워 공통)
`Assets/Resources/URP/Glow.shadergraph`를 직접 열어 노드 그래프를 추적한 결과, 실제 최종 색 계산은 `BaseColor = _Color × _GlowAmount`(Multiply 노드)이고, 그래프에 있는 "Blend" 노드(Base=텍스처, Blend=_Color, Opacity=_GlowAmount)는 **출력이 어디에도 연결되지 않은 죽은 노드**라 텍스처 자체의 색은 최종 출력에 전혀 영향을 못 준다(알파만 사용). 프로젝트의 모든 GlowMat 계열 머테리얼(`GlowMat_Tower`, `GlowMat_Enemy/*` 11종)이 `_GlowAmount: 2`로 고정돼 있어서, 0.7~1.0대 색상 채널이 2배가 되며 1.0을 넘겨 흰색으로 클램프됨 — 스크린샷으로 실제 확인(플레이어가 완전히 하얗게, 몬스터 5종이 파스텔로 뭉개짐).
- **몬스터가 지금 와서야 이상해 보인 이유**: 이 문제 자체는 원래부터 있었지만, 몬스터 프리팹이 얼마 전까지 [[ActorMonster]] 2026-07-22-0에 기록된 것처럼 **존재하지 않는 깨진 머테리얼**을 참조하고 있어서 이 글로우 셰이더 자체가 적용된 적이 없었다(Unity가 조용히 폴백 렌더링). 그 깨진 참조를 GlowMat으로 고치고 나서야(=글로우 셰이더가 처음으로 실제 적용되고 나서야) 이 증폭 버그가 드러난 것 — 새로 만든 버그가 아니라 가려져 있던 기존 버그.
- **수정 범위 확인**: 씬에 전역 URP Bloom(threshold=1, intensity=0.8)도 추가로 영향을 주고 있어(색상 채널이 1.0에 가까우면 Bloom도 걸림) 두 원인이 겹쳐 있었음. 사용자에게 A(`_GlowAmount`만 2→1) / B(Bloom 설정도 조정) / C(색상 값 자체 재조정) 3가지 옵션을 제시했고, **A안**으로 확정.
- 수정: `GlowMat_Tower.mat` + `GlowMat_Enemy/` 11종 전부 `_GlowAmount: 2` → `1`(총 12개 파일). `GlowMat_TitleSquare.mat`/`UIGlowMat.mat`은 이번 범위 밖이라 그대로 둠(둘 다 여전히 2).

### 원인 2 — `SpriteRenderer.color`(표준 틴트) 잔존값이 `_Color`와 곱연산 (타워만 해당)
ActorPlayer의 `SpriteRenderer.color`가 씬에 예전부터 시안(#00E5FF 근사치)으로 baked돼 있었는데(2026-07-20에 크기만 조정하고 색은 안 건드렸던 필드), 이 셰이더의 ShaderGraph 타겟 설정이 `m_DisableTint: false`라 **최종 출력에 SpriteRenderer의 틴트가 자동으로 곱해진다**(ShaderGraph Sprite Unlit 표준 동작). `TowerColorEffect`는 `material.color`(`_Color`)만 바꾸고 `SpriteRenderer.color`는 안 건드렸기 때문에, 실제 화면 색 = `_Color × (예전 시안 틴트)` — 시안은 R성분이 0이라 **Low 티어(빨강)를 넣어도 R이 곱해져서 사라지고 파란-회색으로 보임**. 스크린샷으로 실측: 코드상 `material.color`는 정확히 `(1, 0.2, 0.333)`(빨강)인데 화면은 파란 회색.
- 몬스터는 프리팹의 `SpriteRenderer.m_Color`가 원래 흰색(1,1,1,1)이라 이 문제 없음 — ActorPlayer만 해당.
- 수정: `TowerColorEffect.Start()`에 `m_SpriteRenderer.color = Color.white;` 추가 — 이후 색상은 전부 `material.color` 트윈만으로 결정되도록 틴트를 무력화.

### 파일
- Assets/Resources/Mat/GlowMat_Tower.mat
- Assets/Resources/Mat/Enemy/GlowMat_{Circle,Diamond,Pentagon,Square,Triangle}_{Normal,Elite}.mat, GlowMat_Star_Boss.mat (11개)
- Assets/Scripts/InGame/TowerColorEffect.cs

### 검증 (실제 스크린샷, `manage_camera` + 멀티모달로 육안 확인 — 이번엔 프로퍼티 값이 아니라 렌더링 결과 자체를 봄)
- 수정 전: 플레이어 100% HP 스크린샷 — 완전히 하얗게 클램프. Low 티어(25%) — 코드상 `_Color`는 빨강인데 실제 렌더링은 파란-회색(SpriteRenderer.color 곱연산 확인).
- `_GlowAmount`만 1로 낮춘 중간 테스트(런타임, 에셋 미저장) — 몬스터 5종 색이 뚜렷이 구분되기 시작(더 이상 순백색 아님), 다만 여전히 파스텔(Bloom 잔존 영향, A안 선택 범위 밖).
- `SpriteRenderer.color`를 흰색으로 리셋하는 런타임 테스트 — Low 티어가 즉시 의도한 빨강/핑크로 정상 렌더링됨을 확인.
- 두 수정을 실제 코드/에셋에 반영 후 재검증: High(100%) 선명한 시안, Mid(60%) 밝은 시안(High와 다소 유사 — Bloom이 밝은 색 둘 다에 비슷하게 작용, B안을 안 골랐으므로 예상된 범위), Low(25%) 명확한 빨강/핑크로 정상 렌더링. 몬스터 5종도 서로 뚜렷이 구분되는 파스텔 색상으로 정상 렌더링.
- 콘솔 에러 0건.
- 디버그용으로 생성한 `Assets/Screenshots/` 임시 스크린샷 10장은 검증 후 삭제(내가 만든 임시 산출물).
- **⚠️ 이 검증도 불완전했음** — "몬스터 5종이 서로 구분되는 파스텔 색"을 정상으로 판단했으나, 사용자가 "그 색상도 누리끼리해", "정확히는 물빠진 색상이야"라고 재차 지적. `_GlowAmount` 증폭과 SpriteRenderer 틴트는 둘 다 실재하는 진짜 버그였지만, 전체 채도 저하의 진짜 원인은 아니었다 — 아래 2026-07-22-2 참고.

---

## 2026-07-22-2

### 개요
사용자가 "InGame가서 확인해보고 Title처럼 쨍한 색으로 고쳐놔"라고 재지시, 이후 "그 Title도 내가 위에 겹쳐둔 것"이라 Title도 기준이 될 수 없다고 정정. "글로우 효과를 버리면 안 된다"는 제약 하에 진짜 원인을 계속 추적.

### 진짜 원인 — URP Pipeline Asset의 Color Grading Mode가 LDR
`Assets/Settings/UniversalRP.asset`의 `colorGradingMode`가 `LowDynamicRange`로 설정돼 있었다. Volume에 Tonemapping/ColorAdjustments 오버라이드가 하나도 없어도(둘 다 `TryGet` 결과 `false`), URP는 LDR 그레이딩 모드에서 항상 내부 LUT을 거치며 이 LUT 자체가 채도를 깎는다. **Glow 셰이더와 순정 `Sprites/Default` 셰이더 양쪽에 완전히 동일한 HDR 색상 값(`#00E5FF`)을 넣고 나란히 렌더링해도 둘 다 똑같이 파스텔로 나오는 것**으로 이를 확인 — 즉 셰이더/머테리얼 문제가 아니라 카메라 이후 전역 렌더 파이프라인 문제였다. `colorGradingMode`를 `HighDynamicRange`로 바꾸자 두 셰이더 모두 즉시 기획서 원색(`#00E5FF`)대로 렌더링됨.
- **이전 세션에서 이미 배제했던 용의자들이 실제로 전부 무죄였던 이유**: Bloom threshold를 올려도(1.3), `.linear`/`.gamma` 변환을 시도해도 core color가 그대로 파스텔이었던 건 이 두 가지가 원인이 아니었기 때문 — 진짜 원인(LDR 그레이딩 LUT)은 건드리지 않았으니 당연히 안 고쳐졌던 것.
- **검증 방법**: `Camera.Render()` + `RenderTexture.ReadPixels`로 정확한 픽셀 RGB를 수치로 확인 + `manage_camera` 실제 게임 화면 스크린샷 두 가지 경로 모두로 교차 검증(하나는 수치, 하나는 육안) — 배경(순수 검정)과 원본 텍스처(순수 흰색 1,1,1,1, PNG 파일 직접 디코드해서 확인)가 무죄임을 먼저 배제한 뒤, Bloom on/off 비교(무관함 확인) → Tonemapping/ColorAdjustments 존재 여부 확인(둘 다 없음) → Pipeline Asset의 `colorGradingMode` 확인(LDR 발견) → HDR로 전환 후 즉시 재현되는 개선을 스크린샷으로 확인, 순서로 좁혀갔다.

### 수정
- `Assets/Settings/UniversalRP.asset`: `colorGradingMode` `LowDynamicRange` → `HighDynamicRange` (`SetDirty` + `AssetDatabase.SaveAssets()`로 영구 저장). **프로젝트 전역 그래픽 설정**이라 InGame뿐 아니라 Title 등 다른 씬에도 영향을 준다 — 다른 화면도 재검증 필요(미완료, 사용자 확인 대기 중).
- 이전 2026-07-22-1의 `_GlowAmount: 2→1` 수정과 `SpriteRenderer.color = Color.white` 수정은 그대로 유지 — 둘 다 이것과 무관하게 실재했던 별개의 진짜 버그였음(전자는 흰색 클램프, 후자는 틴트 곱연산으로 Low 티어 빨강이 사라지던 문제).

### 검증 (실제 스크린샷, `manage_camera` game view)
- 타워(육각형) 단독: HDR 전환 후 쨍한 시안 + Bloom halo, 기획서 "Cyan / 강한 글로우" 스펙과 일치.
- 몬스터 6종(Triangle/Circle/Square/Diamond/Pentagon/Star) 나란히 스폰: 채도 저하는 해결됐으나, **Star(노랑)를 제외한 5종이 전부 동일한 핑크색** — 이건 채도 버그와 무관한 별개 문제(머테리얼별 `_Color` 값 자체가 다양화되지 않은 것으로 추정)이며 아직 미해결. 사용자 확인 후 착수 예정.
- 콘솔 에러 0건. 디버그용 스크린샷은 확인 후 정리 예정.

### 남은 작업
1. Title 등 다른 씬도 HDR 전환 후 이상 없는지 재확인
2. 몬스터 5종 동일 핑크색 문제(색상 다양화 재작업) — 사용자 승인 후 착수
3. 기획서에 명시된 "글로우 강도 티어별 변화 + 30% 이하 적색 펄스 점멸"은 아직 미구현(색상 전환만 구현됨) — 사용자 확인 결과 이번 채도 버그 수정 이후 순서로 진행 예정
