using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace OperationHeist.Player
{
    public sealed class CampaignContract
    {
        public static readonly CampaignContract Depot = new("depot", "Primer trabajo", "Heist_Depot", 300);
        public static readonly CampaignContract Bank = new("bank", "Banco central", "Heist_Bank", 500);

        public string Id { get; }
        public string Title { get; }
        public string Scene { get; }
        public int BasePay { get; }
        public CampaignContract Next => this == Depot ? Bank : null;

        private CampaignContract(string id, string title, string scene, int basePay)
        {
            Id = id;
            Title = title;
            Scene = scene;
            BasePay = basePay;
        }
    }

    public readonly struct CampaignReward
    {
        public readonly int Credits;
        public readonly int Experience;

        public CampaignReward(int credits, int experience)
        {
            Credits = credits;
            Experience = experience;
        }
    }

    public sealed class CampaignProgress
    {
        private const int Version = 1;

        [Serializable]
        private sealed class Profile
        {
            public int version;
            public int credits;
            public int experience;
            public bool depotCleared;
            public bool bankCleared;
            public List<string> rewardedAttempts;
        }

        private readonly string _path;
        private Profile _profile;
        private bool _loadedFromBackup;

        public int Credits => _profile?.credits ?? 0;
        public int Experience => _profile?.experience ?? 0;
        public bool Available => _profile != null;

        public CampaignProgress(string path)
        {
            _path = path;
            _profile = Read(path, out bool unsupported);
            if (_profile != null || unsupported) return;
            _profile = Read(path + ".bak", out unsupported);
            _loadedFromBackup = _profile != null;
            if (_profile == null && !unsupported && !File.Exists(path) && !File.Exists(path + ".bak"))
                _profile = new Profile { version = Version, rewardedAttempts = new List<string>() };
        }

        public static CampaignProgress OpenLocal() =>
            new(Path.Combine(Application.persistentDataPath, "campaign.json"));

        public bool IsCleared(CampaignContract contract) =>
            _profile != null && (contract == CampaignContract.Depot ? _profile.depotCleared : _profile.bankCleared);

        public bool IsUnlocked(CampaignContract contract) =>
            Available && (contract == CampaignContract.Depot || (contract == CampaignContract.Bank && _profile.depotCleared));

        public bool TryReward(CampaignContract contract, Guid attempt, int alarmLevel, float remainingSeconds, out CampaignReward reward, bool expert = false)
        {
            reward = default;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_path)));
                using var exclusiveWrite = new FileStream(_path + ".lock", FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
                // Another instance may have saved since this briefing opened.
                var current = new CampaignProgress(_path);
                _profile = current._profile;
                _loadedFromBackup = current._loadedFromBackup;
                return TryWriteReward(contract, attempt, alarmLevel, remainingSeconds, out reward, expert);
            }
            catch (IOException exception)
            {
                Debug.LogWarning("El progreso no se pudo guardar. " + exception.Message);
                return false;
            }
            catch (UnauthorizedAccessException exception)
            {
                Debug.LogWarning("El progreso no se pudo guardar. " + exception.Message);
                return false;
            }
        }

        private bool TryWriteReward(CampaignContract contract, Guid attempt, int alarmLevel, float remainingSeconds, out CampaignReward reward, bool expert)
        {
            reward = default;
            if (!IsUnlocked(contract) || attempt == Guid.Empty) return false;
            string attemptId = attempt.ToString("N");
            if (_profile.rewardedAttempts.Contains(attemptId)) return true;

            int timeBonus = Mathf.Clamp(Mathf.FloorToInt(remainingSeconds), 0, 120) / 4;
            int credits = contract.BasePay + (alarmLevel == 0 ? 100 : 0)
                - Mathf.Clamp(alarmLevel, 0, 3) * 40 + timeBonus;
            int experience = 50 + (alarmLevel == 0 ? 25 : 0);
            if (expert) { credits += credits / 4; experience += experience / 4; }
            var next = new Profile
            {
                version = Version,
                credits = (int)Math.Min(int.MaxValue, (long)_profile.credits + credits),
                experience = (int)Math.Min(int.MaxValue, (long)_profile.experience + experience),
                depotCleared = _profile.depotCleared || contract == CampaignContract.Depot,
                bankCleared = _profile.bankCleared || contract == CampaignContract.Bank,
                rewardedAttempts = new List<string>(_profile.rewardedAttempts) { attemptId }
            };
            string temporary = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temporary, JsonUtility.ToJson(next, true));
                if (File.Exists(_path))
                    File.Replace(temporary, _path, _loadedFromBackup ? null : _path + ".bak");
                else
                    File.Move(temporary, _path);
                _loadedFromBackup = false;
                _profile = next;
                reward = new CampaignReward(credits, experience);
                return true;
            }
            finally
            {
                try { File.Delete(temporary); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static Profile Read(string path, out bool unsupported)
        {
            unsupported = false;
            try
            {
                var data = JsonUtility.FromJson<Profile>(File.ReadAllText(path));
                if (data == null) return null;
                unsupported = data.version > Version;
                return data.version == Version && data.credits >= 0 && data.experience >= 0 &&
                    data.rewardedAttempts != null ? data : null;
            }
            catch (ArgumentException) { return null; }
            catch (IOException) { return null; }
            catch (UnauthorizedAccessException) { return null; }
        }
    }
}
