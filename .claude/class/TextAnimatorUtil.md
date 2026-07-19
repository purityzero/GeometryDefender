# TextAnimatorUtil

연관 클래스: (외부) Text Animator for Unity 3.11.1 — TextAnimator_TMP, TypewriterComponent, TypingDelaysByCharacter / (Glory) TweenUtil(동일 컨셉의 DOTween 래퍼)

## 개요
Text Animator(Febucci) 에셋을 TweenUtil처럼 **정적 헬퍼 한 줄 호출**로 쓰게 해주는 래퍼.
컴포넌트 세팅 없이 TMP 텍스트만 넘기면 필요한 컴포넌트(TextAnimator_TMP, TypewriterComponent)를 자동으로 붙여준다.

- 경로: Assets/Scripts/Glory/TextAnimation/TextAnimatorUtil.cs
- 패키지: Packages/com.febucci.text-animator-unity (로컬 임베드), 전역 설정 에셋: Assets/Plugins/Febucci/Text Animator for Unity/Resources/TextAnimatorSettings.asset

---

## 1. 빠른 시작

```csharp
// 효과 태그가 적용된 텍스트 즉시 표시 (타자기 없음)
TextAnimatorUtil.SetText(m_TitleText, "<wave>GEOMETRY</wave> <rainb>DEFENDER</rainb>");

// 타자기 재생 + 완료 콜백 (DOTween의 OnComplete 감각)
TextAnimatorUtil.PlayTypewriter(m_DialogText, "안녕하세요!<waitfor=0.5> 반갑습니다.", () => ShowNextButton());

// 스킵 / 사라지기 / 속도
TextAnimatorUtil.SkipTypewriter(m_DialogText);
TextAnimatorUtil.HideTypewriter(m_DialogText, () => ClosePanel());
TextAnimatorUtil.SetTypewriterSpeed(m_DialogText, 2f);
```

## 2. API

| 함수 | 설명 |
|---|---|
| `SetText(TMP_Text, string)` | 효과 태그 파싱해서 **즉시 전체 표시** (타자기 없음) |
| `PlayTypewriter(TMP_Text, string, UnityAction onComplete = null)` | 타자기로 한 글자씩 표시. 완료 시 onComplete 1회 호출. TypewriterComponent 반환 |
| `SkipTypewriter(TMP_Text)` | 타자기 스킵(전체 즉시 표시). 타자기 컴포넌트 없으면 무시 |
| `HideTypewriter(TMP_Text, UnityAction onComplete = null)` | 글자를 순차적으로 사라지게 함. 완료 시 onComplete 1회 호출 |
| `SetTypewriterSpeed(TMP_Text, float)` | 타자기 배속 (1 = 기본) |
| `GetAnimator(TMP_Text)` / `GetTypewriter(TMP_Text)` | 컴포넌트 확보(없으면 AddComponent). 세부 설정을 직접 만지고 싶을 때 |

- 모든 함수는 null 텍스트에 에러 로그 후 안전 반환.
- `GetTypewriter`가 런타임 추가 시 기본값: useTypeWriter=true, startTypewriterMode=FromScriptOnly(스크립트로만 시작), 타이밍=TypingDelaysByCharacter 기본값(일반 글자 0.03초, `!?.` 0.6초, `;:)-,` 0.2초).
- **인스펙터에서 미리 세팅한 컴포넌트가 있으면 그대로 재사용**한다(덮어쓰지 않음) — 타이밍/설정 커스텀은 인스펙터 방식 권장.

## 3. 효과 태그 치트시트 (이 프로젝트 기본 데이터베이스 실측)

### 지속 효과 (Behaviors) — `<태그>내용</태그>`
| 태그 | 효과 | 태그 | 효과 |
|---|---|---|---|
| `<wave>` | 물결 | `<bounce>` | 통통 튐 |
| `<shake>` | 흔들림 | `<rot>` | 회전 |
| `<wiggle>` | 꿈틀거림 | `<incr>` | 크기 커짐 |
| `<swing>` | 좌우 흔들기 | `<fade>` | 깜빡임 |
| `<slideh>` / `<slidev>` | 가로/세로 슬라이드 | `<rainb>` | 무지개 색 |
| `<pend>` | 진자 운동 | `<dangle>` | 대롱대롱 |
| `<expand>` | 확장 | | |

### 등장 효과 (Appearances) — `{태그}내용{/태그}`
같은 태그명을 중괄호로 쓰면 "글자가 나타날 때 1회" 연출이 된다. 예: `{size}`, `{fade}`, `{bounce}`

### 퇴장 효과 (Disappearances) — `{#태그}내용{/#태그}`
HideTypewriter로 사라질 때의 연출. 예: `{#fade}`

### 타자기 액션 — 타자기 재생 중에만 동작
| 태그 | 효과 |
|---|---|
| `<waitfor=1.5>` | 해당 위치에서 1.5초 대기 |
| `<speed=2>` | 이후 타자 속도 2배 (`<speed=0.5>`면 절반) |
| `<waitinput>` | 아무 입력이 올 때까지 대기 |

### 이벤트 — `<?메시지>`
해당 글자가 표시되는 순간 `TypewriterComponent.onMessage`로 문자열이 넘어온다 (연출 트리거용).

```csharp
TypewriterComponent typewriter = TextAnimatorUtil.GetTypewriter(m_DialogText);
typewriter.onMessage.AddListener((message) => Debug.Log(message.Message));
```

## 4. 주의

- **UIToastMessage 등 기존 텍스트 세팅 경로와 혼용 금지**: TextAnimator_TMP가 붙은 TMP에 `text` 프로퍼티로 직접 대입하면 태그가 파싱되지 않거나 애니메이션이 갱신 안 될 수 있다 — 반드시 SetText/PlayTypewriter 경유.
- PlayTypewriter를 완료 전에 다시 호출하면 이전 onComplete 리스너가 남아 다음 완료 때 같이 불릴 수 있다 — 반복 호출 화면이면 onComplete 없이 쓰거나 이벤트를 직접 관리할 것.
- 태그가 안 먹으면: TextAnimatorSettings.asset(Resources)과 Effects Database 연결을 먼저 확인.
- Glory 라이브러리 파일이지만 **Text Animator 패키지 의존** — 패키지 없는 프로젝트에 Glory를 복사하면 이 폴더(TextAnimation)는 빼고 복사할 것 (glory.md 허용 의존 참고).

---

## 2026-07-20-0

### 개요
사용자 요청: "TextAnimator Asset도 DOTween처럼 쓰기 쉽게 + 설명서". TweenUtil과 같은 정적 헬퍼 패턴으로 신규 작성.

### 파일
- Assets/Scripts/Glory/TextAnimation/TextAnimatorUtil.cs (신규)

### 구현 근거 (패키지 실측)
- `TypewriterComponent.ShowText()` 후 `StartShowingText(true)` 명시 호출 — 공식 샘플(DefaultEffectsExample) 패턴. startTypewriterMode를 FromScriptOnly로 둬서 유틸 경유 시작만 허용.
- 런타임 AddComponent 시 타이밍 스크립터블이 비어 NRE 위험 → `ScriptableObject.CreateInstance<TypingDelaysByCharacter>()`로 기본 타이밍 주입 (필드 기본값 0.03/0.6/0.2초).
- onComplete는 UnityEvent에 1회용 리스너(AddListenerOnce, 자기 제거 람다)로 구현.
- 태그 치트시트는 Assets/Plugins/Febucci/.../Effects/*.asset의 tagId 실측값 (wave/shake/wiggle/swing/slideh/slidev/bounce/rot/incr/fade/rainb/pend/dangle/expand), 파싱 괄호는 TextAnimatorSettings.asset 실측 (behaviors=`<>`, appearances=`{}`, disappearances=`{#}`), 액션은 Actions/*.asset tagID(speed/waitfor/waitinput).

### 미검증
에디터 미실행 상태 작성. 컴파일, 런타임 AddComponent 경로(특히 TypewriterComponent Awake 시점에 타이밍 null 상태 통과 여부), 타자기/효과 실동작 확인 필요.
