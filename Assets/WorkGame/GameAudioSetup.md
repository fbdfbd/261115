# BGM 설정

## 1. 곡 설정 SO

Project 창에서 `Create > Work Game > Audio > BGM`으로 GameBgmSO를 만듭니다.

- Title / Tutorial / Ending: 타이틀, 향후 MONTH 이전 튜토리얼, 엔딩 반복곡.
- Collection: 타이틀 컬렉션 전용 곡. 미지정 시 Title 유지, 닫으면 Title로 복귀합니다. 별도 씬 연결은 필요 없습니다.
- Months: 12개. Element 0이 MONTH 1, Element 11이 MONTH 12입니다. 각 Work / Care에 곡을 지정합니다.
- Default Work / Default Care: 월별 Clip이 비어 있을 때 사용할 곡입니다. 기본 곡도 없으면 무음으로 전환합니다.
- Volume: 곡별 기본 음량(0~1). 옵션 음량과 별도로 적용됩니다.
- Fade Seconds: 페이드아웃과 페이드인 각각의 시간(초), 기본값 1.5. 곡 교체는 1.5초 아웃 후 1.5초 인으로 총 3초입니다. 0은 즉시 전환입니다.
- 모든 곡은 2D로 반복 재생합니다. 같은 Clip이면 MONTH/구간이 달라져도 처음부터 다시 재생하지 않습니다.

## 2. AudioMixer와 공용 프리팹

1. AudioMixer 에셋을 만들고 Master 아래에 Music, SFX 그룹을 만듭니다.
2. 각 그룹의 Volume을 우클릭하여 Expose하고, Exposed Parameters에서 정확히 `MusicVol`, `SFXVol`로 이름을 지정합니다.
3. 별도 빈 루트 GameObject에 `GameAudioRoot`를 추가하고 프리팹으로 저장합니다.
4. Music Group / Sfx Group에 해당 Mixer 그룹을 연결합니다. 파라미터 이름을 다르게 만들었다면 컴포넌트의 이름도 맞춥니다.
5. 프리팹 루트와 컴포넌트는 활성화합니다. AudioSource는 직접 추가하지 않습니다. 런타임에 BGM용 1개가 생성됩니다.

공용 프리팹은 Canvas, WorkRoot, 카메라를 포함하지 않는 전용 루트로 만듭니다. AudioListener도 추가하지 않습니다.
씬 연결 컴포넌트가 필요할 때 한 번 생성하며 DontDestroyOnLoad로 유지됩니다. 어느 씬에서 직접 실행해도 초기화됩니다.
씬에 공용 프리팹을 따로 배치할 필요는 없습니다.

Mixer 연결이 빠진 경우 BGM은 AudioSource 음량으로 옵션을 반영하고 경고를 한 번 냅니다.
SFX는 공용 재생기가 별도 자식 AudioSource 풀로 재생합니다. BGM과 독립적이며 옵션의 효과음 음량을 따릅니다.
기본 UI 클릭음은 아직 자동 연결하지 않습니다.

## Work 효과음

1. `Create > Work Game > Audio > Work SFX`로 효과음 SO를 만듭니다.
2. 기존 BGMSO의 `Work Sounds`에 연결합니다. 씬·개별 서류·버튼의 추가 연결은 필요 없습니다.
3. Work SFX SO에서 1~14번 헤더를 보고 곡을 지정합니다. 2번 집기/내려놓기는 두 칸으로 분리했습니다.

각 효과음은 Clip / Volume / Pitch / Min Interval을 설정합니다. 빈 Clip은 무음입니다.
Min Interval은 같은 슬롯의 짧은 시간 내 재생을 제한합니다. 기본 호버는 0.06초, 나머지는 제한 없음입니다.
Hold Loop와 Fax Loop는 반복 재생하며 실제 누르기/팩스 동작이 끝나거나 일시정지·퇴근·로드·씬 이동 시 정지합니다.
시간 경고는 기본 남은 10초에 진입할 때 월 1회입니다. Low Time Seconds로 바꾸고 0으로 끌 수 있습니다.
버블은 실제 표시되는 순간 종류별 하나만 재생하며, 즉시완료와 배수 보상이 겹치면 즉시완료 소리를 우선합니다.
방해 금지로 숨긴 버블, 초기 진행도 표시, 월 초기화에는 소리를 내지 않습니다.
성과표는 표시되는 각 행 시작 시 Row, 전체 연출 완료 시 Report Complete를 재생합니다.
Work Sounds는 플레이 시작 전에 연결합니다.

## 3. 씬 연결 (사용자가 설정할 부분)

`01_Title`, `02_Game` 각각에 활성 빈 오브젝트를 하나 만들고 `GameBgmSceneRoot`를 추가합니다.
업무/Care 전환이나 옵션 열고 닫기에 따라 비활성화되는 그룹 안에는 두지 않습니다.

| 필드 | 01_Title | 02_Game |
|---|---|---|
| Scene | Title | Game |
| Music | 공용 GameBgmSO | 같은 SO |
| Audio Prefab | GameAudioRoot 프리팹 | 같은 프리팹 |
| Options Root | 타이틀 GameOptionsRoot | 게임 GameOptionsRoot |
| Work Root | 비움 | 게임 WorkRoot |

씬당 하나의 GameBgmSceneRoot를 사용합니다. 게임 씬의 WorkRoot와 OptionsRoot는 시작부터 활성화합니다.
기존 GameOptionsRoot의 On Music Volume / On Sound Volume에 추가 UnityEvent 연결은 하지 않습니다.
GameBgmSceneRoot가 Options의 이벤트를 직접 구독하고 저장된 현재 음량도 초기 반영합니다.
옵션 프리팹이나 UI 슬라이더를 수정할 필요가 없습니다.

## 전환 규칙

- 타이틀 옵션: Title 유지. 컬렉션 열기/닫기는 Collection↔Title 전환.
- 정규 업무와 급여 정산서: 해당 월 Work 유지.
- Work 제한 시간(기본 90초 + 강화 추가 시간)을 초과하면 BGM을 Fade Seconds에 걸쳐 끕니다.
  야근 후 급여 정산까지 무음을 유지하고 Care 진입 시 해당 월 Care 곡을 재생합니다.
  Care 퇴근 반응의 정시 유예(기본 10초)는 BGM 종료 시점에 적용하지 않습니다.
- 급여 정산 확인 후 Care 진입: 해당 월 Care로 전환.
- Care 대화/가구/강화/시간표/육성 결과: Care 유지.
- 다음 월 업무 시작: 다음 월 Work로 전환.
- EndingChoice / Ended: Ending 반복. 마지막 월 업무 종료만으로 엔딩 곡을 시작하지 않습니다.
- Care가 없는 업무 전용 구성에서는 AfterWork = Care, Finished = Ending입니다.
- 옵션/일시정지: 곡과 페이드 유지. 페이드는 unscaledDeltaTime을 사용합니다.
- 로드/새 게임: 기존 시스템 구독과 임시 이벤트 핸들을 정리하고 복원된 현재 상태로 재선택합니다.
- 씬 종료: 해당 씬의 구독과 임시 핸들을 해제합니다. 다음 씬이 제어권을 얻으면 이전 씬의 요청은 무효입니다.

사운드에서 페이드인아웃은 항상 **기존 곡 완전 페이드아웃 → 정지 → 새 곡 페이드인**을 뜻합니다.
AudioSource 1개를 사용하여 두 곡이 겹치지 않습니다. 무음으로 내려가는 도중 요청이 바뀌면
진행 중인 페이드아웃을 유지하고 목적 곡만 최신 요청으로 교체합니다. 추가 무음 대기 시간은 없습니다.
처음 재생은 페이드인만, 무음 요청은 페이드아웃만 적용합니다. 같은 곡 유지 시에는 재시작하지 않습니다.
장면의 진행 이벤트는 LateUpdate까지 모아 최종 상태를 읽고, 공용 재생기는 그 뒤(실행 순서 1000)에 반영합니다.

## 향후 이벤트 BGM 연결

GameBgmSceneRoot.RequestOverride(GameBgmTrack)는 IDisposable 핸들을 반환합니다.
이벤트 연출 담당자가 핸들을 보관하고 종료/스킵/취소/비활성화에서 Dispose합니다.
연결 컴포넌트가 아직 시작되지 않았거나 제어권이 만료되었으면 null을 반환합니다.

```csharp
private System.IDisposable musicHandle;

private void BeginEvent()
{
    musicHandle?.Dispose();
    musicHandle = bgmSceneRoot.RequestOverride(eventMusic);
}

private void EndEvent()
{
    musicHandle?.Dispose();
    musicHandle = null;
}

private void OnDisable() => EndEvent();
```

- 나중에 시작된 이벤트 곡이 우선합니다. 오래된 핸들을 해제해도 최신 이벤트를 취소하지 않습니다.
- 마지막 이벤트 종료 시 현재 월/구간의 기본곡으로 복귀합니다. 이벤트 시작 당시 기본곡을 저장해 복구하지 않습니다.
- null/Clip 없는 트랙을 요청하면 이벤트 동안 일시적으로 무음입니다.
- 씬 이동, 로드, 새 게임 시 기존 핸들은 무효화됩니다. 이후 늦게 Dispose해도 새 세션에 영향을 주지 않습니다.
- 현재 조기 퇴근 약속/특정 대화에는 연결하지 않았습니다. 추후 대화 ID→곡 매핑 또는 연출 코드에서 사용합니다.
- 이벤트 복귀 시 곡은 필요한 경우 처음부터 재생합니다. 이전 곡의 재생 위치 저장/복원은 구현하지 않습니다.

## 향후 튜토리얼 연결

GameBgmSceneRoot.BeginTutorial() / EndTutorial()을 튜토리얼 시작/종료에서 명시적으로 호출합니다.
씬 연결 컴포넌트의 Start보다 먼저 BeginTutorial을 호출해도 플래그가 유지됩니다.
아직 게임 시스템이 없다는 이유만으로 튜토리얼 곡을 자동 선택하지 않습니다.
현재 WorkRoot.Start는 바로 MONTH를 시작하므로, 실제 튜토리얼 추가 시 업무 시작을 지연하는 흐름은 별도로 구현해야 합니다.
이 API는 음악만 바꾸며 게임 진행을 멈추지 않습니다.

## 검증

GameBgmSessionTests는 이벤트 중 기본곡 갱신, 중첩 이벤트/순서가 뒤바뀐 해제,
만료된 씬 핸들, 중복 해제, 일시 무음, 중복 기본곡 요청을 검사합니다.
GameBgmSelectionTests는 실제 MONTH 1~12 진행에서 급여 정산/육성 결과/엔딩 선택 시점과 미지정 곡의 기본값을 검사합니다.
전환 동작은 기존 곡 완전 정지 후 새 곡 시작, 같은 곡 유지, 정지 상태의 페이드, 전환 중 목적 곡 교체,
음량 0, 현재 기본곡 복귀, 소유권 교체, 세션 종료 후 정지를 확인합니다.
씬/프리팹/곡 연결 후에는 타이틀↔게임, 월별 Work↔Care, 이어하기, 마지막 달 엔딩,
음량 0 및 페이드 중 슬라이더 조작을 실제 음원으로 확인합니다.
