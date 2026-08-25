# VVooOverthrown

Overthrown Unity 6 IL2CPP 현재 빌드를 위한 핵심 UI 한글 패치와 확장 테스트
트레이너를 하나의 Windows 앱으로 제공합니다.

- 원본 Addressables 번들을 변경하지 않는 런타임 한글화
- 사용자 노출 한글화 1,573/1,586개 안정 키, 런타임 고유 원문 1,394개
- 플레이어·기력·이동/시간·인벤토리·왕국 자원·진단 탭
- 세션 판정은 진단으로 표시하고 현재 빌드 해시와 기능 객체 준비 상태를 검증
- 설치 전 사용자 데이터 백업, 소유 파일별 SHA-256 manifest 제거

Release 산출물은 `.artifacts\release\VVooOverthrown-win-x64.zip`이며, 압축을 푼 뒤
`VVooOverthrown.exe`를 실행합니다. 자세한 내용은 [사용 가이드](docs/user-guide.md),
[번역 가이드](docs/translation-guide.md), [현재 빌드 API 근거](docs/analysis/current-build-api-map.md)를
참조하세요.
