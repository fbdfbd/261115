using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace WorkGame.Editor
{
    public static class WorkUpgradeMaker
    {
        private const string Folder = WorkDataMaker.Folder + "/Upgrades";

        [MenuItem("Work Game/Upgrades/Create Sample Tree")]
        public static void CreateSamples()
        {
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            var tree = WorkDataMaker.LoadOrCreate<WorkUpgradeTreeSO>(Folder + "/WorkUpgradeTree.asset");
            var made = new Dictionary<string, WorkUpgradeSO>();

            void Add(string id, string name, string description, int cost, WorkUpgradeEffectKind kind, double value, string parent = null)
            {
                string path = Folder + "/" + id + ".asset";
                var node = AssetDatabase.LoadAssetAtPath<WorkUpgradeSO>(path);
                if (node == null)
                {
                    node = WorkDataMaker.LoadOrCreate<WorkUpgradeSO>(path);
                    node.Id = id; node.Name = name; node.Description = description; node.Cost = cost;
                    node.Effects.Add(new WorkUpgradeEffect { Kind = kind, Value = value });
                    if (parent != null) node.Required.Add(made[parent]);
                    EditorUtility.SetDirty(node);
                }
                made[id] = node;
                if (!tree.Nodes.Contains(node)) tree.Nodes.Add(node);
            }

            Add("fax", "팩스기", "서류를 끌어 넣으면 다른 직원에게 처리를 맡깁니다.", 500, WorkUpgradeEffectKind.FaxUnlock, 1);
            Add("fax_speed_1", "팩스 속도 I", "팩스 처리 속도 +25%", 400, WorkUpgradeEffectKind.FaxSpeedPercent, 25, "fax");
            Add("fax_speed_2", "팩스 속도 II", "팩스 처리 속도 +50% 추가", 800, WorkUpgradeEffectKind.FaxSpeedPercent, 50, "fax_speed_1");
            Add("fax_slots_1", "팩스 인원 I", "동시에 맡길 수 있는 서류 +1", 600, WorkUpgradeEffectKind.FaxSlots, 1, "fax");
            Add("fax_slots_2", "팩스 인원 II", "동시에 맡길 수 있는 서류 +1 추가", 1000, WorkUpgradeEffectKind.FaxSlots, 1, "fax_slots_1");
            Add("sort", "서류 정렬", "서류를 카테고리 순서로 정렬합니다. ON/OFF 가능.", 300, WorkUpgradeEffectKind.SortUnlock, 1);
            Add("newbie_protection", "신입 보호", "모든 오답의 숙련·근태 감점이 50% 감소합니다. 소수점은 버리며 오답 기록과 평가는 유지됩니다.",
                300, WorkUpgradeEffectKind.NewbieProtection, 50);
            Add("early_leave", "칼퇴의 요령", "모든 업무를 끝내고 조기퇴근하면 이달 총 획득 숙련에 남은 시간 비율만큼 보너스를 받습니다. 최대 10%.",
                400, WorkUpgradeEffectKind.EarlyLeaveBonusPercent, 10);
            Add("highlight_1", "검토 강조 I", "비교 항목의 핵심 단어에 색상을 표시합니다.", 400, WorkUpgradeEffectKind.HighlightLevel, 1);
            Add("highlight_2", "검토 강조 II", "서류를 연 뒤 1초 후 정답 후보를 표시합니다.", 800, WorkUpgradeEffectKind.HighlightLevel, 2, "highlight_1");
            Add("highlight_3", "검토 강조 III", "서류를 열자마자 정답 후보를 표시합니다.", 1600, WorkUpgradeEffectKind.HighlightLevel, 3, "highlight_2");
            Add("instant_1", "즉시 처리 I", "서류 클릭 시 10% 확률로 즉시 정답 처리합니다.", 2000, WorkUpgradeEffectKind.InstantChancePercent, 10);
            Add("instant_2", "즉시 처리 II", "즉시 처리 확률 +15%p", 3000, WorkUpgradeEffectKind.InstantChancePercent, 15, "instant_1");
            Add("instant_3", "즉시 처리 III", "즉시 처리 확률 +25%p", 4500, WorkUpgradeEffectKind.InstantChancePercent, 25, "instant_2");
            Add("point_flat_1", "숙련 보상 I", "정답 기본 보상 +20", 200, WorkUpgradeEffectKind.PointFlat, 20);
            Add("point_flat_2", "숙련 보상 II", "정답 기본 보상 +30 추가", 500, WorkUpgradeEffectKind.PointFlat, 30, "point_flat_1");
            Add("point_percent_1", "숙련 효율 I", "숙련 포인트 획득량 +10%", 300, WorkUpgradeEffectKind.PointPercent, 10);
            Add("point_percent_2", "숙련 효율 II", "숙련 포인트 획득량 +20% 추가", 700, WorkUpgradeEffectKind.PointPercent, 20, "point_percent_1");
            Add("point_luck_1", "행운의 숙련", "10% 확률로 숙련 포인트를 2배 획득합니다.", 600, WorkUpgradeEffectKind.PointChancePercent, 10);
            Add("point_luck_2", "행운 확률 I", "배수 획득 확률 +10%p", 900, WorkUpgradeEffectKind.PointChancePercent, 10, "point_luck_1");
            Add("point_luck_3", "행운 확률 II", "배수 획득 확률 +15%p", 1400, WorkUpgradeEffectKind.PointChancePercent, 15, "point_luck_2");
            Add("point_triple", "행운 배수 증가", "배수 발동 시 2배 대신 3배 획득합니다.", 1200, WorkUpgradeEffectKind.PointMultiplier, 3, "point_luck_1");
            Add("tuition_1", "인맥 I", "달이 수업료 10% 할인", 300, WorkUpgradeEffectKind.TuitionDiscountPercent, 10);
            Add("tuition_2", "인맥 II", "수업료 할인율 +10%p", 600, WorkUpgradeEffectKind.TuitionDiscountPercent, 10, "tuition_1");
            Add("tuition_3", "인맥 III", "수업료 할인율 +10%p 추가", 900, WorkUpgradeEffectKind.TuitionDiscountPercent, 10, "tuition_2");
            Add("time_1", "빠른 손 I", "정시 근무시간 +10초. 업무를 끝내면 조기퇴근 가능.", 300, WorkUpgradeEffectKind.ExtraSeconds, 10);
            Add("time_2", "빠른 손 II", "정시 근무시간 +10초 추가", 600, WorkUpgradeEffectKind.ExtraSeconds, 10, "time_1");
            Add("time_3", "빠른 손 III", "정시 근무시간 +10초 추가. 총 +30초.", 900, WorkUpgradeEffectKind.ExtraSeconds, 10, "time_2");
            Add("passion_1", "열정 페이 I", "다음 달 신규 업무량 +10%. 기본 배정량 기준이며 소수점은 버립니다.",
                300, WorkUpgradeEffectKind.WorkloadPercent, 10);
            Add("passion_2", "열정 페이 II", "다음 달 신규 업무량 총 +20%. 미처리 업무에는 중복 적용하지 않습니다.",
                600, WorkUpgradeEffectKind.WorkloadPercent, 20, "passion_1");
            Add("passion_3", "열정 페이 III", "다음 달 신규 업무량 총 +30%. 선택된 카테고리의 항목이 모자라면 다른 카테고리에서 보충합니다.",
                900, WorkUpgradeEffectKind.WorkloadPercent, 30, "passion_2");
            Add("healthy_food_1", "건강식 I", "달이의 수업·시너지 스탯 성장 보정 +5%. 가구 보정률에 합산됩니다.",
                400, WorkUpgradeEffectKind.CareGrowthPercent, 5);
            Add("healthy_food_2", "건강식 II", "달이의 수업·시너지 스탯 성장 보정 총 +10%. 이미 획득한 스탯에는 소급하지 않습니다.",
                800, WorkUpgradeEffectKind.CareGrowthPercent, 10, "healthy_food_1");
            Add("do_not_disturb", "방해금지모드", "ON이면 업무 중 메신저 말풍선 알림을 숨깁니다. OFF로 바꾸면 이후 알림부터 표시합니다. 판정과 보상은 유지됩니다.",
                300, WorkUpgradeEffectKind.DoNotDisturbUnlock, 1);

            EditorUtility.SetDirty(tree);
            tree.Check();
            AssetDatabase.SaveAssets();
            Selection.activeObject = tree;
            Debug.Log("강화 트리 준비 완료. 기존 노드의 수정한 값은 유지했습니다.", tree);
        }

        [MenuItem("Work Game/Upgrades/Fill Empty Highlight Words")]
        public static void FillWords()
        {
            string[] keywords = { "신뢰도", "숙련도", "정확도", "일치율", "성공률", "확률", "실패율", "위험도", "오인식률",
                "오작동률", "오해율", "오류율", "누락률", "불일치율", "미지급", "고장률", "부족", "누설", "오역률",
                "재고량", "비축량", "충족률", "보급률", "완료율", "확보율", "공개율", "활용률", "기여도", "필요도",
                "접근성", "장벽", "지연", "대기", "소비량", "전력", "효율", "전문성", "횟수", "수요량", "일수", "체감온도" };
            foreach (var category in WorkDataMaker.FindCategories())
            {
                Undo.RecordObject(category, "Fill Highlight Words");
                foreach (var metric in category.Metrics.Where(x => x != null && !string.IsNullOrEmpty(x.Name)))
                {
                    if (metric.HighlightWords != null && metric.HighlightWords.Count > 0) continue;
                    string word = keywords.FirstOrDefault(metric.Name.Contains) ?? metric.Name;
                    metric.HighlightWords = new List<string> { word };
                }
                EditorUtility.SetDirty(category);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("비어 있던 강조 단어를 채웠습니다. 각 카테고리 SO에서 자유롭게 수정하세요.");
        }

        [MenuItem("Work Game/Upgrades/Create Sample Tree", true)]
        [MenuItem("Work Game/Upgrades/Fill Empty Highlight Words", true)]
        private static bool CanEdit() => !EditorApplication.isPlayingOrWillChangePlaymode;
    }
}
