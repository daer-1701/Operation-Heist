using System;
using System.IO;
using OperationHeist.Network;
using OperationHeist.Player;
using OperationHeist.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OperationHeist.Editor
{
    public static class MissionSetBuilder
    {
        public const string BankScenePath = "Assets/00_Scenes/Heist_Bank.unity";
        private const string KitRootName = "Kit de produccion";

        public static void BuildAll()
        {
            DressCharacterPrefabs();
            DressDepot();
            DressRemoteAvatar();
            BuildBank();
            NetworkLabBuilder.BuildDepot();
            NetworkLabBuilder.BuildBank();
            AssetDatabase.SaveAssets();
            Debug.Log("MISSION_SET_INTEGRATED");
        }

        private static void DressCharacterPrefabs()
        {
            foreach (var name in new[] { "OH_Agent", "OH_Guard", "OH_BankClerk" })
            {
                string path = "Assets/02_Prefabs/Production/Characters/" + name + ".prefab";
                var root = PrefabUtility.LoadPrefabContents(path);
                if (root.GetComponent<RigidCharacterCycle>() == null)
                    root.AddComponent<RigidCharacterCycle>();
                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void DressDepot()
        {
            var scene = EditorSceneManager.OpenScene(LocalMvpBuilder.ScenePath, OpenSceneMode.Single);
            var existing = GameObject.Find(KitRootName);
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing);
            var kit = new GameObject(KitRootName).transform;

            Place("Security", "FireExtinguisher", new Vector3(-13.15f, 0f, -12f), Quaternion.identity, kit, false);
            Place("Security", "FireExtinguisher", new Vector3(13.15f, 0f, 12f), Quaternion.Euler(0f, 180f, 0f), kit, false);
            Place("Security", "AlarmBeacon", new Vector3(0f, 3.15f, 14.6f), Quaternion.identity, kit, false);
            Place("Security", "WirePanel", new Vector3(13.55f, 1.3f, -2f), Quaternion.Euler(0f, -90f, 0f), kit, false);
            Place("Security", "Keypad", new Vector3(2.55f, 1.35f, -15.45f), Quaternion.Euler(0f, 180f, 0f), kit, false);
            Place("Furniture", "CeilingLamp", new Vector3(0f, 3.72f, -8f), Quaternion.identity, kit, false);
            Place("Furniture", "CeilingLamp", new Vector3(0f, 3.72f, 4f), Quaternion.identity, kit, false);
            Place("Depot", "Locker", new Vector3(12.4f, 0f, 4.5f), Quaternion.Euler(0f, -90f, 0f), kit, false);
            Place("Depot", "CardboardBox", new Vector3(-6.2f, 0f, -8.5f), Quaternion.identity, kit, false);
            Place("Depot", "ShippingCase", new Vector3(7.2f, 0f, 11.5f), Quaternion.Euler(0f, 20f, 0f), kit, false);
            Place("Depot", "Pallet", new Vector3(-7.4f, 0f, 14.2f), Quaternion.identity, kit, false);
            foreach (var corner in new[] { new Vector3(-12.6f, 2.7f, -14.4f), new Vector3(12.6f, 2.7f, -14.4f), new Vector3(-12.6f, 2.7f, 14.4f), new Vector3(12.6f, 2.7f, 14.4f) })
            {
                var camera = Place("Security", "SecurityCamera", corner, Quaternion.identity, kit, false);
                camera.transform.LookAt(new Vector3(0f, 1.2f, 0f));
            }

            var guard = UnityEngine.Object.FindAnyObjectByType<PatrolGuard>();
            foreach (var renderer in guard.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer.transform == guard.transform) continue;
                if (renderer.GetComponentInParent<RigidCharacterCycle>() != null) continue;
                renderer.enabled = false;
            }
            var oldVisual = guard.transform.Find("OH_Guard");
            if (oldVisual != null) UnityEngine.Object.DestroyImmediate(oldVisual.gameObject);
            var visual = Place("Characters", "OH_Guard", guard.transform.position, guard.transform.rotation, guard.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;

            foreach (var loot in UnityEngine.Object.FindObjectsByType<LootItem>(FindObjectsInactive.Include))
            {
                var previous = loot.transform.Find("OH_Dossier");
                if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
                var dossier = Place("Loot", "OH_Dossier", loot.transform.position + Vector3.up * 0.62f, loot.transform.rotation, loot.transform, false);
                dossier.name = "OH_Dossier";
            }

            EditorSceneManager.SaveScene(scene);
        }

        private static void DressRemoteAvatar()
        {
            var root = PrefabUtility.LoadPrefabContents(NetworkLabBuilder.PlayerPrefabPath);
            var previous = root.transform.Find("OH_Agent");
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            var box = root.transform.Find("Agente remoto");
            if (box != null) box.gameObject.SetActive(false);
            var agentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/02_Prefabs/Production/Characters/OH_Agent.prefab");
            var agent = (GameObject)PrefabUtility.InstantiatePrefab(agentPrefab, root.transform);
            agent.transform.localPosition = Vector3.zero;
            agent.transform.localRotation = Quaternion.identity;
            foreach (var collider in agent.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            if (agent.GetComponent<RigidCharacterCycle>() == null) agent.AddComponent<RigidCharacterCycle>();
            var motor = root.GetComponent<NetworkPlayerMotor>();
            var serialized = new SerializedObject(motor);
            serialized.FindProperty("avatar").objectReferenceValue = agent.GetComponentInChildren<Renderer>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, NetworkLabBuilder.PlayerPrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        public static void BuildBank()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Banco").transform;
            for (int x = -2; x <= 2; x++)
            for (int z = -2; z <= 2; z++)
                Place("Architecture", "OH_FloorTile", new Vector3(x * 3f, -.1f, z * 3f), Quaternion.identity, root, true);
            for (int x = -2; x <= 2; x++)
            {
                if (x != 0)
                    Place("Architecture", "OH_Wall", new Vector3(x * 3f, 0f, -7.6f), Quaternion.Euler(0f, 180f, 0f), root, true);
                Place("Architecture", "OH_Wall", new Vector3(x * 3f, 0f, 7.6f), Quaternion.identity, root, true);
            }
            for (int z = -2; z <= 2; z++)
            {
                Place("Architecture", "OH_Wall", new Vector3(-7.6f, 0f, z * 3f), Quaternion.Euler(0f, 90f, 0f), root, true);
                Place("Architecture", "OH_Wall", new Vector3(7.6f, 0f, z * 3f), Quaternion.Euler(0f, -90f, 0f), root, true);
            }
            for (int x = -1; x <= 1; x++)
                Place("Architecture", "OH_CeilingTile", new Vector3(x * 3f, 3f, 0f), Quaternion.identity, root, false);

            Place("Bank", "OH_TellerCounter", new Vector3(0f, 0f, 4.2f), Quaternion.identity, root, true);
            Place("Bank", "OH_ATM", new Vector3(-5.2f, 0f, 2.2f), Quaternion.Euler(0f, 90f, 0f), root, true);
            Place("Bank", "OH_DepositBoxes", new Vector3(6.7f, 0f, 2.5f), Quaternion.Euler(0f, -90f, 0f), root, true);
            var door = Place("Bank", "OH_VaultDoor", new Vector3(5.55f, 0f, 4.65f), Quaternion.identity, root, true);
            Place("Architecture", "OH_Wall", new Vector3(3.45f, 0f, 6.15f), Quaternion.Euler(0f, 90f, 0f), root, true);
            Place("Architecture", "OH_WallHalf", new Vector3(3.95f, 0f, 4.65f), Quaternion.identity, root, true);
            Place("Architecture", "OH_WallHalf", new Vector3(7.15f, 0f, 4.65f), Quaternion.identity, root, true);
            Place("Bank", "OH_QueuePost", new Vector3(-1.3f, 0f, -1f), Quaternion.identity, root, true);
            Place("Bank", "OH_QueuePost", new Vector3(1.3f, 0f, -1f), Quaternion.identity, root, true);
            Place("Bank", "OH_QueueBarrier", new Vector3(0f, 0f, -1f), Quaternion.identity, root, true);
            Place("Furniture", "OH_Chair", new Vector3(-2.2f, 0f, 1.6f), Quaternion.Euler(0f, 180f, 0f), root, true);
            Place("Characters", "OH_BankClerk", new Vector3(0.4f, 0f, 5.35f), Quaternion.Euler(0f, 180f, 0f), root, true);
            var guard = Place("Characters", "OH_Guard", new Vector3(-4.2f, 0f, -3.5f), Quaternion.identity, root, true);
            Place("Furniture", "OH_CeilingLamp", new Vector3(0f, 2.9f, 1.5f), Quaternion.identity, root, false);

            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/02_Prefabs/Player/FPSPlayer.prefab");
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            player.name = "Agente local";
            player.transform.position = new Vector3(0f, 0.05f, -4.5f);
            var motor = player.GetComponent<FpsController>();
            motor.PlayerCamera.backgroundColor = new Color(.07f, .09f, .12f);
            var mission = new GameObject("Misión - Banco central").AddComponent<LocalMission>();
            var security = BuildBankSecurity(root, mission);
            Place("Furniture", "OH_Desk", new Vector3(5.55f, 0f, 6.55f), Quaternion.identity, root, true);
            var dossier = Place("Loot", "OH_Dossier", new Vector3(5.55f, .8f, 6.55f), Quaternion.identity, root, true)
                .AddComponent<LootItem>();
            dossier.Configure("EXPEDIENTE DE LA BÓVEDA");
            dossier.gameObject.SetActive(false);
            var access = door.AddComponent<BankVaultAccess>();
            access.Configure(mission, dossier, door.transform.GetChild(0), security);
            mission.ConfigureVault(access, security);
            mission.Configure(motor, player.AddComponent<LootCarrier>(), 240f, 1);
            player.AddComponent<LocalMissionPresenter>().Configure(mission);
            player.AddComponent<HealthHudPresenter>().Configure(mission.Health);
            mission.gameObject.AddComponent<MissionAudio>().Configure(mission);
            var terminal = Place("Bank", "OH_ATM", new Vector3(0f, 0f, -6.6f), Quaternion.Euler(0f, 180f, 0f), root, true);
            terminal.name = "Terminal de extracción";
            terminal.AddComponent<ExtractionPoint>().Configure(mission);
            Place("Loot", "OH_LootBag", new Vector3(-1.45f, 0f, -6.15f), Quaternion.identity, root, false);
            LocalMvpBuilder.Sign("EXTRACCIÓN", new Vector3(0f, 2.5f, -6.7f), Quaternion.Euler(0f, 180f, 0f), Color.cyan, .18f);
            LocalMvpBuilder.Sign("BÓVEDA", new Vector3(5.55f, 3f, 4.45f), Quaternion.identity, Color.yellow, .18f);

            var route = new GameObject("Ruta de patrulla").transform;
            var positions = new[] { new Vector3(-4.2f, 0f, -3.5f), new Vector3(-4.2f, 0f, .8f),
                new Vector3(3.5f, 0f, .8f), new Vector3(3.5f, 0f, -3.5f) };
            var waypoints = new Transform[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                waypoints[i] = new GameObject("Punto " + (i + 1)).transform;
                waypoints[i].SetParent(route, false);
                waypoints[i].position = positions[i];
            }
            guard.AddComponent<PatrolGuard>().Configure(mission, waypoints);

            var sun = new GameObject("Luz principal").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.5f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(52f, -30f, 0f);
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.5f, .53f, .58f);

            if (!EditorSceneManager.SaveScene(scene, BankScenePath))
                throw new IOException("No se pudo guardar el banco.");
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(entry => entry.path == BankScenePath))
                scenes.Add(new EditorBuildSettingsScene(BankScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static BankSecurity BuildBankSecurity(Transform root, LocalMission mission)
        {
            const float gateZ = 2.1f;
            var gate = Place("Architecture", "OH_SecurityDoor", new Vector3(5.55f, 0, gateZ), Quaternion.identity, root, true);
            Place("Architecture", "OH_WallHalf", new Vector3(4.19f, 0, gateZ), Quaternion.identity, root, true);
            Place("Architecture", "OH_WallHalf", new Vector3(6.94f, 0, gateZ), Quaternion.identity, root, true);
            Place("Architecture", "OH_Wall", new Vector3(3.45f, 0, 3.4f), Quaternion.Euler(0, 90, 0), root, true);
            var camera = Place("Security", "OH_SecurityCamera", new Vector3(7.08f, 2.55f, 4.25f),
                Quaternion.identity, root, false);
            camera.transform.LookAt(new Vector3(5.55f, 1.6f, 2.1f));
            var indicator = new GameObject("Cámara activa", typeof(Light)).GetComponent<Light>();
            indicator.transform.SetParent(camera.transform, false);
            indicator.type = LightType.Spot;
            indicator.color = new Color(1f, .27f, .17f);
            indicator.intensity = 1.4f;
            indicator.range = 6.5f;
            indicator.spotAngle = 70f;
            var security = new GameObject("Seguridad del banco").AddComponent<BankSecurity>();
            security.Configure(mission, gate.transform.GetChild(0), indicator);
            for (int channel = 0; channel < 7; channel++)
            {
                bool wire = channel < 4;
                int index = wire ? channel : channel - 4;
                var position = new Vector3(3.82f + .30f * index, 1.25f, wire ? 1.88f : 4.42f);
                string label = wire ? (index + 1).ToString() : new[] { "011", "110", "100" }[index];
                var control = new GameObject(label, typeof(MeshFilter), typeof(MeshRenderer), typeof(BoxCollider));
                control.transform.SetParent(security.transform, false);
                control.transform.position = position - Vector3.up * .12f;
                control.transform.localScale = new Vector3(.23f, .24f, .12f);
                control.GetComponent<MeshFilter>().sharedMesh = FpsPrototypeBuilder.OriginalCuboid();
                string material = wire ? "OH_Kit_Brass" : "OH_Kit_Teal";
                control.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                    $"Assets/03_Materials/{material}.mat");
                control.AddComponent<BankSecuritySwitch>().Configure(security, channel);
                LocalMvpBuilder.Sign(label, position + Vector3.back * .08f, Quaternion.identity, Color.white, .08f);
            }
            LocalMvpBuilder.Sign("ACCESO  2–4–1–3", new Vector3(4.24f, 1.65f, 1.8f), Quaternion.identity, Color.yellow, .12f);
            LocalMvpBuilder.Sign("CÁMARAS  META 101", new Vector3(4.20f, 1.65f, 4.32f), Quaternion.identity, Color.cyan, .12f);
            return security;
        }

        private static GameObject Place(string category, string name, Vector3 position, Quaternion rotation, Transform parent, bool colliders)
        {
            if (!name.StartsWith("OH_", StringComparison.Ordinal)) name = "OH_" + name;
            string path = "Assets/02_Prefabs/Production/" + category + "/" + name + ".prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new InvalidOperationException("Falta el prefab " + path);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(parent, true);
            instance.transform.SetPositionAndRotation(position, rotation);
            if (!colliders)
            {
                foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
                    collider.enabled = false;
            }
            return instance;
        }
    }
}
