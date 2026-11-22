using System;
using System.Collections.Generic;
using UnityEngine;

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
        private readonly Dictionary<string, CareGiftSO> art = new Dictionary<string, CareGiftSO>();
        private CareSystem care;
        private bool visible;

        private void OnEnable()
        {
            art.Clear();
            if (config != null)
                foreach (var gift in config.Gifts)
                    if (gift != null) art[gift.Id] = gift;
            if (workRoot != null) workRoot.RunStarted += Bind;
            Bind(workRoot != null ? workRoot.System : null);
        }

        private void OnDisable()
        {
            if (workRoot != null) workRoot.RunStarted -= Bind;
            if (care != null) care.Changed -= Refresh;
            care = null;
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
            foreach (var slot in slots)
            {
                if (slot?.Renderer == null) continue;
                string id = care?.Run.GetActiveGift(slot.Kind);
                Sprite sprite = id != null && art.TryGetValue(id, out var gift) && gift.Kind == slot.Kind
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
                        target.transform.localScale = new Vector3(scale, scale, 1);
                        target.transform.localPosition = new Vector3(-bounds.center.x * scale, -bounds.min.y * scale, 0);
                    }
                }
                target.enabled = visible && isActiveAndEnabled && sprite != null;
            }
        }
    }
}
