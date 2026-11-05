using System;
using System.IO;
using OperationHeist.Network;
using OperationHeist.Player;
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

        private static void BuildBank()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Banco").transform;
            for (int x = -2; x <= 2; x++)
            for (int z = -2; z <= 2; z++)
                Place("Architecture", "OH_FloorTile", new Vector3(x * 3f, 0f, z * 3f), Quaternion.identity, root, true);
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
            Place("Bank", "OH_DepositBoxes", new Vector3(5f, 0f, 3.4f), Quaternion.Euler(0f, -90f, 0f), root, true);
            Place("Bank", "OH_VaultDoor", new Vector3(5.8f, 0f, 6.4f), Quaternion.Euler(0f, -90f, 0f), root, true);
            Place("Bank", "OH_QueuePost", new Vector3(-1.3f, 0f, -1f), Quaternion.identity, root, true);
            Place("Bank", "OH_QueuePost", new Vector3(1.3f, 0f, -1f), Quaternion.identity, root, true);
            Place("Bank", "OH_QueueBarrier", new Vector3(0f, 0f, -1f), Quaternion.identity, root, true);
            Place("Furniture", "OH_Chair", new Vector3(-2.2f, 0f, 1.6f), Quaternion.Euler(0f, 180f, 0f), root, true);
            Place("Characters", "OH_BankClerk", new Vector3(0.4f, 0f, 5.35f), Quaternion.Euler(0f, 180f, 0f), root, true);
            Place("Characters", "OH_Guard", new Vector3(-4.2f, 0f, -3.5f), Quaternion.Euler(0f, 30f, 0f), root, true);
            Place("Security", "OH_SecurityCamera", new Vector3(-6.8f, 2.55f, 6.6f), Quaternion.Euler(0f, 140f, 0f), root, false);
            Place("Furniture", "OH_CeilingLamp", new Vector3(0f, 2.9f, 1.5f), Quaternion.identity, root, false);

            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/02_Prefabs/Player/FPSPlayer.prefab");
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            player.name = "Visitante";
            player.transform.position = new Vector3(0f, 0.05f, -4.5f);
            player.GetComponent<FpsController>().PlayerCamera.backgroundColor = new Color(.07f, .09f, .12f);

            var sun = new GameObject("Luz principal").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.5f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(52f, -30f, 0f);
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.5f, .53f, .58f);

            EditorSceneManager.SaveScene(scene, BankScenePath);
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(entry => entry.path == BankScenePath))
                scenes.Add(new EditorBuildSettingsScene(BankScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
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
