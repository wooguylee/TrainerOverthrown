# Overthrown 현재 빌드 API 근거

## 대상 빌드

- Unity `6000.1.10f1 (3c681a6c22ff)`
- IL2CPP metadata `31.1`
- `Overthrown.exe` SHA-256:
  `41A3938AEC61589E85C14FC16394D558B84A568B218799C7981A2936B68D2B1D`
- BepInEx `6.0.0-be.785+6abdba4`
- 생성 interop: `W:\Games\Overthrown\BepInEx\interop\GameAssembly.dll`

## 실행 판정

Helper는 pipe와 Harmony patch를 만들기 전에 아래 대상 빌드의 EXE, GameAssembly,
metadata 세 SHA-256을 게임 폴더에서 다시 계산한다. 설치 후 게임이 업데이트되었거나
파일을 읽을 수 없으면 Helper 전체를 fail-closed 상태로 두고 trainer pipe를 열지 않는다.

| 근거 | 멤버 | 사용 방식 |
| --- | --- | --- |
| 오프라인 설정 | `BNetworkManager.OfflineMode` | 진단 응답에 원시 값 표시 |
| 호스트 권한 | `Mirror.NetworkServer.activeHost`, `Mirror.NetworkClient.active` | 진단 응답에 원시 값 표시 |
| 접속 수 | `Mirror.NetworkServer.connections.Count` | 세션 진단과 원격 참가자 표시 |
| 로컬 플레이어 | `Mirror.NetworkBehaviour.isLocalPlayer` | `PlayerHealth` 후보 중 로컬 인스턴스 선택 |

하나라도 확인되지 않으면 `Uncertain`, 접속 수가 2 이상이면
`RemoteParticipant`로 판단한다. 이 값은 테스트 모드에서 진단 정보이며 명령을 거부하거나
기존 변경을 자동 원복하는 조건으로 사용하지 않는다. 각 명령은 필요한 실제 게임 객체가
없는 경우에만 `FEATURE_UNAVAILABLE`을 반환한다.

## 노출 기능

| capability | 근거 멤버 | 동작 | 원복 |
| --- | --- | --- | --- |
| `player.godMode` | `PlayerHealth.asDamageable`, `Damageable.isInvulnerable`, `Damageable.currentHealth`, `Damageable.effectiveMaxHealth` | 로컬 플레이어만 무적 처리하고 체력을 최대치로 유지 | 무적 플래그 해제 |
| `player.health` | 같은 `Damageable` 체력 멤버 | 최대 체력으로 1회 회복 | 해당 없음 |
| `player.staminaFactor` | `DifficultyManager.Instance.NetworkplayerStaminaFactor` | 기력 소모 배율 `0`~`100` 적용 | 최초 값 복원 |
| `movement.speedMultiplier` | `PlayerMovement.UpdateSpeedFactor`, `speedFactor` | 원래 계산 직후 로컬 플레이어에 `0.1`~`20` 배율 적용 | `1x`로 복원 |
| `world.timeScale` | `GameTime.gameTimeScale` | `0`~`10` 범위 배속 적용 | 최초 값 복원 |
| `inventory.resource` | `PlayerInventory`, `Inventory.GetStoredAmount`, `DepositInternal`, `RemoveAmountFromStacks` | 선택 자원 조회·설정·증감 | 저장 상태이므로 자동 원복 안 함 |
| `kingdom.resource` | `GlobalResourceStorage.GetStoredAmount`, `Deposit`, `Withdraw` | 선택 왕국 자원 조회·설정·증감 | 저장 상태이므로 자동 원복 안 함 |
| `diagnostics.session` | 오프라인·호스트·접속 수와 객체 준비 상태 | 앱 진단 탭에 표시 | 해당 없음 |

인벤토리에 새 자원을 넣을 때는 `GlobalResourceStorage.GetResourceData(type).DefaultItem`으로
기본 아이템을 확인한다. 기본 아이템이 없는 자원은 조회·차감은 가능하지만 새 스택 생성은
`RESOURCE_ITEM_UNAVAILABLE`로 거부한다.

## 런타임 검증

- BepInEx 첫 부팅에서 interop 생성 및 `MainMenu:Start()` 도달.
- 현재 생성 catalog 기준 runtime 번역 사전: `translations=1394`.
- 안정 키 1,586개 중 사용자 노출 번역 가능 항목 1,573개를 검수했다. 대기 13개는
  EULA 법적 원문 1개, `[INTENTIONALLY LEFT BLANK]` 5개, 구분자 `-` 7개뿐이다.
- 동적 template matching의 smart plural 토큰은 실제 영어 단위 `hour(s)`, `day(s)`,
  `second(s)`에만 매칭한다. 일반 문구와 개수 조합을 시간 단위로 오인하지 않는다.
- 정적 현지화 문구는 `LocalizationSettings.StringDatabase.GetAllTables()`로 현재 locale의
  `Data`, `Entities`, `UI` 세 `StringTable`을 한 번 불러온 뒤, 검수된 안정 키 1,573개의
  `StringTableEntry.Value`만 key ID 기준으로 교체한다. 원형 메뉴 설명도
  `BuildMenu.Category.Item.localizedDescription`에서 같은 table entry를 읽으므로 별도의
  UI 탐색이나 반복 감시가 필요 없다. 초기 적용이 끝나면 상태 객체 참조를 해제한다.
- `TMP_Text.set_text` Harmony patch는 문자열 테이블을 사용하지 않는 하드코딩 문구와
  숫자가 결합된 동적 문구의 보조 경로로만 유지한다. 완전 일치 사전을 먼저 확인하고,
  이미 한글이 포함됐거나 영문자가 없는 값은 67개 동적 template 정규식 검사를 생략한다.
  문자열 테이블에서 이미 한글로 넘어온 값은 짧은 문자 검사로만 감지해 해당 TMP 폰트에
  한국어 fallback을 최초 1회 연결한다. 같은 폰트의 후속 기록에는 목록 조회·추가를 하지
  않으며 전역 UI 탐색이나 프레임 반복 작업은 없다.
- 월드 로딩 단계명과 진행률은 `LoadingScreen.SetLoadingMessage(string)` 및
  `SetProgress(float)` 경로에서 결합된다. `UI/LOADING_*` 단계명은 메시지 입구에서 완전
  일치 번역하고, 이미 결합된 `0%`~`100%` 접미사는 숫자를 그대로 보존한다.
- 테스트 모드 상태는 세션 판정과 분리되며 미확인 세션에서도 기능 버튼이 활성화된다.
- 앱 종료·pipe 단절 시 무적·기력·이동 속도·시간 배속을 reset한다. 자원 변경은 저장
  상태이므로 진단 및 자동 검증에서는 조회만 수행한다.
