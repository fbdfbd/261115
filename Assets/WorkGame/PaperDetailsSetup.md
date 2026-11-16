# 상세 서류 연결

씬의 검은 부모에 있는 WorkPaperView와 기존 문서 4종 참조는 그대로 사용합니다.
각 문서의 공통 정보/버튼은 WorkPaperPanelView, 내부 그림은 별도 GraphView가 담당합니다.
WorkPresenter는 일반 C#을 유지하고 입력·진행률을 연결합니다.

## 가장 빠른 연결

Edit Mode에서 완성된 문서 4종에 각각 다음 작업을 합니다.

1. Project에서 `WorkDocument_Dots`, `WorkDocument_Grade`, `WorkDocument_Graph_H`, `WorkDocument_Graph_V` 중 하나를 선택합니다.
2. `Work Game > UI > Connect Selected Paper`를 실행합니다.
3. 네 문서에 반복합니다. `_WorkDocument_…` 빈 틀이 아니라 그래프가 들어간 완성 문서를 고르세요.

또는 씬/Prefab Mode에서 해당 문서 전체 루트를 선택해 같은 메뉴를 실행해도 됩니다.
Project 프리팹 선택 시 해당 프리팹을 저장하고, 씬 객체 선택 시 Undo 가능한 인스턴스 변경으로 적용합니다.
기존 씬 인스턴스가 프리팹을 참조하고 있다면 프리팹의 컴포넌트 연결을 그대로 상속합니다.
이 메뉴는 오브젝트 위치·폰트·크기를 변경하지 않습니다. 정해진 경로에서 참조를 찾아 연결합니다.
경로를 바꾼 문서는 아래 필드를 직접 연결하거나, 메뉴가 알려주는 경로를 확인하세요.
검은 부모 자체가 아니라 **각 문서 루트**를 선택해야 합니다.

## WorkPaperPanelView — 각 문서 루트

| 필드 | 프리팹 경로 |
| --- | --- |
| Title Text | Header/DocumentTitle |
| Number Text | Header/DocumentInfo/Left |
| Month Text | Header/DocumentInfo/Right |
| Rule Text | Header/WorkRule |
| Graph View | GraphContent 안에 추가된 아래 GraphView |
| Choices | ChoiceA/B/C의 WorkChoiceView, A부터 순서대로 |

제목은 원래 정책명, 문서번호는 `조달검토-01-0001` 형식으로 표시합니다.
번호는 배정 월과 서류 ID로 런타임에 만들며 같은 서류를 다시 열어도 유지됩니다.
시행은 `시행  1달`입니다. 이월 서류는 원래 배정된 달을 표시합니다.
지시문은 비교 지표와 높은/낮은 값 선택 규칙으로 조합합니다.

## WorkChoiceView — ChoiceA/B/C

각 버튼 루트에 컴포넌트 하나를 추가합니다. 자동 연결 메뉴가 처리합니다.

| 필드 | 연결 |
| --- | --- |
| Button | 자기 자신에 있는 Button |
| Name Text | 자식 ChoiceName |
| Fill | 자식 Fill 이미지 |

Pointer Down은 홀드 시작, Pointer Up / Exit는 취소입니다. 버튼 On Click이나 EventTrigger에
WorkRoot.StartChoice/StopHold를 중복 등록하지 마세요. WorkPaperView를 거쳐 Presenter로 자동 연결됩니다.
누르고 있는 선택지의 Fill만 왼쪽에서 오른쪽으로 차고, 제출되면 서류가 닫힙니다.
정지·포커스 이탈·서류 닫기에도 홀드를 취소합니다.

## 내부 그래프

공통으로 Metric Title은 그래프 루트의 `MetricTitle`, Unit Text는 있으면 `UnitLabel`입니다.

MetricTitle 하나에 상황 제목과 다음 줄의 작은 `비교 항목 · 이름 (단위)`를 함께 표시합니다.
별도 UnitLabel은 숨깁니다. 기존 필드 연결을 유지하므로 프리팹을 다시 연결할 필요가 없습니다.
상황 제목은 카테고리 SO의 Metrics > Title, 비교 항목은 Name에서 편집합니다.

| 내부 프리팹 | 컴포넌트 |
| --- | --- |
| GraphVerticalBar / GraphHorizontalBar | WorkBarGraphView |
| GraphDots | WorkDotsGraphView |
| GraphGrade — 숫자도 공용 | WorkGradeGraphView |

각 Items 리스트는 CandidateA/B/C 순서입니다.

- 공통 Root → CandidateA/B/C 객체, Name Text → 각 CandidateName.
- 막대: Value Text → ValueLabel, Bar → BarSlider.
- 등급/숫자: Value Text → ValueLabel.
- 원형 별점: Dots → 각 Candidate 바로 아래 Dot1~Dot5 Image, 왼쪽부터 순서대로.

Slider 방향은 프리팹 설정을 유지합니다. 범위는 0~DisplayMax, 값은 후보의 원래 숫자로 설정합니다.
원은 실제 값만큼 Filled Color를 쓰고 나머지는 Empty Color입니다. 배치나 크기는 건드리지 않습니다.
등급은 D/C/B/A/S, 숫자는 단위 포함 문자열을 표시합니다. 원형 별점은 별 문자로 바꾸지 않습니다.
현재 템플릿과 자동 연결은 3개 후보 기준이므로 WorkConfigSO의 ChoiceCount는 3으로 사용하세요.

## 입력 흐름

종이 클릭 → System 선택 → 문서 정보와 그래프 갱신 → 해당 문서 표시
→ 버튼 누르기 → WorkPresenter.StartChoice → System.HoldProgress → 해당 Fill 갱신
→ 제출 완료 → 상세 닫기 → 책상 종이 재사용 / HUD 처리 수 갱신.

열린 서류의 기준 데이터는 WorkRoot.Presenter.OpenedPaper입니다.
그리기와 입력은 View, 판정·누르기 시간은 System에서 담당합니다.
