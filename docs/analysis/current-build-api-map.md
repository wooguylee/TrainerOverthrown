# Overthrown 현재 빌드 API 근거

## 대상 빌드

- Unity `6000.1.10f1 (3c681a6c22ff)`
- IL2CPP metadata `31.1`
- `Overthrown.exe` SHA-256:
  `41A3938AEC61589E85C14FC16394D558B84A568B218799C7981A2936B68D2B1D`
- BepInEx `6.0.0-be.785+6abdba4`
- 생성 interop: `W:\Games\Overthrown\BepInEx\interop\GameAssembly.dll`

## 안전 판정

Helper는 pipe와 Harmony patch를 만들기 전에 아래 대상 빌드의 EXE, GameAssembly,
metadata 세 SHA-256을 게임 폴더에서 다시 계산한다. 설치 후 게임이 업데이트되었거나
파일을 읽을 수 없으면 Helper 전체를 fail-closed 상태로 두고 trainer pipe를 열지 않는다.

| 근거 | 멤버 | 사용 방식 |
| --- | --- | --- |
| 오프라인 설정 | `BNetworkManager.OfflineMode` | 반드시 `true`여야 함 |
| 호스트 권한 | `Mirror.NetworkServer.activeHost`, `Mirror.NetworkClient.active` | 둘 다 `true`여야 함 |
| 접속 수 | `Mirror.NetworkServer.connections.Count` | 정확히 1이어야 함 |
| 로컬 플레이어 | `Mirror.NetworkBehaviour.isLocalPlayer` | `PlayerHealth` 후보 중 로컬 인스턴스 선택 |

하나라도 확인되지 않으면 `Uncertain`, 접속 수가 2 이상이면
`RemoteParticipant`로 판단한다. 두 상태에서는 모든 변경 명령을 거부하고 기존 변경을
원복한다.

## 노출 기능

| capability | 근거 멤버 | 동작 | 원복 |
| --- | --- | --- | --- |
| `player.godMode` | `PlayerHealth.asDamageable`, `Damageable.isInvulnerable`, `Damageable.currentHealth`, `Damageable.effectiveMaxHealth` | 로컬 플레이어만 무적 처리하고 체력을 최대치로 유지 | 무적 플래그 해제 |
| `world.timeScale` | `GameTime.gameTimeScale` | `0.25`~`4.0` 범위 배속 적용 | 최초 값 복원 |

## 미노출 후보

- `PlayerMovement.speedFactor`는 존재하지만 게임 상태 갱신이 덮어쓰는 필드라 아직 노출하지 않는다.
- `Inventory`, `PlayerInventory`, `GlobalResourceStorage`는 서버 권한·동기화 경로가 복잡해
  직접 필드 변경을 하지 않는다.
- 기력은 현재 생성 interop에서 독립적이고 안전한 로컬 쓰기 경로를 확정하지 못했다.
- 건설·연구·왕국 자원은 네트워크 명령과 저장 데이터에 연결되므로 첫 버전에서 제외한다.

## 런타임 검증

- BepInEx 첫 부팅에서 interop 생성 및 `MainMenu:Start()` 도달.
- 현재 생성 catalog 기준 runtime 번역 사전: `translations=1394`.
- 안정 키 1,586개 중 사용자 노출 번역 가능 항목 1,573개를 검수했다. 대기 13개는
  EULA 법적 원문 1개, `[INTENTIONALLY LEFT BLANK]` 5개, 구분자 `-` 7개뿐이다.
- 동적 template matching의 smart plural 토큰은 실제 영어 단위 `hour(s)`, `day(s)`,
  `second(s)`에만 매칭한다. 일반 문구와 개수 조합을 시간 단위로 오인하지 않는다.
- 동적 원형 메뉴의 제목·부제·설명은 `RadialMenuDynamicOption`의 문자열 필드에 저장되고
  `RadialMenuDynamicWheel`의 `title`, `subtitle` TMP에 표시된다. 이 복사는 IL2CPP 네이티브
  내부에서 일어나 managed Harmony 진입점 패치가 호출되지 않으므로, `RuntimeHost`가
  0.2초 간격으로 `RadialMenu.currentActive`의 열린 메뉴만 확인해 옵션 필드와 현재 표시
  필드를 검수 catalog로 보정한다. 씬/전역 TMP 탐색이나 숫자 서식 경로는 건드리지 않는다.
- 월드 로딩 단계명과 진행률은 `LoadingScreen.SetLoadingMessage(string)` 및
  `SetProgress(float)` 경로에서 결합된다. `UI/LOADING_*` 단계명은 메시지 입구에서 완전
  일치 번역하고, 이미 결합된 `0%`~`100%` 접미사는 숫자를 그대로 보존한다.
- 메인 메뉴의 미확인 세션에서 무적·시간 배속 명령이 `OFFLINE_NOT_PROVEN`으로 차단됨.
- 실제 로컬 월드에서의 허용 변경은 사용자가 플레이 중 확인할 수 있으며, 앱 종료·pipe
  단절·멀티플레이 전환 시 항상 reset한다.
