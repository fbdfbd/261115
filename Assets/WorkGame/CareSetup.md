# 육성 코어 연결

상태창·HUD 생성 툴과 View는 CareStatusSetup.md를 참고하세요. 실제 씬과 프리팹은 직접 배치합니다.
기본 콘텐츠는 Data/Care/CareConfig.asset에 연결해 두었습니다.
수업 32개·시너지 8개·선물 12개·엔딩 25개와 친밀도 후일담 75개가 있습니다.
전체 목록과 조건은 CareContent.md, 일괄 편집 원본은 Samples/CareContent.json에서 확인합니다.
엔딩 하나에 친밀도별 후일담 3종을 넣는 구조이므로 엔딩 SO 75개를 만들 필요는 없습니다.

## 활성화

Project의 Create > Work Game > Care > Config로 CareConfigSO를 만듭니다.
수업·시너지·선물·엔딩도 같은 Care 메뉴에서 SO를 만들고 Config 목록에 등록합니다.
WorkRoot의 Care Config에 Data/Care/CareConfig.asset을 연결하면 육성 루프가 켜집니다. 미연결이면 기존 업무 루프를 유지합니다.
무료 홈스쿨링(home)과 휴식(rest)은 Config에서 자동 생성합니다. 이 ID는 일반 수업에 사용하지 않습니다.
빈 Config도 무료 수업으로 진행할 수 있습니다. 홈스쿨링은 주요 스탯 각각 +1, 휴식은 기본 효과 없음입니다.
휴식 효과와 초기 스탯은 Config에서 조절할 수 있습니다. 대화·퇴근·약속 규칙은 CareDialogueSetup.md를 참고하세요.

## 월 흐름

업무 → 퇴근 → 기존 정산서 → 퇴근 후 화면 → 수업 편성·선물·강화 → 기존 다음 달 버튼
→ 수업료 결제·육성 실행 → CareMonthFinished 이벤트 → 결과창 → ContinueAfterCare() → 해당 월 약속 대화
→ 다음 달 업무. 12월은 마지막 결과 확인 후 엔딩 판정으로 갑니다.

UI가 없는 현재 Care Config를 연결하면 다음 달 버튼을 누른 뒤 결과 확인 호출을 기다립니다.
화면이 자동으로 생기지 않습니다. 이후 제작할 육성 결과 View의 확인 버튼을
WorkRoot.ContinueAfterCare에 연결해야 다음 달로 진행합니다.
중복 실행·중복 구매·결과 확인 연타로 결제나 성장이 반복되지 않습니다.

## Presenter / 입력

WorkRoot.CarePresenter는 일반 C# 객체이며 View가 정해지면 연결합니다.

| 입력 | 역할 |
| --- | --- |
| SetLesson(slot, id) | 0~3번 칸에 수업 지정. 반복 선택 가능 |
| SetHome(slot), SetRest(slot) | 무료 수업 지정 |
| Preview() | 비용·수업 성장·시너지·선물 추가량·총 성장 미리보기 |
| BuyGift(id) | 최초 구매 후 해당 종류에 자동 활성화. 이전 물품은 계속 보유 |
| SetActiveGift(id) | 보유한 선물로 교체. 비용·친밀도 재적용 없음 |
| ClearActiveGift(kind) | 책상·장비·의자 중 해당 종류를 비움 |
| FinishMonth() | 현재 달 육성 실행. 실패 시 false, 비용·성장 적용 없음 |
| AddFriendship(amount) | 친밀도 증감. 음수 허용 |

WorkRoot에는 SetHomeLesson(int), SetRestLesson(int), BuyGift(string), SetActiveGift(string), ClearActiveGift(int), AddFriendship(int)도 있습니다.
ClearActiveGift의 종류 번호는 책상 0 / 장비 1 / 의자 2입니다.
UI에서 읽을 상태는 WorkRoot.Care.Run, Care.Plan, Care.Lessons, Care.Gifts, Care.Money입니다.
Run.GetStat(CareStat)으로 수치를, CareStatNames.GetName(CareStat)으로 한글 이름을 읽습니다.
CareConfig.StatMax의 기본값은 세부 300, 대표 합산 최대 900입니다. 실제 성장과 성장 예측 모두 상한을 반영합니다.
Care.GetGrade(대표 스탯)으로 등급을, Preview().GetGain(스탯)으로 대표 합산 성장량 또는 세부 성장량을 읽습니다.
Run.Gifts는 구매 목록, Run.ActiveGifts는 활성 목록이며 Run.GetActiveGift(kind)로 종류별 활성 ID를 읽습니다.
Presenter.Changed를 구독해 화면을 갱신하고 화면이 해제되면 구독을 해제합니다.

## 이벤트

| WorkRoot 이벤트 | 이후 View에서 할 일 |
| --- | --- |
| RunStarted | 새 CarePresenter로 연결 교체 |
| CareMonthFinished(CareMonthResult) | 육성 결과 표시. 확인 시 ContinueAfterCare 호출 |
| CareEndingReady(CareEndingResult) | 선택된 진로 엔딩과 친밀도 후일담 표시 |
| CareBlocked(string) | 잔액·진행 상태 문제 안내 |
| GameFinished | 최종 진행 종료. WorkFlowRoot의 On Run Finished로 이어짐 |

기존 WorkFlowRoot의 Next Month Button은 유지합니다. Care Config가 연결되면 이 버튼이 육성을 실행합니다.
12월에도 정산 후 육성 실행이 가능하며, 기존 즉시 종료 이벤트는 마지막 육성 결과 확인 뒤로 미뤄집니다.

## 데이터 규칙

- 주요: 지식 / 체력 / 감성 / 사교. 세부: 논리·탐구·집중 / 근력·지구력·순발력 / 상상·표현·공감 / 화술·협동·리더십.
- 대표 스탯은 소속 세부 3개의 합입니다. 지식=논리력+탐구력+집중력이며 다른 계열도 같은 방식입니다.
- 저장·성장은 세부 12개만 사용합니다. 수업·시너지·초기 성장량에 대표 스탯을 직접 넣으면 데이터 오류로 알려줍니다.
- 대표 등급 기본값은 B 100 / A 200 / S 350 이상입니다. B 미만은 None(미달)입니다. CareConfig > Grades에서 조절합니다.
- 수업 해금 월은 1 / 3 / 6 / 9 중 하나입니다. 해당 월 퇴근 후부터 이용합니다. 12월 추가 해금은 없습니다.
- 실제 수업 8개씩 해금되는 수량은 수업 SO의 등록 수와 Unlock Month로 설정합니다.
- 매달 빈 4칸으로 시작합니다. 홈스쿨링·휴식도 직접 편성합니다. 창을 닫고 메인에서 진행하며 수업료 부족 시 거절합니다.
- 수업료에는 인맥 강화를 적용합니다. 수업 하나마다 할인 후 소수점을 버리고 합산합니다. 선물 가격에는 적용하지 않습니다.
- 시너지는 해당 월 4칸 전체에서 순서 무관하게 검사합니다. 같은 수업 SO 두 개를 넣으면 그 수업 2회가 조건입니다.
- 같은 시너지는 월 1회, 서로 다른 시너지는 함께 발동합니다.
- 성장 계산은 (세부 수업 성장 + 시너지) × (1 + 활성 선물 보정 합계/100)이며 소수점을 버립니다.
- 선물 대표 스탯 보정은 해당 계열 세부 3개에 적용합니다. 예: 지식 +10%와 논리력 +20%를 함께 활성화하면 논리력 +30%, 탐구력·집중력 +10%입니다.
- 선물 해금 월은 삭제했습니다. 첫 달부터 전부 구매 가능하고, 구매는 물품별 1회입니다.
- 책상·장비·의자별 하나씩 최대 3개 활성화합니다. 같은 종류를 새로 구매하면 교체하고, 이전 선물은 보유 목록에 남습니다.
- 선물 효과는 활성 상태에서만 적용합니다. Friendship은 최초 구매 때만 한 번 적용하며 음수도 허용합니다.
- 친밀도는 음수 허용 정수입니다. 퇴근 반응·월별 대화·약속 대화의 수치는 CareDialogue.json에서 관리합니다.
- Low Friendship Max 이하 / High Friendship Min 이상 / 그 사이는 보통입니다. 임시 기본 경계는 -1 / 50입니다.
- 엔딩 Main Needs는 대표 최소 등급, Needs는 세부 최소 수치입니다. 두 목록의 조건을 모두 만족해야 합니다.
- 예: 데이터 분석가는 지식 A 이상 + 논리력 90 이상입니다. 기준 숫자는 대표 등급 설정을 바꿔 일괄 보정할 수 있습니다.
- CareMonthResult에는 합산 대표값을 포함한 Stats와 해당 월 MainGrades를 저장합니다.
- 엔딩은 Priority 큰 순, 동점은 ID 순입니다. 모두 불충족이면 Fallback Ending을 선택합니다.
- 25개 구성은 Endings 목록 24개 + 별도 Fallback Ending 1개를 권장합니다. 같은 SO를 두 목록에 중복 등록하지 않습니다.
- 판정·성장은 SO를 런타임 정의로 복사해서 사용합니다. 실행 중 SO 원본을 수정하지 않습니다.

컴파일·자동 테스트·플레이 확인은 실행하지 않았습니다.
