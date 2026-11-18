# 달이 시간표 연결

## 생성 및 씬 연결

1. `Work Game > UI > Care Schedule Tool`을 엽니다.
2. `수업 노드 프리팹 생성`을 누릅니다. 공통 외형은 이 프리팹에서 수정합니다.
3. Config와 노드 프리팹을 확인하고 `시간표 프리팹 생성`을 누릅니다.
4. 생성한 페이지를 Canvas 아래 배치합니다. 루트 GameObject는 켜 둡니다. CanvasGroup으로 표시를 제어합니다.
5. WorkRoot의 `Care Schedule View`에 페이지의 CareScheduleView, `Schedule Button`에 육성 화면의 진입 버튼을 연결합니다.

닫기 버튼·바깥 배경·수업·주차·시너지 행은 툴에서 연결합니다. Presenter 컴포넌트를 추가하지 않습니다.
버튼 OnClick을 직접 쓰려면 Schedule Button을 비우고 WorkRoot.OpenSchedule을 연결합니다. 둘을 중복 연결하지 않습니다.

## 조작

- 수업을 클릭하면 1주차부터 첫 빈 칸에 즉시 편성합니다. 아래 주차 카드를 클릭하면 해당 칸을 비웁니다. 별도 × 버튼은 없습니다.
- 4칸이 가득 차면 수업 클릭으로 기존 편성을 덮어쓰지 않습니다. 상세 설명만 바꾸며, 아래 주차를 먼저 비운 뒤 다시 수업을 눌러야 합니다.
- 해금 전 수업은 설명을 읽을 수 있지만 편성되지 않습니다.
- 해금 월은 표시하지 않습니다. 노드 전체를 덮는 LockOverlay 아래 Dim(검정 Image)과 LockImage(자물쇠 Image)로 잠김 상태를 표시합니다.
- 해금 이후 시간표를 처음 열면 LockOverlay가 0.35초 동안 사라지고 카드가 105% → 원래 크기로 돌아옵니다. 처음부터 열린 수업은 연출하지 않습니다.
- 해금 연출 확인 기록은 현재 플레이 중에만 유지합니다. 저장 연동은 포함하지 않습니다.
- 홈스쿨링·휴식도 좌측 하단 버튼을 누르면 첫 빈 칸에 편성합니다. 같은 수업을 반복 편성할 수 있습니다.
- 수업 선택 시 해금된 시너지 상대가 반짝입니다. 같은 수업 두 번 조합은 해당 수업 자체가 반짝입니다.
- 성립한 시너지는 `[주차] 수업명 + [주차] 수업명 | 효과명 | 세부 스텟(주요 스텟) +N`으로 표시합니다. 효과명만 굵게 표시합니다.
- 예: `탐구력(지식) +4`. 증가하는 세부 스텟을 각각 표시하며 여러 개면 ` · `으로 구분합니다. 실제 증가량 데이터는 바꾸지 않습니다.
- 기여한 주차 카드에는 해당 색의 + 표시가 붙습니다. 문구의 대괄호 번호는 시너지 순번이 아니라 주차 번호입니다.
- 시너지 행을 누르면 해당 조합만 강조합니다. 다시 누르면 전체 조합을 강조합니다. 여러 조합에 겹치는 카드는 첫 번째 조합 색으로 반짝이고 번호는 모두 표시합니다.
- 편성 4칸을 채우고 창을 닫으면 기존 메인 다음 달 버튼을 사용할 수 있습니다. 수업료 부족 여부도 기존 플로우에서 검사합니다.
- 편성 줄 아래 주요 스텟 4개가 현재 → 편성 후 값을 표시합니다. 수업·시너지·장착 가구 보정과 세부 스텟 최대치를 반영합니다.
- 슬라이더 안 중앙에 스텟 이름, 그 아래 수치를 표시합니다. 예상 등급은 Bar의 왼쪽 테두리에 중심을 맞춰 안팎으로 걸칩니다.
- 진한 막대는 현재 값, 연한 추가 구간은 예상 값입니다. 편성 변경 시 0.35초 동안 이동합니다.
- 등급이 오를 예정이면 예상 등급의 색이 바뀌고 115%로 커집니다. 편성을 취소하면 원래 표시로 돌아옵니다.
- 주요 스텟의 현재·예상 막대는 C/B/A 등급 안의 진행도를 사용합니다. 등급이 바뀌면 다음 등급의 0부터 이어지고 S는 가득 표시합니다.
- 예측 결과가 현재와 다른 등급이면 현재 Fill을 0으로 두어 이전 등급 막대가 새로운 등급 예상 구간을 가리지 않습니다. 숫자는 실제 누적 값입니다.

## 크기 조절

기본 Page 1300×940, 노드 100×100 정사각형입니다. 지식·체력·감성·사교 네 박스 안에 각각 가로 2 × 세로 4로 수업 8개를 둡니다.
각 박스 안에서는 해금 단계가 빠른 수업부터 위에서 아래로 배치합니다. 카테고리는 수업의 주요 성장 스텟으로 정합니다.
창 폭이 줄면 카테고리 박스 열 수가 줄고 수업 목록만 세로로 스크롤합니다. 좌측에는 스크롤이 없습니다.
제목 36 / 상세 제목 26 / 카드 이름 18 / 주차 이름 18입니다. 노드 이름은 한 줄 말줄임하며 정식 이름은 좌측에서 읽습니다.

| 계층 | 조정 위치 |
| --- | --- |
| Page | RectTransform 크기, Vertical Layout Group의 패딩·간격 |
| Main/SelectedLesson | Layout Element의 Preferred Width: 좌측 상세 폭 |
| Main/SelectedLesson | Vertical Layout Group의 Padding / Spacing: 좌측 내부 여백 |
| SelectedLesson/ImageArea | Layout Element의 Preferred Height: 기본 144. ImageFrame은 항상 정사각형 |
| SelectedLesson/Title, Gains, Cost | Layout Element의 Preferred Height: 각각 64 / 48 / 28 |
| SelectedLesson/Description | 남은 높이를 사용하는 설명. 자동 글자 크기 최소 18 |
| Main/LessonRows | 수업 목록 세로 ScrollRect |
| Main/LessonRows/Viewport | CareSquareGrid: Node Size / Max Columns |
| Main/LessonRows/Viewport/Content | 묶음 사이 Grid Layout Group의 Spacing / Padding |
| Content의 각 카테고리 박스 | Image 배경색, Header 텍스트, Nodes의 2×4 수업 |
| PlanStrip | Layout Element의 Preferred Height: 편성 영역 높이 |
| PlanStrip/Totals | Layout Element의 Preferred Width: 합계 영역 폭 |
| PlanStrip/Weeks | Horizontal Layout Group: 주차 카드 간격 |
| Preview | Layout Element의 Preferred Height: 성장·시너지 영역 높이, 기본 152 |
| Preview/Stats | Horizontal Layout Group: 주요 스텟 4칸의 간격 |
| Preview/Synergies | 세로 ScrollRect: 시너지가 늘면 스크롤 |

Layout Element는 영역의 고정 크기·가변 비율에 사용합니다. 수업 카드와 주차 카드에는 없습니다.
각 박스 안의 수업은 Grid Layout Group으로 항상 정사각형입니다. 폭이 부족하면 박스 열 수가 줄고 세로로 스크롤됩니다. 프레임마다 계산하지 않습니다.
왼쪽 상세는 정사각형 이미지 → 수업 제목 → 설명 → 효과 → 가격 → 무료 수업 버튼 순서입니다. 아이콘 Sprite는 기존 CareLessonSO.Icon을 사용합니다.
좌측 스크롤을 제거하고 이미지 높이와 텍스트 영역을 나눴습니다. 제목·설명·효과·가격에 자동 글자 크기를 적용하며 과도하게 작은 창에서는 말줄임으로 영역을 넘지 않게 합니다.
수업 목록의 Viewport에는 RectMask2D가 있습니다. ContentSizeFitter는 수업 목록과 시너지 목록의 Content에만 사용합니다.
카드 내부는 앵커로 조정하고, 텍스트 크기는 각 TMP에서 수정합니다. 이미지에는 Preserve Aspect를 적용합니다.
수업 아이콘은 CareLessonSO.Icon, 카드 이름은 ShortName, 좌측 정식 이름은 Name을 사용합니다.
ShortName은 SO와 CareContent.json의 shortName에 추가했습니다. 내용·수업 효과는 유지합니다.
공통 수업 노드 수정은 중첩 프리팹으로 반영됩니다. 열 수·노드 크기는 Viewport의 CareSquareGrid에서 조절합니다. 수업 개수를 바꾸면 페이지를 다시 생성합니다.
강조 색은 CareScheduleView, 반짝임 속도는 각 CareScheduleHighlight에서 바꿉니다. 창을 닫으면 반복 Tween을 중지합니다.
자물쇠 이미지는 툴의 자물쇠 이미지 필드로 지정합니다. 비우면 도형으로 만든 임시 자물쇠를 사용합니다.
기존 공통 노드에서는 LockOverlay/LockImage의 Source Image를 지정하면 됩니다. 이미지를 넣으면 임시 Placeholder가 실행 시 자동으로 숨겨집니다.
현재 자물쇠는 외부에서 받은 그림이 아니라 Image 도형으로 만든 Placeholder입니다. 실제 자물쇠 Sprite는 아직 포함하지 않았습니다.
해금 연출 시간·확대 비율은 CareLessonNodeView의 Unlock Seconds / Unlock Scale에서 수정합니다.
미리보기는 CareScheduleStatView의 Move Seconds / Grade Up Scale / Grade Color / Grade Up Color에서 조절합니다.
현재·예상 막대 색은 각 Bar/Current/Fill과 Bar/Preview/Fill의 Image에서 조절합니다. Slider에는 핸들이 없고 조작되지 않습니다.
기존 CareSchedule.prefab에도 네 카테고리 박스와 스크롤 없는 좌측 상세를 적용했습니다. 기존 View ID와 수업 SO 연결은 유지하므로 이번 변경으로 재생성할 필요는 없습니다.

씬 수정·컴파일·플레이·테스트는 실행하지 않았습니다.
