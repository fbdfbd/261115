using System;
using System.Linq;
using UnityEngine;

namespace WorkGame
{
    public sealed class GameCollectionPresenter : IDisposable
    {
        private readonly CareConfigSO config;
        private readonly WorkSaveSystem saves;
        private readonly GameCollectionView view;
        private readonly CareEndingSO[] endings;
        private bool connected;
        public GameCollectionPresenter(CareConfigSO config, WorkSaveSystem saves, GameCollectionView view)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config)); this.saves = saves; this.view = view;
            endings = config.Endings.Concat(new[] { config.FallbackEnding }).Where(ending => ending != null)
                .GroupBy(ending => ending.Id).Select(group => group.First())
                .OrderByDescending(ending => ending.Priority).ThenBy(ending => ending.Id, StringComparer.Ordinal).ToArray();
        }
        public void Connect()
        {
            if (connected || view == null) return;
            connected = true; view.Selected += Select; view.BackRequested += Back;
        }
        public void Open()
        {
            if (view == null) return;
            if (endings.Length > view.Capacity) throw new InvalidOperationException("엔딩 개수보다 모음집 카드가 적습니다.");
            view.Open(endings.Count(ending => saves.WasEndingRead(ending.Id)), endings.Length);
            for (int i = 0; i < endings.Length; i++)
            {
                var ending = endings[i];
                view.ShowItem(i, ending.Id, ending.Name, GetImage(ending), saves.WasEndingRead(ending.Id));
            }
        }
        private void Select(string id)
        {
            if (!view.IsOpen) return;
            var ending = endings.FirstOrDefault(item => item.Id == id);
            if (ending == null) return;
            bool fallback = ending == config.FallbackEnding;
            string main = fallback ? "다른 직업 엔딩의 조건을 충족하지 않음"
                : ending.MainNeeds.Count == 0 ? "대표 능력치 조건 없음"
                : string.Join(" · ", ending.MainNeeds.OrderBy(need => need.Stat).Select(need =>
                    CareStatNames.GetName(need.Stat) + " " + CareGradeNames.GetName(need.Grade) + " 이상"));
            var columns = new string[4]; int maxLines = 0;
            for (int i = 0; i < 4; i++)
            {
                var needs = ending.Needs.Where(need => (int)CareStats.GetMain(need.Stat) == i).OrderBy(need => need.Stat).ToArray();
                maxLines = Math.Max(maxLines, needs.Length);
                columns[i] = needs.Length == 0 ? "" : "<b>" + CareStatNames.GetName((CareStat)i) + "</b>\n"
                    + string.Join("\n", needs.Select(need => CareStatNames.GetName(need.Stat) + " " + need.Amount + " 이상"));
            }
            view.ShowDetail(ending.Name, GetImage(ending), ending.Text, main, columns, maxLines, saves.WasEndingRead(id));
        }
        private static Sprite GetImage(CareEndingSO ending) => ending.CollectionImage != null ? ending.CollectionImage
            : ending.Pages?.FirstOrDefault(page => page != null && page.Image != null)?.Image;
        private void Back() => view.ShowList();
        public void Dispose()
        {
            if (!connected) return;
            connected = false; view.Selected -= Select; view.BackRequested -= Back;
        }
    }
}
