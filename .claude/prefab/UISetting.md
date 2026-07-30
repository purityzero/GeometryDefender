# UISetting (Assets/Resources/Prefabs/UI/UISetting.prefab)

연관 스크립트: [[UISetting]](루트 부착), [[UIText]](정적 라벨 로컬라이즈), [[UIToggleButton]], [[ToggleButtonList]](언어/FPS), `UnityEngine.UI.Slider`(BGM/SFX, 2026-07-29), [[UIPopup]](부모)

## 개요
전역 설정 화면 프리팹(타이틀에서 진입). 루트 RectTransform이 화면 전체(720×1280)를 stretch anchor(0,0)-(1,1)로 채움. 모든 자식 행은 top-center 앵커(`anchorMin=anchorMax=(0.5,1)`, `pivot=(0.5,1)`) + `anchoredPosition.y`가 상단에서부터의 아래 방향 거리.

## 계층 구조 (라벨 → 컨트롤, y좌표는 상단 기준 음수) — 2026-07-29 기준
```
UISetting                              — 루트, UISetting 컴포넌트
├─ Image_BG                            — 배경, stretch
├─ Panel_Top / Btn_Back / Text_Back    — 뒤로가기
├─ Text_Title                          — "설정"
├─ Text_LanguageLabel (y-250)          — "언어"
├─ Panel_Language (y-290)              — ToggleButtonList(m_LanguageToggles), Item_Template(비활성 원본)
├─ Text_BgmLabel (y-370, 2026-07-29 리네임)         — "BGM" (구 Text_SoundLabel)
├─ Slider_Bgm (y-410, 2026-07-29 신규)              — Slider(m_BgmVolumeSlider), Background/Fill Area→Fill/Handle Slide Area→Handle
├─ Text_SfxLabel (y-450, 2026-07-29 신규)           — "SFX"
├─ Slider_Sfx (y-490, 2026-07-29 신규)              — Slider(m_SfxVolumeSlider), Slider_Bgm과 동일 구조
├─ Text_HapticLabel (y-530, 2026-07-29 이동: 구 y-450) — "진동"
├─ Item_Haptic (y-570, 2026-07-29 이동: 구 y-490)      — UIToggleButton(m_HapticToggle)
├─ Text_FpsLabel (y-610, 변경 없음)     — "FPS"
├─ Panel_Fps (y-650, 변경 없음)         — ToggleButtonList(m_FpsToggles), Item_Template(비활성 원본)
├─ Text_EnemyDamageTextLabel (y-690, 변경 없음)  — "적군 데미지 표시" (키: SettingsEnemyDamageTextLabel)
├─ Item_EnemyDamageText (y-730, 변경 없음)       — UIToggleButton(m_EnemyDamageTextToggle), Text_On/Text_Off(SettingsOn/SettingsOff 재사용)
├─ Text_AllyDamageTextLabel (y-770, 변경 없음)   — "아군 데미지 표시" (키: SettingsAllyDamageTextLabel)
└─ Item_AllyDamageText (y-810, 변경 없음)        — UIToggleButton(m_AllyDamageTextToggle)
```
`Item_Sound`(구 사운드 On/Off)와 `Text_LeftHandLabel`/`Item_LeftHand`(왼손 모드)는 2026-07-29에 삭제됨. Sound 1행이 Bgm+Sfx 2행으로 늘고 LeftHand 1행이 사라져 순증감 0 — 그래서 Haptic 한 행만 밀리고 Fps 이하는 전부 원래 좌표 그대로.

## 설계 메모
- 이진 On/Off 토글(진동/적군·아군 데미지 표시)은 전부 `UIToggleButton`(GoOn/GoOff 자식으로 상태 전환) 사용 — 기존 관례 그대로 재사용.
- 다지선다(언어/FPS)는 `ToggleButtonList` + `Item_Template` 방식 — 이진 토글과는 다른 컴포넌트.
- 연속값(BGM/SFX 음량)은 2026-07-29부터 표준 `UnityEngine.UI.Slider`(Background/Fill Area/Fill/Handle Slide Area/Handle 5-오브젝트 표준 계층) 사용 — 이 프로젝트 최초의 Slider라 복제 템플릿이 없었음, 상세 조립 절차는 [[UISetting]] 2026-07-29-0 참고.

---

## 2026-07-23-0

### 개요
사용자 요청("데미지 폰트도 넣어줘 ... Option으로 적군 아군 데미지 받은거 표시하는거 On/Off") — 신규 두 행 추가. 상세는 [[UISetting]] 2026-07-23-0 참고.

### 수정 (오브젝트 단위)
`Item_Sound`/`Text_SoundLabel`을 각각 duplicate → `Item_EnemyDamageText`/`Text_EnemyDamageTextLabel`, `Item_AllyDamageText`/`Text_AllyDamageTextLabel` 생성, `RectTransform.anchoredPosition` 재배치(위 계층 구조 참고). 라벨의 `UIText.m_Key`만 신규 키로 교체, `Item_*`의 Text_On/Text_Off는 그대로(SettingsOn/SettingsOff 재사용). `UISetting` 컴포넌트의 `m_EnemyDamageTextToggle`/`m_AllyDamageTextToggle` 필드를 각 `Item_*`의 `UIToggleButton` 컴포넌트에 연결.

### 검증
`save_prefab_stage` 후 YAML grep으로 필드 연결 실제 반영 확인. Play Mode 실측 — Settings 화면 스크린샷으로 신규 두 행 정상 렌더링, 토글 실제 클릭으로 상태 전환 확인. 콘솔 에러 0건.

---

## 2026-07-29-1 — 좌표 어긋남 버그 수정 (Text_SfxLabel/Slider_Sfx/Text_HapticLabel/Item_Haptic)

### 개요
사용자 요청("UISetting 제대로 프리팹 안고쳐놓을래?")으로 실제 프리팹 YAML을 재검사한 결과, 2026-07-29-0에서 이미 경고했던 "duplicate 후 좌표 어긋남" 버그가 남아있었음을 확인. `Text_SfxLabel`/`Slider_Sfx`/`Text_HapticLabel`/`Item_Haptic` 4개 오브젝트가 문서화된 좌표보다 정확히 -640px 더 내려가 있었음(y=-1090/-1130/-1170/-1210, 의도값은 -450/-490/-530/-570). `Item_AllyDamageText`(-810)와 `Text_SfxLabel` 사이 280px 빈 공백 + SFX/진동 행이 데미지 표시 행보다 한참 아래(화면 하단 근처)로 밀려 보이는 상태였음.

### 수정
4개 오브젝트의 `RectTransform.m_AnchoredPosition.y`를 각각 -450/-490/-530/-570으로 직접 복원(YAML 직접 편집). 다른 필드(anchor/pivot/부모 참조)는 전부 정상이라 좌표값만 수정.

### 검증
`grep`으로 수정 후 6개 행(BgmLabel~PanelFps)의 y좌표가 -370/-410/-450/-490/-530/-570/-610/-650 순으로 80px 간격 유지되는 것 확인. Play Mode 실측은 미완료 — 사용자 확인 필요.

---

## 2026-07-29-0 — 사운드 On/Off → BGM/SFX Slider 2개, 왼손 모드 삭제
상세 절차/설계 근거는 [[UISetting]] 2026-07-29-0 참고. 요약: `Item_Sound`/`Text_LeftHandLabel`/`Item_LeftHand` 삭제 → `Text_SoundLabel`을 `Text_BgmLabel`로 리네임 후 duplicate로 `Text_SfxLabel` 생성 → `Slider_Bgm`(표준 5-오브젝트 계층 신규 조립) 완성 후 duplicate로 `Slider_Sfx` 생성 → `Text_HapticLabel`/`Item_Haptic`만 이동. 이 프로젝트 최초의 `Slider`라 Unity MCP(`manage_gameobject`/`manage_components`)로 직접 조립(사용자가 이 작업 한정 MCP 사용 승인).
검증: `save_prefab_stage` 후 `get_hierarchy` 재조회로 좌표/구조 확인, `UISetting` 컴포넌트 필드 참조 재조회로 확인, 콘솔 에러 0건. Play Mode 조작(슬라이더 드래그) 미검증 — 사용자 지시로 직접 테스트 대기.
