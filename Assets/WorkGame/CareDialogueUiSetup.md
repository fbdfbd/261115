# 대화 UI 생성·연결

## 생성

1. `Work Game > UI > Care Dialogue Tool`에서 폰트·패널·선택지·로그 크기를 지정하고 프리팹을 생성합니다.
2. 씬 Canvas 아래 배치하고 WorkRoot의 Care Dialogue View에 연결합니다.
3. 루트 GameObject는 켜둡니다. 대화·로그 표시 여부는 CanvasGroup으로 제어합니다.
4. 육성 때 숨겨지는 업무 Canvas의 자식으로 두지 않습니다. 육성 Canvas 또는 별도 Canvas에 놓습니다.
5. Canvas의 GraphicRaycaster를 켭니다. 버튼 OnClick과 내부 필드는 자동 연결되므로 수동 추가하지 않습니다.

일반 C# CareDialoguePresenter를 WorkRoot가 만듭니다. 데이터는 기존 CareConfig의 CareDialogue.json을 사용합니다.

## 클릭·정지·나가기

ScreenClick의 투명 Image로 전체 화면 클릭을 받습니다. 매 프레임 입력을 polling하지 않습니다.
본문과 패널의 빈 공간을 눌러도 진행하며 로그·스킵·선택지 버튼 클릭은 대사 진행과 겹치지 않습니다.
선택지를 기다리거나 로그를 보는 동안에는 전체 화면 클릭으로 진행하지 않습니다.
CareDialogueView의 Ignored Click Areas에 정지·나가기 버튼의 RectTransform을 넣습니다.
해당 영역은 프리팹 전체에서 Raycast를 통과시켜 원래 버튼이 입력을 받고 대사는 넘어가지 않습니다.
정지 창을 열 때 WorkRoot.SetCareDialogueBlocked(true), 닫을 때 false를 호출하면 대화 입력도 막습니다.
WorkSystem.IsPaused 중에도 Presenter가 진행을 받지 않습니다. 정지·나가기 창의 Canvas 표시 순서도 대화보다 높게 둡니다.

## 계층·크기·이미지

- DialoguePanel: 하단 중앙 패널. 크기와 아래 위치를 직접 바꿉니다.
- NamePlate/Speaker: 좌상단 화자 이름. Speaker가 비었으면 달이, 나레이션이면 나레이션을 표시합니다.
- Body: 본문 TMP. 타이핑 연출 없이 줄 전체를 표시합니다.
- Toolbar: 로그 / 스킵.
- Choices: 화면 중앙 기준 선택지 3개. 2개 대화도 버튼 크기를 유지하며 중앙 정렬합니다. 선택지 기본 글자는 32입니다.
- LogPanel: 같은 프리팹 내부 로그 모달. 기본 숨김이며 Page는 화면 중앙입니다.
- LogPanel/Page/Scroll/Viewport/Content/Text: 세로 스크롤 로그. 열 때 텍스트 높이를 계산합니다.

LayoutGroup·LayoutElement·ContentSizeFitter 없이 앵커로 배치했습니다.
이름·본문·툴바는 DialoguePanel에 연결돼 있어 패널을 옮기면 함께 움직입니다. 화자 이름 기본 글자는 46입니다.
Choices는 루트의 화면 중앙에 따로 배치합니다. 툴에서 선택지·화자 글자 크기를 본문과 별도로 조절합니다.
본문·로그 TMP의 Raycast Target은 끄고, ScreenClick과 로그 배경의 Raycast Target은 유지합니다.
로그를 편집하려면 LogPanel의 CanvasGroup Alpha를 잠시 1로 바꿉니다. 실행 시 View가 숨깁니다.
이미지는 View의 Image Key Changed 이벤트에 동적 string 인자를 받는 이미지 제어 메서드를 연결합니다.
JSON Lines의 ImageKey가 바뀔 때 전달하며 빈 키는 이전 이미지를 유지합니다. 이미지 자산은 생성하지 않았습니다.

## 로그·스킵

현재 실행의 대사·스킵한 대사·선택한 답변을 월·화자와 함께 기록합니다. 열거나 닫아도 보상을 다시 적용하지 않습니다.
로그는 로그창 닫기 또는 바깥 배경으로 닫습니다. 현재는 메모리 기록이며 새 게임에서 초기화합니다.
세이브 읽음 판정이 연결되기 전에는 스킵이 비활성화됩니다. 판정은 대화 시작 때 고정합니다.
기록 ID는 월별 실행 키가 아니라 DialogueId입니다. 읽은 대화를 다음 달·새 게임에서 재사용할 수 있습니다.

세이브가 생기면 다음 통로에 연결합니다. save는 이후 작성할 저장 서비스 예시입니다.

```csharp
workRoot.SetDialogueReadCheck(save.HasReadDialogue);
workRoot.CareDialogueReadCompleted += save.MarkDialogueRead;
```

ReadCompleted는 선택까지 마쳐 대화가 끝났을 때 발생합니다. 실제 파일 저장은 구현하지 않았습니다.
스킵은 선택지 앞까지 이동하고 선택은 직접 해야 합니다. 선택지가 없는 읽은 대화는 종료합니다.
친밀도·약속은 기존 완료 경로로 한 번만 반영합니다.

실제 프리팹·씬 수정과 컴파일·자동 테스트·플레이 테스트는 하지 않았습니다.
