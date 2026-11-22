# 가구함 생성·연결

## 프리팹 생성

1. `Work Game > UI > Care Furniture Tool`을 엽니다.
2. 폰트를 지정하고 가구 노드 프리팹 생성을 누릅니다.
3. 생성한 노드가 사용 프리팹 칸에 자동 연결됩니다. 필요하면 이 노드부터 꾸밉니다.
4. 육성 Config에 기존 CareConfig.asset을 지정하고 가구함 프리팹 생성을 누릅니다.

가구함 기본은 1300×800입니다. 책상·장비·의자 3행, 각 행 왼쪽은 장착 슬롯, 오른쪽은 등록된 가구 4개입니다.
생성기는 Config의 선물 목록을 읽고 공통 노드의 중첩 프리팹 인스턴스로 배치합니다.
씬 자동 배치나 WorkRoot 자동 수정은 하지 않습니다.

## 씬 연결

1. 생성한 가구함 프리팹을 Canvas 아래 배치하고 GameObject는 켜둡니다. CanvasGroup을 코드가 제어합니다.
2. WorkRoot의 Care Furniture View에 씬의 CareFurnitureView를 연결합니다.
3. WorkRoot의 Furniture Button에 육성 메인의 가구함 진입 버튼을 연결합니다. OnClick 수동 연결은 추가하지 않습니다.
4. 기존 Care Config와 상태창 View 연결은 유지합니다. 상태창의 가구 카드·가구함 버튼도 이 창을 엽니다.

메인 버튼을 수동으로 연결하고 싶다면 Furniture Button 필드는 비우고 OnClick에 WorkRoot.OpenFurniture만 넣습니다.
닫기 버튼과 바깥 배경은 생성기가 연결합니다. 별도 Presenter 컴포넌트는 필요 없습니다.
구매·장착 갱신, 보유 현금, 종류별 장착 슬롯이 자동으로 연결됩니다.
가구함이 열린 동안 메인 다음 달 버튼은 비활성화됩니다. 닫으면 기존 편성 상태에 맞춰 복구합니다.

## 수정하기 쉬운 계층

- 가구함 루트: 전체 화면 모달과 CanvasGroup.
- Page: 실제 창. 크기는 이 RectTransform에서 변경합니다.
- Header: 제목·보유 현금·닫기.
- Rows/Desk, Equipment, Chair: 종류별 한 행. 앵커로 높이를 나눕니다.
- 각 행의 Equipped: 종류명·장착 버튼·아이콘·장착 이름.
- 각 행의 Items: 공통 가구 노드 프리팹 인스턴스 4개.
- 노드: 배경 Button / BuyFill / Name / Icon / PriceOrState / WorkHoldButton / CareFurnitureNodeView.

LayoutGroup·LayoutElement·ContentSizeFitter는 사용하지 않습니다.
루트 Page의 크기를 바꾸면 내부가 앵커를 따라갑니다. 행의 Equipped와 Items 앵커를 바꾸면 좌우 폭을 조정합니다.
노드 내부의 Name / Icon / PriceOrState는 각각 RectTransform으로 위치와 크기를 직접 조절할 수 있습니다.
아이콘은 Preserve Aspect를 켜 두어 이미지 비율이 달라져도 찌그러뜨리지 않습니다.
실제 가구 아이콘은 CareGiftSO.Icon에서 바꿉니다. 상태창·장착 슬롯에도 같은 아이콘을 사용합니다.
배경 이미지는 Image의 Sprite에서 교체합니다. 가구 노드의 상태별 배경 색은 CareFurnitureNodeView의 Color 필드에서 조절합니다.
구매 Fill의 이미지·색·채움 방향은 BuyFill에서 조절하며 시간은 WorkHoldButton의 Hold Seconds에서 조절합니다.
공통 외형은 노드 프리팹에서 바꾸면 페이지의 인스턴스에도 적용됩니다. 개별 배치 조정은 인스턴스의 앵커로 합니다.
Config에 가구를 추가·삭제하면 페이지를 다시 생성하거나 해당 노드와 Nodes 목록을 직접 수정합니다.

## 조작

- 미구매: 0.5초 길게 눌러 구매하고 자동 장착. 짧게 누르면 구매하지 않습니다.
- 보유: 짧게 눌러 장착. 같은 종류의 기존 물품은 교체하고 보유 목록에는 남습니다.
- 장착 중인 노드 또는 왼쪽 장착 슬롯: 짧게 눌러 해제합니다.
- 상태 문구: 미구매는 가격 / 보유 중 / 장착 중. 잔액 부족 가격은 별도 색으로 표시합니다.
- 활성 물품만 성장 보정을 적용합니다. 친밀도는 최초 구매 때만 반영합니다.
- 길게 눌러 구매한 터치의 손을 떼도 바로 해제되지 않도록 처리했습니다. 닫으면 진행 중 구매 홀드를 취소합니다.

## Care 방에 장착 가구 표시

`02_Game` 씬의 `CareRoomFurniture`가 장착 상태를 읽어 방 이미지를 표시합니다. 기존 규칙대로 책상·장비·의자는 종류별 하나씩이며, 최대 세 개입니다. SO의 `Icon`은 가구함용, `RoomSprite`는 방 표시용입니다. 12개 가구의 두 이미지를 모두 연결했습니다.

위치는 `CareRoomFurniture/DeskSlot`, `EquipmentSlot`, `ChairSlot`의 Transform에서 조정합니다. 각 자식 `Visual`은 코드가 비율과 바닥 정렬을 맞추므로 부모 슬롯을 옮기세요. 표시 크기는 루트 컴포넌트의 Slots/Size에서 조정할 수 있습니다. 가구를 교체해도 부모 슬롯의 위치는 유지됩니다.

WorkFlowRoot의 Room Furniture 필드와 표시 View의 Work Root / Config / Renderer 연결은 씬에 완료했습니다. 월말 정산을 닫아 Care에 들어오면 표시하고, 업무·정산·엔딩과 화면 비활성화 시에는 숨깁니다. 일반 Care UI와 분리되어 있어 대화 중에도 가구가 보입니다. 장착 변경 이벤트로 같은 종류의 이미지를 교체하며, 해제하면 해당 슬롯만 숨깁니다. 새 게임·불러오기 때는 새 CareSystem에 다시 연결합니다. 표시용 오브젝트를 매번 생성하지 않습니다.

Unity CLI 없이 파일로 씬을 수정했습니다. 일반 C# 컴파일, 씬 참조 검사와 장착·교체·해제·저장 복원 검사를 실행했으며, Unity 플레이 화면의 배치는 직접 확인해야 합니다.
