# WayPoint

## 연관 클래스
- MonoSingleton (Glory)
- MonsterManager — `WayPoint.instance.GetRandomWayPoint()`로 스폰 위치 결정

## 현재 상태
- 경로: Assets/Scripts/InGame/WayPoint.cs
- `MonoSingleton<WayPoint>` 상속.
- 원형 스폰 링 역할: `GetRandomWayPoint()`가 자신 위치 기준 반지름 `Radius` 원주 위의 랜덤 지점(Vector2)을 반환.
- `isAutoRadiusFromCamera == true`(기본값)면 `Start()`에서 메인 카메라(직교) 화면 대각선 반경 + 1로 Radius 자동 계산 — 화면 바깥에서 스폰되도록.
- `OnDrawGizmos()`로 씬 뷰에 스폰 링 시각화 (세그먼트 수/색상 SerializeField).

## 주요 멤버
| 멤버 | 설명 |
|------|------|
| `Radius` (public float) | 스폰 링 반지름, 기본 12 |
| `isAutoRadiusFromCamera` (public bool) | 카메라 기반 자동 반지름 계산 여부 |
| `GetRandomWayPoint()` | 링 위 랜덤 위치 반환 |

## 작업 내역

### 2026-07-12-0
- 개요: 프로젝트 전체 스캔으로 기본 정보 문서 초기 생성 (코드 수정 없음)
