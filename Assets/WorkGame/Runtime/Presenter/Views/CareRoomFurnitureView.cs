using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame
{
    // 슬롯 위치는 씬의 부모 Transform으로 조정합니다. 교체 시에는 자식 Visual만 갱신합니다.
    public sealed class CareRoomFurnitureView : MonoBehaviour
    {
        [Serializable]
        public sealed class Slot
        {
            public CareGiftKind Kind;
            public SpriteRenderer Renderer;
            public Vector2 Size = new Vector2(3, 3);
        }

        [SerializeField] private WorkRoot workRoot;
        [SerializeField] private CareConfigSO config;
        [SerializeField] private List<Slot> slots = new List<Slot>();
        [Tooltip("사진첩 약속 성공 후 Care 배경에 표시할 사진 오브젝트입니다. 일반 성공과 조기 성공에 공통으로 사용합니다.")]
        [SerializeField] private Image albumPhoto;
        private readonly Dictionary<string, CareGiftSO> art = new Dictionary<string, CareGiftSO>();
        private CareSystem care;
        private bool visible;

        private void OnEnable()
        {
            art.Clear();
            if (config != null)
                foreach (var gift in config.Gifts)
                    if (gift != null) art[gift.Id] = gift;
            if (workRoot != null)
            {
                workRoot.RunStarted += Bind;
                workRoot.CareFlowChanged += Refresh;
            }
            Bind(workRoot != null ? workRoot.System : null);
        }

        private void OnDisable()
        {
            if (workRoot != null)
            {
                workRoot.RunStarted -= Bind;
                workRoot.CareFlowChanged -= Refresh;
            }
            if (care != null) care.Changed -= Refresh;
            care = null;
            if (albumPhoto != null) albumPhoto.gameObject.SetActive(false);
            foreach (var slot in slots)
                if (slot?.Renderer != null) { slot.Renderer.enabled = false; slot.Renderer.sprite = null; }
        }

        private void Bind(WorkSystem _)
        {
            if (care != null) care.Changed -= Refresh;
            care = workRoot != null ? workRoot.Care : null;
            if (care != null) care.Changed += Refresh;
            Refresh();
        }

        public void SetVisible(bool value)
        {
            visible = value;
            Refresh();
        }

        private void Refresh()
        {
            if (albumPhoto != null)
                albumPhoto.gameObject.SetActive(visible && isActiveAndEnabled && workRoot != null
                    && workRoot.CareFlow != null && !workRoot.CareFlow.IsIntro
                    && workRoot.CareFlow.HasReadSuccessfulPromise("album"));
            foreach (var slot in slots)
            {
                if (slot?.Renderer == null) continue;
                string id = care?.Run.GetActiveGift(slot.Kind);
                CareGiftSO gift = null;
                Sprite sprite = id != null && art.TryGetValue(id, out gift) && gift.Kind == slot.Kind
                    ? gift.RoomSprite : null;
                var target = slot.Renderer;
                if (target.sprite != sprite)
                {
                    target.sprite = sprite;
                    if (sprite != null)
                    {
                        var bounds = sprite.bounds;
                        float scale = Mathf.Min(Mathf.Max(0.01f, slot.Size.x) / Mathf.Max(0.01f, bounds.size.x),
                            Mathf.Max(0.01f, slot.Size.y) / Mathf.Max(0.01f, bounds.size.y));
                        scale *= Mathf.Max(0.01f, gift.RoomScale);
                        target.transform.localScale = new Vector3(scale, scale, 1);
                        target.transform.localPosition = new Vector3(-bounds.center.x * scale, -bounds.min.y * scale, 0);
                    }
                }
                target.enabled = visible && isActiveAndEnabled && sprite != null;
            }
        }
    }
}
