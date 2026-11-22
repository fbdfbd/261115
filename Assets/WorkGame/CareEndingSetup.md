# 5년 후 엔딩

## 진행 순서

엔딩 선택 → 5년 후 회고 → 풀스크린 직업 이미지와 근황 → Care 방의 친밀도별 근황 → 마지막 직업 이미지 → 클릭 후 검은 페이드 → 타이틀입니다.
회고는 HUD와 Character를 숨긴 방에서 공통 CareDialogueView로 표시합니다. 따옴표 대사 없이 나레이션만 사용합니다.
직업 이미지는 화면을 꽉 채우며 원본 비율을 유지합니다. 화면 비율에 따라 가장자리가 잘릴 수 있습니다.
친밀도 장면에서는 직업 이미지를 닫고 방을 다시 보여 줍니다. HUD는 끝까지 숨깁니다.
Low는 Character를 숨깁니다. Normal은 따로 산다는 설명 동안 숨기고 현재 방문 장면인 세 번째 문장부터 기본 표정으로 표시합니다. High는 웃음 표정으로 표시합니다.

## 연결

02_Game 씬의 WorkRoot에는 기존 CareDialogueView와 CareEndingView가 연결되어 있습니다.
WorkFlowRoot는 EndingView.SceneChanged를 받아 방과 캐릭터를 제어합니다. 이미지는 전용 하위 Canvas에서 표시합니다. 실행 시 현재 다이얼로그 Canvas보다 한 단계 낮게 맞추므로 기존 Canvas 순서를 바꾸지 않습니다.
CareEnding 프리팹에는 풀스크린 배경과 Picture만 남겼습니다. 기존 편지 UI와 타이틀 버튼은 제거했습니다.
CareEndingTool로 새 프리팹을 만들 경우 WorkRoot의 CareEndingView에 연결하고 Canvas를 일반 Work/Care 전환 그룹 바깥에 배치합니다.

## 마지막 이미지 구간 끄기

CareEndingView의 Show Final Career Image를 끄면 친밀도 대화 종료 후 바로 페이드아웃하고 타이틀로 돌아갑니다.
켜면 직업 이미지를 한 번 더 보여 줍니다. 이때 다이얼로그는 숨기고 화면 클릭을 기다립니다.
Image Seconds는 이미지 등장 시간입니다. Title Fade Seconds는 타이틀 복귀 전 검은 페이드 시간입니다.
현재 프리팹은 마지막 이미지 켜짐 / 이미지 0.3초 / 타이틀 페이드 0.6초입니다.

## 대사와 회고

CareEndingSO.Text와 Pages.Text에는 직업별 근황이 들어 있습니다. Pages의 첫 이미지를 직업 이미지로 사용합니다.
LowText / NormalText / HighText는 친밀도별 마무리입니다. AppendFriendshipText를 끄면 이 부분을 생략합니다.
Samples/CareContent.json도 현재 SO 문구와 맞췄습니다. 데이터 재생성 시 이미지 연결은 기존 가져오기 도구가 보존합니다.
Samples/CareDialogue.json의 EndingText와 약속 결과 텍스트는 이번 회차의 실제 이력으로 회고를 만듭니다.
같은 달에는 최종 선택이나 약속 결과 하나만 보여 줍니다. 명패를 우선하고 약속 결과를 다음으로 골라 최대 3개를 월순으로 표시합니다.

대화창 페이드는 직업 컷씬의 등장·종료 경계에만 적용합니다. 이미지 페이드가 끝난 뒤 0.18초 기다리고 0.22초 페이드인합니다. 일반 문장과 방문 장면에서는 대화창을 유지하고 0.08초 간격만 둡니다.
엔딩 나레이션은 기존 DialogueNext와 DialogueBlip 효과음을 사용합니다. 일반 나레이션의 무음 설정은 유지합니다.
Dialogue Delay Seconds / Dialogue Fade Seconds / Line Delay Seconds에서 조절합니다. 글자 출력은 대화창 등장이 끝난 뒤 시작합니다.

## 입력과 기록

글자 출력 중 클릭하면 현재 대사를 완성하고 다시 클릭하면 다음 대사로 넘어갑니다.
로그에는 현재 엔딩에서 이미 읽은 문장만 표시합니다. 이미 읽은 엔딩은 스킵할 수 있습니다.
일시정지하면 대사와 이미지·타이틀 페이드가 멈춥니다. 메뉴 입력 차단도 공유합니다.
마지막 대사를 끝내면 엔딩 열람을 기록합니다. 이후 이미지 재표시 여부와 관계없이 컬렉션에 반영됩니다.
타이틀 복귀는 기존 엔딩 선택 전 체크포인트를 보존합니다. 이어하기하면 선택 전 상태로 돌아옵니다.

Unity CLI는 사용하지 않았습니다. 별도 C# 컴파일과 순수 도메인 검사로 검증했으며 실제 화면은 Unity 에디터에서 확인해야 합니다.
