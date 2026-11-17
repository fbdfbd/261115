# 달이 엔딩 편지

## 생성과 연결

1. `Work Game > UI > Care Ending Tool`에서 프리팹을 생성합니다. 기본 Page는 1300×800입니다.
2. 씬 Canvas 아래 배치하고 GameObject는 켜 둡니다. CanvasGroup으로 표시를 제어합니다.
3. WorkRoot의 `Care Ending View`에 씬의 CareEndingView를 연결합니다. Presenter 컴포넌트는 필요 없습니다.
4. CareEndingView의 `On Title Requested`에 프로젝트의 타이틀 이동 함수를 연결합니다. 씬 이름을 코드에 고정하지 않았습니다.

엔딩은 별도 Canvas 또는 업무·육성 전환 그룹 **바깥**에 배치하고 가장 앞에 표시하세요.
WorkFlowRoot의 Work Groups / After Work Groups 안에 넣으면 엔딩 진입 시 부모가 숨겨집니다.
기존 On Run Finished는 논리적인 게임 종료 시점입니다. 그 이벤트에 타이틀 이동을 연결했다면 엔딩 View의 On Title Requested로 옮겨야 편지를 읽을 수 있습니다.

12월 육성 결과를 확인하고 엔딩이 판정되면 자동으로 열립니다. 기존 판정 조건·친밀도 분기는 유지합니다.

## 데이터

각 `CareEndingSO`의 Pages 목록에 순서대로 추가합니다.

- Image: 좌측에 표시할 Sprite. 첫 페이지 외에는 비워 두면 이전 이미지를 유지합니다.
- Text: 우측 편지. 줄바꿈을 직접 넣을 수 있고, 긴 문장은 화면 폭에 맞춰 줄이 나뉩니다.
- Append Friendship Text: 친밀도에 맞는 LowText / NormalText / HighText를 마지막 페이지로 덧붙입니다. 기본 켜짐입니다.

Pages를 비우면 기존 Text를 첫 페이지로 사용합니다. 이미지 없이 기존 데이터만으로 글 연출을 볼 수 있습니다.
연출 페이지는 SO에서 관리하고 기존 본문·친밀도 후일담은 CareContent.json을 계속 사용합니다.
build_care_data.py로 다시 가져와도 SO의 Pages / AppendFriendshipText는 보존합니다.

## 연출과 입력

- 기본 0.3초 간격으로 화면상의 한 줄씩 누적 표시합니다. 출력 중 클릭하면 현재 편지를 모두 보여줍니다.
- 출력 완료 후 클릭하면 다음 페이지로 넘어갑니다. 페이지가 바뀌면 글은 위에서 다시 표시합니다.
- 긴 편지는 표시된 줄을 따라 자동으로 스크롤합니다. 직접 드래그해 위쪽 글도 읽을 수 있습니다.
- 다른 이미지가 지정되면 기본 0.3초 동안 나타납니다. 비워 두거나 같은 이미지를 쓰면 유지합니다.
- 마지막 편지를 읽고 클릭하면 타이틀 버튼을 표시합니다. On Completed는 이 시점에 한 번 호출합니다.
- 정지 중에는 클릭·줄 표시·이미지 Tween을 멈춥니다. 외부 모달은 View.SetInputBlocked(bool)로 입력을 막을 수 있습니다.
- 별도 Update는 없습니다. 줄 표시는 DOTween의 간격·콜백으로 진행하며 숨길 때 종료합니다.

## 크기 수정

- Page: 창 크기.
- Page/Body/PictureFrame: 이미지 영역. Picture의 Image는 Preserve Aspect를 켰습니다.
- Page/Body/Letter: 편지 영역. Viewport의 여백, LetterText의 글자 크기·줄 간격을 조절합니다.
- LetterText의 높이는 실행 중 글 길이로 계산하므로 직접 고정하지 않습니다. 폭은 부모 앵커를 따릅니다.
- Title / PageNumber / ContinueHint / TitleButton: 각 RectTransform 앵커로 배치합니다.
- View의 Line Seconds / Image Seconds: 연출 속도.

LayoutGroup / LayoutElement / ContentSizeFitter는 넣지 않았습니다. 프리팹 생성·씬 배치·컴파일·테스트는 실행하지 않았습니다.
