# 업무 화면 씬 연결

씬과 프리팹 배치는 직접 작업합니다. 이번 코드는 HUD·버튼·책상 위 종이·상세 패널 열기/닫기를 연결합니다.
상세 정보·그래프·선택 버튼·Fill은 [PaperDetailsSetup.md](PaperDetailsSetup.md)의 자동 연결 메뉴를 사용합니다.

## 1. 씬 구조

예시입니다. 캔버스의 이름과 분리는 기존 작업대로 유지해도 됩니다.

```text
GameRoot                         WorkRoot
MenuCanvas                       WorkMenuView
  LeaveArea                      CanvasGroup
    LeaveButton                  Button
  PauseButton                    Button
  PausePanel                     배경 Image + 재개 Button
  HelpButton / HelpPanel         참조만 준비
HudCanvas                        WorkHudView
  MonthText                      TMP
  CategoryItems                  미리 배치한 카테고리 표시 객체들
  ProgressText                   TMP
  ProgressSlider                 Slider
DeskCanvas                       WorkDeskView
  LooseRoot                      일반 모드 배치 영역
  SortedRoot                     정리 모드 자리
PaperCanvas
  BlackBackground                Image + Button + CanvasGroup + WorkPaperView
    DotsPanel                    문서 전체
    GradePanel                   문서 전체, 숫자 카드 공용
    HorizontalPanel              문서 전체
    VerticalPanel                문서 전체
EventSystem
```

- Canvas에는 GraphicRaycaster가 있어야 합니다.
- CanvasScaler의 기준 해상도는 캔버스끼리 일관되게 맞추세요.
- 새 Input System을 쓰면 EventSystem에 InputSystemUIInputModule을 사용합니다.
- View 컴포넌트와 GameRoot는 활성 상태로 두세요. 숨김은 CanvasGroup 또는 자식 패널로 처리합니다.
- PaperCanvas는 DeskCanvas보다 위에, PausePanel은 문서보다 위에 보이게 Sorting Order를 설정하세요.

## 2. GameRoot — WorkRoot

씬에 붙이는 컴포넌트는 WorkRoot입니다. WorkPresenter는 일반 C# 클래스라 붙이지 않습니다.
WorkRoot가 System과 Presenter를 생성하고 시간 전달, 활성/비활성 연결, 종료 시 정리를 담당합니다.
WorkPresenter는 생성자로 System/View를 받아 입력과 화면 상태만 연결합니다.

| Inspector 필드 | 연결할 것 |
| --- | --- |
| Config | Data/WorkConfig.asset |
| Seed | 같은 문제 구성을 재현할 숫자 |
| Menu View | MenuCanvas의 WorkMenuView |
| Hud View | HudCanvas의 WorkHudView |
| Desk View | DeskCanvas의 WorkDeskView |
| Paper View | PaperCanvas의 WorkPaperView |

Play 시작 시 새 회차를 생성합니다. WorkRoot는 게임 화면에 하나만 둡니다.
기존 컴포넌트의 스크립트 GUID와 직렬화 필드 이름은 WorkRoot로 이어서 씬 참조를 유지합니다.
아래 View에 연결한 버튼의 On Click에는 Presenter 메서드를 중복 등록하지 마세요. 코드가 자동으로 연결합니다.

## 3. WorkMenuView

| 필드 | 연결할 것 |
| --- | --- |
| Leave Button | 퇴근 버튼 |
| Leave Group | 퇴근 버튼만 감싼 CanvasGroup |
| Pause Button | 정지 버튼 |
| Pause Panel | 정지 화면 |
| Resume Button | 정지 화면의 재개 버튼 |
| Help Button / Help Panel | 도움말 참조. 동작과 내용은 미구현 |

Show Leave / Show Pause / Show Help로 각 버튼 표시를 끌 수 있습니다.
퇴근 가능 전에는 Leave Group이 alpha 0, Interactable 및 Blocks Raycasts가 꺼집니다.
업무 전부 제출 또는 120초 경과 시 표시합니다. 정지 중에는 업무 시간과 홀드가 멈춥니다.
전체 MenuCanvas에 Leave Group을 걸면 정지 버튼도 같이 숨으므로 퇴근 영역만 연결하세요.
PausePanel은 뒤의 입력을 막을 수 있는 Image를 배경으로 두세요.

## 4. WorkHudView와 카테고리 프리팹

| 필드 | 연결할 것 |
| --- | --- |
| Month Text | `1월` 등을 표시할 TMP |
| Category Slots | 씬에 미리 배치한 WorkCategoryView 목록 |
| Progress Text | `0/0`을 표시할 TMP |
| Progress Slider | 처리 진행도 Slider |

카테고리 프리팹은 두 계층이면 됩니다.

```text
CategoryItem       Image + WorkCategoryView
  Name             TMP
```

WorkCategoryView의 Background에는 루트 Image, Name Text에는 자식 TMP를 넣습니다.
카테고리 이름은 ShortName, 배경색은 카테고리 SO의 Hud Color를 사용합니다.
카테고리 표시 객체를 씬에 미리 배치하고 각 WorkCategoryView를 Category Slots에 순서대로 넣습니다.
생성/삭제 없이 필요한 객체만 켜고 나머지는 끕니다. 부모 레이아웃은 원하는 대로 직접 조정하세요.
이번 달에 새로 배정된 업무의 카테고리만 중복 없이 표시합니다. 이월 서류의 카테고리는 추가하지 않습니다.
모두 처리해도 그달 표시는 유지합니다. 표시 칸이 부족하면 경고가 나오므로 충분한 수를 연결하세요.
현재 월별 카테고리 수에는 3개 제한이 없습니다. 기본 샘플은 후반에 최대 15개를 배정합니다.

진행도는 `이번 달 제출 수 / 월 시작 시 이월분 포함 전체 수`입니다. 오답 제출도 처리 수에 포함합니다.
분기 정답률과는 다릅니다. Slider는 0~1, 정수 옵션 꺼짐, 조작 불가로 맞춥니다.
진행 애니메이션은 WorkHudView.SetProgress() 한 곳에 추가하면 됩니다.

## 5. WorkDeskView와 재사용할 종이 목록

| 필드 | 연결할 것 |
| --- | --- |
| Loose Root | 종이를 흩뿌릴 RectTransform |
| Sorted Root | 정리 모드용 RectTransform. 현재는 비워도 됨 |
| Paper Views | 씬에 미리 배치한 WorkPaperItemView 목록 |
| Max Papers | 동시에 표시할 종이 수. 기본 8. 리스트 전체 개수와 별개 |
| Spread Padding | 배치 영역 안쪽 여백 |
| Paper Gap | 종이 사이 최소 여유 |
| Jitter | 칸 안에서 위치가 흔들리는 범위 |
| Overlap | 허용할 겹침 정도. 후보 중 서로 덜 가리는 위치를 우선 선택 |
| Max Angle | 무작위 회전 최대 각도 |
| Placement Seed | 종이 배치 재현용 숫자 |

처음에는 Loose Mode 켜기, Sorted Mode 끄기로 둡니다. 둘 다 끄면 책상이 숨겨지고,
둘 다 켜면 연결된 Sorted Root를 우선 사용합니다. 정리 모드는 컨테이너 전환 자리만 마련한 상태이며
업그레이드·자동 정리 규칙은 없습니다. 테스트하려면 Sorted Root에 GridLayoutGroup 등을 직접 두면 됩니다.
Loose Root에는 LayoutGroup을 넣지 마세요. 직접 계산한 종이 위치와 충돌합니다.
작은 종이 객체를 씬에 여러 개 배치하고 각각 WorkPaperItemView를 붙인 뒤 Paper Views에 연결하세요.
Stretch가 아닌 적절한 고정 Width/Height로 만들고 배치 영역보다 작게 두세요.
코드는 객체를 생성하거나 파괴하지 않습니다. 사용하지 않는 객체는 끄고, 처리한 자리에 다음 서류를 연결합니다.
실제로 표시할 장수만 계산에 사용합니다. 각 종이마다 후보 12개 중 덜 겹치는 위치를 고르고,
초기 배치 후 두 번 보정해 초반의 나쁜 위치 때문에 한곳에 몰리지 않게 합니다.
위치와 회전은 무작위이며, 반복 횟수는 고정입니다. 매 프레임 배치 계산을 하지 않습니다.
처리할 때마다 다른 종이의 위치를 바꾸지 않습니다. 화면 크기나 배치 설정 변경 시에만 칸을 다시 계산합니다.

표시 슬롯 수 = min(Max Papers, 연결한 종이 수, 이번 달 전체 업무 수)입니다.
실제로는 남은 업무 수까지만 표시하며 처리할 때 나머지 종이 위치는 유지합니다.
실행 중 WorkDeskView Inspector의 **연결된 종이 수 / 최대 표시 장수**에서 확인하세요.
넘치는 업무는 대기하고, 한 장을 처리하면 해당 종이 객체를 재사용해 다음 업무를 보여줍니다.
재사용 객체를 많이 연결해도 Max Papers 이상을 한꺼번에 밀어 넣지 않습니다.
영역에 비해 종이 자체가 너무 크면 Max Papers나 종이 Width/Height도 조절하세요.
종이 크기/호버 각도를 실행 중 바꿨다면 Inspector의 배치 다시 계산을 누르세요.

종이 프리팹 루트에 Image, Button, WorkPaperItemView를 붙입니다. RectTransform과 CanvasGroup은 자동으로 붙습니다.

| WorkPaperItemView 필드 | 연결할 것 |
| --- | --- |
| Click Button | 루트 Button. 비워도 같은 객체에서 찾음 |
| Paper Group | 루트 CanvasGroup. 비워도 같은 객체에서 찾음 |
| Hover Outline | 호버 시 켤 UI Outline. 생략 가능 |
| Hover Wiggle | DOTween 호버 꿈틀 사용 여부 |
| Wiggle Angle / Seconds | 기본 4도 / 0.22초. 재생 중 중복 시작하지 않음 |
| On Show | 등장 연출을 연결할 선택 사항 |
| On Complete | 처리 완료 연출을 연결할 선택 사항 |
| Wait For Remove Animation | 연출 연결 전에는 끄기 |

버튼 Target Graphic과 실제 종이 Image를 맞추고 Image의 Raycast Target을 켭니다.
일반 모드의 종이에는 카테고리 텍스트를 넣지 않습니다. 종이를 열기 전에는 카테고리를 알 수 없습니다.
카테고리 구분 표시는 나중에 정리 기능의 컨테이너 쪽에서 구현합니다.
기본 Outline은 꺼두어도 됩니다. 코드가 호버 때 켜고, 빠져나오면 끕니다.
애니메이션을 추가할 때 Wait For Remove Animation을 켜고, 완료 연출 끝에 해당 종이의
WorkPaperItemView.FinishRemove()를 호출하세요. 이 메서드는 파괴하지 않고 비활성화해 풀로 반환합니다.
체크만 하고 호출을 안 하면 그 종이가 완료 연출 상태에 머무르며 재사용되지 않습니다.
배치 후 등장 이벤트를 실행하고, 처리되면 입력을 막은 뒤 완료 이벤트를 실행합니다.
호버는 회전 Tween 하나를 재사용하며, 비활성화·반환·재배치 때 해당 Tween만 종료하고 기본 각도로 복구합니다.

## 6. WorkPaperView — 상세 서류

| 필드 | 연결할 것 |
| --- | --- |
| Paper Group | 최상위 검은 배경의 CanvasGroup |
| Dots Panel | 원형 별점용 문서 전체 GameObject |
| Grade Panel | 등급/숫자 카드용 문서 전체 GameObject |
| Horizontal Panel | 가로 Slider용 문서 전체 GameObject |
| Vertical Panel | 세로 Slider용 문서 전체 GameObject |
| Outside Button | 최상위 검은 배경 자체의 Button |

네 Panel에는 GraphContent 내부만이 아니라 **문서 전체 루트**를 연결합니다.
검은 배경이 네 문서의 부모입니다. 별도의 공통 부모나 닫기 버튼은 만들지 않습니다.
검은 배경의 RectTransform은 화면 전체 Stretch, Image의 Raycast Target은 켜둡니다.
문서 루트에도 Image가 있어야 빈 문서 영역을 클릭할 때 바깥 버튼으로 입력이 통과하지 않습니다.
코드는 선택한 문서 루트의 Graphic이 있다면 Raycast Target을 켭니다.
문서 루트에는 WorkPaperClickBlocker를 자동으로 붙여 자식 문서 클릭이 부모 배경의 닫기로 이어지지 않게 합니다.
Paper Group은 처음 alpha 0으로 두어도 됩니다. 검은 배경 GameObject 자체는 활성 상태로 유지하세요.

종이를 클릭하면 해당 서류의 Display에 맞는 패널 한 개만 표시합니다.
Stars → Dots, Grade/Number → Grade, HorizontalBar → Horizontal, VerticalBar → Vertical입니다.
검은 배경 클릭은 상세를 숨기고 홀드를 취소합니다. 닫기만 하면 종이는 책상에 남습니다.
실제로 제출이 완료되면 상세를 숨기고 해당 책상 종이의 완료 연출/제거를 실행합니다.

## 현재 연결 범위와 확인 순서

1. Play: 월과 카테고리가 나오고, 책상에 종이가 흩어지는지 확인합니다.
2. 서로 다른 종이를 클릭해 대응하는 상세 패널이 열리는지 봅니다.
3. 서류 안을 클릭하면 유지되고, 검은 배경을 클릭하면 닫히며 종이는 사라지지 않는지 확인합니다.
4. 정지/재개 시 Presenter.System.Run.Seconds가 멈추고 다시 증가하는지 확인합니다.
5. 120초가 지나면 퇴근 버튼이 나타나는지 확인합니다.

상세 문서 4종은 PaperDetailsSetup.md에 따라 연결하면 제목·번호·시행 달·지시문과 그래프가 실제 데이터로 갱신됩니다.
선택 버튼의 WorkChoiceView가 입력을 자동 연결하므로 Button.onClick이나 EventTrigger에 홀드를 중복 연결하지 않습니다.

퇴근하면 AfterWork에서 대기합니다. 결과/육성 화면은 아직 없으므로 다음 달까지 시험하려면
임시 다음 달 Button의 On Click에 WorkRoot.StartNextMonth()를 연결하세요.
12월 퇴근 후에는 Finished 상태라 다음 달로 넘어가지 않습니다.
