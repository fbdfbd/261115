# 업무 코어 PoC

## 문서 프리팹 생성기

`Work Game > UI > Document Prefab Tool`에서 생성합니다. 기본 크기는 1300×800입니다.
중간 그래프+하단 버튼 또는 왼쪽 그래프+오른쪽 버튼 배치를 선택해 프리팹 하나를 저장합니다.
기존 한글 TMP 폰트를 기본으로 사용하며 제목/본문 폰트를 따로 지정할 수 있습니다.
루트에 종이색 배경 Image가 있고, 문서번호·우측 정렬한 시행 달·제목·디바이더와 추천 버튼 3개를 포함합니다.

`Body/GraphArea/GraphContent`는 비어 있는 그래프 영역입니다.
상하 배치의 `Body/ChoicesArea/ChoiceHeading`에는 좌측 제목과 우측 누르기 안내가 있습니다.
좌우 배치에서는 오른쪽에 제목 → 버튼 3개 → 누르기 안내 순서로 세로 배치됩니다.
`Body/ChoicesArea/ChoiceButtons/ChoiceA`~`ChoiceC`에는 중앙에 크게 표시하는 `ChoiceName`과
`Fill`이 있습니다. Choice 자체가 Button이며 Fill은 버튼 전체 크기를 덮고
글자 뒤에서 왼쪽부터 오른쪽으로 채워집니다. Fill의 Image.fillAmount에 처리 진행률을 연결합니다.
생성한 틀의 데이터와 홀드 입력은 `Connect Selected Paper` 메뉴로 연결합니다.

글자는 고정 높이와 자동 축소를 사용하지 않고 줄바꿈/선호 높이로 배치합니다.
문서 높이를 늘리면 남은 높이는 GraphContent가 사용합니다.
루트에는 ContentSizeFitter를 사용하지 않으므로 직접 지정한 높이를 유지합니다.
매우 큰 폰트나 긴 내용으로 최소 필요 높이가 커지면 루트 높이도 늘려 주세요.
작은 화면에서 전체 문서를 스크롤하려면 외부 ScrollRect로 감싸서 사용합니다.
`생성 후 씬에도 배치`를 켜면 선택한 Canvas 아래에 넣거나, 선택한 Canvas가 없으면
CanvasScaler가 설정된 미리보기 Canvas를 새로 만듭니다. UI 입력용 EventSystem은 포함하지 않습니다.

## 그래프 내부 프리팹 생성기

`Work Game > UI > Graph Prefab Tool`에서 표현을 고르고 생성합니다.
`기본 5종 한 번에 생성`으로 세로 막대 / 가로 막대 / 원형 별점 / 등급 / 숫자 카드 틀을 저장할 수 있습니다.
숫자 카드는 도입 비용·재고량·에너지·행정절차·불량률·참여 기회·이해충돌·협업의 예시를 선택할 수 있습니다.
일괄 생성은 기존 파일을 덮어쓰지 않고 새 이름으로 저장합니다.

문서 프리팹을 Prefab Mode로 열고 `Body/GraphArea/GraphContent`를 선택합니다.
툴의 GraphContent 칸에 해당 객체를 넣고, 생성한 내부 프리팹을 골라 `GraphContent에 넣기`를 누르세요.
직접 GraphContent 아래로 프리팹을 드래그해도 됩니다. 루트 앵커는 Stretch, 여백은 0입니다.
단독으로 열린 내부 프리팹보다 부모 문서 안에서 크기를 확인하세요.
배치 버튼은 Undo를 지원하고 GraphContent의 최소 높이를 확보합니다. 기존 자식은 삭제하지 않으므로
다른 그래프를 시험할 때는 이전 인스턴스를 직접 빼고 한 종류씩 확인하세요.

문서별 크기는 문서 Prefab Variant를 만든 다음 문서의 크기, GraphContent의 LayoutElement / Min Height,
본문의 가로 배치 비율과 간격을 조절하면 됩니다. 내부 그래프도 별도 Variant로 만들 수 있습니다.
프리팹에는 A/B/C의 예시만 있으며 게임 데이터와의 연결은 나중에 합니다.
각 후보의 `ValueLabel` / `CandidateName`을 수정할 수 있습니다.
세로·가로 막대는 Unity 기본 `BarSlider`입니다. Inspector의 Min Value / Max Value / Value로 조정합니다.
세로는 Bottom To Top, 가로는 Left To Right이며 정수 값, 손잡이 없음, 조작 불가로 설정됩니다.
실제 데이터를 연결할 때 Slider.value와 ValueLabel을 함께 갱신하세요. 예시 라벨은 자동 연동되지 않습니다.
별점은 Unity 기본 `UI/Skin/Knob.psd` 원형 이미지 5개입니다.
원형 별점의 CandidateA/B/C 자체가 HorizontalLayoutGroup입니다.
각 Candidate 바로 아래에 CandidateName과 Dot1~Dot5 이미지만 있습니다.
Candidate의 HorizontalLayoutGroup / Spacing으로 간격을 조절하고,
라벨과 원의 크기는 각 RectTransform의 Width / Height로 직접 조절합니다.
Child Control Size와 Child Force Expand는 꺼져 있고, 별점 쪽에는 LayoutElement가 없습니다.
원의 색상으로 채운 원과 흐린 원을 표시합니다.
내부 프리팹에는 버튼이 없고 문서의 ChoiceA/B/C 버튼을 사용합니다.

Unity 6000.3.23f1. HUD·메뉴·책상 종이·상세 패널 표시를 연결했습니다.
상세 헤더·그래프 값·선택 홀드와 Fill도 연결했습니다. [PaperDetailsSetup.md](PaperDetailsSetup.md)의 메뉴로 필드를 채우세요.
육성과 저장은 아직 구현하지 않았습니다.

## 바로 테스트하기

1. Unity에서 스크립트 컴파일이 끝나면 `Work Game > PoC`를 엽니다.
2. Config에 `Assets/WorkGame/Data/WorkConfig.asset`을 넣습니다. 기본값으로 자동 선택됩니다.
3. `Start / Restart`를 누르고 선택지를 0.5초간 누릅니다.
4. `+120 seconds`로 시간을 넘긴 뒤 `퇴근`, `다음 달`을 누릅니다.

Play 모드나 게임 씬 없이 에디터에서 실행됩니다. 실제 시간 진행은 `Real time`으로 조정합니다.
누르기를 멈추거나 선택지 밖으로 드래그하거나 창의 포커스를 잃으면 누르기가 취소됩니다.
실시간을 끄면 누르기 진행도 멈춥니다. 선택지를 테스트할 때는 실시간을 켜세요.
PoC 창은 숫자·단위, 별점 기호, 등급 문자를 표시합니다. 막대는 숫자와 비율로 확인합니다.
실제 가로·세로 막대를 그리는 게임 View는 나중에 연결합니다.

기본 데이터가 없다면 `Work Game > Create Sample Data`를 실행하세요.
생성기를 다시 실행해도 기존 설정·카테고리·월 계획은 보존됩니다.
샘플 수량, 보너스, 근태 감소량은 테스트용 임시 밸런스입니다.

## 코드 읽는 순서

- `Runtime/Domain/WorkPaper.cs`: 서류 상태와 답안.
- `Runtime/Domain/WorkRules.cs`: 정답과 등급 판정.
- `Runtime/System/WorkSystem.cs`: 월 진행, 누르기, 퇴근, 평가.
- `Runtime/Data/WorkConfigSO.cs`: SO 검사와 새 게임 생성.
- `Runtime/Presenter/WorkRoot.cs`: 씬 참조, 생성, Update와 생명주기 관리.
- `Runtime/Presenter/WorkPresenter.cs`: 일반 C# 클래스. 캔버스별 View와 System 연결.

Domain은 Unity를 사용하지 않습니다. System은 외부에서 시간을 받아 실행되며,
변경 알림은 일반 C# 이벤트를 사용합니다. Editor 코드는 빌드에서 제외됩니다.
SO 설정과 생성된 서류는 실행 시작 시 복사되므로 플레이가 SO 원본을 바꾸지 않습니다.
System 하나는 한 회차용입니다. 재시작할 때 기존 System을 Dispose하고 새로 생성합니다.
향후 업무 스킬은 `SetWorkSpeed(2)`처럼 연결할 수 있습니다. 2배속은 처리 시간만 절반으로 줄이고 월 시간은 바꾸지 않습니다.

## 확정한 진행 규칙

- 1~12월. 월 기본 시간은 120초, 초과근무 제한 없음.
- 이월분을 포함한 남은 서류를 모두 제출하면 120초 전에도 조기퇴근 가능.
- 서류가 남아 있어도 120초부터 퇴근 가능. 자동 퇴근하지 않음.
- 책상에서 미처리 서류 중 하나를 선택해 상세 한 장을 표시합니다. PoC 창은 순차 표시를 유지합니다.
- 누르기 완료 시 한 번 제출. 오답은 근태 감소 후 처리 완료되어 재시도할 수 없음.
- 미처리 서류만 이월. 완료 서류는 평가 기록을 위해 목록에 남음.
- 3·6·9·12월 퇴근 시 평가. 정답률 85% 이상 S, 70% 이상 A, 나머지 B.
- 분모는 그 분기에 새로 배정된 서류 수. 평가 시점까지의 미처리는 오답으로 계산.
- 이전 분기 서류를 나중에 끝내도 지난 평가는 바뀌지 않고 다음 분기 분모에도 들어가지 않음.
- 근태는 별도 기록. 현재 등급 산식은 정답률만 사용.
- 업무가 없는 분기는 B, 보너스 0.
- 퇴근 중 시간은 흐르지 않음. 12월 퇴근 후 Finished 상태.

`Run.LastResult`에 월별 정답/오답 수, 남은 서류 수, 근태, 초과근무 초, 분기 평가가 있습니다.
정답/오답 수는 이번 달 처리량이며, 이전 달 이월 서류의 처리도 포함합니다.
`Run.Bonus`는 누적 분기 보너스입니다. 향후 육성 파트는 월 결과를 받은 뒤
`StartNextMonth()`를 호출하면 됩니다. 육성 디메리트 자체는 아직 적용하지 않습니다.

## UI 연결

씬 구성과 모든 직렬화 필드 연결은 [SceneSetup.md](SceneSetup.md)를 확인하세요.
씬에 WorkRoot 하나와 WorkMenuView / WorkHudView / WorkDeskView / WorkPaperView를 연결합니다.
WorkRoot가 일반 C# 클래스인 WorkPresenter를 생성합니다. Presenter에는 MonoBehaviour, SerializeField, Update가 없습니다.
버튼 리스너와 System.Changed 구독은 Presenter와 View가 관리합니다.
HUD 카테고리와 종이는 월 또는 업무 상태가 바뀔 때만 갱신합니다.
종이는 씬의 Paper Views 목록을 재사용합니다. 연결한 수만큼 겹침을 허용해 배치하고, 표시 수를 넘는 서류는
대기하다가 완료한 자리에 들어갑니다. 처리 시 다른 종이의 위치는 유지됩니다.
DOTween 호버는 짧은 회전 Tween 하나를 반복 사용하고 비활성화/반환 시 정리합니다.
월 진행도는 MonthDone/MonthTotal, 선택한 상세는 WorkRoot.Presenter.OpenedPaper로 확인할 수 있습니다.

## SO 편집과 CSV

`Work Game > Data Editor`:

- Settings: 월 시간, 누르기 시간, 근태 감소, 등급 기준과 보너스.
- Categories: 카테고리와 비교 지표를 한 창에서 편집.
- 카테고리의 `Title`은 기존 정책명, `ShortName`은 화면용 짧은 이름입니다.
  서류에서는 `CategoryName`과 `CategoryShortName`으로 읽습니다. 짧은 이름이 비어 있으면 기존 정책명을 표시합니다.
- Month Plan: 월별 배정 목록과 월 범위/카테고리별 수량 일괄 수정.
- CSV Import: 파일 읽기 → 검사와 미리보기 → Apply Preview.

카테고리는 월 계획에 처음 배정되는 달에 해금됩니다. Count 0은 배정하지 않는 행입니다.
같은 월/카테고리는 한 행으로 관리하세요. 지표는 서류마다 무작위로 선택합니다.
선택지 숫자는 중복 없이 생성하므로 정답은 항상 하나입니다. 동일 Seed면 같은 문제를 생성합니다.

게임 씬의 WorkRoot는 기본적으로 매 실행마다 새 시드를 사용합니다. 같은 업무를 재현하려면
Use Fixed Seed를 켜고 Seed를 지정하세요. 월별 카테고리와 서류 수는 WorkPlanSO에 고정하며,
각 서류의 항목은 해당 카테고리의 Metrics 풀에서 무작위로 뽑습니다. 같은 달의 신규 배정 안에서는
항목이 중복되지 않으며 다음 달에는 다시 나올 수 있습니다. 이월 서류는 기존 항목을 유지합니다.
월별 배정 수가 카테고리 풀 크기보다 많으면 데이터 오류로 알려줍니다.

Metrics의 Title은 상황 제목, Name은 비교 항목, Unit은 단위입니다. CSV에서는 카테고리 Title과
구분하기 위해 상황 제목을 PaperTitle 열로 저장합니다. 기존 CSV도 PaperTitle 없이 가져올 수 있습니다.

`Samples/Categories.csv`는 **15개 카테고리 / 카테고리당 10개 / 총 150개 항목의 기본 설정 원본**입니다.
생성기도 이 CSV를 읽으므로 표시 방식과 범위를 두 곳에서 관리하지 않습니다.
카테고리 CSV 전체를 적용하면 모든 기본 카테고리의 지표를 갱신합니다.
일부 항목만 갱신하려면 해당 카테고리의 지표 행만 남긴 CSV를 사용하세요.
`Samples/Months.csv`는 일부 월 배정을 갱신하는 예제입니다.
카테고리 CSV는 ID가 같은 SO의 지표 전체를 교체하고 SO 참조는 유지합니다.
카테고리 CSV는 `Title` 다음에 `ShortName` 열을 넣습니다. 기존 형식의 CSV도 지원하며,
`ShortName` 열이 없으면 기존 짧은 이름을 유지합니다.
월 CSV는 월+카테고리가 같은 행의 수량만 바꾸며, 없는 행은 추가합니다.
CSV에 없는 카테고리와 배정 행은 삭제하지 않습니다. 값 0으로 신규 배정을 끌 수 있습니다.
CSV 적용과 Inspector 편집, 수량 일괄 수정은 Undo를 지원합니다.

Rule: `Highest`, `Lowest`.
Display: `Number`, `VerticalBar`, `HorizontalBar`, `Stars`, `Grade`.
별점/등급의 표시 스케일도 Min/Max로 설정하세요. 예를 들어 별 1~5개는 1,5.
별점과 등급은 1~5 범위만 허용합니다. 등급은 1=D, 2=C, 3=B, 4=A, 5=S입니다.
`Min`/`Max`는 문제에서 생성할 숫자 범위입니다. `ScaleMax`는 막대가 꽉 차는 값이며,
0이면 `Max`를 사용합니다. 막대는 0부터 시작하고 `ScaleMax`는 `Max` 이상이어야 합니다.
예: 생성 범위 10~90, ScaleMax 100 → 값 90은 막대의 90% 길이입니다.
별점/등급의 숫자 비교 방향은 그대로 유지됩니다. Lowest인 등급 문제는 D가 가장 낮습니다.
게임 UI는 `paper.GetChoiceText(index)`로 숫자+단위/별점/등급을,
`paper.GetBarFill(index)`로 0~1 막대 비율을 읽으면 됩니다. `DisplayMax`도 함께 전달됩니다.
CSV의 마지막 `ScaleMax` 열은 생략 가능합니다. 기존 형식은 0으로 가져옵니다.
현재 수치와 별점·등급은 게임용 초기 설정이며 실제 조달의 평가 점수를 의미하지 않습니다.
Min~Max에는 선택지 수 이상의 서로 다른 정수가 들어갈 수 있어야 합니다.
카테고리 CSV에 포함한 각 카테고리는 유지할 지표를 모두 작성하세요.

## 검증

Unity Test Runner의 EditMode에서 `WorkGame.Tests`를 실행합니다.
시간 경계, 누르기 취소·중복 제출, 이월·분기 집계, 등급 경계, 12월 종료,
SO 원본 보존, 동일 Seed, CSV 미리보기·검증·정상 적용을 검사합니다.

`Tools/WorkGameCheck`는 Unity 실행이 어려울 때 같은 순수 코어 테스트를 실행하는 보조 도구입니다.
SO 및 에디터 테스트는 Unity Test Runner가 필요합니다. 실제 검증 결과는 `Logs/work-tests.xml`에 있습니다.
