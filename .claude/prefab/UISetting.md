# UISetting (Assets/Resources/Prefabs/UI/UISetting.prefab)

연관 스크립트: [[UISetting]](루트 부착), [[UIText]](정적 라벨 로컬라이즈), [[UIToggleButton]], [[ToggleButtonList]](언어/FPS), [[UIPopup]](부모)

## 개요
전역 설정 화면 프리팹(타이틀에서 진입). 루트 RectTransform이 화면 전체(720×1280)를 stretch anchor(0,0)-(1,1)로 채움. 모든 자식 행은 top-center 앵커(`anchorMin=anchorMax=(0.5,1)`, `pivot=(0.5,1)`) + `anchoredPosition.y`가 상단에서부터의 아래 방향 거리.

## 계층 구조 (라벨 → 컨트롤, y좌표는 상단 기준 음수)
```
UISetting                              — 루트, UISetting 컴포넌트
├─ Image_BG                            — 배경, stretch
├─ Panel_Top / Btn_Back / Text_Back    — 뒤로가기
├─ Text_Title                          — "설정"
├─ Text_LanguageLabel (y-250)          — "언어"
├─ Panel_Language (y-290)              — ToggleButtonList(m_LanguageToggles), Item_Template(비활성 원본)
├─ Text_SoundLabel (y-370)             — "사운드"
├─ Item_Sound (y-410)                  — UIToggleButton(m_SoundToggle), Text_On/Text_Off(키: SettingsOn/SettingsOff)
├─ Text_HapticLabel (y-450)            — "진동"
├─ Item_Haptic (y-490)                 — UIToggleButton(m_HapticToggle)
├─ Text_LeftHandLabel (y-530)          — "왼손 모드"
├─ Item_LeftHand (y-570)               — UIToggleButton(m_LeftHandToggle)
├─ Text_FpsLabel (y-610)               — "FPS"
├─ Panel_Fps (y-650)                   — ToggleButtonList(m_FpsToggles), Item_Template(비활성 원본)
├─ Text_EnemyDamageTextLabel (y-690, 2026-07-23 추가)  — "적군 데미지 표시" (키: SettingsEnemyDamageTextLabel)
├─ Item_EnemyDamageText (y-730, 2026-07-23 추가)       — UIToggleButton(m_EnemyDamageTextToggle), Text_On/Text_Off(SettingsOn/SettingsOff 재사용)
├─ Text_AllyDamageTextLabel (y-770, 2026-07-23 추가)   — "아군 데미지 표시" (키: SettingsAllyDamageTextLabel)
└─ Item_AllyDamageText (y-810, 2026-07-23 추가)        — UIToggleButton(m_AllyDamageTextToggle)
```
행 간격 패턴: 라벨→컨트롤 -40, 컨트롤→다음 라벨 -40(언어→사운드 사이만 예외적으로 -80). 신규 두 행은 이 패턴을 그대로 이어감(Fps 패널 y-650 다음 -690/-730/-770/-810).

## 설계 메모
- 이진 On/Off 토글(사운드/진동/왼손모드/적군·아군 데미지 표시)은 전부 `UIToggleButton`(GoOn/GoOff 자식으로 상태 전환) 사용 — 기존 관례 그대로 재사용. `Item_Sound`를 duplicate해서 만들면 `Text_On`/`Text_Off`의 `UIText.m_Key`(SettingsOn/SettingsOff)까지 그대로 따라와서 별도 설정 불필요.
- 다지선다(언어/FPS)는 `ToggleButtonList` + `Item_Template` 방식 — 이진 토글과는 다른 컴포넌트.

---

## 2026-07-23-0

### 개요
사용자 요청("데미지 폰트도 넣어줘 ... Option으로 적군 아군 데미지 받은거 표시하는거 On/Off") — 신규 두 행 추가. 상세는 [[UISetting]] 2026-07-23-0 참고.

### 수정 (오브젝트 단위)
`Item_Sound`/`Text_SoundLabel`을 각각 duplicate → `Item_EnemyDamageText`/`Text_EnemyDamageTextLabel`, `Item_AllyDamageText`/`Text_AllyDamageTextLabel` 생성, `RectTransform.anchoredPosition` 재배치(위 계층 구조 참고). 라벨의 `UIText.m_Key`만 신규 키로 교체, `Item_*`의 Text_On/Text_Off는 그대로(SettingsOn/SettingsOff 재사용). `UISetting` 컴포넌트의 `m_EnemyDamageTextToggle`/`m_AllyDamageTextToggle` 필드를 각 `Item_*`의 `UIToggleButton` 컴포넌트에 연결.

### 검증
`save_prefab_stage` 후 YAML grep으로 필드 연결 실제 반영 확인. Play Mode 실측 — Settings 화면 스크린샷으로 신규 두 행 정상 렌더링, 토글 실제 클릭으로 상태 전환 확인. 콘솔 에러 0건.
