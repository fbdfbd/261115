# 달이의 성장 기록

GameCollection.prefab을 생성하고 `01_Title`의 모음집 버튼에 이미 연결했습니다.
기존 프리팹을 다시 만들거나 씬에서 추가 연결할 필요는 없습니다.

## 생성 툴

`Work Game > UI > Game Collection Tool`

- 프리팹 생성: 기본 1760×980, 목록은 5열×5행입니다.
- 타이틀 배치: 01_Title에 배치하고 GameTitleRoot의 Collection Root를 연결합니다.
  이미 배치된 모음집은 재사용하여 중복 생성하지 않습니다.
- 육성 데이터: CareConfigSO의 일반 엔딩 24개와 FallbackEnding 1개를 사용합니다.

## 이미지와 데이터

- 각 CareEndingSO의 **Collection Image**에 Sprite를 넣습니다.
- 비워 두면 Pages에서 첫 번째로 이미지가 있는 페이지의 Sprite를 사용합니다.
- 양쪽에 이미지가 없어도 이름·설명·조건은 정상 표시하고 이미지 틀은 유지합니다.
- 목록 Picture와 상세 Picture는 모두 Simple Image + Preserve Aspect입니다. 프레임은 1920×1080과 같은 16:9입니다.
  가로/세로 이미지 교체 시 고정 프레임 안에 맞춰 표시하며 찌그러뜨리거나 잘라내지 않습니다.
- 설명은 엔딩 SO의 Text, 조건은 MainNeeds와 Needs를 그대로 사용합니다.
- 엔딩 데이터 생성기 build_care_data.py는 이미 지정한 Collection Image도 유지하도록 수정했습니다.
  데이터 생성기는 이번 작업에서 실행하지 않았습니다.

## 동작

- 슬롯과 별도로 보관하는 엔딩 읽음 기록으로 해금을 판정합니다.
- 미획득 항목: 이름은 ???, 이미지는 숨기고 검은 딤을 표시합니다. 버튼은 클릭할 수 있습니다.
- 미획득 상세: 이름·이미지·설명은 숨기고 대표 등급과 세부 스탯 조건만 공개합니다.
- 획득 상세: 이름·이미지·설명·조건을 모두 표시합니다.
- 백수 조건은 다른 직업 엔딩의 조건을 충족하지 않음으로 표시합니다.
- 목록으로는 왼쪽 위, 닫기는 오른쪽 위입니다. 바깥 배경도 모음집을 닫습니다.
- 친밀도별 후일담 3종을 따로 해금하는 화면은 포함하지 않았습니다. 현재 모음집은 직업 25종 기준입니다.

## 크기·배치 수정

- Page: 중앙 앵커/피벗, 크기를 바꾸면 내부 목록과 상세 영역이 앵커에 따라 늘어납니다.
- ListPage/Cards: 16:9 갤러리 영역 안에 5×5 앵커로 배치합니다. LayoutElement나 GridLayoutGroup은 사용하지 않습니다.
- 카드 ImageFrame: 16:9 Fit In Parent로 사진을 크게 표시합니다. Name은 별도 배경 없이 사진의 위쪽에 겹칩니다.
- Name Color는 이미지 위의 글자색, No Image Name Color는 이미지가 없는 획득 항목의 글자색입니다.
- DetailPage/Content/ImageRow: 기본 높이 420. 이미지 프레임은 이 행 안에서 16:9를 유지합니다.
- 상세 헤더는 고정이고 DetailPage 본문만 세로 스크롤합니다. 감도는 ScrollRect의 Scroll Sensitivity에서 조절합니다.
- 상세 Content의 Vertical Layout Group은 폭만 통제합니다. 본문은 ContentSizeFitter로 높이를 계산합니다.
- 세부 조건은 지식·체력·감성·사교 4열로 묶고 글자의 실제 높이에 맞춰 영역을 늘립니다.

프리팹에서 상세를 편집하려면 DetailPage와 Back을 활성화하고 ListPage를 숨기면 됩니다.
런타임에는 View가 상태를 관리합니다. 루트 GameCollection 오브젝트는 활성화 상태로 둡니다.

컴파일·플레이·테스트 실행은 하지 않았습니다.
