using System;
using System.Collections.Generic;
using UnityEngine;

namespace WorkGame
{
    // Work 화면에 배치합니다. RunStarted를 다시 받아 새 게임·불러오기에도 상태를 갱신합니다.
    public sealed class WorkStoryDecorationView : MonoBehaviour
    {
        [Serializable] public sealed class Decoration
        {
            public string Unlock = "dali_nameplate";
            public GameObject Target;
        }
        [SerializeField] private WorkRoot workRoot;
        [SerializeField] private List<Decoration> decorations = new List<Decoration>();
        private void OnEnable()
        {
            if (workRoot != null)
            {
                workRoot.RunStarted += Bind;
                workRoot.CareFlowChanged += Refresh;
            }
            Refresh();
        }
        private void OnDisable()
        {
            if (workRoot == null) return;
            workRoot.RunStarted -= Bind;
            workRoot.CareFlowChanged -= Refresh;
        }
        private void Bind(WorkSystem _) => Refresh();
        private void Refresh()
        {
            foreach (var item in decorations)
                if (item.Target != null) item.Target.SetActive(workRoot != null && workRoot.CareFlow != null
                    && workRoot.CareFlow.HasStoryUnlock(item.Unlock));
        }
    }
}
