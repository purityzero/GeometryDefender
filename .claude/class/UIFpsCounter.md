# UIFpsCounter

연관 클래스: UpdatableBehaviour, BaseScene, [[UIInGameHUD]](같은 프리팹의 Text_Fps에 부착)

## 개요
현재 프레임률을 표시하는 드롭인(자기 완결형) 런타임 컴포넌트. 아무 씬의 아무 Canvas 밑에 TextMeshProUGUI 하나와 함께 붙이면 바로 동작하도록 설계됨(재사용/이식 목적, "쉽게 붙였다 뗐다" 요구사항).

## 현재 상태
- 경로: Assets/Scripts/UI/UIFpsCounter.cs
- `public class UIFpsCounter : UpdatableBehaviour` — `OnEnable`/`OnDisable`에서 `BaseScene.Current.Register/Unregister` 자동 처리(베이스 그대로 사용, 오버라이드 없음).
- 필드: `[SerializeField] private TextMeshProUGUI m_FpsText;`
- `UpdateLogic()`: `Time.unscaledDeltaTime`을 0.5초(`REFRESH_INTERVAL`)간 누적한 프레임 수로 평균 FPS 계산 후 `"FPS: {value}"` 형식으로 표시. `unscaledDeltaTime`을 쓰는 이유: [[UICheatWindow]]에서 `Time.timeScale`을 바꿔도(배속 테스트) 실제 기기 성능을 보여줘야 하므로.

## 작업 내역

### 2026-07-23-4
사용자 요청("InGameScene에 FPS 표시 UI") — 신규 생성. [[UIInGameHUD]] 프리팹의 Text_Fps 오브젝트에 부착, `m_FpsText`는 같은 GameObject의 TMP를 자기참조.

미검증: Unity MCP 미연결, 컴파일/Play 확인 안 됨.
