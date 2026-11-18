# 저장 시스템

PlayerPrefs에 JSON으로 저장합니다. 자동 1개 + 수동 4개, 총 5칸입니다.
기존 WorkRoot와 WorkFlowRoot에 자동 저장과 복원 분기를 연결했으므로 새 컴포넌트를 붙일 필요가 없습니다.

## 저장 시점

| 시점 | 이어하기 위치 |
| --- | --- |
| 매월 업무 시작 | 해당 월의 0초. 제출·팩스 등 도중 행동은 취소 |
| 급여 정산 확인 후, 육성 첫 진입 | 급여 정산은 생략. 퇴근 반응/약속 결과부터 다시 시작 |
| 12월 육성 결과 확인 후 | 저장 당시 엔딩 후보 최대 5개. 후보 한 개면 바로 그 엔딩 |

육성 중 가구 구매·장착, 강화 구매, 대화, 시간표 편성, 육성 결과창 표시 도중에 종료해도 육성 시작으로 돌아갑니다.
수동 저장도 **현재 루프의 시작 상태**를 복사합니다. 종료할 때 진행 중 상태로 덮어쓰지 않습니다.
엔딩을 읽거나 대화를 끝낸 기록은 별도로 즉시 저장하며, 슬롯을 불러오거나 새 게임을 시작해도 유지합니다.

## 나중에 UI 연결

게임 씬 안에서 C# 코드로 호출:

```csharp
bool saved = workRoot.SaveManual(1); // 1~4
bool loaded = workRoot.LoadAuto();
bool loadedSlot = workRoot.LoadManual(2); // 1~4
string error = workRoot.SaveError;
```

타이틀에서 게임 씬으로 넘어가기 **전에** 요청합니다:

```csharp
WorkStartRequest.ContinueAuto(); // 자동 저장 이어하기
// 또는 WorkStartRequest.LoadManual(1);
// 또는 WorkStartRequest.NewRun();
SceneManager.LoadScene("02_Game");
```

요청은 게임 씬의 WorkRoot.Start가 한 번 소비합니다. 불러오기에 실패하면 새 게임으로 덮어쓰지 않습니다.
요청 없이 게임 씬을 직접 실행하면 기존처럼 새 게임을 시작하고 자동 슬롯을 갱신합니다.

슬롯 목록·삭제 등은 같은 저장 서비스로 연결합니다:

```csharp
var saves = new WorkSaveSystem(new PlayerPrefsSaveStore());
bool exists = saves.HasSave(SaveSlot.Manual1);
if (saves.TryLoad(SaveSlot.Manual1, out var info, out var error))
{
    // info.Work.Month, info.Point, info.SavedAtUtc로 슬롯 표시
}
saves.Delete(SaveSlot.Manual1); // 읽음 기록은 삭제하지 않음
```

키는 `WorkGame.Save.Auto`, `WorkGame.Save.Manual1`~`Manual4`, `WorkGame.Save.Profile`입니다. 기존 4칸의 키와 버전은 유지합니다.
Profile에는 엔딩·대화 읽음 기록과 새 회차에 계승할 구매 스킬 ID를 보관합니다.

## 새 회차 계승

- 새로하기: 확정된 체크포인트에서 구매했던 업무 스킬을 가져오고 현재 트리 SO로 효과를 다시 계산합니다.
- 남은 숙련포인트·현금·가구·달이 스탯·친밀도는 계승하지 않습니다.
- 육성 중 아직 확정하지 않은 구매는 기존 시작 체크포인트 복원 규칙에 따라 되돌아갑니다.
- 슬롯 불러오기는 해당 슬롯의 상태를 정확히 복원합니다. 계승 기록을 과거 슬롯에 덧씌우지 않습니다.
- 이전 버전 저장 파일에서도 구매 스킬을 읽습니다. 없는 계승 필드는 빈 목록으로 처리하며 저장 버전은 유지합니다.
- 스킬 계승 기록은 슬롯 삭제로 지워지지 않습니다. 엔딩·대화 읽음 기록을 갱신해도 유지됩니다.

## 형식과 오류

읽음 기록 파일의 오류는 `saves.ProfileError`로 확인할 수 있습니다.
업무는 Seed로 다시 생성하며, 저장 당시와 업무 설정/생성 결과가 달라졌으면 복원을 거부합니다.
육성 가구·엔딩·대화의 저장 ID가 삭제된 경우도 복원 오류로 알려줍니다.
현재 형식 버전은 1입니다. 이후 데이터 구조 변경 시 버전 변환을 추가할 자리를 두었습니다.

## 유닛테스트

Unity **Test Runner > EditMode > WorkGame.Tests.WorkSaveTests**에서 실행합니다.
실제 PlayerPrefs 대신 메모리 저장소를 써서 플레이 중 저장 파일을 건드리지 않습니다.

- 자동 저장 시점, 자동 1개/수동 4개 독립성, 타이틀의 1회 이어하기 요청
- 업무 타이머·보상·팩스 롤백, 육성 수업료·가구·스텟·대화 롤백
- 확정한 육성/강화/가구 유지, 이월 서류·분기 평가·난수 보상 복원
- 약속 결과의 중복 적용 방지, 엔딩 1개/5개/백수 복귀, 읽음 기록 유지
- 잘못된 JSON/버전/중첩 데이터/업무 원본 거부, 저장 실패 시 체크포인트 유지
- 프레임별 저장 방지, long 현금, 음수 친밀도, 주요 스텟 자동 합산

테스트 코드를 추가했으며, 컴파일·테스트 실행은 하지 않았습니다.
