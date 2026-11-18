# 공문서 데이터 확인

- `WorkCategories.json`: 실제 저장된 `Data/Category_01.asset` ~ `Category_15.asset`에서 내보낸 열람용 데이터. 15개 업무, 총 675개 상황.
- `Categories.csv`: 기존 Unity 데이터 편집기의 가져오기 원본.
- 게임이 읽는 데이터는 SO입니다. JSON을 편집해도 게임에 자동 반영되지 않습니다.

JSON에서 `"Id": "10"`을 찾으면 평가위원 업무입니다. 각 업무의 `Metrics`에 상황들이 순서대로 들어 있습니다.

| 필드 | 의미 |
| --- | --- |
| Title / ShortName | 업무 이름 / 짧은 이름 |
| HudColor | SO에 설정된 HUD 색상. CSV에는 없는 필드 |
| Target | 후보 이름 뒤에 붙는 대상. 평가위원 업무는 `A 후보`, `B 후보`, `C 후보` |
| Metrics.Title / Agency | 상황 제목 / 요청 기관 |
| Metrics.Difficulty | Easy: 쉬움, Normal: 보통. 카테고리마다 쉬움 20개·보통 25개 |
| Metrics.Name / Unit | 비교 항목 / 단위 |
| Rule | Highest: 가장 큰 값 선택, Lowest: 가장 작은 값 선택 |
| Display | Number: 숫자, VerticalBar: 세로 막대, HorizontalBar: 가로 막대, Stars: 원형 점수, Grade: 등급 |
| Min / Max / ScaleMax | 후보 수치 생성 범위 / 막대 전체 길이 기준. ScaleMax가 0이면 Max 사용 |
| HighlightWords | 비교 항목에서 힌트 색상을 적용할 단어 |

개별 A/B/C의 수치는 실행 중 생성됩니다. JSON에는 그 생성 범위가 들어 있습니다.

1~3월에는 매달 카테고리 1개에서 Easy만 출제합니다. 4~6월은 카테고리 2개, 7~12월은 3개에서 Easy·Normal 전체를 추첨합니다. 같은 달·카테고리 안에서 상황은 중복되지 않으며, 기존 월별 문서 수는 유지합니다. SO Inspector와 Work Game/Data Editor의 각 Metrics 항목에서도 Difficulty를 편집할 수 있습니다.

과거 CSV처럼 Difficulty 열이 없거나 비어 있으면 Normal로 처리합니다. 이 경우 1~3월에 필요한 Easy 문항 수를 별도로 확보해야 합니다.

프로젝트 루트에서 다시 내보내기:

```text
python Tools/work_document_data.py export
```

저장된 SO ↔ CSV ↔ JSON 대조:

```text
python Tools/work_document_data.py check
```

차이가 있으면 업무·상황 순번·필드와 양쪽 값을 출력하고 실패 코드로 종료합니다. CSV 대조는 CSV에 존재하는 필드 전부를, JSON 대조는 HUD 색상까지 포함한 SO 데이터 필드 전부를 확인합니다. 메모리에만 있고 아직 저장하지 않은 에디터 변경은 검사 대상이 아닙니다.
