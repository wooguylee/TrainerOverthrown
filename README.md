# VVooOverthrown

Overthrown Unity 6 IL2CPP 현재 빌드를 위한 핵심 UI 한글 패치와 싱글플레이 전용
트레이너를 하나의 Windows 앱으로 제공합니다.

- 원본 Addressables 번들을 변경하지 않는 런타임 한글화
- 핵심 UI 검수 327/1,586개 키, 런타임 고유 원문 269개
- 검증된 트레이너 기능: 로컬 플레이어 무적, 월드 시간 배속
- 오프라인·호스트·접속 수를 모두 확인하는 fail-closed 안전 가드
- 설치 전 사용자 데이터 백업, 소유 파일별 SHA-256 manifest 제거

Release 산출물은 `.artifacts\release\VVooOverthrown-win-x64.zip`이며, 압축을 푼 뒤
`VVooOverthrown.exe`를 실행합니다. 자세한 내용은 [사용 가이드](docs/user-guide.md),
[번역 가이드](docs/translation-guide.md), [현재 빌드 API 근거](docs/analysis/current-build-api-map.md)를
참조하세요.
