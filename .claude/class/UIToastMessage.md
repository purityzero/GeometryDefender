# UIToastMessage

연관 클래스: [Factory](./Factory.md)(FactoryObject 정의), [TweenEffectPlayer](./TweenEffectPlayer.md), [UIManager](./UIManager.md)(사용처, 풀링/스태킹 소유자), [TextAnimationPlayer](./TextAnimationPlayer.md)(메시지 타자기 출력, 프리팹 부착)
프리팹: Assets/Resources/Prefabs/UI/UIToastMessage.prefab (.claude/prefab/UIToastMessage.md 참고)

## 개요
잠시 떴다가 자동으로 사라지는 공용 토스트 메시지 위젯(Glory 라이브러리, 프로젝트 비의존). `UIManager.instance.ShowToast(message)`로 사용 — 풀링·스태킹은 UIManager가 소유하고, 이 클래스는 개별 토스트 아이템 하나의 표시/애니메이션만 담당.

## 현재 상태
```csharp
public class UIToastMessage : FactoryObject
{
    [SerializeField] private RectTransform m_RectTransform;
    [SerializeField] private Image m_Image;
    [SerializeField] private TextMeshProUGUI m_MessageText;
    [SerializeField] private TweenEffectPlayer m_TweenPlayer;
    [SerializeField] private TextAnimationPlayer m_TextPlayer;  // 타자기 출력 (배속은 프리팹 인스펙터 값 2)
    [SerializeField] private float m_MoveDuration = 0.2f;

    public RectTransform rectTransform => m_RectTransform;

    public override void Open() { ... }   // SetAlpha(0f) — Image/TMP 알파 직접 0으로
    public override void Close() { ... }  // m_TweenPlayer.Stop()
    public void Show(string _message, UnityAction<UIToastMessage> _onClosed) { ... }
    public void MoveTo(Vector2 _anchoredPosition) { ... }  // 스태킹 리포지션용, DOKill 후 이동
    private void SetAlpha(float _alpha) { ... }  // m_Image.color.a + m_MessageText.alpha 동시 설정
}
```
- **CanvasGroup을 쓰지 않는다** — 사용자 지적("Tween 기능 Fade 기능 있잖아")으로 제거. 대신 배경(Image)과 텍스트(TMP) 각각의 알파를 `FadeTweenEffect`로 직접 애니메이션 — `FadeTweenEffect.CreateTween()`이 CanvasGroup→Image→SpriteRenderer→TMP 순으로 자기 오브젝트에서 자동 탐색하는 걸 활용, 루트엔 Image용 FadeTweenEffect 2개, Text_Message 자식엔 TMP용 FadeTweenEffect 2개를 붙여 TweenEffectPlayer의 `m_Effects`에서 Join으로 짝지어 동시 재생.
- 페이드인→대기→페이드아웃 시퀀스는 코드가 아니라 프리팹의 `TweenEffectPlayer` + `FadeTweenEffect` 4개(이미지 in Append → 텍스트 in Join → 이미지 out Append → 텍스트 out Join)로 구성 — 대기 시간은 세 번째(이미지 out) FadeTweenEffect의 `m_Delay`로 표현(0.2s 페이드인 → 1.5s 대기 → 0.3s 페이드아웃). 배경은 완전 불투명이 아니라 0.9까지만 페이드인(원래 반투명 배경 디자인 유지).
- `TweenEffectPlayer.m_isPlayOnEnable = false` 필수 — 풀에서 재사용(SetActive(true))될 때 아직 메시지 텍스트도 안 정해진 상태로 자동재생 시작되는 걸 방지. `Show()`가 명시적으로 `Play(콜백)` 호출.
- `MoveTo`의 목표 위치는 스태킹 인덱스 기반으로 매번 런타임에 계산되는 값이라 `TweenEffectPlayer`(고정 시퀀스용) 대신 `TweenUtil.MoveAnchored` 직접 호출 — 단 duration(`m_MoveDuration`)만 프리팹 필드로 노출.
- 풀링 대상이라 `FactoryObject` 상속 필수(Awake/OnEnable 대신 Open/Close) — glory.md 규칙 준수.
- 루트 RectTransform은 **화면 중앙 앵커**(0.5, 0.5) — [UIManager](./UIManager.md)가 별도 ToastCanvas/ToastRoot 없이 기존 PopupCanvas에 바로 이 프리팹을 Instantiate하므로, 위치 배치는 전부 이 프리팹 자신의 anchor/pivot이 담당.

## 2026-07-18-0

### 개요
사용자 요청("Shard가 부족합니다 같은 문구 뜨는 공용 알람 팝업") → "잠시 뜨면 자동 사라짐 + 4~5개 풀로 위로 쌓이면서 사라지는 형식"으로 구체화. 처음엔 별도 `ToastManager` MonoSingleton으로 설계했으나, 사용자 지적으로 [UIManager](./UIManager.md)에 편입(풀링/스태킹 로직은 UIManager 쪽 문서 참고). 이후 사용자 지적으로 Show/Hide 시퀀스를 수작업 TweenSequenceBuilder 대신 프로젝트의 [TweenEffectPlayer](./TweenEffectPlayer.md) 인스펙터 조립 시스템으로 교체.

### 파일
- Assets/Scripts/Glory/UI/Toast/UIToastMessage.cs (신규)
- Assets/Resources/Prefabs/UI/UIToastMessage.prefab (신규)

### 수정 전/후 (Show 시퀀스)
```csharp
// 1차: 수작업 TweenSequenceBuilder
public void Show(string _message, float _displayDuration, UnityAction<UIToastMessage> _onClosed)
{
    m_MessageText.SetText(_message);
    m_OnClosed = _onClosed;
    m_ShowSequence = TweenSequenceBuilder.Create()
        .Append(TweenUtil.Fade(m_CanvasGroup, 1f, 0.2f))
        .Delay(_displayDuration)
        .Append(TweenUtil.Fade(m_CanvasGroup, 0f, 0.3f))
        .OnComplete(OnShowSequenceComplete)
        .Play();
}

// 2차: TweenEffectPlayer로 교체 (현재)
public void Show(string _message, UnityAction<UIToastMessage> _onClosed)
{
    m_MessageText.SetText(_message);
    m_OnClosed = _onClosed;
    m_TweenPlayer.Play(OnShowComplete);
}
```
대기시간(`_displayDuration` 파라미터)은 프리팹의 두 번째 FadeTweenEffect의 `m_Delay`로 이동 — 메서드 시그니처에서 파라미터 자체가 사라짐.

### 미검증
컴파일, 실제 페이드/스태킹 애니메이션, 라이브 에디터 확인 필요.

---

## 2026-07-18-1

### 개요
사용자 지적 3건. (1) "중앙에서부터 띄우게" — 하단 앵커 → 중앙 앵커로 변경. (2) "ToastCanvas 만들지 말고 ... Popup이랑 같이 써" — [UIManager](./UIManager.md)가 전용 Canvas/ToastRoot를 만들지 않고 기존 PopupCanvas에 바로 이 프리팹을 올리도록 변경(UIManager.md 참고), 그 결과 이 프리팹의 앵커가 곧 화면상 배치 기준이 됨. (3) "CanvasGroup UIToastMessage 프리팹에 넣지마, Tween기능 Fade기능 있잖아" — CanvasGroup 제거, Image/TMP 각각을 FadeTweenEffect로 직접 페이드.

에디터(사용자 또는 임포트 과정)가 CanvasGroup을 지운 뒤 자동으로 CanvasRenderer를 추가해 둔 상태였음(Image가 CanvasRenderer를 요구하는데 원래 프리팹에 빠져 있었던 것으로 보임 — 최초 작성 시 실수).

### 파일
- Assets/Scripts/Glory/UI/Toast/UIToastMessage.cs
- Assets/Resources/Prefabs/UI/UIToastMessage.prefab

### 수정 전/후 (코드)
```csharp
// Before
[SerializeField] private CanvasGroup m_CanvasGroup;
...
public override void Open()
{
    base.Open();
    m_CanvasGroup.alpha = 0f;
}

// After
[SerializeField] private Image m_Image;
...
public override void Open()
{
    base.Open();
    SetAlpha(0f);
}

private void SetAlpha(float _alpha)
{
    Color imageColor = m_Image.color;
    imageColor.a = _alpha;
    m_Image.color = imageColor;
    m_MessageText.alpha = _alpha;
}
```

### 수정 (프리팹, 오브젝트 단위)

**UIToastMessage (루트)**
- RectTransform: anchor(0.5,0) pivot(0.5,0) → anchor(0.5,0.5) pivot(0.5,0.5) (중앙)
- CanvasGroup 컴포넌트 제거, CanvasRenderer 추가(Image 렌더링에 필요 — 누락돼 있던 것 보정)
- UIToastMessage 컴포넌트: `m_CanvasGroup` 필드 제거, `m_Image` 필드 추가(자기 자신의 Image, fileID ...1003)
- 기존 FadeTweenEffect(...1006, 페이드인): `m_TargetAlpha` 1 → 0.9 (배경 반투명 유지)

**Text_Message (자식)**
- FadeTweenEffect 2개 신규 추가: ...1014(StepType=Join, Duration 0.2, TargetAlpha 1 — 이미지 페이드인과 동시), ...1015(StepType=Join, Duration 0.3, TargetAlpha 0 — 이미지 페이드아웃과 동시)

**TweenEffectPlayer(...1005) m_Effects**
- 전: `[1006(이미지 in), 1007(이미지 out)]`
- 후: `[1006(이미지 in, Append), 1014(텍스트 in, Join), 1007(이미지 out, Append), 1015(텍스트 out, Join)]`

### 미검증
컴파일, 프리팹 컴포넌트 연결(missing 아님), 실제 중앙 배치/페이드 애니메이션 확인 필요.

---

## 2026-07-20-0

### 개요
사용자 요청: "ToastMessage에 TextAnimator 적용, 타이핑 2배". 메시지 텍스트 세팅을 직접 대입(SetText)에서 TextAnimatorUtil 타자기(2배속)로 교체. 프리팹은 변경 없음 — TextAnimator_TMP/TypewriterComponent는 첫 Show 때 유틸이 런타임 자동 부착하고 풀링으로 재사용된다.

### 파일
- Assets/Scripts/Glory/UI/Toast/UIToastMessage.cs

### 수정 전/후 (Show)
```csharp
// Before
m_MessageText.SetText(_message);
m_OnClosed = _onClosed;

m_TweenPlayer.Play(OnShowComplete);

// After  (+ 클래스 상단에 private const float TYPEWRITER_SPEED = 2f;)
m_OnClosed = _onClosed;

TextAnimatorUtil.SetTypewriterSpeed(m_MessageText, TYPEWRITER_SPEED);
TextAnimatorUtil.PlayTypewriter(m_MessageText, _message);

m_TweenPlayer.Play(OnShowComplete);
```

### 주의
- 이 변경으로 UIToastMessage가 Text Animator 패키지에 의존하게 됨 — 패키지 없는 프로젝트로 Glory 복사 시 이 타자기 호출 두 줄을 SetText로 되돌려야 한다 (glory.md 의존 주의 항목 갱신됨).
- 기존 텍스트 페이드(FadeTweenEffect의 TMP.alpha 트윈)와 타자기 등장이 겹쳐 재생됨 — 이론상 TMP 갱신 이벤트로 공존하지만 실기 확인 필요.

### 미검증
컴파일, 타자기+텍스트 알파 페이드 공존(첫 0.2초 구간), 풀 재사용 시 두 번째 Show 정상 동작 확인 필요.

---

## 2026-07-20-1

### 개요
사용자 요청: "Util 직접 호출 말고 컴포넌트 붙이는 식으로". 2026-07-20-0에서 넣은 TextAnimatorUtil 정적 호출을 프리팹에 부착한 [TextAnimationPlayer](./TextAnimationPlayer.md) 참조로 교체. 배속 2는 코드 상수에서 프리팹 인스펙터 값(m_TypewriterSpeed=2)으로 이동.

### 파일
- Assets/Scripts/Glory/UI/Toast/UIToastMessage.cs
- Assets/Resources/Prefabs/UI/UIToastMessage.prefab (.claude/prefab/UIToastMessage.md 2026-07-20-0 참고)

### 수정 전/후 (Show)
```csharp
// Before (+ private const float TYPEWRITER_SPEED = 2f;)
TextAnimatorUtil.SetTypewriterSpeed(m_MessageText, TYPEWRITER_SPEED);
TextAnimatorUtil.PlayTypewriter(m_MessageText, _message);

// After (+ [SerializeField] private TextAnimationPlayer m_TextPlayer; 필드 추가, 상수 제거)
m_TextPlayer.Play(_message);
```

### 미검증
컴파일, 프리팹 참조 연결, 타자기 2배속 실동작 확인 필요.

---

## 2026-07-22-0

### 개요
사용자 요청("모든 기본 텍스트 LiberationSans SDF 글꼴로 바꿔줘") — 프로젝트 전체 TMP 텍스트 중 이 프리팹의 `Text_Message`만 유일하게 `DungGeunMo Bitmap` 폰트 에셋을 쓰고 있던 것을(전체 49곳 중 4곳만 예외, 나머지는 전부 `LiberationSans SDF`) 프로젝트 기본 폰트로 통일.

### 파일
- Assets/Resources/Prefabs/UI/UIToastMessage.prefab

### 수정 (오브젝트 단위)
**Text_Message**
- `m_fontAsset`: `DungGeunMo Bitmap`(guid `7e00a561b2f97e04bbe6e3b6876e22e5`) → `LiberationSans SDF`(guid `8f586378b4e144a9851e7b34d9b748ee`)
- `m_sharedMaterial`: 대응 폰트의 기본 머테리얼로 교체 — 프로젝트 내 `LiberationSans SDF` 사용처 49곳 중 49곳이 쓰는 `{fileID: 2180264, guid: 8f586378b4e144a9851e7b34d9b748ee}` 패턴 그대로 적용.

### 검증 (2026-07-22, Play Mode)
`UIManager.instance.ShowToast("선행 조건을 먼저 해금하세요.")` 실제 호출 → 토스트에 한글 문구가 깨지지 않고 정상 렌더링되는 것 확인(`text.font.name == "LiberationSans SDF"`). 콘솔 에러 0건.

**주의(추후 정정됨)**: 이 검증은 `text.font.name`/`text.text` 프로퍼티만 확인했을 뿐 실제 렌더링 픽셀(스크린샷)은 확인하지 않았음 — 실제로는 `LiberationSans SDF`에 한글 글리프가 없어 화면엔 깨져 보였다. 근본 수정은 [[UIText]] 2026-07-22-0(폰트 자체에 Fallback 등록) 참고.
