# 번역 확장 가이드

## 파일

- `translation/source.en.json`: 게임에서 읽기 전용 추출한 영어 키/원문
- `translation/reviewed-core.ko.json`: 검수된 원문별 한국어
- `translation/ko.json`: 안정 ID별 생성 catalog
- `translation/glossary.ko.json`: 용어집
- `translation/coverage.json`: 검수 수와 오류 보고서

## 원문 재추출

```powershell
.\tools\extract-localization.ps1 -GameDir 'W:\Games\Overthrown'
```

UnityPy `1.25.0`과 종속 버전은 `tools/python-requirements.lock`, 도구 체크섬은
`tools/tool-manifest.json`에 고정되어 있다. 원본 번들은 수정하지 않는다.

## 번역 추가

1. `source.en.json`의 원문과 키 문맥을 확인한다.
2. `reviewed-core.ko.json`에 영어 원문과 한국어를 추가한다.
3. placeholder와 TMP 태그를 문자 그대로 보존한다.
4. catalog와 coverage를 다시 생성·검증한다.

```powershell
python .\tools\build_korean_catalog.py `
  --source .\translation\source.en.json `
  --reviewed .\translation\reviewed-core.ko.json `
  --output .\translation\ko.json

.\.tools\dotnet-sdk\dotnet.exe run `
  --project .\tools\VVooOverthrown.LocalizationTool -- `
  validate .\translation\source.en.json .\translation\ko.json .\translation\coverage.json
```

오류가 하나라도 있으면 런타임 payload에 포함하지 않는다. 같은 영어 원문에 서로 다른
검수 번역이 생기면 Helper도 catalog 로드를 거부한다.
