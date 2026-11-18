# 공용 정지 메뉴

## 씬에서 할 일

1. `Work Game > UI > Game Pause Tool`에서 프리팹을 생성합니다. 기본 정지창은 중앙 460×760입니다.
2. 가장 위에 그려지는 공용 UI Canvas에 배치합니다. 업무/육성 전환 그룹 바깥에 둡니다.
3. `GamePauseRoot`의 **Work Root**, **Options Root**, **Save Slots Root**에 기존 씬 객체를 연결합니다.
   현재 `02_Game`의 Save Slots Root 참조는 코드 작업과 함께 연결했습니다.
4. 기존 `GameOptionsRoot > Open Buttons`에 공용 정지 버튼을 넣었다면 **제거합니다**. 이제 정지 버튼은 정지 메뉴를 엽니다.
5. 두 프리팹 루트는 활성화 상태로 둡니다. 내부 CanvasGroup으로 숨깁니다.

버튼 OnClick을 추가로 등록할 필요는 없습니다. GamePauseRoot가 WorkRoot.MenuView의 공용 정지·도움말 버튼 이벤트를 구독합니다.
WorkMenuView의 예전 Pause Panel / Resume Button / Help Panel 필드에는 이 프리팹을 넣지 않습니다.

현재 `02_Game`의 공용 버튼은 Canvas_HUD_Default에 있어 전환 그룹 바깥입니다.
WorkMenuView의 Help Button이 정지 버튼을 가리키고 있던 참조를 실제 HelpButton으로 수정했습니다.

## 동작

- 정지 버튼: 업무·육성 양쪽에서 같은 정지 메뉴를 엽니다.
- 이어하기 / 정지창 바깥 배경: 정지 메뉴를 닫고 재개합니다.
- 버튼 순서: 이어하기 → 저장하기 → 불러오기 → 설정 → 타이틀로 돌아가기.
- 저장하기 / 불러오기: 같은 슬롯 패널을 저장 모드 / 불러오기 모드로 엽니다.
  패널을 취소하면 정지 메뉴로 돌아옵니다. 불러오기에 성공하면 복원된 시작 상태에서 재개합니다.
  On Save Requested / On Load Requested 이벤트는 추가 연출용이며, 기본 패널 연결을 다시 등록할 필요는 없습니다.
- 설정: 기존 GameOptionsRoot를 엽니다. 설정을 닫으면 정지 메뉴로 돌아오며, 자동으로 재개하지 않습니다.
- 타이틀로 돌아가기: 기존 루프 시작 체크포인트와 옵션을 저장한 뒤 모든 플랫폼에서 `01_Title`로 돌아갑니다.
  저장 실패 시 이동하지 않고 오류를 표시합니다. 앱 종료나 에디터 Play 중지는 하지 않습니다.
- 도움말 버튼: 같은 프리팹의 별도 도움말 패널을 엽니다. 도움말을 닫으면 이전 정지 상태로 돌아갑니다.

업무 시간·홀드·팩스는 정지하며, 육성의 수업 편성·구매·대화 선택·다음 달 진행도 막습니다.
UI의 글자/등장 애니메이션은 계속 재생될 수 있습니다. Time.timeScale을 변경하지 않습니다.

## 편집

- `PausePanel/Page`: 정지창 크기. 중앙 앵커/피벗입니다.
- `PausePanel/Page/Body`: Vertical Layout Group에서 버튼 간격 조절. 각 버튼 Height는 64입니다.
- TitleGap은 타이틀 버튼 위에 추가 간격을 주는 빈 RectTransform입니다.
- `HelpPanel/Page/Scroll/Content`: 도움말 본문 TMP 텍스트. 툴에서도 내용을 입력할 수 있습니다.
- 도움말을 프리팹 편집 중 보려면 HelpPanel의 CanvasGroup Alpha를 1로 올립니다. 실행 시에는 자동으로 숨깁니다.
- Options Root는 정지 프리팹과 같은 Canvas에 두면 설정을 열 때 앞쪽으로 이동합니다.
  Canvas를 나눈 경우 옵션 Canvas의 Sorting Order를 정지 메뉴보다 높게 두세요.

컴파일·플레이·테스트 실행은 하지 않았습니다.
