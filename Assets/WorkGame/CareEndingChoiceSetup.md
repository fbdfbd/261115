# 엔딩 선택 카드

12월 육성 결과 확인 후 조건에 맞는 엔딩을 최대 5개 표시합니다. Priority 내림차순과 ID 순서로 고릅니다.
후보가 하나면 선택창을 생략합니다. 조건을 충족한 직업이 없으면 백수 엔딩으로 진행합니다.

제목은 5년 후의 달이의 모습은…입니다. 안 본 엔딩은 ???로 표시하고 이미 읽은 엔딩은 직업 이름을 표시합니다.
설명은 CareEndingSO.ChoiceText에서 가져옵니다. Samples/CareContent.json의 choiceText도 같은 내용입니다.
카드 배경은 가장 높은 MainNeeds 등급의 스탯색입니다. 동률이면 데이터 순서에서 앞선 스탯을 사용합니다.
지식은 연파랑 #E3EDFA / 체력은 연초록 #E0F0E3 / 감성은 연분홍 #FAE8ED / 사교는 연노랑 #FAF2DB입니다.
요구 스탯이 없는 백수는 흰색입니다.

선택 후 연출과 옵션은 CareEndingSetup.md를 참고합니다. 기존 CareEndingView는 직업 풀스크린 이미지를 담당하고 CareDialogueView를 함께 사용합니다.
엔딩 대화를 끝까지 읽으면 WorkRoot가 열람 기록을 저장합니다. 이후 타이틀 컬렉션과 다음 회차 카드 이름에 반영합니다.

카드 크기·간격·열기와 선택 페이드 시간은 CareEndingChoiceView에서 조정합니다.
카드 버튼 이벤트는 Presenter가 연결하므로 OnClick을 수동으로 추가하지 않습니다.
