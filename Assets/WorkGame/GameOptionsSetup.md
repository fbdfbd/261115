# 옵션 패널

1. `Work Game > UI > Game Options Tool`에서 프리팹을 생성합니다. 기본 크기는 중앙 620×700입니다.
2. 씬의 최상단 Canvas 아래에 배치합니다. 업무/육성 전환용 CanvasGroup 안에는 넣지 않습니다.
3. 생성된 `GameOptionsRoot`의 `Work Root`에 게임의 WorkRoot를, `Open Buttons`에 옵션을 여는 버튼들을 연결합니다.
   공용 정지 메뉴(GamePauseRoot)를 함께 쓰면 정지 버튼은 Open Buttons에서 뺍니다. 정지 메뉴의 설정 버튼이 옵션을 엽니다.
   공용 정지 메뉴를 쓰지 않고 옵션을 바로 여는 별도 버튼만 이 목록에 연결합니다.
4. 프리팹 루트는 **활성화 상태**로 둡니다. 실행 시 CanvasGroup으로 숨기고, 버튼/바깥 배경 클릭으로 열고 닫습니다.

View와 Root, 각 버튼·슬라이더 참조는 툴이 연결합니다. OnClick을 추가로 등록할 필요는 없습니다.
업무 중 옵션을 열면 업무 시간이 멈추고 닫으면 재개됩니다. 크레딧의 배경/닫기는 크레딧만 닫습니다.

## 편집

- 옵션 `Page`의 RectTransform 크기를 변경할 수 있습니다. 중앙 앵커/피벗을 사용합니다.
- `Page/Body`의 Vertical Layout Group에서 간격과 정렬을 조절합니다. 각 행의 RectTransform Height로 높이를 조절합니다.
- 행에는 LayoutElement를 붙이지 않았습니다. 크레딧 본문만 긴 글을 위해 ScrollRect와 ContentSizeFitter를 사용합니다.
- 크레딧은 툴의 내용 칸 또는 생성한 `CreditsPanel/Page/Scroll/Content`의 TMP 텍스트를 수정합니다.
  편집 중 확인할 때는 CreditsPanel의 CanvasGroup Alpha를 1로 올립니다. 런타임에는 View가 숨깁니다.

## 시스템

- 지원 해상도를 Screen.resolutions에서 받아 중복 주사율을 제거하고 현재 창 크기도 포함합니다.
- 해상도 목록의 `Dropdown/Template` ScrollRect에서 Scroll Sensitivity를 조절합니다. 기본값은 35입니다.
- 볼륨 FillArea는 배경 전체 폭과 일치합니다. HandleArea만 핸들 반지름인 좌우 12만큼 안쪽에 둡니다.
- 창 모드 ON은 일반 창, OFF는 전체 화면 창입니다. PC에서 실제 화면에 적용합니다.
- 웹/모바일은 해상도·창 모드 조작을 비활성화하고 기기 설정을 사용합니다.
- 배경음/효과음은 각각 0~1로 관리합니다. 아직 오디오 재생기는 연결하지 않았습니다.
  나중에 Root의 `On Music Volume`, `On Sound Volume` 이벤트에 AudioManager 등을 연결합니다.
  연결 시 현재 저장값을 한 번 보내고, 슬라이더 변경 때마다 새 값을 보냅니다.
- 설정은 `WorkGame.Options` 키로 게임 슬롯과 별도 저장합니다. 창을 닫거나 앱 포커스를 잃을 때 저장합니다.
  슬라이더를 움직이는 매 프레임에는 디스크에 저장하지 않습니다.
- 타이틀 버튼은 **기존 루프 시작 체크포인트**를 자동 슬롯에 다시 저장한 뒤 `01_Title`로 이동합니다.
  업무/육성 도중 상태를 새 체크포인트로 캡처하지 않습니다. 저장 실패 시 이동하지 않습니다.
- 실제 타이틀 씬 이름은 `01_Title`입니다. EditorBuildSettings에 추가했으며 기존 게임 씬 순서는 유지했습니다.

컴파일·플레이·테스트 실행은 하지 않았습니다. 기존 저장 테스트에는 타이틀 복귀용 SaveAuto가 시작 상태를 유지하는 케이스를 추가했습니다.
