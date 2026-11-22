# 대각선 화면 전환

프리팹: `Assets/WorkGame/Prefab/DiagonalTransition.prefab`

씬 루트에 독립적으로 배치합니다. 자체 Screen Space - Overlay Canvas가 있으며
기본 Sorting Order는 30000입니다. Hierarchy의 맨 위라는 뜻이 아니라,
다른 UI Canvas보다 높은 Sorting Order로 그립니다. 부모 Canvas 아래에 넣지 않는 것을 권장합니다.
씬 배치는 수동이며, WorkFlowRoot의 Report Transition 필드로 정산서/Care 흐름에 연결합니다.

## Inspector

- Background Color: 완전히 가리는 배경색. 투명도는 항상 1로 적용합니다.
- Pattern Sprite / Color: 기존 Pattern_1을 기본 연결했습니다. 색상과 알파로 패턴 강도를 조절합니다.
  패턴은 화면 전체 UV에 고정되어 띠가 늘어날 때 찌그러지지 않습니다.
- Stripe Count / Angle: 띠 개수 및 기울기. 양수는 위쪽이 오른쪽으로 기울어집니다.
- Pop Stripes: 기본 켜짐. 띠의 영역은 고정하고 알파가 0→100%로 짧게 나타납니다.
  퇴장 시에는 100→0%로 사라집니다. 알파 전환 시간은 Cover Stripe Seconds / Stripe Seconds입니다.
  띠 사이 간격은 Stripe Delay로 조절하며, 끄면 기존의 부드러운 채움으로 돌아갑니다.
  아이콘의 등장/퇴장 모션과 전체 재생 시간은 유지합니다.
- Cover Stripe Seconds: 덮을 때 각 띠의 채움 시간. 기본값 0.14초.
- Stripe Seconds: 걷힐 때 각 띠의 시간. 기본값 0.32초.
- Stripe Delay: 왼쪽부터 띠가 시작하는 시간 간격. 기본값 0.045초.
- Hold Seconds: 전체 재생에서 완전히 가린 뒤 유지하는 시간.
  기본값 0.43초로, 빠르게 채워지는 만큼 대기를 늘려 전체 재생 길이를 유지합니다.
- Dot Sprite: 나중에 사용할 아이콘 Sprite를 연결합니다. 비어 있으면 도트를 그리지 않습니다.
- Dot Color / Spacing / Size / Seconds / Rise: 색상, 간격, 크기, 등장 시간, 올라오는 거리.
  아이콘은 줄마다 살짝 엇갈리게 배치됩니다.
- Play On Enable: 활성화될 때 전체 재생. 기본값은 꺼짐.
- Block Input: 연출 및 가림 유지 중 뒤쪽 uGUI 포인터 입력 차단.
  게임 코드에서 직접 읽는 키보드/컨트롤러 입력은 호출부에서 제어합니다.

배경/패턴 메시와 도트 Image는 실행 시 자식으로 생성되며 재사용됩니다.
시작 상태는 투명하고 입력을 가로채지 않습니다. Time.timeScale이 0이어도 재생됩니다.

## 호출 / 이벤트

- `Play()`: 왼쪽부터 덮기 → On Covered → 유지 → 왼쪽부터 걷기 → On Completed.
- `Cover()`: 덮기 → On Covered. 가려진 상태로 유지합니다.
- `Reveal()`: 완전히 가린 상태에서 걷기 → On Completed.
- `HideImmediate()`: 진행을 취소하고 즉시 투명하게 만듭니다. 완료 이벤트는 호출하지 않습니다.
- 재생 중 새 호출은 이전 재생을 취소하고 새로 시작합니다. 비활성화 시에도 취소됩니다.
- `IsPlaying`, `IsCovered`로 상태를 확인할 수 있습니다.
- Inspector의 On Covered/On Completed 또는 코드의 `Covered`/`Completed`에 연결합니다.

예: 일반적인 화면 교체는 On Covered에서 배경 UI를 교체하고, 걷힌 화면을 보여줍니다.
비동기 로딩에는 Cover → 로딩 완료 → Reveal 순서로 호출합니다.

## WorkMonthReport → Care 연결

1. DiagonalTransition 프리팹을 씬 루트에 배치하고 활성 상태로 둡니다.
2. WorkRoot 오브젝트의 WorkFlowRoot 컴포넌트에서 **Report Transition**에
   씬에 배치한 DiagonalTransitionView를 드래그합니다. 프로젝트의 프리팹 에셋이 아닌 씬 인스턴스를 연결합니다.
3. Play On Enable은 끈 상태로 둡니다. 도트 이미지는 Dot Sprite에 지정합니다.

이벤트를 Inspector에서 추가로 연결할 필요는 없습니다.
정산서 페이드아웃 완료 → 대각선으로 덮기 → 완전히 가려진 순간 Care 화면으로 교체
→ 유지 → 띠가 걷히면서 Care 화면 표시 순서입니다.
전환 중에는 강화 및 다음 달 진입을 차단합니다.
필드가 비어 있거나 전환 오브젝트가 비활성 상태이면 기존처럼 즉시 Care로 넘어갑니다.
진행 중 전환 오브젝트를 비활성화하면 대기를 끝내고 Care 진입을 이어갑니다.
새 게임 바인딩이나 WorkFlowRoot 비활성화 시 대기와 전환을 취소합니다.
