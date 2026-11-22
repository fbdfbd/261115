# 육성 대화·퇴근·약속 연결

원본은 `Samples/CareDialogue.json`입니다. CareConfig.asset의 Dialogues(TextAsset)에 연결했습니다.
새 게임 시작 시 JSON을 읽습니다. 대사만 고치면 SO를 재생성할 필요가 없습니다.
대화 UI 생성 툴은 CareDialogueUiSetup.md를 참고하세요. 실제 씬·프리팹·이미지는 직접 배치·연결합니다.

## 새 게임 인트로

IntroDialogue는 intro_meeting입니다. 새 게임은 업무 Ready 상태에서 인트로를 재생하고, 종료 전환이 화면을 덮으면 1월 업무를 시작합니다.
빈 Care 방에서 시작하며 HUD·가구는 숨깁니다. HideCharacter가 true인 줄에서는 달이를 숨기고, 집으로 데려온 줄부터 울음 표정으로 표시합니다.
인트로에서만 나르미의 자동 대사를 허용합니다. 로그 날짜는 한 달 전으로 표시하고 기존 월별 대화·선택지 규칙은 유지합니다.
intro_squeak는 씬 Presentation의 Sounds에 연결된 기존 찍찍 효과음을 네 번 재생합니다. intro_timepass는 기존 대각선 전환을 사용합니다.
인트로 중에는 업무 시간·급여·육성 월 진행이 발생하지 않습니다. 수동 저장은 막으며, 종료 후 생성되는 1월 WorkStart 체크포인트부터 이어하기합니다.
인트로 도중 타이틀로 돌아가면 기존 저장을 덮어쓰지 않습니다. 다시 새 게임을 시작하면 인트로 처음부터 재생합니다.
추가 에디터 연결은 없습니다. Unity CLI 없이 코드·씬 데이터 연결과 별도 C# 컴파일·일반/스킵/기록 복원 검증을 진행했습니다.

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

### Character 표정 연결

별도 View 없이 씬의 `CareDialogueView`에 추가된 `Character` 필드로 연결합니다. 씬에 배치한 Character 프리팹 루트의 `CharacterExpressionAnimator`를 드래그하세요. `WorkFlowRoot`의 `Care Character Root`에는 같은 Character 루트 GameObject를 연결합니다. 프리팹 원본이 아니라 씬 인스턴스를 연결합니다.

Character는 대화 중 숨기는 `After Work Groups`나 업무 전용 그룹의 자식으로 두지 마세요. 별도 루트 또는 대화 중에도 활성 상태를 유지하는 Care 배경 아래에 둡니다. WorkFlowRoot는 정산 종료 후 Care에서만 활성화하고 업무·정산·엔딩에서는 비활성화합니다. 일반 Care UI가 숨겨지는 이벤트·대화 중에도 Character는 유지합니다. 씬과 프리팹의 실제 연결은 수동으로 합니다.

`Samples/CareDialogue.json`의 모든 대사에 `Emotion`을 지정했습니다. 0=기본, 1=슬픔(현재 프리팹의 Cry), 2=화남, 3=웃음입니다. 대사마다 절대값으로 적용하므로 이전 표정이 남지 않습니다. 나르미·나레이션 대사의 Emotion도 화면에 있는 달이의 반응을 뜻합니다. 선택 후 이어지는 대화와 스킵 후 선택 대사에도 동일하게 적용하고, 대화를 닫으면 기본으로 돌아옵니다. Character가 비활성 상태여도 설정값을 보관하고 활성화할 때 적용합니다. 기존 ImageKey 이미지 이벤트는 그대로 사용할 수 있습니다.

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

`Leave.Months`에 1~12월별 네 가지 퇴근 반응을 등록했습니다. 초반의 조심스러운 동거 생활에서 중반의 일상 공유, 후반의 서로를 챙기는 관계로 말투와 소재가 달라집니다. 친밀도 수치에 따른 추가 분기는 없으며 각 반응은 두 줄입니다. 나르미의 자동 발화 없이 달이의 대사와 표정만 사용합니다. 이벤트 성공 여부를 전제로 하지 않으며, 약속 결과가 있는 날에는 기존처럼 약속 대화가 우선합니다.

월별 데이터가 없는 설정은 기존 공통 퇴근 반응으로 돌아갑니다. 기존 공통 대화 ID도 남겨 두어 과거 대화 로그를 복원할 수 있습니다. 월별 추가 데이터에 대한 새 씬 연결은 필요 없습니다.

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


## 월말 스토리와 씬 연결 (2026-11-21)

기존 CareDialogue 대화창을 재사용합니다. 결과 확인 → 해당 월 Stories 순서대로 → 약속 제안 → 다음 달 Work입니다.
6월은 친구 이야기 다음에 사진첩 약속을 제안합니다. 결과 확인 버튼의 기존 ContinueAfterCare 연결은 그대로 사용합니다.
2월 이름표 / 4월 쿠키와 과거 / 6월 청설모 친구 / 8월 진로가 Samples/CareDialogue.json에 등록되어 있습니다.
4월은 baking 약속 성공 시 함께 만든 쿠키, 실패·거절 시 달이가 따로 구운 쿠키로 도입부가 달라집니다.

### 대화 이벤트 이미지 — Image 하나로 시작

1. CareDialogueView가 있는 활성 오브젝트에 CareDialoguePresentationView를 추가합니다.
   같은 오브젝트라면 자동으로 찾습니다. 다른 오브젝트에 추가했다면 CareDialogueView의 Presentation 필드에 연결합니다.
2. 대화창의 시각적 배경 영역에 UI Image 하나를 추가합니다. 대사·선택지보다 뒤에 표시되도록 배치합니다.
   Raycast Target은 끄고 Preserve Aspect는 켭니다. 오브젝트는 활성 상태로 유지하고, Image.enabled는 시스템이 제어합니다.
3. Presentation의 Layers에 Key = event, Image = 위 Image를 연결합니다.
4. Pictures에는 아래 키와 실제 Sprite를 등록합니다. 이미지 파일은 이번 작업에서 생성하지 않았습니다.

| Key | 용도 |
| --- | --- |
| story_nameplate | 2월 이름표 |
| story_cookie | 4월 쿠키 |
| story_friends | 6월 친구 이야기 |
| story_future | 8월 진로 이야기 |

연결하지 않은 이미지는 생략되며 대화와 월 진행은 정상 작동합니다.
나중에 배경이 필요하면 Layers에 background 같은 키와 Image를 추가하고 같은 키의 ShowImage 명령을 사용하면 됩니다.
캐릭터 표정은 기존 ImageKey / imageKeyChanged를 계속 사용합니다. 빈 ImageKey는 기존 표정을 유지합니다.
서로 다른 대화 이벤트 사이에는 추가 이미지가 초기화되고, 선택 후 이어지는 답변 대화 안에서는 유지됩니다.

### Work 이름표

#### 명패 이벤트 기획 — 함께 이름 쓰기

2월 육성 결과 뒤, 달이가 도토리와 잎사귀로 꾸민 빈 명패를 회사 책상에서 쓰라며 건넵니다. 글자를 아직 다 배우지 않아 나르미의 이름을 혼자 완성하지 못했고, 이름을 같이 적어 달라고 부탁합니다. 플레이어는 천천히 따라 쓰기 / 만든 마음에 감사하며 같이 쓰기 / 연필을 함께 잡기 중 하나를 고릅니다. 세 선택 모두 다정한 반응이며 보상 차이는 없습니다. 이름은 `나르미`만 적고 추가 문구는 넣지 않습니다. 엔딩 회고에도 둘이 함께 이름을 썼다는 내용을 반영합니다.

컷신은 이벤트 첫 대사부터 표시합니다. `story_02_nameplate` 첫 줄의 Custom 명령이 `CareDialogueCutsceneView`를 재생하며, `02_Game`의 `Canvas_NameplateCutscene`에는 `Art/CG/event_01.png`가 연결되어 있습니다. CanvasGroup 알파를 0→1로 0.35초 동안 올리고 대화창은 컷신 위에 유지합니다. 빈 명패를 만들었지만 글자를 덜 배웠다는 설명 뒤에 세 가지 나르미 선택지가 나옵니다. 선택 이후의 별도 답변 대화는 재생하지 않습니다. 기존 답변 ID는 이전 저장 이력을 복원하는 용도로만 남겨 둡니다.

선택지를 누르면 입력을 막고 기존 대각선 전환이 화면을 덮습니다. 완전히 덮인 시점에 선택을 기록하고 명패를 해금한 뒤 3월 Work로 바로 진행합니다. 컷신은 그때 숨기며, 전환이 걷히면 업무 화면과 명패가 보입니다. Care/HUD로 돌아가 기다리거나 다음 달 버튼을 한 번 더 누르는 과정은 없습니다. 스킵도 선택지를 자동으로 고르지 않으며, 세 선택지 모두 같은 전환을 사용합니다. 나레이션에서는 이름표를 숨깁니다.

Work용 명패 이미지는 씬의 `DaliNameplate` Image에 `Art/도토리와 잎사귀 장식 나무 팻말.png`를 연결했습니다. 아래의 기존 해금·표시 규칙은 유지합니다.

Work 화면의 활성 그룹에 WorkStoryDecorationView를 추가하고 Work Root를 연결합니다.
Decorations에 Unlock = dali_nameplate, Target = 이름표 이미지 오브젝트를 지정합니다.
Target은 이 컴포넌트 자신이나 부모가 아닌 별도 자식 장식으로 두세요. 장식의 Sprite는 직접 지정합니다.
이름표는 2월 스토리 완료 후 해금되어 3월부터 Work에 보입니다. 새 게임에는 숨겨지고 저장 불러오기 때 복원됩니다.

### 선택지와 능력치 표시

선택지의 NextDialogue로 답변 대화를 이어갑니다. 해당 답변까지 끝나야 스토리가 완료됩니다.
8월의 긍정 답변 두 개는 HighestStatGain = 10입니다. 스케줄 결과가 반영된 세부 능력치 중 최고 값을 선택하고,
동률이면 CareStat 정의 순서(논리력 → 탐구력 → 집중력 → 근력 → 지구력 → 순발력 → 상상력 → 표현력 → 공감력 → 화술 → 협동심 → 리더십)를 따릅니다.
기존 상한을 적용하며 가구·수업 배율은 적용하지 않습니다. 최고 능력치가 상한이면 다른 능력치로 넘기지 않습니다.
대상·예상 실증가량은 선택할 때 정하고 실제 적용은 답변 대화 완료 시 한 번 수행합니다.
선택 사항으로 대화창 자식 TMP Text를 CareDialogueView의 Story Reward Text에 연결하면 답변 중 논리력 +10처럼 표시합니다.

### 이미지·연출 데이터 확장

각 Lines 항목에 Cues 배열을 추가할 수 있습니다. JSON의 Kind는 숫자입니다.

| Kind | 기능 | 필드 |
| --- | --- | --- |
| 0 ShowImage | 이미지 표시·교체, Seconds 동안 페이드인 | Layer / Key / Seconds |
| 1 HideImage | Seconds 동안 페이드아웃 후 숨김 | Layer / Seconds |
| 2 Wait | 대기 | Seconds |
| 3 Sound | 효과음 재생, 필요하면 대기 | Key / Seconds |
| 4 Custom | 커스텀 연출 후 필요하면 대기 | Key / Seconds |

예: {"Kind":0,"Layer":"event","Key":"story_nameplate","Seconds":0.25}

명령은 배열 순서대로 실행됩니다. Sound는 Presentation의 Sounds에 연결합니다. 전용 AudioSource가 없으면 공통 SFX 플레이어를 사용해 옵션 음량·정지·중단을 적용합니다.
Custom Cue UnityEvent는 시작 알림용입니다. 실제 완료를 기다려야 하는 Animator / Timeline 연출은
CareDialogueCueExtension을 상속하고 Extensions 목록에 연결하세요.
CanPlay(key), Play(cue), ApplyFinal(cue)를 구현합니다. Play는 완료까지 yield하고,
ApplyFinal은 스킵·화면 재연결 때 최종 시각 상태만 적용합니다. SetPaused / Cancel로 정지·중단도 처리합니다.
보상과 저장 상태 변경은 연출 코드에 넣지 않습니다. 연출 완료 전에는 다음 대사·선택·스킵 입력이 차단됩니다.
일시정지하면 기본 페이드·대기·전용 오디오는 정지합니다. 커스텀 연출은 SetPaused 구현에서 정지시킵니다.
읽은 대화를 스킵할 때는 소리·대기를 생략하고 이미지 최종 상태를 적용하며 선택지에서는 멈춥니다.

### 저장과 검증

story/이벤트ID로 완료를 기록하고 선택은 기존 대화 로그에 보관합니다. 해금 키는 Flow.StoryUnlocks에 저장합니다.
기존 저장의 StoryUnlocks 누락은 빈 목록으로 읽습니다. 저장 버전과 기존 슬롯은 유지합니다.
스토리 도중 저장·나가기는 기존 CareStart 체크포인트를 보존합니다. 이어하기는 Care부터 다시 진행하며,
다음 WorkStart 체크포인트에는 완료 기록·해금·보상이 함께 저장됩니다.
CareStoryTests에 월말 순서·대답 연결·능력치 동률/상한·스킵/입력차단·저장 복원·쿠키 분기 테스트를 추가했습니다.
이번 작업은 Unity CLI 사용 중단 요청에 따라 에디터 컴파일 및 Test Runner 실행을 하지 않았습니다.
