# UIErrorWindow (Assets/Resources/Prefabs/UI/UIErrorWindow.prefab)

연관 스크립트: [[UIErrorWindow]] (루트 부착)
중첩 프리팹: 없음

## 개요
에러 발생 시 화면 제일 앞에 뜨는 디버그 팝업. 루트 Canvas 없음(풀스트레치) — `UIManager`가 PopupCanvas 밑에 생성. 로드 경로: `Prefabs/UI/UIErrorWindow` / 프리팹 guid: `35c5280e130b4aa99a362d8dfd113e56`.

fileID 대역: `9011000000000001XXX`(뒤 4자리 표기, 1001~1034 사용, [[UICheatWindow]]의 9010 대역과 겹치지 않게 9011 대역 사용). Unity MCP 미연결 상태(2026-07-25 확인, `ListMcpResourcesTool` 결과 mcpForUnity 인스턴스 0개)라 YAML을 손으로 직접 작성 — [[UICheatWindow]] 프리팹의 검증된 블록(Image/Button/TMP/ScrollRect/Viewport/Content)을 그대로 복사해 guid 오류 위험을 없앴다. 작성 후 grep으로 fileID 34개 중복 0건, dangling reference 0건 확인 완료.

## 계층 구조 (뒤 4자리 표기, 정확한 값은 위 대역 기준)
```
UIErrorWindow (...1001)                    — RectTransform 풀스트레치, UIErrorWindow 컴포넌트(...1003)
├─ Image_BG (...1004)                      — 어두운 적색 반투명 배경(에러 팝업임을 시각적으로 구분), RaycastTarget ON
├─ Text_Title (...1008)                    — "ERROR", 흰색 볼드
├─ Btn_Close (...1012)                     — 우상단 72×72 "X" 버튼(적갈색 배경) → Close()
└─ ScrollView (...1021, ScrollRect)
   └─ Viewport (...1024, RectMask2D)
      └─ Content (...1027, VerticalLayoutGroup+ContentSizeFitter, 패딩 24/24/16/24, spacing12)
         └─ Text_EntryTemplate (...1031, 비활성, m_EntryTemplate) — 코드가 에러 발생마다 Instantiate
```

## 설계 메모
- Content의 `VerticalLayoutGroup`은 `m_ChildControlHeight: 1`(UICheatWindow의 고정 높이 Row와 차이점) — 에러 메시지+스택트레이스는 줄 수가 가변이라, VLG가 각 자식 TMP의 preferredHeight를 직접 쿼리해 항목별 높이를 자동 조절하게 했다. 별도 `ContentSizeFitter`를 Text_EntryTemplate에 추가하지 않음(VLG의 ChildControlHeight=1만으로 충분 — TMP가 ILayoutElement를 구현해 wrap된 텍스트의 preferredHeight를 보고함).
- 모든 텍스트 `m_fontColor: {r:1,g:1,b:1,a:1}`(흰색) — 사용자 명시 요구사항.
- `Btn_Close`의 `m_OnClick.m_PersistentCalls.m_Calls`는 빈 배열 — 인터랙션은 `UIErrorWindow.cs`의 `Show()`에서 코드로 `AddListener` 연결(UICheatWindow와 동일 컨벤션).
- Image_BG 색상은 다른 팝업(어두운 남색 계열 `{0.039, 0.039, 0.058}`)과 다르게 어두운 적색 계열 `{0.102, 0.020, 0.020}`로 설정 — "에러 상태"임을 배경색만으로도 구분 가능하게 함(사용자가 명시 요청한 사항은 아니고, 화면이 까매져 잘 안 보인다는 문제 상황에 맞춰 시각적으로 확실히 눈에 띄게 하려는 판단 — 필요시 사용자 피드백으로 조정).

## 작업 내역

### 2026-07-25-0 — 신규 생성
[[UICheatWindow]] 프리팹 참고해 신규 제작. 상세 경위는 [[UIErrorWindow]](class.md), [[ErrorLogManager]](class.md) 참고.

### 미검증
Unity MCP 미연결, 컴파일 진단(`mcp__ide__getDiagnostics`)만 확인하고 Play Mode 실측은 전혀 안 됨. 특히:
- 에러 발생 시 팝업이 실제로 최상단에 뜨는지(다른 팝업이 열려있는 상태에서도)
- ScrollView가 긴 스택트레이스 여러 개를 넣었을 때 항목별 높이가 올바르게 계산되는지(`ChildControlHeight: 1` 조합이 실제로 의도대로 동작하는지)
- 자동 스크롤(`verticalNormalizedPosition = 0f`)이 새 항목 추가 직후 레이아웃이 갱신된 상태에서 정확히 맨 아래로 가는지
에디터에서 직접 확인 필요.
