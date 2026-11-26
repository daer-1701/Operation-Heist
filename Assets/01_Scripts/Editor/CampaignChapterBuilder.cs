using System;
using System.Collections.Generic;
using OperationHeist.Player;
using OperationHeist.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OperationHeist.Editor
{
    public static class CampaignChapterBuilder
    {
        private const string SceneFolder = "Assets/00_Scenes/";
        private const string Production = "Assets/02_Prefabs/Production/";
        private const string Art = "Assets/02_Prefabs/Art/CampaignArt/";
        private static readonly Vector3 Spawn = new(0, .05f, -18);
        private static readonly Vector3 Exit = new(0, 0, -21);

        public static void Build()
        {
            BuildChapter(CampaignContract.Museum, 720, 2, Museum);
            BuildChapter(CampaignContract.Blackout, 840, 2, Blackout);
            BuildChapter(CampaignContract.Finale, 960, 2, Finale);
            InstallDossierVisual();
            AssetDatabase.SaveAssets();
            Debug.Log("CAMPAIGN_CHAPTERS_GENERATED");
        }

        public static void InstallDossierVisual()
        {
            const string path = Production + "Loot/OH_Dossier.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var previous = root.transform.Find("Modelo Hunyuan");
                if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
                if (root.TryGetComponent(out MeshRenderer oldRenderer)) UnityEngine.Object.DestroyImmediate(oldRenderer);
                if (root.TryGetComponent(out MeshFilter oldFilter)) UnityEngine.Object.DestroyImmediate(oldFilter);
                if (root.transform.Find("Expediente visual") == null)
                {
                    var replacement = Place(root.transform, Art + "Priority/OH_DossierArt", Vector3.zero);
                    replacement.name = "Expediente visual";
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void BuildChapter(CampaignContract contract, int seconds, int required, Action<Transform> layout)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject(contract.Title).transform;
            bool museum = contract == CampaignContract.Museum;
            string wall = museum ? "OH_Kit_Ivory" : "OH_NetWall";
            Box(root, "Pavimento", new Vector3(0, -.25f, 0), new Vector3(40, .5f, 46), "OH_NetFloor");
            Box(root, "Muro oeste", new Vector3(-20, 2, 0), new Vector3(.5f, 4, 46), wall);
            Box(root, "Muro este", new Vector3(20, 2, 0), new Vector3(.5f, 4, 46), wall);
            Box(root, "Muro norte", new Vector3(0, 2, 23), new Vector3(40, 4, .5f), wall);
            Box(root, "Muro sur", new Vector3(0, 2, -23), new Vector3(40, 4, .5f), wall);

            layout(root);
            for (int i = -1; i <= 1; i++)
                Box(root, "Cobertura de acceso", new Vector3(i * 8, .72f, -11),
                    new Vector3(1.5f, 1.44f, 2.5f), wall);

            var zone = Box(root, "Zona de extraccion", Exit + new Vector3(0, .012f, .5f),
                new Vector3(7, .024f, 4), "OH_Kit_Teal");
            UnityEngine.Object.DestroyImmediate(zone.GetComponent<BoxCollider>());
            var terminal = Place(root, Production + "Security/OH_ExtractionTerminal", Exit + Vector3.forward * .25f);
            Sign("EXTRACCIÓN", Exit + new Vector3(0, 2.65f, -.1f), true);

            var player = Place(null, "Assets/02_Prefabs/Player/FPSPlayer", Spawn);
            var mission = new GameObject("Misión").AddComponent<LocalMission>();
            var controls = root.GetComponentsInChildren<MissionControl>();
            Array.Sort(controls, (first, second) => first.Step.CompareTo(second.Step));
            mission.ConfigureControls(controls);
            mission.Configure(player.GetComponent<FpsController>(), player.AddComponent<LootCarrier>(), seconds, required);
            terminal.AddComponent<ExtractionPoint>().Configure(mission);
            Patrol(root, mission, "Patrulla de acceso", new Vector3(-12, 0, -13), new Vector3(-12, 0, -8),
                new Vector3(12, 0, -8), new Vector3(12, 0, -13));
            Patrol(root, mission, "Patrulla interior", new Vector3(-16, 0, 21), new Vector3(-16, 0, 14),
                new Vector3(16, 0, 14), new Vector3(16, 0, 21));
            player.AddComponent<LocalMissionPresenter>().Configure(mission);
            player.AddComponent<HealthHudPresenter>().Configure(mission.Health);
            mission.gameObject.AddComponent<MissionAudio>().Configure(mission);

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = museum ? new Color(.44f, .43f, .36f) : new Color(.31f, .36f, .42f);

            string localPath = SceneFolder + contract.Scene + ".unity";
            string lanPath = SceneFolder + contract.CoopScene + ".unity";
            if (!EditorSceneManager.SaveScene(scene, localPath))
                throw new InvalidOperationException("No se pudo guardar " + localPath);
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(item => item.path == localPath))
                scenes.Add(new EditorBuildSettingsScene(localPath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            NetworkLabBuilder.BuildContract(localPath, lanPath, false);
        }

        private static void Patrol(Transform root, LocalMission mission, string name, params Vector3[] route)
        {
            var guard = Place(root, Production + "Characters/OH_Guard", route[0]);
            guard.name = name;
            var waypoints = new Transform[route.Length];
            for (int i = 0; i < route.Length; i++)
            {
                waypoints[i] = new GameObject(name + " · Punto " + (i + 1)).transform;
                waypoints[i].SetParent(root, false);
                waypoints[i].position = route[i];
            }
            guard.AddComponent<PatrolGuard>().Configure(mission, waypoints);
        }

        private static void Museum(Transform root)
        {
            var service = Partition(root, "Galería de servicio", -7, -2, "OH_Kit_Ivory");
            var gallery = Partition(root, "Sala de exposición", 6, 8, "OH_Kit_Ivory");
            Control(root, 0, "Abre la galería de servicio", new Vector3(-8, 0, -6), service);
            var release = Control(root, 1, "Libera las vitrinas", new Vector3(8, 0, 4.8f), gallery);
            for (int i = -1; i <= 1; i++)
            {
                Box(root, "Tabique de exposición", new Vector3(i * 10, 1.4f, 15), new Vector3(.45f, 2.8f, 6), "OH_Kit_Ivory");
                Place(root, Art + "Museum/OH_MuseumShowcase", new Vector3(i * 10 + 4, 0, 18));
            }
            release.ConfigureLids(new[] {
                Exhibit(root, "Museum/OH_MuseumSignalIdol", "Ídolo de la galería", new Vector3(-12, 0, 17)),
                Exhibit(root, "Museum/OH_MuseumCustodyTablet", "Tablilla de custodia", new Vector3(12, 0, 18))
            });
            AreaLights(root, new Color(1f, .86f, .64f), 7f);
        }

        private static void Blackout(Transform root)
        {
            var offices = Partition(root, "Acceso técnico", 10, -2, "OH_NetWall");
            var data = Partition(root, "Sala de servidores", -9, 10, "OH_NetWall");
            var normal = new GameObject("Iluminación normal").transform;
            normal.SetParent(root, false);
            AreaLights(normal, new Color(.73f, .88f, 1f), 7.5f);
            var emergency = new GameObject("Iluminación de emergencia").transform;
            emergency.SetParent(root, false);
            AreaLights(emergency, new Color(1f, .41f, .24f), 4.2f);
            Place(normal, Art + "Blackout/OH_BlackoutStatusNormal", new Vector3(-10.8f, 1.35f, 7));
            Place(emergency, Art + "Blackout/OH_BlackoutStatusEmergency", new Vector3(-10.8f, 1.35f, 7));
            emergency.gameObject.SetActive(false);
            Control(root, 0, "Aísla la sala técnica", new Vector3(11, 0, -6), offices, "Blackout/OH_BlackoutControlTerminal");
            Control(root, 1, "Conmuta la energía", new Vector3(-10, 0, 6.8f), data, "Blackout/OH_BlackoutSwitchCabinet",
                normal.gameObject, emergency.gameObject);
            for (int i = -2; i <= 2; i++)
            {
                Place(root, Art + "Blackout/OH_BlackoutServerRack", new Vector3(i * 5, 0, 15));
                Place(root, Production + "Furniture/OH_Desk", new Vector3(i * 6, 0, 2));
            }
            Loot(root, "Blackout/OH_BlackoutPowerCartridge", "Cartucho de registros", new Vector3(-11, .9f, 19));
            Loot(root, "Shared/OH_HackerModule", "Copia fuera de línea", new Vector3(9, 0.9f, 19));
        }

        private static void Finale(Transform root)
        {
            const string bulkhead = "Final/OH_FinalBulkhead";
            const string console = "Final/OH_FinalStageConsole";
            var checkpoint = Partition(root, "Puesto de control", 0, -4, "OH_NetWall", bulkhead);
            var tunnel = Partition(root, "Esclusa", -11, 5, "OH_NetWall", bulkhead);
            var archive = Partition(root, "Núcleo", 9, 13, "OH_NetWall", bulkhead);
            Control(root, 0, "Habilita el puesto de control", new Vector3(3, 0, -7), checkpoint, console);
            Control(root, 1, "Abre la esclusa interior", new Vector3(-12, 0, 1.7f), tunnel, console);
            Control(root, 2, "Libera el núcleo del archivo", new Vector3(10, 0, 9.8f), archive, console);
            for (int i = -1; i <= 1; i++)
                Box(root, "Pilar de contención", new Vector3(i * 6, 1.5f, 9), new Vector3(2, 3, 1.7f), "OH_NetFloor");
            Place(root, Art + "Final/OH_FinalCoreCradle", new Vector3(10, 0, 19));
            Loot(root, "Final/OH_FinalArchiveCore", "Núcleo del archivo", new Vector3(10, 1.1f, 19));
            Loot(root, "Priority/OH_DossierArt", "Registro de custodia", new Vector3(-9, .95f, 18));
            AreaLights(root, new Color(.64f, .82f, 1f), 6f);
        }

        private static GameObject Partition(Transform root, string name, float opening, float z, string material, string prop = null)
        {
            const float edge = 20;
            const float gap = 1.65f;
            float left = opening - gap;
            float right = opening + gap;
            Box(root, name + " O", new Vector3((left - edge) * .5f, 2, z), new Vector3(left + edge, 4, .4f), material);
            Box(root, name + " E", new Vector3((right + edge) * .5f, 2, z), new Vector3(edge - right, 4, .4f), material);
            if (prop == null)
                return Box(root, name, new Vector3(opening, 1.45f, z), new Vector3(2 * gap, 2.9f, .35f), "OH_Kit_Teal");
            var model = Place(root, Art + prop, new Vector3(opening, 0, z));
            model.name = name;
            model.transform.localScale = new Vector3(1.83f, 1.2f, 1);
            var door = model.AddComponent<BoxCollider>();
            door.center = new Vector3(0, 1.2025f, 0);
            door.size = new Vector3(1.81f, 2.405f, .43f);
            return model;
        }

        private static MissionControl Control(Transform root, int step, string title, Vector3 placement, GameObject gate,
            string prop = null, GameObject normal = null, GameObject emergency = null)
        {
            var art = Place(root, Art + (prop ?? "Priority/OH_ElectricalPanelArt"), placement + Vector3.up * (prop == null ? .75f : 0f));
            var anchor = art.transform.Find("Contact");
            var target = new GameObject(title).AddComponent<MissionControl>();
            target.transform.SetParent(root, false);
            target.transform.position = anchor != null ? anchor.position + Vector3.back * .08f : placement + new Vector3(0, 1.21f, -.36f);
            target.Target.size = new Vector3(.78f, .9f, .36f);
            target.Configure(step, title, gate, normal, emergency);
            Sign(title.ToUpperInvariant(), placement + new Vector3(0, 1.87f, -.43f));
            return target;
        }

        private static Transform Exhibit(Transform root, string visual, string label, Vector3 position)
        {
            var showcase = Place(root, Art + "Museum/OH_MuseumShowcase", position);
            Loot(root, visual, label, showcase.transform.Find("Exhibit").position);
            return showcase.transform.Find("Lid");
        }

        private static void Loot(Transform root, string visual, string label, Vector3 position)
        {
            var instance = Place(root, Art + visual, position);
            if (!instance.TryGetComponent(out BoxCollider box)) box = instance.AddComponent<BoxCollider>();
            var renderers = instance.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            box.center = instance.transform.InverseTransformPoint(bounds.center);
            box.size = Vector3.Max(bounds.size, new Vector3(.38f, .28f, .34f));
            instance.AddComponent<LootItem>().Configure(label);
        }

        private static void AreaLights(Transform root, Color color, float intensity)
        {
            for (int z = -17; z <= 19; z += 9)
                foreach (int x in new[] { -12, 0, 12 })
                {
                    var light = new GameObject("Luminaria").AddComponent<Light>();
                    light.transform.SetParent(root, false);
                    light.transform.position = new Vector3(x, 3.75f, z);
                    light.type = LightType.Point;
                    light.color = color;
                    light.intensity = intensity;
                    light.range = 12;
                }
        }

        private static GameObject Place(Transform root, string path, Vector3 position)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path + ".prefab");
            if (prefab == null) throw new InvalidOperationException("Falta " + path);
            var instance = (GameObject)(root != null
                ? PrefabUtility.InstantiatePrefab(prefab, root)
                : PrefabUtility.InstantiatePrefab(prefab));
            instance.transform.position = position;
            return instance;
        }

        private static GameObject Box(Transform root, string name, Vector3 position, Vector3 scale, string material)
        {
            var box = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer), typeof(BoxCollider));
            box.transform.SetParent(root, false);
            box.transform.position = position;
            box.transform.localScale = scale;
            box.GetComponent<MeshFilter>().sharedMesh = FpsPrototypeBuilder.OriginalCuboid();
            box.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/03_Materials/" + material + ".mat");
            if (box.GetComponent<MeshRenderer>().sharedMaterial == null)
                throw new InvalidOperationException("Falta material " + material);
            return box;
        }

        private static void Sign(string text, Vector3 position, bool south = false) =>
            LocalMvpBuilder.Sign(text, position, Quaternion.Euler(0, south ? 180 : 0, 0), Color.white, .12f);
    }
}
