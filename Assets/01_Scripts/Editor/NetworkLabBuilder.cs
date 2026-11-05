using System.IO;
using System.Reflection;
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
        public const string DepotScenePath = "Assets/00_Scenes/Heist_DepotCoop.unity";
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

            CreateSession(player, null, new Vector3(0, 5, -8), Quaternion.Euler(22, 0, 0));

            var loot = Box("BotinCompartido", new Vector3(3, 0.35f, 3), new Vector3(0.7f, 0.7f, 0.7f), lootMat);
            loot.AddComponent<NetworkObject>();
            loot.AddComponent<NetworkLootItem>().Configure("DOSSIER RED / 01");

            var panel = Box("PanelCableado", new Vector3(-4f, 1.1f, 4f), new Vector3(1.2f, 1.4f, 0.2f), accent);
            panel.AddComponent<WirePatchMinigame>().Configure(4, 1);
            Sign("LAB RED LAN", new Vector3(0, 2.6f, -9.7f), accent.color);
            Sign("H dano / F reanimar", new Vector3(0, 2.2f, -9.7f), Color.white);
            var light = new GameObject("Sol").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.6f;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
            RenderSettings.skybox = null;
            RenderSettings.ambientLight = new Color(0.4f, 0.45f, 0.5f);

            SaveNetworkScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("NETWORK_LAB_GENERATED");
        }

        [MenuItem("Operation Heist/Red/Generar depósito cooperativo")]
        public static void BuildDepot()
        {
            if (!Application.isBatchMode && File.Exists(DepotScenePath) &&
                !EditorUtility.DisplayDialog("Regenerar depósito cooperativo",
                    "Se reemplazará la copia cooperativa con el depósito local guardado.", "Regenerar", "Cancelar")) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (player == null) player = CreatePlayerPrefab();
            var scene = EditorSceneManager.OpenScene(LocalMvpBuilder.ScenePath, OpenSceneMode.Single);
            var localMission = Object.FindAnyObjectByType<LocalMission>();
            var localPlayer = Object.FindAnyObjectByType<LootCarrier>();
            Vector3 spawn = localPlayer.transform.position;
            Object.DestroyImmediate(localMission.gameObject);
            Object.DestroyImmediate(localPlayer.gameObject);

            var point = Object.FindAnyObjectByType<ExtractionPoint>();
            var terminal = point.GetComponent<Collider>();
            Object.DestroyImmediate(point);
            var zone = GameObject.Find("Zona de extraccion").GetComponent<Renderer>();

            var localLoot = Object.FindObjectsByType<LootItem>(FindObjectsInactive.Include);
            var dossiers = new NetworkLootItem[localLoot.Length];
            for (int i = 0; i < localLoot.Length; i++)
            {
                var item = localLoot[i];
                var root = item.gameObject;
                string label = item.DisplayName;
                Object.DestroyImmediate(item);
                root.AddComponent<NetworkObject>();
                dossiers[i] = root.AddComponent<NetworkLootItem>();
                dossiers[i].Configure(label);
            }

            var guard = Object.FindAnyObjectByType<PatrolGuard>();
            // The cooperative mission explicitly steps the same patrol on the host.
            guard.enabled = false;
            guard.GetComponent<CapsuleCollider>().enabled = true;
            guard.gameObject.AddComponent<NetworkObject>();
            guard.gameObject.AddComponent<NetworkTransform>();

            var mission = new GameObject("Misión cooperativa", typeof(NetworkObject), typeof(NetworkDepotMission))
                .GetComponent<NetworkDepotMission>();
            mission.Configure(dossiers, terminal, zone, guard, spawn);
            CreateSession(player, mission, new Vector3(0, 2.5f, -11), Quaternion.Euler(8, 0, 0));
            SaveNetworkScene(scene, DepotScenePath);

            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(entry => entry.path == DepotScenePath))
                scenes.Add(new EditorBuildSettingsScene(DepotScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("NETWORK_DEPOT_GENERATED");
        }

        public static void BuildAll()
        {
            Build();
            BuildDepot();
        }

        private static void CreateSession(GameObject player, NetworkDepotMission mission, Vector3 cameraPosition,
            Quaternion cameraRotation)
        {
            var root = new GameObject("HeistNetworkSession", typeof(NetworkManager), typeof(UnityTransport),
                typeof(HeistNetworkSession), typeof(NetworkSessionHud));
            var manager = root.GetComponent<NetworkManager>();
            manager.NetworkConfig.NetworkTransport = root.GetComponent<UnityTransport>();
            manager.NetworkConfig.PlayerPrefab = player;
            manager.NetworkConfig.ConnectionApproval = true;
            root.GetComponent<HeistNetworkSession>().Configure(player, mission);

            var camera = new GameObject("Vista de espera", typeof(Camera)).GetComponent<Camera>();
            camera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.055f, .085f, .11f);
            root.GetComponent<NetworkSessionHud>().Configure(camera);
        }

        private static GameObject CreatePlayerPrefab()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/02_Prefabs/Player/FPSPlayer.prefab");
            var instance = Object.Instantiate(source);
            instance.name = "NetworkFPSPlayer";
            if (instance.GetComponent<NetworkObject>() == null) instance.AddComponent<NetworkObject>();
            var sync = instance.AddComponent<NetworkTransform>();
            sync.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
            var motor = instance.AddComponent<NetworkPlayerMotor>();
            if (instance.GetComponent<NetworkLootInteractor>() == null) instance.AddComponent<NetworkLootInteractor>();
            var health = instance.GetComponent<PlayerHealth>() ?? instance.AddComponent<PlayerHealth>();
            health.Configure(100f, 12f, false);
            if (instance.GetComponent<NetworkPlayerHealth>() == null) instance.AddComponent<NetworkPlayerHealth>();
            if (instance.GetComponent<NetworkReviveInteractor>() == null) instance.AddComponent<NetworkReviveInteractor>();
            var hud = instance.AddComponent<OperationHeist.UI.HealthHudPresenter>();
            hud.Configure(health);
            hud.enabled = false;
            var avatar = Box("Agente remoto", Vector3.zero, Vector3.one,
                FpsPrototypeBuilder.OriginalMaterial("OH_NetAccent", new Color(.1f, .65f, .58f)));
            Object.DestroyImmediate(avatar.GetComponent<Collider>());
            avatar.transform.SetParent(instance.transform, false);
            avatar.transform.localPosition = new Vector3(0, .88f, 0);
            avatar.transform.localScale = new Vector3(.52f, 1.7f, .42f);
            motor.Configure(avatar.GetComponent<Renderer>(), hud);
            var prompt = instance.GetComponent<OperationHeist.UI.InteractionPromptPresenter>();
            if (prompt != null) Object.DestroyImmediate(prompt, true);
            var localInteractor = instance.GetComponent<PlayerInteractor>();
            if (localInteractor != null) Object.DestroyImmediate(localInteractor, true);
            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, PlayerPrefabPath);
            Object.DestroyImmediate(instance);
            SavePlayerNetworkId();
            return prefab;
        }

        public static void SavePlayerNetworkId()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            ValidateNetworkId(prefab.GetComponent<NetworkObject>());
            PrefabUtility.SavePrefabAsset(prefab);
        }

        private static void SaveNetworkScene(Scene scene, string path)
        {
            EditorSceneManager.SaveScene(scene, path);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            foreach (var instance in Object.FindObjectsByType<NetworkObject>(FindObjectsInactive.Include))
                ValidateNetworkId(instance);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ValidateNetworkId(NetworkObject instance)
        {
            // NGO 2.13.3 assigns its ID after Unity has saved the object and assigned a GlobalObjectId.
            var validate = typeof(NetworkObject).GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic);
            validate.Invoke(instance, null);
            if (instance.PrefabIdHash == 0)
                throw new System.InvalidOperationException("Objeto de red sin identificador: " + instance.name);
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
