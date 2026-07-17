# UIToastMessage

연관 클래스: [Factory](./Factory.md)(FactoryObject 정의), [TweenEffectPlayer](./TweenEffectPlayer.md), [UIManager](./UIManager.md)(사용처, 풀링/스태킹 소유자)
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
