# Actor

## 연관 클래스
- FactoryObject (Glory) — 베이스 클래스
- ActorMonster — 파생 클래스

## 현재 상태
- 경로: Assets/Scripts/InGame/Actor/Actor.cs
- `FactoryObject`를 상속한 빈 클래스 — 인게임 액터 공통 베이스 자리만 잡아둔 상태.
- 주의: FactoryObject 계열이므로 초기화는 `Awake()`/`Start()` 대신 베이스의 훅(`Open()`/`Close()` 등)을 오버라이드할 것.

## 작업 내역

### 2026-07-12-0
- 개요: 프로젝트 전체 스캔으로 기본 정보 문서 초기 생성 (코드 수정 없음)
