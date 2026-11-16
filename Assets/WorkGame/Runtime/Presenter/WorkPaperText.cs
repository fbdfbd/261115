namespace WorkGame
{
    public static class WorkPaperText
    {
        public static string GetNumber(WorkPaper paper) => $"조달검토-{paper.Month:00}-{paper.Id:0000}";

        public static string GetRule(WorkPaper paper)
        {
            string direction = paper.Rule == PickRule.Highest ? "높은" : "낮은";
            string target = paper.Target ?? "대상";
            char last = target.Length == 0 ? '\0' : target[target.Length - 1];
            bool hasFinal = last >= '가' && last <= '힣' && (last - '가') % 28 != 0;
            return $"검토 기준: {paper.Metric}\n값이 가장 {direction} {target}{(hasFinal ? "을" : "를")} 추천하여 주시기 바랍니다.";
        }
    }
}
