# 엔딩 선택 화면

## 생성 및 연결

1. `Work Game > UI > Care Ending Choice Tool`에서 프리팹을 생성합니다.
2. 활성화된 Canvas_Ending 아래 배치하고 루트 GameObject도 켜 둡니다. CanvasGroup으로 숨김을 제어합니다.
3. WorkRoot의 `Care Ending Choice View`에 씬의 CareEndingChoiceView를 연결합니다.
4. 기존 `Care Ending View`는 엔딩 편지 연출 View를 그대로 연결합니다.

선택 카드 5개와 버튼 이벤트는 생성기가 연결합니다. Presenter 컴포넌트나 카드 OnClick을 수동으로 추가하지 않습니다.
선택창도 업무·육성 전환 그룹 바깥에 둡니다. 엔딩 편지 패널과 함께 사용하며 타이틀 이동은 기존 엔딩 View에서 연결합니다.

## 플로우

12월 육성 결과 확인 → 조건을 충족한 엔딩 후보 → 선택 → 선택한 엔딩 편지 연출입니다.

- 기존 Priority가 높은 순서로 최대 5개를 표시합니다. 동점은 ID 순서입니다.
- 후보가 하나면 선택창을 생략합니다.
- 조건을 충족하는 일반 엔딩이 하나도 없으면 백수 엔딩으로 바로 진행합니다.
- 표시된 후보만 선택할 수 있고 연속 클릭으로 중복 진입하지 않습니다.
- 선택한 엔딩의 친밀도별 후일담을 기존 기준으로 결정합니다.

CareSystem.GetEndingChoices()가 후보를 반환합니다. CareFlowSystem.ChooseEnding(id)가 플로우를 확인하고 선택을 확정합니다.
WorkRoot.ChooseEnding(id)는 별도 UI를 붙일 때 사용할 통로입니다. 생성한 UI는 Presenter가 직접 연결합니다.
ResolveEnding()은 후보가 하나일 때만 자동 결정하며, 여러 후보라면 선택 전까지 null을 반환합니다.

## 카드 내용

안 본 엔딩은 ???, 본 엔딩은 직업 이름을 표시합니다. 부연 설명은 열람 여부와 관계없이 표시합니다.
각 CareEndingSO의 Choice Text에서 설명을 수정합니다. 기존 본문 Text와 분리했으며 설명은 25개에 추가했습니다.
CareContent.json의 choiceText에도 반영했고 build_care_data.py에서 가져옵니다.

## 열람 기록 연결

- 현재 WorkRoot가 살아 있는 동안에는 끝까지 읽은 엔딩 ID를 기억합니다. StartRun으로 재시작해도 유지합니다.
- 씬을 나가거나 앱을 종료한 뒤의 영구 저장은 아직 추가하지 않았습니다.
- 나중에 `WorkRoot.SetEndingReadCheck(id => 저장된기록에있는지)`를 연결하면 기존 열람 기록을 조회합니다.
- `WorkRoot.EndingReadCompleted` 이벤트에서 ID를 받아 저장하면 됩니다.
- 열람 완료는 엔딩 편지 마지막 페이지를 다 표시한 뒤 클릭해서 타이틀 버튼이 나타나는 시점입니다.
- 직업 엔딩 ID 기준이므로 친밀도 후일담이 달라도 같은 엔딩 이름을 공개합니다.

## 크기와 추가 연출

- 루트: 풀스크린 배경 Image와 CanvasGroup입니다. 기본은 1920×1080 Canvas 기준으로 배치합니다.
- Cards: Horizontal Layout Group의 Middle Center로 후보 수에 맞춰 중앙 정렬합니다.
- CareEndingChoiceView.Card Size: 기본 280×380. 화면 폭이 부족하면 카드 폭을 줄입니다.
- Cards의 Spacing: 카드 간격입니다. 카드 자체에 Layout Element는 없습니다.
- 카드의 Name / Description / Divider: 앵커·폰트·색을 직접 수정합니다. 그림 칸은 없습니다.
- View의 Open Seconds / Select Seconds: 등장·선택 페이드 시간입니다.
- On Open / On Select(string): 효과음이나 추가 연출을 연결할 이벤트입니다. On Select는 선택한 엔딩 ID를 전달합니다.

프리팹 생성·씬 수정·컴파일·플레이·테스트는 실행하지 않았습니다.
