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
