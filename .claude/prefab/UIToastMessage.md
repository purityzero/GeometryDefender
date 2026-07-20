# UIToastMessage (Assets/Resources/Prefabs/UI/UIToastMessage.prefab)

연관 스크립트: [UIToastMessage](../class/UIToastMessage.md)(루트), [TweenEffectPlayer](../class/TweenEffectPlayer.md) + [FadeTweenEffect](../class/TweenEffectBase.md) ×2, [TextAnimationPlayer](../class/TextAnimationPlayer.md)(Text_Message, 타자기), [UIManager](../class/UIManager.md)(런타임 풀링/스태킹 소유자)
중첩 프리팹: 없음
기획 근거: 사용자 요청 — "Shard가 부족합니다" 같은 문구가 뜨는 공용 알람 토스트. 잠시 뜨면 자동으로 사라짐, 4~5개 오브젝트 풀 사용, 위로 점점 쌓이면서 사라지는 형식.

## 개요
`UIManager.instance.ShowToast(message)`로 뜨는 개별 토스트 아이템의 원본 프리팹. `MemoryPooling<UIToastMessage>`가 이 프리팹을 `Prefabs/UI/UIToastMessage` 경로로 로드해 최대 5개까지 미리 생성(Prewarm)해두고 재사용한다.
로드 경로: `Prefabs/UI/UIToastMessage`
프리팹 guid: 4de431cca41c4a00ad82cab81a55124e

## 계층 구조
```
UIToastMessage (fileID ...1000)                — RectTransform(중앙 앵커(0.5,0.5), pivot(0.5,0.5), sizeDelta 600×72)
                                                  + Image(...1003, 배경 #0F0F16, 페이드인 목표 0.9로 반투명 유지)
                                                  + UIToastMessage(...1004, m_MoveDuration=0.2)
                                                  + TweenEffectPlayer(...1005, m_isPlayOnEnable=false, m_Effects=[1006,1014,1007,1015])
                                                  + FadeTweenEffect 이미지-in(...1006, Append, Duration 0.2 Delay 0 TargetAlpha 0.9)
                                                  + FadeTweenEffect 이미지-out(...1007, Append, Duration 0.3 Delay 1.5 TargetAlpha 0)
                                                  + CanvasRenderer(fileID 6496391864058027193 — Unity가 Image 요구사항으로 자동 추가, 최초 작성 시 누락됐던 것)
└─ Text_Message (...1010, TMP comp 1013)        — TMP "Message" placeholder, 흰색(#EBEBF5), 20pt, 중앙정렬, 좌우 패딩 24
                                                  + FadeTweenEffect 텍스트-in(...1014, Join, Duration 0.2 TargetAlpha 1 — 이미지-in과 동시)
                                                  + FadeTweenEffect 텍스트-out(...1015, Join, Duration 0.3 TargetAlpha 0 — 이미지-out과 동시)
                                                  + TextAnimationPlayer(...1016, Target=TMP 1013, Typewriter 모드, PlayOnStart=false, 배속 2)
```
- fileID 대역: 9002000000000001000~1016 (+ Unity 자동 부여 CanvasRenderer 6496391864058027193)
- **CanvasGroup 없음** — 배경(Image)과 텍스트(TMP) 알파를 각각 FadeTweenEffect로 직접 애니메이션(2026-07-18-1, 사용자 지적으로 CanvasGroup 제거). `FadeTweenEffect.CreateTween()`이 자기 오브젝트에서 CanvasGroup→Image→SpriteRenderer→TMP 순서로 자동 탐색하므로, 루트의 FadeTweenEffect는 Image를, Text_Message의 FadeTweenEffect는 TMP를 자동으로 찾아 애니메이션한다.
- **대기시간(1.5초)은 이미지-out FadeTweenEffect(...1007)의 `m_Delay`로 표현** — TweenSequenceBuilder.Append가 지연을 시퀀스 갭으로 반영하는 동작을 이용. 텍스트-out(...1015)은 Join이라 자체 Delay 없이 이미지-out과 같은 시점에 시작.
- 이미지 배경은 완전 불투명(1.0)이 아니라 0.9까지만 페이드인 — 원래 CanvasGroup으로 구현했을 때의 반투명 배경 느낌을 유지하기 위한 값(TargetAlpha 1로 하면 배경이 완전 불투명해져 버림).

## 참조 GUID (실파일 대조 완료)
- UIToastMessage.cs: 235434680d79467687814e743855fcec
- TweenEffectPlayer.cs: 7c3ea041f2d16d8ea05c9fb37e416c24
- FadeTweenEffect.cs: 5a1c8e2fd0b94b6c8e3a7d915c2f4a02
- TextAnimationPlayer.cs: f35d9039a8794c02afad522a498e8325
- UnityEngine.UI.Image: fe87c0e1cc204ed48ad3b37840f39efc (UIMetaTree.prefab과 동일, 재확인)
- TMPro.TextMeshProUGUI: f4688fdb7df04437aeb418b961361dc5
- TMP 폰트: guid 7e00a561b2f97e04bbe6e3b6876e22e5 (에디터가 라이브 임포트 중 자동 수정 — 원래 UIMetaTree.prefab에서 재확인했던 LiberationSans SDF guid `8f586378b4e144a9851e7b34d9b748ee`와 다름. 이 프리팹은 이 값이 최신 기준)

## UIToastMessage(...1004) 직렬화 필드
- m_RectTransform → 루트 자신(...1001)
- m_Image → 루트의 Image(...1003)
- m_MessageText → Text_Message의 TMP(...1013)
- m_TweenPlayer → TweenEffectPlayer(...1005)
- m_TextPlayer → Text_Message의 TextAnimationPlayer(...1016)
- m_MoveDuration → 0.2 (스태킹 리포지션 이동 시간, UIManager의 `TOAST_SLOT_HEIGHT`와는 별개 — 위치 간격은 UIManager 소관, 이동 속도는 이 프리팹 소관)

## 설계 메모
- 표시용 Canvas/Root를 이 프리팹도, UIManager도 별도로 만들지 않는다(2026-07-18-1, 사용자 지적 "ToastCanvas 만들지 말고 ... Popup이랑 같이 써") — `UIManager.instance.ShowToast`가 기존 `GetCanvas(true)`(PopupCanvas)를 그대로 풀 부모로 사용해 이 프리팹을 바로 그 아래에 Instantiate한다. 화면상 위치는 전적으로 이 프리팹 자신의 RectTransform 앵커(중앙)가 결정.
- `m_isPlayOnEnable`은 반드시 `false` — 풀에서 SetActive(true)로 재활성화되는 시점과 실제 메시지 텍스트가 세팅되는 시점이 다르기 때문에(둘 다 `UIManager.ShowToast`가 순서대로 처리), 자동재생을 켜두면 빈 텍스트로 애니메이션이 시작해버린다.

---

## 2026-07-18-0

### 개요
신규 생성. 토스트 위젯 프리팹 — 배경 + 메시지 텍스트 + 토스트 표시/사라짐 애니메이션(TweenEffectPlayer 기반).

### 파일
- Assets/Resources/Prefabs/UI/UIToastMessage.prefab (+.meta)

### 미검증
에디터 미실행 상태 YAML 직접 작성. 컴파일, 스크립트 참조 연결(missing 아님), 실제 페이드/스태킹 애니메이션 확인 필요.

---

## 2026-07-18-1

### 개요
사용자 지적 3건 반영 — 상세 배경은 [UIToastMessage.md](../class/UIToastMessage.md), [UIManager.md](../class/UIManager.md) 참고.
1. 루트 RectTransform 앵커를 하단중앙 → 중앙으로.
2. CanvasGroup 컴포넌트 삭제, m_CanvasGroup 필드 참조 제거.
3. Image(...1006)/TMP(...1014,1015) 각각을 위한 FadeTweenEffect로 재구성.

### 파일
- Assets/Resources/Prefabs/UI/UIToastMessage.prefab

### 수정 (오브젝트 단위)

**UIToastMessage (루트, ...1000)**
- RectTransform(...1001): `m_AnchorMin/Max: {0.5, 0}` → `{0.5, 0.5}`, `m_Pivot: {0.5, 0}` → `{0.5, 0.5}`
- m_Component 목록에서 CanvasGroup(...1002) 삭제, CanvasRenderer(6496391864058027193) 추가(Unity가 Image 요구사항 충족을 위해 자동 삽입한 것으로 보임)
- UIToastMessage(...1004): `m_CanvasGroup` 필드 삭제, `m_Image: {fileID: ...1003}` 필드 추가
- FadeTweenEffect(...1006): `m_TargetAlpha` 1 → 0.9

**Text_Message (자식, ...1010)**
- m_Component 목록에 FadeTweenEffect 2개 추가: ...1014(StepType Join, Duration 0.2, TargetAlpha 1), ...1015(StepType Join, Duration 0.3, TargetAlpha 0)

**TweenEffectPlayer(...1005)**
- m_Effects: `[1006, 1007]` → `[1006, 1014, 1007, 1015]`

### 미검증
컴파일, 프리팹 컴포넌트 연결(missing 아님), 실제 중앙 배치/페이드 애니메이션 확인 필요.

---

## 2026-07-20-0

### 개요
사용자 요청 — 토스트 타자기 출력을 Util 직접 호출 대신 컴포넌트 부착 방식으로. Text_Message에 [TextAnimationPlayer](../class/TextAnimationPlayer.md) 추가, 루트 UIToastMessage의 m_TextPlayer 필드로 연결.

### 파일
- Assets/Resources/Prefabs/UI/UIToastMessage.prefab

### 수정 (오브젝트 단위)

**Text_Message (자식, ...1010)**
- m_Component 목록에 TextAnimationPlayer(...1016) 추가
- TextAnimationPlayer(...1016) 신규 블록: m_TargetText → TMP(...1013), m_PlayMode 1(Typewriter), m_Content 빈 값, m_isPlayOnStart 0(풀 재사용 시 자동재생 방지 — TweenEffectPlayer의 m_isPlayOnEnable=false와 같은 이유), m_TypewriterSpeed 2

**UIToastMessage (루트, ...1004)**
- `m_TextPlayer: {fileID: ...1016}` 필드 추가

### 미검증
컴파일, 프리팹 참조 연결, 타자기 2배속 실동작 확인 필요.
