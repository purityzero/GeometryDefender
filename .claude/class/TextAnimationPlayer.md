# TextAnimationPlayer

연관 클래스: TextAnimatorUtil(내부에서 호출하는 정적 헬퍼), (외부) TMP_Text, UnityEvent

## 개요
TextAnimatorUtil을 **인스펙터에서 붙여 쓰는 컴포넌트 래퍼**. TMP 텍스트가 있는 오브젝트에 부착하면
Start 시점에 설정된 내용을 자동 재생하거나, 코드/UnityEvent에서 `Play()`를 호출해 재생할 수 있다.

- 경로: Assets/Scripts/Glory/TextAnimation/TextAnimationPlayer.cs (guid: f35d9039a8794c02afad522a498e8325)

## 직렬화 필드

| 필드 | 타입 | 설명 |
|---|---|---|
| `m_TargetText` | TMP_Text | 대상 텍스트. 비어 있으면 Awake에서 같은 오브젝트의 TMP_Text 자동 탐색 |
| `m_PlayMode` | eTextPlayMode | SetText(0)=즉시 표시 / Typewriter(1)=타자기 |
| `m_Content` | string(TextArea) | 재생할 내용(효과 태그 포함 가능) |
| `m_isPlayOnStart` | bool | true면 Start에서 자동 재생 (기본 true) |
| `m_TypewriterSpeed` | float | 타자기 배속 (기본 1) |
| `OnComplete` | UnityEvent | 재생 완료 시 호출 (SetText 모드는 즉시 호출) |

## 공개 API
- `Play()` — 인스펙터의 m_Content 재생
- `Play(string _content)` — 지정 내용 재생
- `Skip()` — 타자기 스킵
- `Hide()` — 글자 순차 퇴장

## 주의
- Play를 완료 전에 재호출하면 OnComplete가 중복 호출될 수 있음 (TextAnimatorUtil.md 주의사항과 동일).
- 전역 enum `eTextPlayMode`가 이 파일 상단에 선언되어 있음 (eTweenStepType과 동일 관례).

## 사용처
- TitleScene: `Canvas/Test_TextAnimation` — 테스트용 데모 오브젝트
  - GameObject fileID 900100030 / RectTransform 900100031 / TMP 900100033(DungGeunMo 폰트) / 본 컴포넌트 900100034
  - Typewriter 모드, PlayOnStart, 내용: `<wave>텍스트 애니메이터</wave> 컴포넌트 테스트!<waitfor=0.5> {fade}타자기 <rainb>성공!</rainb>{/fade}`
  - 확인 후 삭제해도 되는 임시 오브젝트

---

## 2026-07-20-1

### 개요
사용자 요청: "TextAnimatorUtil을 Component 형식으로 붙여 쓸 수 있게 + TitleScene에 테스트 GameObject 부착".

### 파일
- Assets/Scripts/Glory/TextAnimation/TextAnimationPlayer.cs (신규)
- Assets/Scripts/Glory/TextAnimation/TextAnimationPlayer.cs.meta (신규, guid f35d9039a8794c02afad522a498e8325)
- Assets/Scenes/TitleScene.unity — 메인 Canvas(RectTransform 655750138) 자식으로 Test_TextAnimation 추가 (fileID 900100030~900100034 대역)

### 수정
- 신규 작성이므로 전/후 비교 없음. TweenUtil→TweenEffectPlayer와 같은 "정적 헬퍼→인스펙터 컴포넌트" 패턴을 따름.

### 미검증
에디터 미실행 상태 작성. 컴파일, 씬 파싱, DungGeunMo 폰트의 데모 문구 글리프 포함 여부, 타자기 실동작 확인 필요.
TextAnimatorUtil의 런타임 AddComponent 경로(2026-07-20-0 미검증 항목)도 이 테스트로 함께 검증 가능.
