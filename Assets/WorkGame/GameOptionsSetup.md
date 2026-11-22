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

- 해상도는 1280×720 / 1600×900 / 1920×1080 / 2560×1440 중 현재 모니터 크기에 들어가는 항목만 표시합니다. 기본값은 1920×1080입니다.
- 2560×1440은 현재 모니터가 해당 크기를 지원할 때만 나옵니다. 옵션을 열 때 모니터를 다시 확인하며 지원하지 않는 저장 해상도는 가능한 값으로 보정합니다.
- 작은 모니터에 기본 항목이 하나도 들어가지 않으면 화면 안에 들어가는 16:9 해상도를 사용합니다.
- Player Settings의 Resizable Window는 꺼져 있습니다. 창 가장자리를 드래그해 크기를 바꾸지 못하며 기본 시작 크기는 1920×1080입니다.
- 해상도 목록의 `Dropdown/Template` ScrollRect에서 Scroll Sensitivity를 조절합니다. 기본값은 35입니다.
- 볼륨 FillArea는 배경 전체 폭과 일치합니다. HandleArea만 핸들 반지름인 좌우 12만큼 안쪽에 둡니다.
- 창 모드 ON은 일반 창, OFF는 전체 화면 창입니다. PC에서 실제 화면에 적용합니다.
- 웹/모바일은 해상도·창 모드 조작을 비활성화하고 기기 설정을 사용합니다.
- 배경음/효과음은 각각 0~1로 관리합니다. BGM 재생기 연결은 [GameAudioSetup.md](GameAudioSetup.md)를 따릅니다.
  GameBgmSceneRoot가 Options 이벤트를 구독해 저장 음량과 슬라이더 변경을 반영합니다.
  이 구성을 쓸 때 Root의 `On Music Volume`, `On Sound Volume`에는 중복 연결하지 않습니다.
- 설정은 `WorkGame.Options` 키로 게임 슬롯과 별도 저장합니다. 창을 닫거나 앱 포커스를 잃을 때 저장합니다.
  슬라이더를 움직이는 매 프레임에는 디스크에 저장하지 않습니다.
- 타이틀 버튼은 **기존 루프 시작 체크포인트**를 자동 슬롯에 다시 저장한 뒤 `01_Title`로 이동합니다.
  업무/육성 도중 상태를 새 체크포인트로 캡처하지 않습니다. 저장 실패 시 이동하지 않습니다.
- 실제 타이틀 씬 이름은 `01_Title`입니다. EditorBuildSettings에 추가했으며 기존 게임 씬 순서는 유지했습니다.

## Game 씬 16:9 고정

02_Game의 Main Camera에 GameAspectView를 연결했습니다. 카메라 표시 영역을 중앙의 16:9로 맞추고 남는 영역은 검은 UI로 덮습니다.
여백은 클릭을 차단하며 대각선 전환이나 컷씬 중에도 검은색을 유지합니다. 컴포넌트 비활성화 시 원래 카메라 영역으로 복원합니다.
기존 배경·가구 위치는 변경하지 않았습니다. Game 씬의 카메라용 CanvasScaler 12개는 기존 1920×1080 / Expand 설정을 그대로 사용합니다.
전체화면 창도 16:9 해상도로 요청합니다. Unity의 전체화면 창은 표시 비율이 다르면 자체 레터박스를 적용합니다.
참고: [FullScreenWindow](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/fullscreenmode/fullscreenwindow), [Camera.rect](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/camera/rect).

Unity CLI 없이 별도 C# 컴파일을 확인했습니다. 모니터별 옵션·저장 해상도 보정·카메라 영역 계산 247개 검사를 통과했습니다. 실제 빌드의 창 동작은 에디터 밖에서 확인해야 합니다.
