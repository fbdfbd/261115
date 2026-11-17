# 달이 상태 UI 생성·연결

## 생성

1. `Work Game > UI > Care Status Tool`을 엽니다.
2. 폰트와 크기를 설정합니다. 상태창 기본은 스킬트리와 같은 1300×800입니다. HUD는 세로 4줄이며 권장 크기는 520×300입니다.
3. 상태창 프리팹 생성 / 주요 스탯 HUD 프리팹 생성을 각각 실행합니다.
4. 생성한 프리팹을 씬의 Canvas 아래에 배치합니다. GameObject는 켜둡니다.
5. WorkRoot의 Care Hud View와 Care Status View에 씬의 각 View를 연결합니다.
6. Care Config에는 기존 Data/Care/CareConfig.asset을 연결합니다.

별도 Presenter 컴포넌트를 붙이지 않습니다. WorkRoot가 일반 C# CareStatusPresenter를 생성합니다.
버튼 OnClick과 스탯 필드는 생성기가 연결합니다. 수동 OnClick 추가는 필요 없습니다.
Canvas에는 GraphicRaycaster가 있어야 합니다.

HUD의 자세히 버튼은 육성 메인의 자유 행동 상태에서 상태창을 엽니다.
HUD는 `이름 · 등급 · 슬라이더(중앙 수치)` 형식으로 주요 스탯 4개를 표시합니다. 시간표 미리보기와 같은 막대 색을 기본으로 사용합니다.
HUD는 지식·체력·감성·사교를 위에서 아래로 4줄 배치합니다. 막대·배경 색과 줄 내부 간격은 툴에서 지정합니다.
자세히 버튼은 HUD 오른쪽 위에 두고 스탯 4줄은 버튼 아래부터 배치합니다.
HUD 루트에는 배경용 Image를 추가했습니다. 초기에는 알파 0이므로 배경을 쓰려면 Sprite를 넣고 색의 A를 올립니다.
CareHud 루트에는 Horizontal Layout Group을 둡니다. Padding의 Top(기본 48)으로 오른쪽 위 버튼 자리를 확보합니다.
그 안의 Stats는 Vertical Layout Group으로 4줄을 배치하며 Spacing으로 줄 사이 간격을 바꿉니다.
Details는 루트 바로 아래에 두고 Layout Element의 Ignore Layout을 켜서 오른쪽 위 앵커 배치를 유지합니다.
각 스탯 줄은 `Name / Grade / Slider`를 직접 자식으로 둔 Horizontal Layout Group입니다.
각 줄의 Spacing으로 세 객체 사이 간격을, Padding으로 줄 안쪽 여백을 조절합니다.
이름·등급은 TMP의 글자 폭을 사용하며 별도 Layout Element를 넣지 않습니다. Slider에만 Flexible Width 1을 두어 남은 폭을 채웁니다.
HUD만 새로 만들려면 주요 스탯 HUD 프리팹 생성만 누릅니다. 기존 상태창을 다시 생성할 필요는 없습니다.
대화·다른 패널·육성 결과 중에는 열지 않습니다. 대화 View가 아직 없다면 기존처럼 대화 입력을 기다립니다.
닫기 버튼과 바깥 배경은 패널 상태를 함께 닫습니다. 창 내부의 빈 공간은 바깥 닫기로 전달되지 않습니다.

## 크기 조절

상태창 프리팹의 Page RectTransform 크기를 바꿉니다. 루트는 전체 화면 모달 영역입니다.
Body/Left와 Body/Details의 앵커로 좌우 비율을 조절하고, Left/MainStats와 Left/Equipped로 하단 가구 비율을 조절합니다.
툴의 좌측 폭 비율·가구 높이 비율은 생성 시 초기 배치를 정하는 값입니다.
상태창은 LayoutGroup과 ContentSizeFitter를 사용하지 않습니다. HUD는 각 줄에 Horizontal Layout Group을 사용합니다. 기존 앵커를 유지하면 크기에 맞춰 배치가 따라갑니다.
글자 크기는 정수로 생성하며 필요하면 각 TMP에서 조절합니다.
우측 그래프는 기본 Slider를 사용합니다. Fill Image 색을 바꾸면 계열별 색상을 수정할 수 있습니다.

## 스탯

대표 4개는 소속 세부 스탯 3개의 합입니다. HUD와 상태창 좌측에는 수치·등급을 표시합니다.
HUD Slider의 Value 텍스트는 Slider 자식이며 막대 위에 표시됩니다. 슬라이더는 핸들 없는 표시 전용으로 조작되지 않습니다.
우측은 지식·체력·감성·사교 단락별로 세부 3개씩 수치와 막대를 표시합니다.
CareConfig.StatMax의 기본값은 300이며 세부 스탯의 실제 상한입니다. 대표 최대는 900입니다.
초기 스탯·수업·시너지·선물 성장 모두 상한을 넘지 않습니다. Preview.TotalGains는 상한에 걸린 실제 적용량입니다.
세부 스탯 Slider는 런타임에 StatMax를 읽으므로 상한 변경 시 프리팹을 다시 만들 필요가 없습니다.
주요 스탯 Slider는 등급 내 진행도입니다. 기본 C 0~99 / B 100~199 / A 200~349 / S 350 이상이며 S는 가득 표시합니다.
100에서 B의 0%, 200에서 A의 0%로 돌아갑니다. 막대 연출 중에도 경계를 넘으면 등급과 채움이 함께 전환됩니다.
막대 안 숫자는 실제 누적 수치입니다. 예를 들어 105는 숫자 105, 등급 B, 막대 5/100입니다.
등급 이름 None은 화면에서 C로 표시하며 기존 엔딩 조건과 등급 ID는 유지합니다. 기준은 CareConfig.Grades를 사용합니다.
주요 스탯 표시 계산은 CareGradeSettings.GetProgress를 공통 사용합니다.
텍스트와 Slider는 값이 달라질 때만 갱신합니다.

## 장착 표시와 가구함 바로가기

현재 장착 분류는 책상·장비·의자 3개입니다. 요청한 4자리 중 마지막은 Reserved가 켜진 예비 빈 자리입니다.
네 번째 종류의 장착 기능을 추가한 것은 아닙니다. 필요 없으면 그 슬롯을 삭제하고 Equipped 목록에서도 빼도 됩니다.
활성 물품 이름은 런타임 데이터에서, 아이콘은 CareGiftSO.Icon에서 읽습니다. 아이콘이 없으면 이름만 표시합니다.
상태창에서는 장착을 바꾸지 않습니다. 가구 카드 또는 가구함 버튼을 누르면 WorkRoot.FurnitureRequested 이벤트를 보냅니다.
CareFurnitureSetup.md의 가구함 생성기를 사용하고 WorkRoot.CareFurnitureView를 연결하면 바로가기가 자동으로 가구함을 엽니다.
가구함 View 미연결이면 상태창을 유지합니다. FurnitureRequested 이벤트는 별도 추가 연출에도 사용할 수 있습니다.
가구함 닫기는 CloseCarePanel(), 장착 선택은 SetActiveGift(id), 해제는 ClearActiveGift(kind)를 사용합니다.
친밀도는 최초 구매 때만 변하며 물품 교체·해제로 반복 지급하지 않습니다.

기존 CareHud.prefab에도 루트 Image와 줄별 Horizontal Layout Group을 적용했습니다. 기존 View·Button 참조는 유지합니다. 씬 수정·컴파일·플레이·자동 테스트는 실행하지 않았습니다.
