using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WorkGame
{
    public interface IGameSaveStore
    {
        string Read(string key);
        void Write(string key, string json);
        void Delete(string key);
    }

    public sealed class PlayerPrefsSaveStore : IGameSaveStore
    {
        public string Read(string key) => PlayerPrefs.GetString(key, "");
        public void Write(string key, string json) { PlayerPrefs.SetString(key, json); PlayerPrefs.Save(); }
        public void Delete(string key) { PlayerPrefs.DeleteKey(key); PlayerPrefs.Save(); }
    }

    // 현재 루프의 시작 JSON을 보관합니다. 수동 저장은 이 JSON만 복사합니다.
    public sealed class WorkSaveSystem
    {
        public const int Version = 1;
        private readonly IGameSaveStore store;
        private readonly string prefix;
        private string checkpoint;
        private GameProfileData profile;
        public bool HasCheckpoint => checkpoint != null;
        public string ProfileError { get; private set; }

        public WorkSaveSystem(IGameSaveStore store, string prefix = "WorkGame.Save")
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            if (string.IsNullOrWhiteSpace(prefix)) throw new ArgumentException("저장 키가 비어 있습니다.");
            this.prefix = prefix;
            profile = ReadProfile();
        }

        public GameSaveData Capture(int seed, SavePoint point, WorkSystem work, CareSystem care, CareFlowSystem flow)
        {
            var data = new GameSaveData
            {
                Version = Version, SavedAtUtc = DateTime.UtcNow.ToString("O"), Seed = seed, Point = point,
                Work = work.CaptureSave(), Care = care?.CaptureSave(point), Flow = flow?.CaptureSave()
            };
            UseCheckpoint(data);
            return data;
        }

        // 복원에 성공한 뒤 호출합니다. 수동 슬롯은 그대로 두고 자동 슬롯을 갱신합니다.
        public void UseCheckpoint(GameSaveData data)
        {
            Check(data);
            string json = JsonUtility.ToJson(data);
            store.Write(Key(SaveSlot.Auto), json);
            checkpoint = json;
        }

        public bool SaveManual(SaveSlot slot)
        {
            CheckSlot(slot);
            if (slot == SaveSlot.Auto || checkpoint == null) return false;
            store.Write(Key(slot), checkpoint);
            return true;
        }

        public bool SaveAuto()
        {
            if (checkpoint == null) return false;
            store.Write(Key(SaveSlot.Auto), checkpoint);
            return true;
        }

        public bool TryLoad(SaveSlot slot, out GameSaveData data, out string error)
        {
            data = null; error = null;
            try
            {
                string json = store.Read(Key(slot));
                if (string.IsNullOrEmpty(json)) { error = "비어 있는 저장 슬롯입니다."; return false; }
                var loaded = JsonUtility.FromJson<GameSaveData>(json);
                Check(loaded);
                // 슬롯의 포인트 등은 그대로 두고 구매 스킬만 최신 공용 상태로 복원합니다.
                loaded.Work.Upgrades = GetInheritedUpgrades();
                data = loaded;
                return true;
            }
            catch (Exception e) { error = "저장 파일을 읽지 못했습니다: " + e.Message; return false; }
        }

        public bool HasSave(SaveSlot slot) => TryLoad(slot, out _, out _);
        public SaveSlotInfo GetSlotInfo(SaveSlot slot)
        {
            try
            {
                if (string.IsNullOrEmpty(store.Read(Key(slot)))) return new SaveSlotInfo(slot, SaveSlotState.Empty);
                if (TryLoad(slot, out var data, out var error)) return new SaveSlotInfo(slot, SaveSlotState.Ready, data);
                return new SaveSlotInfo(slot, SaveSlotState.Invalid, error: error);
            }
            catch (Exception error) { return new SaveSlotInfo(slot, SaveSlotState.Invalid, error: error.Message); }
        }
        public void Delete(SaveSlot slot) => store.Delete(Key(slot));
        public bool WasEndingRead(string id) => profile.Endings.Contains(id);
        public bool HasReadEnding => profile.Endings.Any(id => !string.IsNullOrWhiteSpace(id));
        public bool WasDialogueRead(string id) => profile.Dialogues.Contains(id);
        public string[] GetInheritedUpgrades()
        {
            if (!profile.SharedUpgradesReady) MigrateUpgrades();
            return profile.Upgrades.ToArray();
        }
        public void MarkEndingRead(string id) => MarkRead(id, true);
        public void MarkDialogueRead(string id) => MarkRead(id, false);

        private void MarkRead(string id, bool ending)
        {
            if (string.IsNullOrWhiteSpace(id) || (ending ? WasEndingRead(id) : WasDialogueRead(id))) return;
            var next = new GameProfileData
            {
                Version = Version,
                Endings = ending ? profile.Endings.Append(id).ToArray() : profile.Endings,
                Dialogues = ending ? profile.Dialogues : profile.Dialogues.Append(id).ToArray(),
                Upgrades = profile.Upgrades,
                SharedUpgradesReady = profile.SharedUpgradesReady
            };
            store.Write(prefix + ".Profile", JsonUtility.ToJson(next));
            profile = next;
        }

        public void SaveUpgrades(IEnumerable<string> bought)
        {
            if (!profile.SharedUpgradesReady) MigrateUpgrades();
            WriteUpgrades(bought);
        }

        private void MigrateUpgrades()
        {
            var merged = profile.Upgrades.ToHashSet();
            // 이전 버전의 슬롯에서 한 번만 이전합니다. 이후에는 공용 기록만 사용합니다.
            foreach (SaveSlot slot in Enum.GetValues(typeof(SaveSlot)))
            {
                string json = store.Read(Key(slot));
                if (string.IsNullOrEmpty(json)) continue;
                GameSaveData data;
                try { data = JsonUtility.FromJson<GameSaveData>(json); Check(data); }
                catch (Exception) { continue; }
                merged.UnionWith(data.Work.Upgrades.Where(id => !string.IsNullOrWhiteSpace(id)));
            }
            WriteUpgrades(merged);
        }

        private void WriteUpgrades(IEnumerable<string> bought)
        {
            var ids = bought.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().OrderBy(id => id, StringComparer.Ordinal).ToArray();
            if (profile.SharedUpgradesReady && profile.Upgrades.SequenceEqual(ids)) return;
            var next = new GameProfileData
            {
                Version = Version, Endings = profile.Endings, Dialogues = profile.Dialogues,
                Upgrades = ids, SharedUpgradesReady = true
            };
            store.Write(prefix + ".Profile", JsonUtility.ToJson(next));
            profile = next;
        }

        private GameProfileData ReadProfile()
        {
            try
            {
                string json = store.Read(prefix + ".Profile");
                if (!string.IsNullOrEmpty(json))
                {
                    var data = JsonUtility.FromJson<GameProfileData>(json);
                    if (data == null || data.Version != Version || data.Endings == null || data.Dialogues == null)
                        throw new ArgumentException("읽음 기록의 형식 또는 버전을 확인하세요.");
                    data.Upgrades = (data.Upgrades ?? Array.Empty<string>()).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToArray();
                    return data;
                }
            }
            catch (Exception e) { ProfileError = e.Message; }
            return new GameProfileData { Version = Version };
        }

        private string Key(SaveSlot slot) { CheckSlot(slot); return prefix + "." + slot; }
        private static void CheckSlot(SaveSlot slot)
        {
            if (!Enum.IsDefined(typeof(SaveSlot), slot)) throw new ArgumentOutOfRangeException(nameof(slot));
        }

        private static void Check(GameSaveData data)
        {
            if (data == null || data.Version != Version) throw new ArgumentException("지원하지 않는 저장 버전입니다.");
            if (!Enum.IsDefined(typeof(SavePoint), data.Point) || data.Work == null
                || data.Work.Month < 1 || data.Work.Month > 12 || (data.Care == null) != (data.Flow == null)
                || data.Work.Papers == null || data.Work.MonthPapers == null || data.Work.Months == null || data.Work.Upgrades == null)
                throw new ArgumentException("저장 데이터가 불완전합니다.");
            bool working = data.Point == SavePoint.WorkStart;
            if (working ? data.Work.Phase != WorkPhase.Working || data.Work.Seconds != 0
                : data.Work.Phase != (data.Work.Month == 12 ? WorkPhase.Finished : WorkPhase.AfterWork))
                throw new ArgumentException("루프 시작 상태만 저장할 수 있습니다.");
            if (working && (data.Work.Right != 0 || data.Work.Wrong != 0 || data.Work.PointsEarned != 0 || data.Work.PointPenalty != 0
                || data.Work.Papers.Any(paper => paper == null || (data.Work.MonthPapers.Contains(paper.Id) && paper.Choice != -1))))
                throw new ArgumentException("업무 도중의 상태는 시작 체크포인트로 저장할 수 없습니다.");
            if (data.Point != SavePoint.WorkStart && data.Care == null)
                throw new ArgumentException("육성 데이터가 없습니다.");
            if (data.Care != null && data.Care.FinishedMonth != (data.Point == SavePoint.EndingChoice ? 12 : data.Work.Month - 1))
                throw new ArgumentException("육성 진행 월이 올바르지 않습니다.");
            if (data.Care != null && (data.Care.Stats == null || data.Care.Stats.Length != 12 || data.Care.Stats.Any(value => value < 0)
                || data.Care.Gifts == null || data.Care.ActiveGifts == null
                || data.Care.EndingChoices == null || data.Flow.Completed == null || data.Flow.History == null || data.Flow.Promises == null))
                throw new ArgumentException("육성 저장 데이터가 불완전합니다.");
            if (data.Point == SavePoint.EndingChoice && (data.Work.Month != 12
                || data.Care.EndingChoices == null || data.Care.EndingChoices.Length < 1 || data.Care.EndingChoices.Length > 5))
                throw new ArgumentException("엔딩 선택 데이터가 올바르지 않습니다.");
        }
    }
}
