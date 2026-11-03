using System.IO;
using OperationHeist.Network;
using OperationHeist.Player;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OperationHeist.Editor
{
    public static class NetworkLabBuilder
    {
        public const string ScenePath = "Assets/00_Scenes/Heist_NetworkLab.unity";
        public const string PlayerPrefabPath = "Assets/02_Prefabs/Network/NetworkFPSPlayer.prefab";

        [MenuItem("Operation Heist/Red/Generar laboratorio LAN (reemplaza escena de red)")]
        public static void Build()
        {
            if (!Application.isBatchMode && File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("Regenerar laboratorio de red", "Se reemplazará Heist_NetworkLab.", "Regenerar", "Cancelar")) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Directory.CreateDirectory("Assets/02_Prefabs/Network");
            Directory.CreateDirectory("Assets/00_Scenes");
            var floor = FpsPrototypeBuilder.OriginalMaterial("OH_NetFloor", new Color(0.1f, 0.14f, 0.17f));
            var wall = FpsPrototypeBuilder.OriginalMaterial("OH_NetWall", new Color(0.28f, 0.34f, 0.38f));
            var accent = FpsPrototypeBuilder.OriginalMaterial("OH_NetAccent", new Color(0.1f, 0.65f, 0.58f));
            var lootMat = FpsPrototypeBuilder.OriginalMaterial("OH_NetLoot", new Color(1f, 0.55f, 0.15f));

            var player = CreatePlayerPrefab();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Box("Suelo", new Vector3(0, -0.25f, 0), new Vector3(20, 0.5f, 20), floor);
            Box("Pared N", new Vector3(0, 1.5f, 10), new Vector3(20, 3, 0.4f), wall);
            Box("Pared S", new Vector3(0, 1.5f, -10), new Vector3(20, 3, 0.4f), wall);
            Box("Pared O", new Vector3(-10, 1.5f, 0), new Vector3(0.4f, 3, 20), wall);
            Box("Pared E", new Vector3(10, 1.5f, 0), new Vector3(0.4f, 3, 20), wall);
            Box("Cobertura", new Vector3(0, 0.6f, 0), new Vector3(2.5f, 1.2f, 1.2f), wall);

            var sessionRoot = new GameObject("HeistNetworkSession", typeof(NetworkManager), typeof(UnityTransport), typeof(HeistNetworkSession), typeof(NetworkSessionHud));
            var session = sessionRoot.GetComponent<HeistNetworkSession>();
            session.Configure(player);
            var networkManager = sessionRoot.GetComponent<NetworkManager>();
            networkManager.NetworkConfig.PlayerPrefab = player;

            var loot = Box("BotinCompartido", new Vector3(3, 0.35f, 3), new Vector3(0.7f, 0.7f, 0.7f), lootMat);
            loot.AddComponent<NetworkObject>();
            loot.AddComponent<NetworkLootItem>().Configure("DOSSIER RED / 01");
            networkManager.NetworkConfig.PlayerPrefab = player;

            Sign("LAB RED LAN", new Vector3(0, 2.6f, -9.7f), accent.color);
            var light = new GameObject("Sol").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.6f;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
            RenderSettings.skybox = null;
            RenderSettings.ambientLight = new Color(0.4f, 0.45f, 0.5f);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("NETWORK_LAB_GENERATED");
        }

        private static GameObject CreatePlayerPrefab()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/02_Prefabs/Player/FPSPlayer.prefab");
            var instance = Object.Instantiate(source);
            instance.name = "NetworkFPSPlayer";
            if (instance.GetComponent<NetworkObject>() == null) instance.AddComponent<NetworkObject>();
            if (instance.GetComponent<NetworkTransform>() == null) instance.AddComponent<NetworkTransform>();
            if (instance.GetComponent<NetworkPlayerMotor>() == null) instance.AddComponent<NetworkPlayerMotor>();
            if (instance.GetComponent<NetworkLootInteractor>() == null) instance.AddComponent<NetworkLootInteractor>();
            var prompt = instance.GetComponent<OperationHeist.UI.InteractionPromptPresenter>();
            if (prompt != null) Object.DestroyImmediate(prompt, true);
            var localInteractor = instance.GetComponent<PlayerInteractor>();
            if (localInteractor != null) Object.DestroyImmediate(localInteractor, true);
            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, PlayerPrefabPath);
            Object.DestroyImmediate(instance);
            return prefab;
        }

        private static GameObject Box(string name, Vector3 position, Vector3 scale, Material material)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer), typeof(BoxCollider));
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<MeshFilter>().sharedMesh = FpsPrototypeBuilder.OriginalCuboid();
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        private static void Sign(string value, Vector3 position, Color color)
        {
            var go = new GameObject(value);
            go.transform.position = position;
            var text = go.AddComponent<TextMesh>();
            text.text = value;
            text.anchor = TextAnchor.MiddleCenter;
            text.characterSize = 0.12f;
            text.fontSize = 64;
            text.color = color;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }
}
