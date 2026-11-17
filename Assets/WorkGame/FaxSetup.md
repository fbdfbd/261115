# 팩스기 씬 연결

## 드래그 영역

- 업무 Canvas 아래 빈 RectTransform `DragRoot`를 만들고 화면 전체 Stretch로 둡니다.
- 일반 / 정렬 영역과 나란히 놓습니다. Mask / RectMask2D가 있는 부모 안에 넣지 않습니다.
- 종이보다 위에 그려지게 Hierarchy 뒤쪽에 배치합니다. 별도 Canvas라면 Sorting Order도 확인합니다.
- LayoutGroup과 Image는 필요 없습니다. WorkDeskView의 Drag Root에 연결합니다.

## 팩스기

- 팩스기 루트에 WorkFaxView를 추가합니다. CanvasGroup은 자동으로 붙습니다.
- Group에는 그 CanvasGroup을 연결합니다. 루트는 활성 상태로 둡니다.
- 드롭받을 Image를 같은 오브젝트 또는 자식에 놓고 Drop Area에 연결합니다. Raycast Target을 켭니다.
- Count Text(TMP)와 Progress Slider는 선택 사항입니다. Slider를 연결하면 진행도를 0~1로 표시합니다.
- WorkRoot의 Fax View에 씬의 WorkFaxView를 연결합니다.
- 팩스기는 업무 중이고 강화에서 팩스기를 해금했으며 표시가 ON인 경우에만 나타납니다.
- 해금 없이 확인하려면 플레이 중 WorkFaxView의 Force Enabled를 켭니다. 표시와 드래그 접수를 모두 허용합니다. 일시정지·퇴근 상태와 슬롯 제한은 유지됩니다. 끄면 기존 해금·표시 설정을 따릅니다.
- 전용 Unity Layer나 Collider는 필요 없습니다. Canvas의 GraphicRaycaster와 EventSystem을 사용합니다.

서류를 드롭 영역으로 끌어 놓으면 빈 슬롯이 있을 때 접수합니다.
밖에 놓거나 슬롯이 가득 찬 경우 원래 부모·UI 순서·위치·크기로 즉시 복귀합니다.
접수 성공 시 책상 종이는 숨겨 회수하고, 자동 처리 완료 시 기존 정답 보상 경로를 사용합니다.
드래그 중 다른 종이가 완료돼도 책상 갱신은 드래그 종료 후 처리합니다.
모드 변경·영역 크기 변경으로 배치를 다시 만들거나 일시정지·퇴근할 때는 진행 중 드래그를 취소합니다.

기존 WorkPaperItemView가 달린 종이를 그대로 사용합니다. 버튼 OnClick이나 드롭 이벤트를 수동 연결하지 않습니다.
팩스기 처리 시간은 Upgrade Tree SO의 Fax Seconds, 속도와 슬롯 수는 기존 강화 노드에서 조절합니다.

컴파일·자동 테스트·씬 수정은 실행하지 않았습니다.
