# GlowAmountTweenEffect

연관 클래스: TweenEffectBase(베이스), TweenUtil(`Float(Material, string, float, float)` 재사용), ColorTweenEffect/FadeTweenEffect(동일 패턴 참고), TweenEffectPlayer

## 개요
`TweenEffectBase` 파생 컴포넌트. 같은 오브젝트의 `Image`→`SpriteRenderer` 순으로 대상을 탐색해 `material`의 `_GlowAmount`를 트윈한다. `ColorTweenEffect`/`FadeTweenEffect`와 완전히 동일한 설계(대상 자동 탐색 + `TweenUtil` 위임)를 따르되, 값이 `Color`나 `Alpha`가 아니라 커스텀 글로우 셰이더의 `_GlowAmount` 하나뿐이라 프로퍼티명은 상수(`GLOW_AMOUNT_PROPERTY = "_GlowAmount"`)로 고정했다(별도 SerializeField로 노출하지 않음 — 이 클래스의 존재 목적 자체가 `_GlowAmount` 전용이라 설정 가능하게 만들 이유가 없음).

## 경로
Assets/Scripts/Glory/Tween/GlowAmountTweenEffect.cs

## 왜 만들었나
타이틀 화면 헥사곤/Play 버튼의 "은은하게 숨쉬는 glow" 연출을 알파 페이드가 아니라 `_GlowAmount`(밝기)로 구현하기 위해 신설(2026-07-22). 알파를 페이드하면 오브젝트 자체가 투명해져 사라지는 것처럼 보이지만, `_GlowAmount`만 오르내리면 오브젝트는 항상 불투명하게 유지되면서 Bloom으로 인한 halo 밝기만 변한다.

## ⚠️ 공유 머테리얼 사용 시 필수 주의사항 (2026-07-22 실제 사고)
**이 컴포넌트를 프로젝트에 이미 존재하는 공유 머테리얼 에셋(`GlowMat_Tower.mat` 등)에 직접 붙이면 안 된다.** `Image.material`/`SpriteRenderer.material`은 **Play Mode에서 자동으로 인스턴스를 복제**하지만, **Edit Mode에서 Play를 시작하지 않고 값을 읽거나, 혹은 트윈이 실행되다가 Play Mode를 종료하는 시점의 값이 그대로 "공유 에셋 파일"에 영구적으로 저장되어버리는 사고가 실제로 발생했다** — `GlowMat_Tower.mat`(InGameScene 타워와 공유)의 `_GlowAmount`가 트윈 도중 값(0.4~1 사이 임의값)에 멈춘 채로 디스크에 저장되어, 관련 없는 InGameScene 타워의 밝기까지 오염시켰다(다음 세션에서 `git diff`로 발견, `git checkout`으로 복구).
- **원칙**: `GlowAmountTweenEffect`를 붙이는 대상은 반드시 **그 오브젝트 전용으로 새로 만든 머테리얼 에셋**(`GlowMat_TitleHexagon.mat`처럼 `_TitleXXX` 등 용도가 명확한 이름)을 써야 한다. 다른 오브젝트(특히 다른 씬의 오브젝트)와 머테리얼 에셋을 공유하지 말 것.
- 새 전용 머테리얼을 만들 때 원본 셰이더의 `_MainTexture`(또는 `_MainTex`) 텍스처 슬롯이 `[PerRendererData]`가 아니라 정적 슬롯이면(`Shader Graphs/Glow`가 이 경우 — SpriteRenderer의 스프라이트를 자동으로 안 물어오고 머테리얼에 박제된 텍스처만 그림) 반드시 원하는 모양의 텍스처(예: `shape_hexagon.png`)를 머테리얼에 직접 할당해야 한다 — 안 하면 텍스처 슬롯이 비어(흰색 기본값) 알파가 전부 1이 되어 도형이 아니라 사각형 전체가 칠해진다.

## 사용 예 — 및 실제로 부적합했던 사례 (2026-07-22, TitleScene Hexagon)
최초엔 `Game/Hexagon`(코어, `GlowMat_TitleHexagon.mat`) 자체에 부착해 `_GlowAmount` 0.4~1로 pulsing시켰으나, **이 셰이더는 `BaseColor = _Color × _GlowAmount`라 코어의 채우기 색 자체가 흐려졌다 진해졌다 하는 것으로 보여 사용자에게 "색이 물빠진다"는 지적을 받고 되돌림**(2026-07-22-7). 코어처럼 "형태가 항상 고정돼 보여야 하는" 오브젝트에는 이 컴포넌트를 붙이면 안 된다 — 대신 코어보다 큰 별도의 halo 레이어(코어 가장자리 밖으로 삐져나오는 크기)를 만들어 그 레이어에 `FadeTweenEffect`(알파 페이드)를 붙이는 쪽이 맞았다. `GlowAmountTweenEffect`는 애초에 "형태가 안 보이거나 안 보여도 상관없는" 오브젝트(예: 이미 알파가 낮거나, 자체가 halo 전용인 레이어)에만 적합 — 현재 TitleScene에는 실사용처가 없고, 재사용 가능한 유틸리티로만 남아있다. 상세 경위는 [[TitleScene]] 2026-07-22-5/6/7 참고.
