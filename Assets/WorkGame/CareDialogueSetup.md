# 육성 대화·퇴근·약속 연결

원본은 `Samples/CareDialogue.json`입니다. CareConfig.asset의 Dialogues(TextAsset)에 연결했습니다.
새 게임 시작 시 JSON을 읽습니다. 대사만 고치면 SO를 재생성할 필요가 없습니다.
대화 UI 생성 툴은 CareDialogueUiSetup.md를 참고하세요. 실제 씬·프리팹·이미지는 직접 배치·연결합니다.

## 메인과 패널

WorkRoot.CareFlow는 일반 C# 클래스입니다. Phase는 Working / Report / Dialogue / Free / Panel / Result / Ended입니다.
메인 진행 버튼은 Free 상태이고 편성 4칸이 모두 찼을 때만 활성화합니다.
매달 빈 4칸으로 시작하며 홈스쿨링·휴식도 직접 넣습니다. 편성창을 닫아야 메인에서 진행할 수 있습니다.
수업료는 실행 순간 재검사하며 부족하면 CareBlocked로 알립니다.

| 입력 | WorkRoot 연결 |
| --- | --- |
| 스킬트리 | OpenUpgrades() 또는 OpenCarePanel(1) |
| 스탯·보유 물품 창 | OpenCarePanel(2) |
| 편성창 | OpenCarePanel(3) |
| 선물 상점 | OpenCarePanel(4) |
| 가구함 | OpenCarePanel(5) |
| 패널 닫기·바깥 배경 | CloseCarePanel() |
| 편성 지우기 | ClearCareLesson(slot), 0~3 |
| 수업 지정 | CarePresenter.SetLesson(slot, id) |
| 무료 수업 지정 | SetHomeLesson(slot) / SetRestLesson(slot) |
| 메인 진행 | 기존 WorkFlowRoot.StartNextMonth() 유지 |
| 달이와 대화 | TalkToDali() |
| 다음 대사·선택지 없는 마지막 줄 닫기 | NextCareDialogue() |
| 선택지 | ChooseCareDialogue(index), 0부터 |
| 육성 결과 확인 | ContinueAfterCare() |

OpenCarePanel은 화면을 생성하지 않습니다. 이후 View가 CareFlow.Panel에 맞춰 창을 표시합니다.
기존 스킬트리는 실제 열기·닫기까지 연결했고 원래 닫기 버튼을 유지합니다.
Presenter는 편성 입력을 편성창에서만, 구매를 상점·가구함에서, 물품 교체를 가구함에서만 받습니다.

## View 조회

- `WorkRoot.CareFlowChanged`: 상태·대사·선택·편성 변경 때 갱신합니다. 매 프레임 polling은 필요 없습니다.
- `CareFlow.CanGoNext / CanTalk`: 메인 진행 / 그달 대화 버튼 활성 여부.
- `CareFlow.Dialogue.IsOpen / Current / Line / LineIndex / HasChoices`: 현재 대화 상태.
- `Dialogue.Line.Text / ImageKey`: 대사와 이미지 키. 빈 키는 이전 이미지를 유지합니다.
- `Dialogue.Current.Choices`: 마지막 줄에서 HasChoices일 때 표시할 선택지.
- `CareFlow.Promises.Records`: 약속의 수락·거절·성공·실패 기록.

ImageKey는 View가 이미지로 매핑하는 문자열 하나입니다. 이미지 자산은 아직 연결하지 않았습니다.
기본 키: dali_normal / dali_happy / dali_shy / dali_proud / dali_thinking / dali_sad / dali_sleepy / dali_surprised.
RunStarted에서 새 CareFlow로 연결을 교체하고 화면 해제 시 이벤트 구독을 해제합니다.

## 퇴근 판정

퇴근 순간 `실제 경과 시간 - 강화 반영 업무 제한 시간`을 MonthResult.LeaveDifference에 저장합니다.
퇴근 후 강화해도 지난달 판정은 바뀌지 않습니다. 급여 정산서 확인 뒤 WorkFlowRoot가 EnterCare()를 호출합니다.

| 종류 | 기준 | 일반 반응 친밀도 초기값 |
| --- | --- | ---: |
| Early | 30초 이상 남김 | -1 |
| SlightlyEarly | 10초 초과~30초 미만 남김 | 0 |
| OnTime | 기준의 -10초~+10초 포함 | 0 |
| Overtime | 기준보다 10초 초과 | -2 |

시간은 Leave.EarlySeconds / OnTimeSeconds에서, 친밀도는 각 대화 Friendship에서 조절합니다.
일찍 퇴근도 -1인 것은 합의한 규칙입니다. 대사는 달이의 깜짝 준비가 들킨 상황으로 작성했습니다.
일반 반응은 월 1회이며, 수동 월별 대화도 따로 월 1회 완료 가능합니다.

## 약속

| 제안 | 판정 월 | 조건 | 성공 | 실패 |
| --- | --- | --- | ---: | ---: |
| 3월 육성 결과 뒤 | 4월 | Early, 30초 이상 남김 | +8 | -6 |
| 6월 육성 결과 뒤 | 7월 | NoOvertime, 야근이 아니면 성공 | +8 | -6 |
| 9월 육성 결과 뒤 | 10월 | Early, 30초 이상 남김 | +10 | -8 |

제안의 accept / decline 선택은 모두 변화 0입니다. 수락한 뒤 대상 월에만 판정합니다.
Early 약속은 SlightlyEarly·OnTime도 실패합니다. NoOvertime은 Early·SlightlyEarly·OnTime 모두 성공합니다.
NoOvertime 약속에 일찍 도착하면 EarlySuccessDialogue를 사용합니다. 기본 데이터는 보상은 같고 대사만 달라집니다.
약속 결과는 대상 월 정산 후 육성 진입 시 특수 대화로 표시하고, 일반 반응을 대체해 효과가 중복되지 않습니다.

## JSON과 적용 시점

Dialogues: Id / Lines / Choices / Friendship. Lines: Speaker / Text / ImageKey. Choices: Key / Text / Friendship.
친밀도는 기본 대화 값 + 선택지 값을 합산해 대화 완료 때 한 번만 적용합니다.
선택지가 있으면 마지막 줄에서 Next로 넘길 수 없고 선택해야 합니다.
완료 기록은 entry/월, talk/월, offer/월로 나눠 재호출에 따른 중복 효과를 막습니다.
약속 제안은 친밀도 0인 accept / decline 두 선택지여야 합니다.

메인 진행 → 결제·성장 → CareMonthFinished → 결과 연출·확인 → ContinueAfterCare → 해당 월 약속 대화 → 다음 달.
12월은 마지막 육성 결과 확인 뒤 기존 CareEndingReady / GameFinished로 이어집니다.
컴파일·유닛 테스트·플레이 테스트는 실행하지 않았습니다.
