# UIErrorWindow

연관 클래스: [[ErrorLogManager]], UIPopup, UIManager, GameManager

## 개요
Application.logMessageReceived로 잡힌 에러/예외를 화면 제일 앞(PopupCanvas 최상단)에 스크롤 가능한 로그로 보여주는 디버그 팝업. 화면이 까매지는 등 콘솔을 볼 수 없는 상황에서 어디서 에러가 났는지 확인하기 위한 용도(사용자 요청, 2026-07-25).

## 현재 상태
- 경로: Assets/Scripts/UI/UIErrorWindow.cs
- `public class UIErrorWindow : UIPopup` — `UIManager.instance.Get<UIErrorWindow>()`로 오픈(UITable에 Popup 타입으로 등록됨, Id=9).
- 필드: `m_CloseButton`(Button) / `m_ScrollRect`(ScrollRect) / `m_EntryContainer`(Transform, Content) / `m_EntryTemplate`(TextMeshProUGUI, 비활성 템플릿).
- `Show()`: `base.Show()` 후 `m_CloseButton`에 `Close` 리스너 연결(재오픈마다 RemoveAllListeners 후 재등록, UICheatWindow와 동일 패턴).
- `AddErrorEntry(string _message, string _stackTrace)`: `ResUtil.Create(m_EntryTemplate, m_EntryContainer)`로 템플릿 복제 → `[HH:mm:ss] 메시지\n스택트레이스` 형식으로 텍스트 세팅 → `Canvas.ForceUpdateCanvases()` 후 `m_ScrollRect.verticalNormalizedPosition = 0f`로 최신 항목까지 자동 스크롤.
- 텍스트는 전부 흰색(`m_fontColor: {r:1,g:1,b:1,a:1}`) — 사용자 요청("하얀색 글씨로 나오게 해야해", 화면이 까매서 안 보이는 문제 때문).
- Show()는 새 에러가 들어올 때마다(=매번 `UIManager.Get<T>()` 호출) 실행되며, `Get<T>()` 내부의 `SetAsLastSibling()`이 항상 최상단으로 끌어올려 다른 팝업(치트 창 등) 위에도 항상 표시된다.
- `ErrorLogManager`가 호출 주체 — 이 클래스 자체는 로그 구독을 하지 않는다(팝업은 표시만 담당, 캐치는 별도 매니저 책임 분리).

## 작업 내역

### 2026-07-25-0 — 신규 생성
사용자 요청: "Error 날 때, 팝업창 제일 앞에 Error 메시지와 어디서 났는지 구체적으로 나오는 스크롤뷰로 이루어진 만들어줘. 하얀색 글씨로 나오게 해야해. 화면이 다 까매서". [[UICheatWindow]]의 스크롤뷰 구조(ScrollRect+Viewport+RectMask2D+Content(VerticalLayoutGroup+ContentSizeFitter))를 그대로 재사용해 신규 제작.

Unity MCP 미연결 상태(`ListMcpResourcesTool` 확인, mcpForUnity 인스턴스 0개)라 프리팹은 YAML 직접 편집으로 생성. Button/Image/TMP/ScrollRect/RectMask2D/VerticalLayoutGroup/ContentSizeFitter guid는 전부 기존 프리팹(UICheatWindow.prefab, UIMetaTree.prefab)에서 grep으로 직접 대조해 확인 후 사용(추측 금지 원칙).

Content의 `VerticalLayoutGroup`은 `m_ChildControlHeight: 1`(UICheatWindow의 고정 높이 Row와 다르게, 에러 메시지+스택트레이스는 줄 수가 가변이라 TMP의 preferredHeight를 VLG가 직접 쿼리하도록 설정 — 이러면 Text_EntryTemplate에 별도 ContentSizeFitter 없이도 항목별로 자동 높이 조절됨).

`mcp__ide__getDiagnostics`로 컴파일 에러 0건 확인. **Play Mode 실측은 미검증**(Unity MCP 미연결) — 다음에 에디터에서 실제로 에러를 발생시켜 팝업이 뜨는지, 스크롤이 자동으로 맨 아래로 내려가는지, 여러 팝업이 열려있을 때 항상 제일 위로 오는지 확인 필요.
