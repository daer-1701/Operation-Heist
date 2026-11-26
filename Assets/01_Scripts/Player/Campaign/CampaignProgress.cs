using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace OperationHeist.Player
{
    public sealed class CampaignContract
    {
        public static readonly CampaignContract Depot = new(1, "depot", "Primer trabajo", "Heist_Depot", 300,
            "El primer expediente señala a los responsables. Recupera las tres copias de seguridad del depósito y regresa al terminal.");
        public static readonly CampaignContract Bank = new(2, "bank", "Banco central", "Heist_Bank", 500,
            "La contabilidad del depósito dirige al equipo al banco. Abre el acceso, desconecta las cámaras y extrae el expediente de la bóveda.");
        public static readonly CampaignContract Museum = new(3, "museum", "Sombras del museo", "Heist_Museum", 675,
            "Los pagos del banco encubren compras del museo. Abre la galería de servicio, libera las vitrinas y recupera el ídolo y la tablilla.");
        public static readonly CampaignContract Blackout = new(4, "blackout", "Blackout", "Heist_Blackout", 850,
            "La tablilla identifica el centro de datos. Aísla la sala técnica, conmuta la energía y extrae los registros antes del cierre.");
        public static readonly CampaignContract Finale = new(5, "finale", "El golpe perfecto", "Heist_Finale", 1100,
            "Las copias señalan un archivo subterráneo. Cruza la esclusa, libera el núcleo y sal con las pruebas que vinculan toda la operación.");
        public static readonly IReadOnlyList<CampaignContract> All = Array.AsReadOnly(
            new[] { Depot, Bank, Museum, Blackout, Finale });

        public int Number { get; }
        public string Id { get; }
        public string Title { get; }
        public string Scene { get; }
        public string CoopScene => Scene + "Coop";
        public string Briefing { get; }
        public int BasePay { get; }
        public CampaignContract Previous => Number > 1 ? All[Number - 2] : null;
        public CampaignContract Next => Number < All.Count ? All[Number] : null;

        private CampaignContract(int number, string id, string title, string scene, int basePay, string briefing)
        {
            Number = number;
            Id = id;
            Title = title;
            Scene = scene;
            BasePay = basePay;
            Briefing = briefing;
        }

        public static CampaignContract ForScene(string scene)
        {
            foreach (var contract in All)
                if (scene == contract.Scene || scene == contract.CoopScene) return contract;
            return null;
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

    public enum CampaignEquipment { None, Capacitor, Armor, Beacon }

    public static class CampaignEquipmentCatalog
    {
        public static string Name(CampaignEquipment equipment) => equipment switch
        {
            CampaignEquipment.Capacitor => "Condensador",
            CampaignEquipment.Armor => "Chaleco",
            CampaignEquipment.Beacon => "Baliza",
            _ => "Sin equipo"
        };

        public static string Benefit(CampaignEquipment equipment) => equipment switch
        {
            CampaignEquipment.Capacitor => "+1 pulso defensivo",
            CampaignEquipment.Armor => "+20 puntos de vida",
            CampaignEquipment.Beacon => "+15 s de evacuación de emergencia",
            _ => ""
        };

        public static int Price(CampaignEquipment equipment) => (int)equipment * 150;
        public static int RequiredExperience(CampaignEquipment equipment) => (int)equipment * 50;
        public static bool CanPurchase(CampaignEquipment equipment) =>
            equipment >= CampaignEquipment.Capacitor && equipment <= CampaignEquipment.Beacon;
    }

    public sealed class CampaignProgress
    {
        private const int Version = 2;

        [Serializable]
        private sealed class Profile
        {
            public int version;
            public int credits;
            public int experience;
            public bool depotCleared;
            public bool bankCleared;
            public int clearedThrough;
            public int equipment;
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
            Available && contract != null && contract.Number <= _profile.clearedThrough;

        public bool IsUnlocked(CampaignContract contract) =>
            Available && contract != null && contract.Number <= _profile.clearedThrough + 1;

        public bool Owns(CampaignEquipment equipment) => equipment == CampaignEquipment.None ||
            Available && CampaignEquipmentCatalog.CanPurchase(equipment) && (_profile.equipment & (1 << (int)equipment)) != 0;

        public bool TryPurchase(CampaignEquipment equipment)
        {
            if (!CampaignEquipmentCatalog.CanPurchase(equipment)) return false;
            return UpdateProfile(() =>
            {
                if (Owns(equipment)) return true;
                int cost = CampaignEquipmentCatalog.Price(equipment);
                if (!Available || Credits < cost || Experience < CampaignEquipmentCatalog.RequiredExperience(equipment)) return false;
                var next = CopyProfile();
                next.credits -= cost;
                next.equipment |= 1 << (int)equipment;
                Persist(next);
                return true;
            });
        }

        public bool TryReward(CampaignContract contract, Guid attempt, int alarmLevel, float remainingSeconds, out CampaignReward reward, bool expert = false)
        {
            CampaignReward result = default;
            bool success = UpdateProfile(() => TryWriteReward(contract, attempt, alarmLevel, remainingSeconds, out result, expert));
            reward = result;
            return success;
        }

        private bool UpdateProfile(Func<bool> update)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_path)));
                using var exclusiveWrite = new FileStream(_path + ".lock", FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
                // Another instance may have saved since this briefing opened.
                var current = new CampaignProgress(_path);
                _profile = current._profile;
                _loadedFromBackup = current._loadedFromBackup;
                return update();
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
            var next = CopyProfile();
            next.credits = (int)Math.Min(int.MaxValue, (long)next.credits + credits);
            next.experience = (int)Math.Min(int.MaxValue, (long)next.experience + experience);
            next.clearedThrough = Math.Max(next.clearedThrough, contract.Number);
            next.rewardedAttempts.Add(attemptId);
            Persist(next);
            reward = new CampaignReward(credits, experience);
            return true;
        }

        private Profile CopyProfile() => new()
        {
            version = Version,
            credits = Credits,
            experience = Experience,
            clearedThrough = _profile.clearedThrough,
            equipment = _profile.equipment,
            rewardedAttempts = new List<string>(_profile.rewardedAttempts)
        };

        private void Persist(Profile next)
        {
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
                if (data.version == 1)
                    data.clearedThrough = data.bankCleared ? 2 : data.depotCleared ? 1 : 0;
                return (data.version == 1 || data.version == Version) && data.credits >= 0 && data.experience >= 0 &&
                    data.clearedThrough >= 0 && data.clearedThrough <= CampaignContract.All.Count &&
                    data.rewardedAttempts != null ? data : null;
            }
            catch (ArgumentException) { return null; }
            catch (IOException) { return null; }
            catch (UnauthorizedAccessException) { return null; }
        }
    }
}
