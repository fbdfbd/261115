using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WorkGame
{
    // Work 화면에 배치합니다. RunStarted를 다시 받아 새 게임·불러오기에도 상태를 갱신합니다.
    public sealed class WorkStoryDecorationView : MonoBehaviour
    {
        [Serializable] public sealed class Decoration
        {
            public string Unlock = "dali_nameplate";
            public string Upgrade;
            public GameObject Target;
        }
        [SerializeField] private WorkRoot workRoot;
        [SerializeField] private List<Decoration> decorations = new List<Decoration>();
        private WorkSystem work;
        private void OnEnable()
        {
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
            if (work != null) work.Changed -= Refresh;
            work = null;
        }
        private void Bind(WorkSystem system)
        {
            if (work != null) work.Changed -= Refresh;
            work = system;
            if (work != null) work.Changed += Refresh;
            Refresh();
        }
        private void Refresh()
        {
            foreach (var item in decorations)
            {
                if (item.Target == null) continue;
                bool unlocked = !string.IsNullOrEmpty(item.Upgrade)
                    ? work != null && work.Run.BoughtUpgrades.Contains(item.Upgrade)
                    : workRoot != null && workRoot.CareFlow != null && workRoot.CareFlow.HasStoryUnlock(item.Unlock);
                if (item.Target.activeSelf != unlocked) item.Target.SetActive(unlocked);
            }
        }
    }
}
