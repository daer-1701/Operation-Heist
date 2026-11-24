using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace OperationHeist.Editor
{
    public static class CampaignArtReview
    {
        private const int Tile = 320;
        private const string ReviewDir = "CampaignArtEvidence";
        private static readonly string[] OriginalFolders = {
            "Assets/04_Models/nuevosmodelos", "Assets/04_Models/Hunyuan"
        };
        private static readonly string[] RenderFolders = {
            "Assets/02_Prefabs/Art/CampaignArt"
        };

        public static void AuditOriginals()
        {
            var paths = AssetDatabase.FindAssets("t:Model", OriginalFolders)
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path).ToArray();
            Audit(paths, "originales");
        }

        public static void AuditCampaign()
        {
            var paths = AssetDatabase.FindAssets("t:Prefab", RenderFolders)
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path).ToArray();
            Audit(paths, "campana");
        }

        public static void BuildAndAudit()
        {
            CampaignArtBuilder.Build();
            AuditCampaign();
            var critical = new[] {
                "Architecture/OH_SecurityDoor", "Bank/OH_VaultDoor",
                "Characters/OH_Agent", "Characters/OH_Guard", "Characters/OH_BankClerk",
                "Security/OH_ElectricalPanel", "Security/OH_WirePanel", "Security/OH_SecurityCamera",
                "Security/OH_ExtractionTerminal", "Loot/OH_LootBag", "Loot/OH_Dossier"
            }.Select(path => "Assets/02_Prefabs/Production/" + path + ".prefab").ToArray();
            Audit(critical, "produccion");
            CapturePlayerView();
            var originals = AssetDatabase.FindAssets("t:Model", OriginalFolders)
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path).ToArray();
            Audit(originals, "originales-reverso", true);
        }

        public static void BuildAndVerifyAll()
        {
            BuildAndAudit();
            AuditReusableAndPoses();
        }

        public static void AuditReusableAndPoses()
        {
            var reused = new[] {
                "Architecture/OH_Wall", "Architecture/OH_WallHalf", "Architecture/OH_WallCorner",
                "Architecture/OH_FloorTile", "Architecture/OH_CeilingTile", "Architecture/OH_Column",
                "Architecture/OH_DoorFrame", "Architecture/OH_RollingShutter", "Architecture/OH_Stairs",
                "Depot/OH_WarehouseRack", "Depot/OH_Pallet", "Depot/OH_ShippingCase", "Depot/OH_CardboardBox",
                "Depot/OH_Locker", "Bank/OH_TellerCounter", "Bank/OH_ATM", "Bank/OH_DepositBoxes",
                "Bank/OH_SafeCabinet", "Bank/OH_QueueBarrier", "Loot/OH_CashBundle", "Loot/OH_GoldBar",
                "Security/OH_Keypad", "Security/OH_AlarmBeacon", "Furniture/OH_Desk", "Furniture/OH_Chair",
                "Furniture/OH_Bench", "Furniture/OH_OfficeCabinet", "Furniture/OH_WallLight",
                "Furniture/OH_CeilingLamp", "Furniture/OH_Planter", "Equipment/OH_ToolCase",
                "Equipment/OH_Medkit", "Equipment/OH_Flashlight", "Equipment/OH_AccessCard"
            }.Select(path => "Assets/02_Prefabs/Production/" + path + ".prefab").ToArray();
            Audit(reused, "reutilizables");
            var moving = new[] {
                "Priority/OH_SecurityDoorArt", "Priority/OH_VaultDoorArt", "Priority/OH_ElectricalPanelArt",
                "Final/OH_FinalBulkhead", "Priority/OH_AgentArt", "Priority/OH_GuardArt",
                "Priority/OH_BankClerkArt", "Shared/OH_ReinforcementGuard"
            }.Select(path => "Assets/02_Prefabs/Art/CampaignArt/" + path + ".prefab").ToArray();
            Audit(moving, "poses", pose: true);
        }

        private static void Audit(string[] paths, string label, bool reverse = false, bool pose = false)
        {
            if (paths.Length == 0) throw new InvalidOperationException("Sin modelos para revisar.");
            Directory.CreateDirectory(ReviewDir);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.57f, .59f, .63f);
            var light = new GameObject("Luz principal").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(34, -30, 0);
            light.intensity = reverse ? .85f : 1.7f;
            var fill = new GameObject("Luz de relleno").AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.transform.rotation = Quaternion.Euler(20, 145, 0);
            fill.intensity = reverse ? .35f : .6f;

            var camera = new GameObject("Cámara").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.16f, .18f, .21f);
            camera.fieldOfView = 38;
            var target = new RenderTexture(Tile, Tile, 24);
            camera.targetTexture = target;
            PreparePipeline();
            var lines = new List<string> { "asset\tguid\tinstances\tmeshes\ttriangles\tbounds\tminimum\tuv0\tnormals\tgroups\tmaterials\tsubassets\tposes" };
            const int columns = 4;
            const int pageSize = 12;
            var previous = RenderTexture.active;
            try
            {
                for (int page = 0; page * pageSize < paths.Length; page++)
                {
                    int count = Math.Min(pageSize, paths.Length - page * pageSize);
                    int rows = (count + columns - 1) / columns;
                    var board = new Texture2D(columns * Tile, rows * Tile, TextureFormat.RGB24, false);
                    for (int cell = 0; cell < count; cell++)
                    {
                        string path = paths[page * pageSize + cell];
                        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (asset == null) throw new InvalidOperationException(path);
                        var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
                        var renderers = instance.GetComponentsInChildren<Renderer>(true);
                        var meshes = instance.GetComponentsInChildren<MeshFilter>(true).Select(f => f.sharedMesh)
                            .Concat(instance.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(r => r.sharedMesh))
                            .Where(m => m != null).ToArray();
                        if (meshes.Length == 0 || renderers.Length == 0) throw new InvalidOperationException(path);
                        int triangles = meshes.Sum(mesh => Enumerable.Range(0, mesh.subMeshCount)
                            .Sum(index => (int)mesh.GetIndexCount(index) / 3));
                        var bounds = renderers[0].bounds;
                        foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                        if (triangles <= 0 || !float.IsFinite(bounds.size.sqrMagnitude))
                            throw new InvalidOperationException(path);
                        string poses = CheckMovements(instance);
                        if (pose && string.IsNullOrWhiteSpace(poses)) throw new InvalidOperationException(path);
                        if (pose)
                        {
                            ApplyReviewPose(instance);
                            bounds = renderers[0].bounds;
                            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                        }
                        var materials = renderers.SelectMany(r => r.sharedMaterials).Where(m => m != null)
                            .Select(m => m.name + "/" + m.shader.name).Distinct();
                        var groups = instance.GetComponentsInChildren<Transform>(true).Skip(1).Select(t => t.name);
                        var subassets = AssetDatabase.LoadAllAssetsAtPath(path).GroupBy(a => a.GetType().Name)
                            .Select(g => g.Key + "=" + g.Count());
                        lines.Add(string.Join("\t", path, AssetDatabase.AssetPathToGUID(path),
                            renderers.Length, meshes.Length, triangles, Format(bounds.size), Format(bounds.min),
                            meshes.Count(m => m.HasVertexAttribute(VertexAttribute.TexCoord0)),
                            meshes.Count(m => m.HasVertexAttribute(VertexAttribute.Normal)),
                            string.Join(",", groups), string.Join(",", materials), string.Join(",", subassets), poses));
                        float distance = Mathf.Max(.15f, bounds.extents.magnitude * 3.0f);
                        var view = (reverse ? new Vector3(-1.05f, .62f, 1.75f) :
                            new Vector3(1.05f, .62f, -1.75f)).normalized;
                        camera.transform.position = bounds.center + view * distance;
                        camera.transform.LookAt(bounds.center);
                        camera.nearClipPlane = Mathf.Max(.002f, distance * .025f);
                        camera.farClipPlane = distance * 6f;
                        RenderPipeline.SubmitRenderRequest(camera,
                            new RenderPipeline.StandardRequest { destination = target });
                        RenderTexture.active = target;
                        board.ReadPixels(new Rect(0, 0, Tile, Tile), cell % columns * Tile,
                            (rows - 1 - cell / columns) * Tile, false);
                        Object.DestroyImmediate(instance);
                    }
                    board.Apply();
                    File.WriteAllBytes($"{ReviewDir}/{label}-{page + 1}.png", board.EncodeToPNG());
                    Object.DestroyImmediate(board);
                }
            }
            finally
            {
                RenderTexture.active = previous;
                target.Release();
            }
            File.WriteAllLines($"{ReviewDir}/{label}.tsv", lines);
            Debug.Log($"CAMPAIGN_ART_REVIEW_OK {label} {paths.Length}");
        }

        private static void ApplyReviewPose(GameObject root)
        {
            foreach (var joint in root.GetComponentsInChildren<Transform>())
            {
                switch (joint.name)
                {
                    case "Door": joint.Rotate(0, 77, 0, Space.Self); break;
                    case "RightShoulder": joint.Rotate(30, 0, 0, Space.Self); break;
                    case "LeftShoulder": joint.Rotate(-22, 0, 0, Space.Self); break;
                    case "RightElbow": joint.Rotate(-34, 0, 0, Space.Self); break;
                    case "RightHip": joint.Rotate(-19, 0, 0, Space.Self); break;
                    case "LeftHip": joint.Rotate(16, 0, 0, Space.Self); break;
                    case "LeftKnee": joint.Rotate(23, 0, 0, Space.Self); break;
                }
            }
        }

        private static string CheckMovements(GameObject root)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            var moving = transforms.Where(t => t.name == "Door" || t.name == "RightShoulder" ||
                t.name == "RightKnee" || t.name == "Swivel" || t.name == "Lid");
            var results = new List<string>();
            foreach (var joint in moving)
            {
                var renderer = joint.GetComponentInChildren<Renderer>();
                if (renderer == null) continue;
                var fixedRenderer = transforms.Where(t => !t.IsChildOf(joint))
                    .Select(t => t.GetComponent<Renderer>()).FirstOrDefault(r => r != null);
                Vector3 fixedCenter = fixedRenderer == null ? Vector3.zero : fixedRenderer.bounds.center;
                Vector3 pivot = joint.position;
                Vector3 movedCenter = renderer.bounds.center;
                Transform sight = joint.Find("SightOrigin");
                Vector3 sightBefore = sight == null ? Vector3.zero : sight.position;
                Renderer boot = joint.name == "RightKnee" ? joint.Find("Details")?.GetComponent<Renderer>() : null;
                Vector3 bootBefore = boot == null ? Vector3.zero : boot.bounds.center;
                Quaternion original = joint.localRotation;
                Vector3 axis = joint.name == "Door" || joint.name == "Swivel" ? Vector3.up : Vector3.right;
                joint.localRotation = original * Quaternion.AngleAxis(joint.name == "Door" ? 80 : 34, axis);
                if (Vector3.Distance(joint.position, pivot) > .0001f ||
                    (fixedRenderer != null && Vector3.Distance(fixedRenderer.bounds.center, fixedCenter) > .0001f) ||
                    Vector3.Distance(movedCenter, renderer.bounds.center) < .0001f ||
                    (sight != null && Vector3.Distance(sight.position, sightBefore) < .0001f) ||
                    (boot != null && Vector3.Distance(boot.bounds.center, bootBefore) < .0001f))
                    throw new InvalidOperationException(root.name + "/" + joint.name + ": pivote o movimiento inválido");
                joint.localRotation = original;
                if (joint.name == "RightShoulder" || joint.name == "RightKnee")
                    SampleCharacterPose(root, joint);
                results.Add(joint.name);
            }
            return string.Join(",", results);
        }

        private static void CapturePlayerView()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.58f, .62f, .67f);
            var light = new GameObject("Luz").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(42, -29, 0);
            light.intensity = 1.55f;
            var camera = new GameObject("Vista de jugador").AddComponent<Camera>();
            camera.transform.position = new Vector3(0, 1.65f, -3.2f);
            camera.transform.LookAt(new Vector3(0, 1.05f, 1.3f));
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.13f, .15f, .18f);
            camera.fieldOfView = 54;
            var target = new RenderTexture(1280, 720, 24);
            camera.targetTexture = target;
            var previous = RenderTexture.active;
            PreparePipeline();
            try
            {
                CaptureRoom(camera, target, "museo", new[] {
                    ("Museum/OH_MuseumGalleryDivider", new Vector3(0, 0, 2.6f)),
                    ("Museum/OH_MuseumShowcase", new Vector3(-.9f, 0, .7f)),
                    ("Museum/OH_MuseumPlinth", new Vector3(.85f, 0, 1.0f)),
                    ("Museum/OH_MuseumCustodyTablet", new Vector3(.85f, 1.01f, .98f)),
                    ("Museum/OH_MuseumSignalIdol", new Vector3(-.9f, 1.05f, .7f))
                });
                CaptureRoom(camera, target, "blackout", new[] {
                    ("Blackout/OH_BlackoutServerRack", new Vector3(-1.44f, 0, 2.35f)),
                    ("Blackout/OH_BlackoutServerRack", new Vector3(-.73f, 0, 2.35f)),
                    ("Blackout/OH_BlackoutSwitchCabinet", new Vector3(.05f, 0, 2.4f)),
                    ("Blackout/OH_BlackoutControlTerminal", new Vector3(.9f, 0, .95f)),
                    ("Blackout/OH_BlackoutStatusEmergency", new Vector3(.05f, 1.63f, 2.17f))
                });
                CaptureRoom(camera, target, "final", new[] {
                    ("Final/OH_FinalTunnelWall", new Vector3(-2.59f, 0, 2.6f)),
                    ("Final/OH_FinalTunnelWall", new Vector3(2.25f, 0, 2.6f)),
                    ("Final/OH_FinalBulkhead", new Vector3(-.18f, 0, 2.6f)),
                    ("Final/OH_FinalCoreCradle", new Vector3(.95f, 0, .56f)),
                    ("Final/OH_FinalArchiveCore", new Vector3(.95f, .85f, .56f)),
                    ("Final/OH_FinalStageConsole", new Vector3(-1.27f, 0, .77f))
                });
            }
            finally
            {
                RenderTexture.active = previous;
                target.Release();
            }
        }

        private static void CaptureRoom(Camera camera, RenderTexture target, string label,
            (string path, Vector3 position)[] placements)
        {
            var room = new GameObject(label);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.SetParent(room.transform);
            floor.transform.localPosition = new Vector3(0, -.06f, 1);
            floor.transform.localScale = new Vector3(7, .12f, 7);
            floor.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/03_Materials/OH_Kit_Steel.mat");
            foreach (var (path, position) in placements)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/02_Prefabs/Art/CampaignArt/" + path + ".prefab");
                if (asset == null) throw new FileNotFoundException(path);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
                instance.transform.SetParent(room.transform, false);
                instance.transform.localPosition = position;
            }
            var frame = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            try
            {
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target;
                frame.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                frame.Apply();
                File.WriteAllBytes($"{ReviewDir}/vista-{label}.png", frame.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(frame);
                Object.DestroyImmediate(room);
            }
        }

        private static void SampleCharacterPose(GameObject root, Transform joint)
        {
            var clip = new AnimationClip { legacy = true };
            Quaternion original = joint.localRotation;
            var path = AnimationUtility.CalculateTransformPath(joint, root.transform);
            try
            {
                clip.SetCurve(path, typeof(Transform), "localEulerAnglesRaw.x",
                    AnimationCurve.Linear(0f, original.eulerAngles.x, .45f, original.eulerAngles.x + 34f));
                clip.SampleAnimation(root, .45f);
                if (Quaternion.Angle(original, joint.localRotation) < 15)
                    throw new InvalidOperationException(root.name + "/" + joint.name + ": la pose no animó la articulación");
            }
            finally
            {
                joint.localRotation = original;
                Object.DestroyImmediate(clip);
            }
        }

        private static string Format(Vector3 value) =>
            FormattableString.Invariant($"{value.x:F3},{value.y:F3},{value.z:F3}");

        private static void PreparePipeline()
        {
            if (RenderPipelineManager.currentPipeline != null) return;
            var flags = BindingFlags.Static | BindingFlags.NonPublic;
            var method = typeof(RenderPipelineManager).GetMethod("TryPrepareRenderPipeline", flags)
                ?? typeof(RenderPipelineManager).GetMethod("PrepareRenderPipeline", flags);
            if (method == null) throw new InvalidOperationException("No se pudo inicializar URP de 6000.5.6f1.");
            method.Invoke(null, new object[] { GraphicsSettings.currentRenderPipeline });
        }
    }
}
