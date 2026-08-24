# VVooOverthrown 사용 가이드

## 설치

1. Overthrown을 완전히 종료한다.
2. `VVooOverthrown.exe`를 실행한다.
3. 게임 경로가 `W:\Games\Overthrown`인지 확인한다.
4. `한글 패치 설치`를 누른다. 설치 전에 사용자 데이터가 자동 백업된다.
5. `게임 실행`을 누른다. 첫 실행은 IL2CPP interop 생성 때문에 평소보다 오래 걸릴 수 있다.

현재 지원 빌드와 해시가 다르거나 대상 파일이 이미 있으면 설치하지 않는다. 다른 BepInEx
모드가 설치된 폴더를 자동으로 덮어쓰지 않는다.
설치 후 게임이 업데이트되어 해시가 달라져도 Helper가 게임 시작 시 세 파일을 다시
검증하고 trainer pipe와 변경 기능을 열지 않는다.

## 한글 패치

- 영어 원본 Addressables 번들을 수정하거나 교체하지 않는다.
- 현재 핵심 UI 903/1,586개 안정 키가 검수 상태다.
- 같은 원문의 중복을 합친 실제 런타임 사전은 762개다.
- 미번역 항목은 영어로 유지된다.
- 맑은 고딕 기반 TextMeshPro 동적 fallback으로 한글 글리프를 표시한다.

## 트레이너

1. 게임에서 오프라인 싱글플레이 월드에 들어간다.
2. 앱에서 `상태 새로고침`, `Helper 연결`을 순서대로 누른다.
3. 상태가 `로컬 싱글플레이 확인됨`일 때만 기능 버튼이 활성화된다.
4. 첫 버전에서 검증된 기능은 `플레이어 무적`과 `월드 시간 배속`이다.

멀티플레이 참가자, 온라인 모드, 서버 권한 불확실 상태에서는 모든 변경이 차단된다.
앱 종료, Helper 연결 해제, 멀티플레이 감지 시 활성 변경을 원복한다.

## 제거와 복구

- 게임을 종료하고 앱의 `안전하게 제거`를 누른다.
- manifest에 기록된 VVooOverthrown 소유 파일만, 설치 당시 해시와 같은 경우에만 제거한다.
- 사용자가 변경한 파일은 보존한다.
- 백업 기본 위치: `%LOCALAPPDATA%\VVooOverthrown\Backups`

명령행 설치·제거도 지원한다.

```powershell
.\VVooOverthrown.exe --install --game 'W:\Games\Overthrown'
.\VVooOverthrown.exe --remove --game 'W:\Games\Overthrown'
```
