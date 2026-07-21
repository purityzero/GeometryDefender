# 미완료 작업

## 2026-07-22

### 개요
"Glow 셰이더가 물빠진 색으로 보인다"는 지적을 끝까지 추적해 **진짜 근본 원인을 확정하고 수정 완료**. 이어서 기획서(`02_combat.html`) 재확인 중 발견한 **미구현 연출**이 하나 남아있음.

### 완료됨 (이어서 볼 필요 없음)
- **채도 저하 버그 — 완전 해결**: 원인은 셰이더가 아니라 `Assets/Settings/UniversalRP.asset`(URP Pipeline Asset)의 `colorGradingMode`가 `LowDynamicRange`였던 것. `HighDynamicRange`로 전환 + 영구 저장(`SetDirty`+`SaveAssets`) 완료. 상세 원인/검증 과정은 [.claude/class/TowerColorEffect.md](class/TowerColorEffect.md) 2026-07-22-2, [.claude/qa/client-issues.md](qa/client-issues.md) 2026-07-22-2 참고.
- **Title 씬 재검증 완료**: HDR 전환 후 부작용 없음, 스크린샷으로 확인(육각형 시안 + 배경 데코 사각형 레드 모두 쨍하게 정상).
- **몬스터 색상 — 검증 완료, 수정 불필요**: 5종(Normal 변종)이 전부 동일한 `#FF3355`인 것은 버그가 아니라 기획서(`03_enemy.html` 36줄 "모두 적색 베이스") 그대로. Elite(`#FF00AA`)/Boss(`#FFD600`)도 머테리얼 실측값이 기획서 hex와 정확히 일치 확인함. **추가 작업 불필요.**

### 남은 작업 — 다음 세션에서 이어갈 것
**기획서(`Assets/Design/02_combat.html` 77~106줄) "타워 HP 시각 표현" 스펙 중 일부 미구현:**
> "HP가 낮을수록 **글로우가 약해지고**, 30% 이하에서는 **적색 펄스가 점멸**한다"

현재 `TowerColorEffect.cs`(`Assets/Scripts/InGame/TowerColorEffect.cs`)는 3단계 **색상 전환**만 구현돼 있고(이건 기획서 hex와 정확히 일치, 정상), 아래 두 가지가 빠져있음:
1. HP 티어별 **글로우 강도** 변화 (기획서 CSS 참고치: High `drop-shadow(0 0 8px + 0 0 20px)` 강함 → Mid `4px+10px` 중간 → Low `6px+14px` 자체는 약간 있지만 "점멸"이 핵심)
2. Low 티어(30% 이하)에서 **적색 펄스 점멸** 애니메이션

**참고**: 글로우 강도는 `GlowMat_Tower.mat`의 `_GlowAmount` 프로�터티로 제어 가능(현재 `1`로 고정, 이전 세션에 흰색 클램프 버그 수정하며 고정한 값 — 이제 채도 버그가 해결됐으니 이 값을 HP 티어별로 트윈하는 방식 재검토 가능). `TweenUtil`에 이미 `Color(Material, Color, float)` 오버로드가 있으니, `_GlowAmount`용 `Float(Material, string, float, float)` 류 오버로드 추가가 필요할 수 있음(없으면 신규 추가).

**사용자 승인 이력**: 이 작업 순서는 사용자가 명시적으로 "채도 버그 먼저, 펄스는 나중"으로 확정함(2026-07-22 대화 중 AskUserQuestion 응답). 채도 버그가 이제 해결됐으니 다음 세션에서 바로 착수 가능.

### 관련 파일
- Assets/Scripts/InGame/TowerColorEffect.cs
- Assets/Resources/Mat/GlowMat_Tower.mat
- Assets/Scripts/Glory/Tween/TweenUtil.cs (신규 오버로드 필요 시)
- Assets/Design/02_combat.html (77~106줄, 스펙 원문)
- .claude/class/TowerColorEffect.md (작업 기록 이어서 추가할 것)
