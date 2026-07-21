# UIPopup

## 연관 클래스
- UIBase (부모)
- UIManager — `RegisterPopup`/`UnregisterPopup`/`CloseAllPopups`/뒤로가기 스택 소유자
- UIMetaTree, UIRunOver, UIPause, UICardDraft (전부 파생 클래스로 전환)
- SceneManager — `NextScene()`에서 `CloseAllPopups()` 호출

## 개요
"오버레이로 열리고, 닫힐 수 있고, 뒤로가기의 대상이 되는" 화면들의 공용 베이스(Glory 라이브러리, 프로젝트 비의존). `UIBase`와의 차이는 딱 세 가지: (1) `Show()`/`Close()`에서 `UIManager`의 팝업 스택에 자동 등록/해제 (2) `OnPressBackBtn()` 오버라이드 지점 제공 (3) 그 결과로 `UIManager.CloseAllPopups()`(씬 전환 시)가 이 타입만 일괄 정리 대상으로 잡을 수 있음.

## 현재 상태
- 경로: Assets/Scripts/Glory/UI/UIPopup.cs
```csharp
public abstract class UIPopup : UIBase
{
    public override void Show()
    {
        base.Show();
        UIManager.instance.RegisterPopup(this);
    }

    public override void Close()
    {
        UIManager.instance.UnregisterPopup(this);
        base.Close();
    }

    public virtual void OnPressBackBtn()
    {
        Close();
    }
}
```
- 기본 `OnPressBackBtn()`은 그냥 `Close()` — 뒤로가기로 안 닫혀야 하는 팝업(예: 필수 확인창)은 이 메서드를 오버라이드해서 원하는 동작(무시/커스텀 처리)으로 바꾸면 됨. 아직 그런 요구가 있는 실제 팝업은 없어서 훅만 만들어둠(2026-07-22).
- `UIMetaTree`/`UIRunOver`는 이미 자체 `Show()`를 오버라이드하고 있었는데 원래부터 `base.Show()`를 호출하고 있었기 때문에, `UIBase → UIPopup`으로 상속만 바꿔도 별도 코드 수정 없이 자동으로 스택 등록됨.

## 설계 근거
- **왜 `MonoSingleton<T>`이 아니라 `UIManager`가 스택을 들고 있나**: 팝업 인스턴스 자체는 `UIManager`가 이미 캐싱/생성을 전담하고 있어서(`Get<T>()`), "현재 열려있는 팝업 목록"도 자연스럽게 같은 소유자(UIManager)가 드는 게 중복 상태를 만들지 않음.
- **뒤로가기 감지는 새 Input System 기준**: 프로젝트가 `ProjectSettings.activeInputHandler: 1`(새 Input System 전용, 레거시 `UnityEngine.Input` 사용 불가)이라 `Keyboard.current.escapeKey.wasPressedThisFrame`으로 감지(`UIManager.Update()`). 안드로이드 하드웨어 뒤로가기는 플랫폼 레벨에서 Escape로 매핑되어 들어오는 것이 Unity의 표준 동작(Input System/레거시 공통).
- **씬 전환 시엔 무조건 전부 닫음**: `OnPressBackBtn()`을 오버라이드해 뒤로가기를 무시하는 팝업이 있어도, `SceneManager.NextScene()`이 호출하는 `UIManager.CloseAllPopups()`는 예외 없이 전부 `Close()`한다 — 씬 자체가 사라지는 상황에서 "뒤로가기 저항"은 의미가 없기 때문. 두 정리 경로(뒤로가기 vs 씬 전환)를 의도적으로 분리.

## 작업 내역

### 2026-07-22-0

#### 개요
사용자 요청 3건을 한 번에 반영:
1. "UIMetaTree, UIToastMessage, UIRunOver등 (Popup) 씬이 이동하면 정리대상이야, UIPopup을 따로 상속받아서 사용하게 하는건 어떨까?"
2. "안드로이드나 IOS 같은 플랫폼에서는 뒤로가기가 있는데... OnPressBackBtn()같은 함수를 오버라이딩 할 수 있게"
3. "UIRunOver에서 MetaTree를 들어갔는데. 왜 MetaTree가 UIRunOver보다 아래에 있게 되는거야? PopUp은 마지막에 열린게 가장 상단에 있어야해"

#### 신규 파일
- Assets/Scripts/Glory/UI/UIPopup.cs (Unity MCP `manage_script`로 생성, guid 자동 발급)

#### 연관 수정 (다른 파일)
- [[UIManager]] — 팝업 스택(`m_PopupStack`)/`RegisterPopup`/`UnregisterPopup`/`CloseAllPopups`/뒤로가기 감지 추가, **`SetAsFirstSibling()` → `SetAsLastSibling()` 버그 수정**(3번 요청의 직접 원인 — 이미 2026-07-15-3에 "확인 필요"로만 기록돼있던 의심 지점이 이번에 실제로 재현/확정됨)
- [[UIMetaTree]], [[UIRunOver]], [[UIPause]], [[UICardDraft]] — `UIBase` → `UIPopup` 상속으로 전환
- Assets/Resources/Table/UITable.csv — `UIMetaTree`/`UIRunOver`의 `UIType`을 `Normal`→`Popup`으로 변경(PopupCanvas로 이동, 이미 Popup이던 UICardDraft/UIPause와 같은 레이어·같은 스택 소속이 되도록)
- [[SceneManager]] — `NextScene()` 시작 부분에 `UIManager.instance.CloseAllPopups();` 추가

#### 검증 (Play Mode 실측, TitleScene→Btn_Play→InGameScene 실제 흐름)
- `TowerHealth.TakeDamage(1000)`로 `UIRunOver` 오픈 → `transform.parent.name == "PopupCanvas"` 확인(UIType 변경 반영), `siblingIndex=0`.
- `Btn_MetaTree` 클릭(실제 `Button.onClick.Invoke()`) → `UIMetaTree`가 열리고 `siblingIndex=1`(`UIRunOver`의 `0`보다 큼 = 화면상 위) — **버그 재현 후 수정 확인**.
- `UIManager`의 `m_PopupStack`을 리플렉션으로 조회 → `[UIRunOver, UIMetaTree]`(등록 순서 = 아래→위) 정상.
- `OnPressBackButton()`(private, 리플렉션으로 직접 호출 — 실제 `Keyboard` 디바이스 이벤트를 코드로 주입하는 건 새 Input System 특성상 번거로워 로직만 직접 검증)을 1회 호출 → `UIMetaTree`만 닫힘(`activeInHierarchy=false`), `UIRunOver`는 그대로 열린 채 유지, 스택은 `[UIRunOver]`로 축소 — 스택 최상단만 뒤로가기 대상이 되는 것 확인.
- `UIManager.instance.CloseAllPopups()` 호출 → 남은 `UIRunOver`도 닫히고(`active=false`) 스택이 `Count=0`으로 완전히 비워짐.
- 컴파일 에러 0건, 콘솔 에러 0건.
- **미검증**: 실제 안드로이드/iOS 기기(또는 에디터의 시뮬레이션된 하드웨어 뒤로가기 이벤트)로 `Keyboard.current.escapeKey` 감지 자체는 코드 리뷰로만 확인 — 실기기 테스트 필요.
