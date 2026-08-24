# 프로젝트 작업 기록

## 2026-08-25 요청 및 결정

### 사용자 요청

1. `W:\Games\Overthrown` 게임의 한글 패치를 만들 수 있도록 지원한다.
2. `Z:\Work\WorkAI\VVooCheat` 프로젝트처럼 Overthrown용 트레이너를 만든다.
3. 트레이너는 싱글플레이/오프라인 전용으로 하고 멀티플레이에서는 변경 기능을 차단한다.
4. 한글화는 핵심 UI MVP부터 시작해 아이템·건물·연구·퀘스트·도움말까지 단계적으로 확장한다.
5. 트레이너 첫 버전은 플레이어, 인벤토리, 왕국 운영, 시간과 안전 기능을 후보 범위로 삼되 실제 게임 구조에서 안전하게 검증된 기능만 활성화한다.
6. 한 Windows 데스크톱 앱이 한글 패치와 Helper 설치·제거, 게임 연결, 트레이너 조작, 진단을 통합한다.
7. 번역은 영어 원문과 키를 보존하고 자동 초벌, 용어집, 서식 토큰 검사, 문맥 검수를 거치는 방식으로 관리한다.
8. 이후 추가 확인 없이 권장안으로 구현하며 실제 EXE와 간단 테스트 결과가 나올 때까지 진행한다.
9. 현재 대화를 포함한 작업 내용을 현재 프로젝트에 Markdown 등으로 저장한다.

### 초기 로컬 조사

- 게임 루트: `W:\Games\Overthrown`
- 회사/제품: `Brimstone / Overthrown`
- 엔진: Unity `6000.1.10f1 (3c681a6c22ff)`
- 스크립팅 백엔드: IL2CPP (`GameAssembly.dll`, `global-metadata.dat` 존재)
- Addressables: `2.6.0`
- Unity Localization, TextMeshPro, Addressables 사용 확인
- 영어, 중국어 간체/번체, 일본어, 프랑스어, 독일어, 이탈리아어,
  포르투갈어(브라질), 러시아어, 스페인어 문자열 번들이 있으나 한국어 번들은 없음
- Mirror, FizzySteam, PlayFab Multiplayer가 포함되어 있어 멀티플레이 안전 차단이 필요
- 게임 데이터의 난이도 설정에서 `playerVitalsFactor`, `playerStaminaFactor` 확인
- 기존 게임 폴더에는 BepInEx가 설치되어 있지 않음
- 사용자 데이터 경로: `%USERPROFILE%\AppData\LocalLow\Brimstone\Overthrown`

### 현재 빌드 지문

| 파일 | SHA-256 |
| --- | --- |
| `Overthrown.exe` | `41A3938AEC61589E85C14FC16394D558B84A568B218799C7981A2936B68D2B1D` |
| `GameAssembly.dll` | `28FFF76B50ED06FC0343EC218B9465AABBE927B40655D6E36F5A5DFEE7B15B1A` |
| `global-metadata.dat` | `D6B15EB0DAA94C16E818619872CF313544BAF81A46D68AFE23DA45334E56BA3B` |

### 기술 조사 결론

- 권장 경로는 BepInEx 6 IL2CPP Helper와 외부 WPF 앱이다.
- BepInEx 공식 문서상 IL2CPP Windows x64가 지원되지만 최신/bleeding-edge
  빌드가 필요하다. Unity 6+ 메타데이터 지원 변경이 최근 반영되었으므로 실제 게임
  부팅을 첫 호환성 관문으로 둔다.
- XUnity AutoTranslator는 IL2CPP에서 텍스트 후킹 누락 등 제한을 명시하므로 핵심
  런타임으로 채택하지 않는다.
- 기존 언어 번들을 직접 덮어쓰는 방식은 업데이트·복구 위험 때문에 사용하지 않는다.
- 한글화는 자체 번역 사전과 TextMeshPro 런타임 적용 계층을 사용하고 원문 번들은
  보존한다.

### 참고 자료

- BepInEx IL2CPP 설치: https://docs.bepinex.dev/master/articles/user_guide/installation/unity_il2cpp.html
- BepInEx 저장소: https://github.com/BepInEx/BepInEx
- BepInEx 템플릿: https://github.com/BepInEx/BepInEx.Templates
- XUnity AutoTranslator IL2CPP 제한: https://github.com/bbepis/XUnity.AutoTranslator/blob/master/README.md

## 2026-08-25 구현 기록

### 기반 계획과 개발 환경

- 설계를 안전한 앱·설치, 한글화, 오프라인 런타임의 세 구현 계획으로 분리했다.
- 저장소는 사용자의 현재 프로젝트 직접 진행 지시에 따라 `master`에서 초기화했다.
- 시스템 .NET 설치를 변경하지 않고 프로젝트 `.tools\dotnet-sdk`에 SDK `10.0.302`를 설치했다.
- 최초 부트스트랩에서 설치 스크립트 내부의 stale `$LASTEXITCODE` 때문에 성공한 설치를
  실패로 판정하는 문제가 재현되었다. PowerShell 호출 성공 상태 `$?`를 즉시 저장하도록
  수정하고 회귀 스크립트를 추가해 재설치 성공을 확인했다.
- 프로토콜 TDD RED: `LengthPrefix` 네임스페이스 부재로 컴파일 실패 확인.
- 프로토콜 GREEN: UTF-8 한글 payload 왕복과 최대 frame 길이 차단 2건 통과.
- 메시지 TDD RED/GREEN: `Envelope` 부재 실패 후 JSON 한글 payload 왕복 통과.
- 현재 프로토콜 테스트: 3/3 통과.
- 빌드 검증 TDD RED: `GameBuildValidator` 부재로 컴파일 실패 확인.
- 빌드 검증 GREEN: 정확한 세 파일 승인, 변경 DLL 거부, 누락 메타데이터 거부 3건 통과.
- 게임 경로 TDD RED/GREEN: 예상 EXE·Data 폴더가 있는 루트만 승인하는 2건 통과.
- 현재 Core 테스트: 5/5 통과. 실제 게임 세 파일의 해시는 설계 시 읽기 전용으로
  계산한 현재 빌드 profile과 일치한다.
- installer/backup TDD RED: `Installation`, `Saves` 네임스페이스 부재 실패 확인.
- installer/backup GREEN: payload/manifest 설치, 중간 실패 롤백, 사용자 변경 파일
  보존 제거, 실행 중 설치 거부, 백업 파일·SHA-256 검증 5건 통과.
- installer는 기존 파일/폴더 충돌을 덮어쓰지 않고, manifest 경로를 게임 루트 내부로
  제한하며, 해시가 달라진 설치 파일은 제거하지 않는다.
- WPF 상태/UI TDD RED/GREEN: 빌드·설치·실행·연결 상태 전이와 지원 빌드 설치 서비스
  테스트를 추가해 App 테스트 6건을 통과했다.
- 첫 self-contained EXE 스모크에서 읽기 전용 `EventLog`를 TextBox 기본 TwoWay로
  바인딩해 시작 직후 예외가 발생했다. STA WPF 창 스모크 테스트로 같은 예외를 재현하고
  `Mode=OneWay`로 고쳐 App 테스트 7/7과 실제 창 생성·정상 종료(exit 0)를 확인했다.
- Release 패키징에서 Windows PowerShell 5.1에 없는 `Path.GetRelativePath` 사용이
  드러났다. 패키지 루트 prefix 검증과 substring 방식으로 교체하고 회귀 스크립트 통과,
  중간 ZIP 생성까지 확인했다. 이 ZIP은 아직 Helper/번역 payload 전의 기반 산출물이다.
- 기반 전체 결과: Protocol 3, Core 10, App 7로 총 20개 테스트 통과, 빌드 경고 0,
  오류 0. 중간 EXE 창 제목과 정상 종료를 프로세스 수준에서 확인했다.
- 번역 validator TDD RED/GREEN: placeholder 누락, TMP 태그 변경, 중복 ID를 거부하고
  검수/대기 coverage를 집계하는 4건이 통과했다. 한국어 핵심 용어집을 JSON으로 추가했다.

### 영어 원문 추출 및 한국어 MVP

- AssetRipper `1.3.14`로 Unity 6 IL2CPP 구조와 StringTable 타입을 확인했으나
  `SerializeReference` 필드 복원 제한으로 실제 문자열 항목이 누락되었다. 도구와 서버는
  분석 종료 후 PID `4692`를 중지했다.
- UnityPy `1.25.0`은 동일 번들의 TypeTree에서 `Entities_en` 379개, `UI_en` 535개,
  `Data_en` 672개를 정상 복원했다. 공유 테이블의 ID/키와 결합해 총 1,586개 원문을
  `translation/source.en.json`으로 읽기 전용 추출했다.
- UnityPy Windows CPython 3.14 wheel SHA-256은
  `163404965B7784F34E1BD944E55A511569B53B6AB4A28C12C3B1DDAFF54396B6`이며,
  버전·종속 버전과 AssetRipper 체크섬을 `tools/tool-manifest.json`에 고정했다.
- 원본 영어 번들 SHA-256은
  `76E739553AE5877EF16E0BF2595CE7FDEBC05C7B5FDF4947508958E43F3487FF`, 공유 번들은
  `59FCC50BBCFABF2157655F00BF0E9B861EF57B2F30576E15E0CBD2CA6F6656C8`이다.
- 합성 TableData/SharedData 결합 테스트를 먼저 실패시킨 뒤, 공유 키 결합·ordinal 정렬·
  중복 안정 ID 차단을 구현해 Python 테스트 2/2를 통과했다.
- 메뉴, 난이도, 설정, 조작 프롬프트, 공통 HUD, 로딩 문구 중심의 검수 사전을 만들었다.
  동일 원문이 여러 안정 키에서 쓰이는 경우를 포함해 307/1,586개 항목이 `reviewed`이며,
  placeholder/TMP 태그 오류 0으로 CLI 검증을 통과했다. 나머지 1,279개는 영어 fallback을
  유지하는 `pending` 상태다.
- 번역 CLI TDD RED/GREEN: source/ko 결합, coverage 생성, 검수 번역의 token 오류 시
  exit code 2를 확인하는 테스트 2/2가 통과했다.

### BepInEx 호환성과 Helper 런타임

- 공식 BepInEx bleeding-edge Windows x64 IL2CPP 빌드
  `6.0.0-be.785+6abdba4`를 사용했다. 다운로드 archive SHA-256은
  `2A7CBF74D26ABE4765C3E662DB1721B923BAC39849EBFEF2CA5DC7DE7E2D9B7F`이며
  URL·버전·해시를 `tools/tool-manifest.json`에 고정했다.
- 호환성 부팅 전에 `%USERPROFILE%\AppData\LocalLow\Brimstone\Overthrown`의 파일 9개를
  `.artifacts\save-backups\20260825-034329`에 복사하고 SHA-256 manifest를 만들었다.
- 첫 게임 부팅에서 BepInEx가 IL2CPP interop을 정상 생성했고 `MainMenu:Start()`까지
  도달했다. 분석용 loader 파일 228개는 해시를 대조해 제거했으며 생성 interop과 로그는
  이후 정식 Helper 빌드·진단용으로 보존했다.
- Helper는 별도 `net6.0` Core와 BepInEx plugin으로 분리했다. Unity main thread에서만
  게임 객체를 읽고 쓰며, 사용자 전용 named pipe `VVooOverthrown.<game-pid>`로 WPF 앱과
  길이 제한 JSON frame을 교환한다.
- 세션 가드는 `BNetworkManager.OfflineMode`, `NetworkServer.activeHost`,
  `NetworkClient.active`, `NetworkServer.connections.Count == 1`을 모두 요구한다.
  하나라도 확인되지 않으면 `Uncertain`, 원격 참가자가 있으면 `RemoteParticipant`로
  판정해 변경을 거부하고 적용 중인 상태를 reset한다.
- 생성 interop에서 안전한 쓰기 경로가 확인된 기능만 공개했다.
  - `player.godMode`: 로컬 `PlayerHealth`의 `Damageable.isInvulnerable`과 체력을 사용
  - `world.timeScale`: `GameTime.gameTimeScale`, 허용 범위 `0.25`~`4.0`
- 이동 속도, 기력, 인벤토리, 왕국 자원은 동기화·서버 권한 또는 안정적 쓰기 경로가
  확정되지 않아 WPF에서 `지원 준비 중`으로 표시하고 capability로 광고하지 않는다.
- Helper 종료, pipe 단절, 세션 차단 전환, 앱 reset 명령에서 무적 플래그와 시간 배속을
  원복하도록 구현했다.
- 최종 안전 검토에서 정상 앱 disconnect만 reset 명령을 보내고 비정상 pipe 단절은
  Helper에 전달되지 않는 경로를 발견했다. 연결된 client가 끊길 때 callback이 실행되는
  회귀 테스트를 먼저 컴파일 실패로 확인한 뒤, 서버 callback이 thread-safe reset flag를
  세우고 다음 Unity `Update`에서 원복하도록 수정했다. 집중 테스트 1개와 Helper 전체
  12개가 통과했다.

### 런타임 한글화

- 검수 catalog는 309/1,586개 안정 ID, 대기 1,277개, validation 오류 0이다. 같은 영어
  원문을 합친 실제 런타임 번역 사전은 253개다.
- 원본 Addressables를 수정하지 않고 Harmony로 `TMP_Text.set_text`를 후킹해 정확히
  일치하는 검수 원문만 교체한다. `Malgun Gothic` 동적 TMP fallback을 추가해 한글
  글리프를 제공한다.
- 실제 게임 메인 메뉴에서 `새 월드`, `월드 불러오기`, `월드 참가`, `설정`, `게임 종료`와
  정상 한글 글리프 표시를 육안 확인했다.
- 종료 확인창에서 발견된 누락 원문 `Yes`와
  `Are you sure you want to quit the game?`도 각각 `예`,
  `정말 게임을 종료하시겠습니까?`로 catalog에 추가하고 validator를 다시 통과했다.
  이 마지막 두 문구는 catalog 검증까지만 수행하고 별도 화면 재확인은 하지 않았다.

### 앱과 실제 설치 스모크

- WPF 앱은 설치·안전 제거·게임 실행·Helper 연결·상태 조회·무적 on/off·시간
  `0.5x/1x/2x` 조작을 한 화면에서 제공한다.
- self-contained Windows x64 EXE를 일반 실행해 창 handle과 제목
  `VVooOverthrown · Overthrown 한글 패치 & 오프라인 트레이너`를 확인하고 정상 종료했다.
- 동일 EXE의 headless `--install --game 'W:\Games\Overthrown'`를 실행해 exit code 0과
  `%LOCALAPPDATA%\VVooOverthrown\last-command.txt`의 `SUCCESS` 기록을 확인했다.
- 정식 설치 manifest는 233개 소유 파일을 기록한다. 설치 후 233개 경로의 존재와
  SHA-256을 다시 계산한 결과 불일치 0개였다.
- 정식 payload로 게임 PID `14664`를 실행했을 때 BepInEx 로그에서
  `Trainer pipe ready`, `helper loaded; translations=253`,
  `Chainloader startup complete`를 확인했고 `[Error]` 로그는 없었다. Il2CppInterop의
  일반 경고 `Class::Init signatures have been exhausted, using a substitute!` 한 건만 있었다.
- 메인 메뉴에서는 월드 세션이 아직 확정되지 않으므로 상태가 `Uncertain`이었다.
  capability 응답은 `player.godMode,world.timeScale`였고, 무적 요청은 예상대로
  `OFFLINE_NOT_PROVEN`으로 차단되었으며 reset 응답은 무적 `false`, 시간 `1`이었다.
- 실제 오프라인 월드 안에서의 허용 변경은 자동 UI 조작을 중단한 뒤에는 수행하지 않았다.
  허용/온라인/원격/불확실 판정, 활성 변경 원복 판정, 원래 값 latch는 순수 단위 테스트로
  검증했다. 실제 `Damageable`/`GameTime` 쓰기는 생성 interop 컴파일과 불확실 상태 차단
  스모크까지만 확인했으며, 허용 상태 쓰기는 첫 실제 플레이 확인 항목으로 명시한다.
- 스모크 종료 후 `Overthrown`과 `VVooOverthrown` 실행 프로세스가 모두 0개임을 확인했다.

### 자동화 검증과 배포 구성

- Release 빌드는 경고 0, 오류 0이었다.
- xUnit은 Protocol 3, Core 11, App 8, Helper 20, LocalizationTool 6으로 총 48개가
  통과했고 Python localization extractor 테스트 2개도 통과했다.
- 최종 ZIP에는 self-contained EXE, BepInEx 6 IL2CPP payload, Helper DLL,
  `translation/ko.json`, 설치 profile, 패키지 manifest, README, 프로젝트 기록,
  사용·번역·현재 빌드 API 가이드를 포함한다.
- 실제 게임 폴더는 최종 payload가 설치된 상태로 남겼으며 원본 EXE, GameAssembly,
  Addressables 번들은 직접 수정하지 않았다.
### 최종 코드 리뷰 보완

- 별도 코드 리뷰에서 unsupported build의 Helper 재검증 부재, 시간 배속 단독 활성 상태의
  세션 전환 원복 누락을 Critical로 확인했다.
- Helper가 시작할 때 EXE, GameAssembly, metadata 세 파일의 SHA-256을 직접 다시 계산하고
  한 파일이라도 다르면 Harmony, pipe, 변경 runtime을 만들지 않는 이중 build guard를
  추가했다. WPF 연결 서비스와 버튼 조건도 지원 빌드·설치 상태를 다시 요구한다.
- 무적이 꺼져 있고 시간 배속만 활성화된 경우도 매 Unity `Update`에서 세션 판정을 확인해
  Allowed가 아니면 모든 변경을 즉시 원복한다.
- 무적 기능은 실제로 바꾼 `Damageable` 인스턴스와 기존 `isInvulnerable` 값을 latch해
  reset 시 게임 원래 값을 복원한다.
- 번역 JSON 누락·손상·충돌은 한글화만 비활성화하고 trainer runtime은 계속 시작한다.
  개별 TMP component의 fallback 실패도 해당 text 한 건에서 격리한다.
- `.artifacts\bepinex`가 없는 clean checkout에서는 `build.ps1`이 고정 URL과 SHA-256을
  사용하는 `fetch-bepinex.ps1`을 자동 호출하도록 패키지 재현 경로를 보완했다.
- 새 회귀 테스트는 runtime build hash 변경 거부, time-scale-only 세션 원복 판정,
  원래 무적 값 latch, 잘못된 번역 JSON 격리, pipe 비정상 단절 callback을 포함한다.

### 최종 리뷰 반영 Release 검증

- 최종 ZIP: `.artifacts\release\VVooOverthrown-win-x64.zip`
- 최종 ZIP SHA-256은 산출물 옆 `VVooOverthrown-win-x64.zip.sha256`에 기록한다.
- Release 전체 빌드는 경고 0, 오류 0이고 xUnit 48개와 Python 2개가 모두 통과했다.
  번역 검증은 전체 1,586, 검수 309, 대기 1,277, 오류 0이었다.
- ZIP을 새 검증 디렉터리에 풀어 package manifest 239개 파일의 길이와 SHA-256을 전부
  계산했고 불일치 0개였다. build profile `Unity 6000.1.10f1`, EXE, Helper/Core,
  번역 JSON, README, 프로젝트 기록, 세 가이드가 모두 존재한다.
- 최종 EXE로 이전 manifest의 233개 소유 파일을 안전 제거(exit 0)하고 새 payload를
  재설치(exit 0)했다. 새 설치 manifest 233개 파일의 존재·SHA-256 불일치는 0개다.
- 새 Helper를 실제 게임 PID `4652`에서 부팅해 `translations=253`, pipe 준비,
  Chainloader 완료, BepInEx 오류 0을 확인했다. 메인 메뉴 `Uncertain` 상태에서 status와
  reset이 응답했고 무적 요청은 `OFFLINE_NOT_PROVEN`으로 차단됐다. 저장을 만들지 않는
  headless 스모크라 게임은 확인 후 해당 PID만 종료했다.
- 최종 WPF EXE를 일반 실행해 창 제목을 확인하고 `CloseMainWindow`로 정상 종료(exit 0)했다.
  강제 종료는 사용하지 않았고 최종 게임·앱 잔여 프로세스는 0개다.
- 재검토에서 파괴된 Unity `Damageable`이 null로 비교될 때 원래 값 latch가 남는 경계를
  추가로 확인했다. 대상 생존 여부와 관계없이 latch를 먼저 소비하고, 살아 있는 동일
  대상에만 원래 값을 쓰도록 고쳐 새 플레이어에 이전 값을 적용하지 않는다.
- 검증되지 않은 BepInEx cache의 다른 runtime 파일이 섞일 가능성을 없애기 위해 Release
  빌드마다 공식 고정 URL에서 archive를 새로 받는다. 전체 archive SHA-256을 확인해 새
  staging 디렉터리에 추출하고, 핵심 파일 5개의 SHA-256까지 대조한 뒤 기존 cache를
  교체하고 payload를 만든다.
- 초기 MVP 배포 ZIP SHA-256:
  `275754C13D7954562C71971694CFE8AE23D93F4F878DED8004AFB24555E5C01B`
- 초기 MVP 배포 EXE SHA-256:
  `7F4DE27B8817334EE228B14E5A9F8E1B15062F0975181585FF975F98F1D6B1EE`
- 최종 archive 재검증 빌드 후 ZIP manifest 239개 불일치 0, 설치 payload 233개 불일치
  0을 확인했다. 게임 PID `20664`에서 Helper가 준비됐고 무적 요청은 불확실 세션으로
  차단됐으며 BepInEx 오류 0이었다. WPF 정상 종료 후 잔여 프로세스도 0개였다.

## 2026-08-25 새 월드 만들기 한글화 보완

- 사용자 스크린샷에서 새 월드 만들기 화면의 프리셋 이름, 크기, 선택 설명이 영어로
  남아 있는 것을 확인했다. source catalog에는 안정 키가 있었지만 검수 사전에 원문이
  없어 runtime catalog에서 `pending`으로 유지된 것이 원인이었다.
- 월드 프리셋 7종 `Archipelago`, `Canyon`, `Foothills`, `Highland Plateau`,
  `Lost Isles`, `Rolling Hills`, `Seaside Cliffs`와 각 설명 7개를 한국어로 추가했다.
- 크기 원문 `Small`, `Large`는 각각 `소형`, `대형`으로 추가했다. `Medium`은 그래픽
  품질 등에서도 공유되는 일반 원문이므로 기존 번역 `중간`을 유지했다.
- catalog 재생성 결과 검수 327/1,586개, 대기 1,259개, runtime 고유 원문 269개이며
  placeholder/TMP 검증 오류는 0개다.
- 사용자 지시에 따라 자동 테스트는 실행하지 않고 Release 컴파일·publish만 수행한다.
- 보완 Release의 최신 ZIP SHA-256은 산출물 옆
  `VVooOverthrown-win-x64.zip.sha256`에 기록하며, ZIP 내부 기록에는 자기 참조가 되는
  현재 ZIP 해시를 넣지 않는다.
- 새 월드 한글화 보완 Release ZIP SHA-256:
  `FA2F157F029FF6D357733B4ECB7F9D889E9341BB0ABA659F680175BC052A0E84`
- 보완 EXE SHA-256:
  `F03405BB9DC328252AD33B9B546553CA65D4C188CE04B75E57EB25349CE92D86`
- `-SkipTests`로 테스트를 실행하지 않고 Release build/publish/package를 완료했으며,
  최종 EXE로 기존 소유 payload를 제거한 뒤 새 payload를 재설치해 exit code 0과
  `SUCCESS --install`을 확인했다.
- 최종 ZIP과 인접 `.sha256`이 일치했고, 새 디렉터리에 푼 package manifest 239개 파일의
  길이·해시 불일치는 0개였다. 최종 payload를 재설치한 manifest 233개도 불일치 0개다.
- 최종 게임 PID `20152` 부팅에서 Helper 준비와 BepInEx 오류 0을 다시 확인했고,
  메인 메뉴 무적 요청은 `OFFLINE_NOT_PROVEN`으로 차단됐다. 게임 종료 후 최종 WPF 창도
  일반 종료(exit 0)했으며 잔여 `Overthrown`/`VVooOverthrown` 프로세스는 0개다.

## 2026-08-25 목표·퀘스트 한글화 보완

- 플레이 화면 좌측 상단의 `Your Reign Begins`, `Claim the Crown`은 각각
  `Data/OBJECTIVE_ReignBegins`, `Data/OBJECTIVE_ReignBegins_Crown` 안정 키를 사용하는
  목표 제목과 수행 항목임을 확인했다.
- 동일 계열인 `Data/OBJECTIVE_*` 92개 전체를 분석해 통치 시작, 정착지 건설, 연구,
  주민·식량, 무법자 습격, 왕관 회수, 재정 목표의 제목과 수행 항목을 모두 한국어로
  추가했다. 목표 계열은 92/92개가 `reviewed` 상태다.
- 수량과 시간이 들어가는 목표는 화면 표시 시점에 `{0}`, `{1}` 등이 실제 숫자로 바뀌어
  기존의 완전 일치 사전으로 찾을 수 있었다. Helper에 서식 원문의 placeholder 위치를
  캡처해 한국어 서식에 다시 넣는 template matching을 추가했고, 시간·일·초 단위의
  smart plural 토큰도 한국어 단위로 정규화한다.
- catalog 재생성 결과 검수 419/1,586개, 대기 1,167개, runtime 고유 원문 361개이며
  placeholder/TMP 검증 오류는 0개다.
- Helper 코드가 변경되어 해당 프로젝트만 Release 빌드했고 경고 0, 오류 0이었다.
  배포 payload는 `.artifacts\payload`에 갱신했으며, 사용자 지시에 따라 자동 테스트와
  ZIP 패키징은 실행하지 않았다.
- 실행 중인 게임과 트레이너가 없는 상태에서 기존 설치기의 `--install`을 대기 실행해
  exit code 0을 확인했다. 게임 폴더의 Helper/Core DLL과 번역 JSON 3개는 staged payload와
  SHA-256이 모두 일치하며, 설치 manifest 233개 파일의 누락·길이/해시 불일치는 0개다.

## 2026-08-25 중앙 상호작용 HUD 한글화 보완

- 사용자 스크린샷의 `Oak (Medium)`과 `Wood`는 각각
  `Entities/WILD_Tree_OakMedium`, `Entities/PHYSICALTYPE_Wood` 안정 키이며 검수 사전에
  원문이 없어 `pending`으로 남아 있음을 확인했다.
- 같은 화면 계열을 기준으로 `Entities/*` 월드 엔티티 379개 중 실제 문구 372개,
  `Data/ITEM_Type_*` 43개, `Data/RESOURCE_Type_*` 45개, `UI/PROMPT_*` 67개를 모두
  `reviewed` 상태로 보완했다. 원문이 `-`뿐인 엔티티 설명 7개는 번역하지 않았다.
- 원문 기반 runtime 사전을 공유하는 `Apothecary`, `Medicine`은 건물·직업과 자원·기술
  문맥을 모두 해치지 않도록 각각 중립 용어 `약제상`, `의약`으로 통일했다.
- `Lift [HOLD]`는 `Lift` 번역이 이미 있었지만 `ContextualPrompt.GetPromptText`가
  `[HOLD]` 접미사를 합친 뒤 TMP에 전달해 완전 일치 사전을 벗어난 것이 원인이었다.
  TranslationCatalog가 검수된 프롬프트 원문 뒤의 서식 태그와 `[HOLD]`만 제한적으로
  인식해 `들어 올리기 [길게 누르기]`처럼 번역하도록 보완했다.
- catalog 재생성 결과 검수 903/1,586개, 대기 683개, runtime 고유 원문 762개이며
  placeholder/TMP 검증 오류는 0개다.
- Helper 코드 변경으로 해당 프로젝트만 Release 빌드했고 경고 0, 오류 0이었다.
  사용자 지시에 따라 자동 테스트와 ZIP 패키징은 실행하지 않았다.
- 기존 설치기의 안전 제거·설치를 각각 exit code 0으로 완료했다. 설치 manifest 233개
  파일은 누락·길이/해시 불일치 0개이며 설치된 Helper와 `ko.json`은 빌드/staged
  산출물과 SHA-256이 일치한다.

## 2026-08-25 전체 한글화 선제 보완

- 사용자가 플레이 중 미번역 화면을 매번 제보하지 않아도 되도록 추출된 전체 안정 키
  1,586개를 다시 분류했다. UI, 설정, 오류, 저장, 모집, 계절, 연구, 장비, 게임 알림,
  NPC 직업·행동·감정, 지형, 지도 범례, 자원 분류, 생성 유형을 일괄 검토했다.
- 기존 상호작용 HUD 보완 이후 남아 있던 안정 키 683개 중 사용자에게 노출되는 번역
  가능 항목 670개를 추가 검수했다. 최종 catalog는 1,573/1,586개 검수, 대기 13개,
  런타임 고유 원문 1,394개이며 placeholder/TMP 검증 오류는 0개다.
- 대기 13개는 번역 누락이 아니다. 법적 효력이 있는 장문 EULA 원문 1개는 공인 법률
  번역 없이 의미를 바꾸지 않도록 유지하고, 개발자가 의도적으로 비워 둔 연구 설명
  `[INTENTIONALLY LEFT BLANK]` 5개와 화면 구분자 `-` 7개도 그대로 둔다.
- 번역 JSON과 문서만 변경되어 EXE/Helper 빌드는 실행하지 않는다. 사용자 정책에 따라
  자동 테스트와 ZIP 패키징도 실행하지 않고, catalog 정적 검증과 설치 manifest/hash
  확인만 수행한다.

## 2026-08-25 동적 문구 `~초` 오인식 수정

- 플레이 화면의 `Unemployed초`, `Insufficient초`를 포함해 여러 동적 문구가 영어 단어
  뒤에 `초`를 붙이는 현상을 분석했다. 개별 번역 누락이 아니라
  `{0} {0:plural:second|seconds}` 템플릿 정규식이 공백으로 구분된 임의의 두 값을 허용해
  이슈명과 개수 같은 일반 동적 문구까지 시간 표현으로 오인한 것이 원인이었다.
- smart plural placeholder가 실제 `second/seconds`, `day/days`, `hour/hours` 중 하나일
  때만 해당 template을 허용하도록 공통 `TranslationCatalog`의 캡처 패턴을 제한했다.
  따라서 같은 원인의 `~초`, `~일`, `~시간` 오염을 전역에서 차단하면서 정상 시간
  문자열의 한국어 단위 변환은 유지한다.
- Helper 컴파일 코드가 변경되어 해당 프로젝트만 Release 빌드했으며 경고 0, 오류
  0이었다. 자동 테스트와 ZIP 패키징은 사용자 정책에 따라 실행하지 않았다.
- 최신 Helper를 설치 payload와 기존 앱 payload에 반영한 뒤 게임 폴더를 안전 제거·
  재설치했으며 두 명령 모두 exit code 0, 설치 Helper SHA-256 일치를 확인했다.

## 2026-08-25 선택 휠 설명 한글화 보완

- 사용자 스크린샷의 `Town Hall` 설명 원문은
  `Entities/BUILDING_TownHall_Description`에 이미 검수된 한국어
  `모든 주거 공간이 찰 때까지 새 시민을 꾸준히 모집합니다.`로 존재했다. 같은 건물
  설명 계열의 의미 있는 문구도 모두 `reviewed`이며, 대기 상태는 화면 구분자 `-`뿐이다.
- 건설·연구 선택 화면은 공통 `RadialMenuDynamicWheel`을 사용하고, 선택이 바뀔 때
  `RefreshInformation()`이 `title`과 설명용 `subtitle` TMP 필드를 별도 경로로 갱신한다.
  이 네이티브 새로고침 뒤에는 기존 `TMP_Text.text` 기록 패치만으로 번역이 유지되지
  않을 수 있어, 새로고침 postfix에서 화면의 두 필드를 같은 검수 catalog로 다시
  대조하도록 제한적으로 보완했다.
- 공통 선택 휠을 고쳤으므로 시청 하나가 아니라 해당 휠을 공유하는 건물·연구 등 선택
  항목의 검수된 제목과 설명에 함께 적용된다. 원문이 catalog와 일치할 때만 교체하며,
  동적 수치·비용과 미검수 문구는 변경하지 않는다.
- Helper 컴파일 코드가 변경되어 해당 프로젝트만 Release 빌드했고 경고 0, 오류 0이었다.
  자동 테스트와 ZIP 패키징은 사용자 정책에 따라 실행하지 않았다.
- 최신 Helper를 설치 payload와 기존 앱 payload에 반영한 뒤 게임 폴더를 안전 제거·
  재설치했다. 두 명령 모두 exit code 0이며 설치 manifest 233개는 누락 0, SHA-256
  불일치 0이다.

## 2026-08-25 선택 휠 설명 재보완 및 로딩 문구 한글화

- 사용자의 재확인 스크린샷에서 시청 제목은 한국어지만 설명은 여전히 영어임을 확인했다.
  해당 게임 세션 로그에는 Helper `translations=1394`가 정상 로드됐고 Harmony 오류가
  없으며, 설치 Helper 해시도 최신 빌드와 일치했다. 따라서 설치·catalog 문제가 아니라
  `RadialMenuDynamicWheel.RefreshInformation()` 표시 후단 보완이 실제 옵션의 영어
  설명 저장값을 바꾸지 못한 것이 원인이었다.
- 공통 `RadialMenu.AddOption()` 입구의 `name`, `subtitle`, `description`을 검수 catalog의
  완전 일치 항목으로 먼저 치환하도록 수정했다. 이후 선택 휠은 저장된 한국어 설명을
  사용하며, 기존 새로고침 postfix는 TMP 글꼴 fallback과 후단 안전망 역할만 맡는다.
- 로딩 화면의 `Growing plants 79%`는 `UI/LOADING_GrowingPlants`의 검수 원문
  `Growing plants` 뒤에 진행률이 결합돼 완전 일치 사전을 벗어난 경우다. 같은
  `UI/LOADING_*` 18개는 모두 `reviewed` 상태다.
- `LoadingScreen`과 `PreloadLoadingScreen`의 `SetLoadingMessage(string)` 입구에서 단계명을
  완전 일치 번역한다. 화면에 이미 결합된 경우도 마지막 토큰이 `0%`~`100%`일 때만
  단계명 부분을 번역하고 퍼센트는 그대로 유지해 다른 동적 문구에는 영향을 주지 않는다.
- Helper 컴파일 코드만 변경됐으므로 해당 프로젝트만 Release 빌드한다. 사용자 정책에
  따라 자동 테스트와 ZIP 패키징은 실행하지 않는다.

## 2026-08-25 선택 휠 실제 호출 경로 교정

- 재설치 후 사용자 스크린샷에서도 시청 설명이 영어로 남아 있어, 직전의
  `RadialMenu.AddOption()` 전단 패치가 실제로 실행되지 않았음을 확인했다. 최신 게임
  세션은 Helper `translations=1394`를 오류 없이 로드했고 설치 DLL 해시도 최신 빌드와
  일치하므로 catalog·설치 문제는 아니다.
- interop caller 근거를 다시 대조한 결과 `RadialMenu.AddOption()`은 호출자 0개인 미사용
  API이고, 실제 선택 항목을 저장하는 `RadialMenuDynamicWheel.AddOption()`은 호출자
  5개다. 두 메서드의 이름과 인수 구성이 비슷해 잘못된 타입을 대상으로 잡은 것이
  연속 실패의 원인이었다.
- 기존 exact-only prefix의 대상만 실제 호출되는 `RadialMenuDynamicWheel.AddOption()`으로
  교정하고 인수도 실제 시그니처인 `title`, `subtitle`, `description`에 맞춘다. 번역
  catalog, 로딩 화면 패치, 다른 UI 경로는 변경하지 않는다.

## 2026-08-25 TMP 최종 기록 경로 보완

- 세 번째 사용자 확인에서도 시청 설명이 영어로 유지되어 `RadialMenu` 계열 중간
  메서드를 패치하는 접근을 폐기했다. 최신 세션은 Helper와 최신 DLL을 정상 로드했으므로
  설치나 catalog 문제가 아니다.
- `Unity.TextMeshPro` interop을 다시 확인한 결과 `TMP_Text.set_text`는 IL2CPP 네이티브 토큰
  `100664626`, `SetText(string)`은 `100664832`, `SetText(string, bool)`은 `100664833`인
  별도 네이티브 메서드다. 기존 Helper는 property setter만 가로채므로 `SetText`로 직접
  기록되는 설명은 번역기를 완전히 우회했다.
- 효과가 없던 `RadialMenuTranslationPatch.cs`를 제거하고 기존 TMP 공통 패치에 정적
  문자열 `SetText` 두 오버로드만 추가한다. 두 경로 모두 기존 검수 catalog와 글꼴
  fallback을 재사용하며, 숫자 서식용 `SetText(string, float, ...)` 오버로드는 변경하지
  않아 동적 수치 문구의 범위를 넓히지 않는다.

## 이후 작업 방식

- 사용자가 별도로 선택하도록 묻지 않고 항상 권장안으로 구현한다.
- 한글화 보완이 `reviewed-core.ko.json`, 생성 catalog, coverage 및 설치본 번역 JSON만
  바꾸는 경우 EXE/Helper 빌드를 실행하지 않는다.
- Helper, 앱, 빌드 도구 등 컴파일 대상 코드가 바뀌어야 할 때만 필요한 빌드를 실행한다.
- ZIP/배포 패키징은 사용자가 명시적으로 요청한 경우에만 실행한다.
- 구현 작업은 사용자의 별도 지시가 없으면 자동 테스트를 실행하지 않고 커밋·push로
  마무리한다.
