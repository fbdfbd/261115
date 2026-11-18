# ProgressSlider 흐름 효과 연결

02_Game 씬의 HudView ProgressSlider에는 아래 구성을 연결해 두었습니다. 다른 씬에 적용할 때 참고하세요.

## 권장 계층

```text
ProgressSlider                 기존 Slider 유지
├─ Background                   빈 게이지 배경
├─ RoundMask                    Image + Mask, 항상 전체 크기
│  └─ FlowFill                  RawImage + WorkProgressFlow, 항상 전체 크기
├─ Frame                        테두리 이미지 (필요한 경우)
└─ ProgressText                 기존 텍스트
```

1. 기존 Slider의 **Fill Rect를 None**으로 비웁니다. 기존 Fill 이미지는 비활성화하거나 제거합니다. Handle Rect도 None으로 둡니다. Slider 자체와 HudView의 Progress Slider 연결은 유지합니다.
2. RoundMask를 게이지 내부 영역 전체에 맞춥니다. Image에는 내부가 불투명하고 모서리 바깥만 투명한 둥근 이미지를 넣습니다. **테두리 선만 있는 이미지는 마스크로 사용할 수 없습니다.** 필요하면 Image의 Sliced를 사용하고 Sprite Editor의 Border를 설정합니다. 이 이미지는 진행도와 무관하게 전체 크기를 유지하므로 저진행도 압축이 없습니다.
3. RoundMask에 **Mask**를 추가하고 **Show Mask Graphic을 끕니다.** Image의 Raycast Target도 끕니다. RectMask2D 대신 둥근 모양을 따르는 일반 Mask를 사용합니다.
4. 자식 FlowFill에 **RawImage**를 추가합니다. 앵커는 가로·세로 Stretch, Left/Right/Top/Bottom은 모두 0입니다. 진행도에 따라 이 오브젝트의 크기를 줄이지 않습니다.
5. RawImage의 **Texture = Pattern_1.png**, **Material = Assets/WorkGame/Art/UI/ProgressFlow.mat**, **Color = White**으로 설정합니다. Maskable은 켜고 Raycast Target은 끕니다. UV Rect는 기본값을 유지합니다. 원본 이미지를 수정하거나 Sprite Atlas에 넣을 필요가 없습니다.
6. FlowFill에 **WorkProgressFlow**를 추가합니다. RawImage가 먼저 추가된 상태여야 합니다. Add Component로 WorkProgressFlow를 추가하면 RawImage도 자동으로 먼저 생성됩니다. 같은 오브젝트에 추가적인 커스텀 머티리얼 수정 컴포넌트는 두지 않습니다.
7. HudView의 WorkHudView 컴포넌트에서 새 **Progress Flow** 필드에 FlowFill의 WorkProgressFlow를 연결합니다. Slider On Value Changed 이벤트를 연결할 필요는 없습니다. 기존 SetValueWithoutNotify 경로에서도 HudView가 효과에 직접 진행도를 전달합니다.
8. 별도 Frame이 있다면 RoundMask보다 뒤쪽 형제로 두어 위에 그려지게 합니다. ProgressText 역시 마스크 밖의 형제로 유지합니다.

## 조절

| 항목 | 기본값 | 의미 |
|---|---:|---|
| Base Color | 기존 노란색 유지 | 진행 중 게이지 색상입니다. |
| Completed Color | 초록색 | 100% 완료 시 게이지 색상입니다. |
| Color Fade Seconds | 0.35 | 채우기가 끝난 뒤 완료 색상으로 전환하는 시간입니다. |
| Tile Size | 160 | 원본 정사각형 패턴 한 장을 표시할 UI 크기. 작을수록 무늬가 촘촘합니다. |
| Speed | 24 | 초당 이동하는 UI 단위. 양수는 오른쪽, 음수는 왼쪽, 0은 정지입니다. |
| Strength | 0.3 | 기본 게이지 색과 패턴을 혼합하는 강도입니다. |
| Pattern Color | White | 패턴에 곱하는 색상과 혼합 알파입니다. |
| Fill Seconds | 0.35 | 진행도 증가 시 부드럽게 차오르는 시간입니다. 0이면 즉시 반영합니다. |
| Progress | 0.5 | 에디터의 정적 미리보기. 플레이 중에는 HudView가 실제 값으로 덮어씁니다. |

기본 색과 완료 색은 WorkProgressFlow의 Base Color / Completed Color로 정합니다. RawImage의 Color는 White를 유지합니다(추가 틴트와 알파에 곱해집니다). 패턴이 선명하지 않으면 Strength를 0.4~0.5까지 올려 봅니다. 전체 효과 투명도는 RawImage Color의 Alpha 또는 부모 CanvasGroup으로 조절합니다.

02_Game에서는 `Canvas_HUD_Default` 아래 `ProgressSlider / RoundMask / FlowFill`을 선택해 색상을 바꿉니다. **WorkProgressFlow → Base Color**가 기본 게이지 색, **Completed Color**가 완료 색, **Color Fade Seconds**가 완료 색상 전환 시간입니다. **Pattern Color**는 무늬 색, **Strength**는 무늬 혼합 강도입니다. 플레이를 종료해도 유지하려면 편집 모드에서 바꿉니다.

진행도 증가는 GPU에서 OutCubic 곡선으로 보간합니다. 연속 증가하면 현재 표시 위치에서 새 목표까지 이어집니다. 최초 데이터 표시와 진행도 감소(새 달 시작 등)는 즉시 반영합니다. 전용 Update나 Tween은 추가하지 않았습니다.

에디터에서는 Progress를 0 / 0.01 / 0.1 / 0.5 / 1로 바꾸어 외곽과 잘림을 확인할 수 있습니다. 흐름은 플레이 모드에서 확인합니다. 진행 끝부분은 수직으로 잘리고, 전체 게이지의 외곽만 둥글게 유지됩니다.

## 비용과 제한

- 흐름 전용 Update, 코루틴, Tween은 없습니다. Unity의 셰이더 시간으로 패턴을 움직입니다. Time.timeScale이 0이면 흐름도 멈추지만, 게임 시스템의 독립적인 pause 플래그에는 연결하지 않았습니다.
- RawImage 한 장과 패턴 텍스처 샘플링 1회입니다. Mask의 스텐실 비용 및 전용 머티리얼로 인한 배치 분리는 별도입니다. 보이지 않는 진행 영역도 전체 사각형의 래스터화 대상이 될 수 있으므로 대형 UI 다수에 무조건 저비용인 방식은 아닙니다.
- 설정·진행도·크기가 바뀔 때만 머티리얼을 갱신합니다. 진행도는 0~1로 제한하며 무늬의 밀도와 속도는 진행도와 무관합니다.
- UI 스텐실, CanvasGroup 알파, RectMask2D 클리핑과 softness에 대응합니다. 머티리얼 복사본은 컴포넌트별로 재사용하고 비활성화 시 정리합니다. 공유 머티리얼은 수정하지 않습니다.
- 셰이더에서 UV를 반복하므로 원본 텍스처의 Clamp 설정을 변경할 필요가 없습니다. 다만 원본 이미지 자체의 양쪽 경계가 일치하지 않으면 이음새가 보일 수 있습니다. 이 경우 이미지 타일 경계 보정이 필요합니다.
- 실제 모바일 성능과 색상/이음새는 씬 연결 후 기기에서 확인해야 합니다.

100%까지 부드럽게 채워진 뒤 완료 색상으로 SmoothStep 전환합니다. 최초 표시가 이미 100%라면 완료 색상을 바로 표시하고, 새 달 시작 등 진행도가 내려가면 기본 색으로 즉시 돌아옵니다. 색상 전환도 GPU에서 처리하며 Update나 Tween은 추가하지 않았습니다.
