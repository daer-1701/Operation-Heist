using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace OperationHeist.Editor
{
    public static class ProductionModelsBuilder
    {
        [Serializable] private class Recipes { public Model[] models; }
        [Serializable] private class Model { public string name, category; public Part[] parts; }
        [Serializable] private class Part
        {
            public string name, group;
            public Vector3 position, size, pivot, rotation;
            public int material, shape;
        }
        private const string MeshFolder = "Assets/04_Models/Codex/Production";
        private const string PrefabFolder = "Assets/02_Prefabs/Production";
        private static Material[] materials;
        private static Mesh cube, cylinder, ingot;

        [MenuItem("Operation Heist/Modelos/Generar kit depósito y banco")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(MeshFolder);
            Directory.CreateDirectory(PrefabFolder);
            Directory.CreateDirectory("Docs/QA");
            var recipes = JsonUtility.FromJson<Recipes>(File.ReadAllText("Tools/ModelRecipes.json"));
            string[] names = { "Steel", "Dark", "Brass", "Ivory", "Wood", "Teal", "Red", "Cloth", "Skin", "Screen" };
            Color[] colors = {
                new(.22f,.32f,.35f), new(.035f,.055f,.07f), new(.78f,.53f,.22f),
                new(.81f,.79f,.68f), new(.46f,.3f,.17f), new(.08f,.62f,.5f),
                new(.65f,.12f,.08f), new(.22f,.28f,.22f), new(.56f,.36f,.23f), new(.12f,.38f,.44f)
            };
            materials = new Material[names.Length];
            for (int i = 0; i < names.Length; i++)
                materials[i] = FpsPrototypeBuilder.OriginalMaterial("OH_Kit_" + names[i], colors[i]);
            cube = FpsPrototypeBuilder.OriginalCuboid();
            ingot = Object.Instantiate(cube);
            var ingotVertices = ingot.vertices;
            for (int i = 0; i < ingotVertices.Length; i++)
                if (ingotVertices[i].y > 0) { ingotVertices[i].x *= .75f; ingotVertices[i].z *= .75f; }
            ingot.vertices = ingotVertices;
            ingot.RecalculateNormals(); ingot.RecalculateBounds();
            ingot = SaveMesh(ingot, MeshFolder + "/OH_KitIngot.asset");
            cylinder = SaveMesh(CreateCylinder(), MeshFolder + "/OH_KitCylinder.asset");
            var gallery = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var report = new List<string> {
                "# Modelos originales para depósito y banco", "",
                "Generados con Codex el 5 de noviembre de 2026. Recetas en Tools/ModelRecipes.json y ProductionModelsBuilder.cs.",
                "Unidades en metros, eje Y arriba, frente hacia -Z, escala de raíz uno. Materiales planos URP originales.",
                "Los personajes usan piezas rígidas articuladas. Las animaciones y la integración con gameplay quedan como trabajo posterior.",
                "", "| Modelo | Categoría | Tamaño en metros | Triángulos |", "| --- | --- | --- | --- |"
            };
            for (int i = 0; i < recipes.models.Length; i++)
            {
                var model = recipes.models[i];
                var root = CreateModel(model);
                var bounds = BoundsOf(root);
                if (bounds.min.y < -.001f) throw new InvalidOperationException("Modelo bajo el suelo: " + model.name);
                int triangles = 0;
                foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
                {
                    var mesh = filter.sharedMesh;
                    triangles += mesh.triangles.Length / 3;
                    foreach (var vertex in mesh.vertices)
                        if (!float.IsFinite(vertex.x) || !float.IsFinite(vertex.y) || !float.IsFinite(vertex.z))
                            throw new InvalidOperationException("Vértice inválido: " + model.name);
                }
                string directory = PrefabFolder + "/" + model.category;
                Directory.CreateDirectory(directory);
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, directory + "/OH_" + model.name + ".prefab");
                Object.DestroyImmediate(root);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, gallery);
                instance.transform.position = new Vector3(i % 8 * 4f, 0, i / 8 * 4f);
                report.Add($"| OH_{model.name} | {model.category} | {bounds.size.ToString("F2")} | {triangles} |");
            }
            Lighting();
            EditorSceneManager.SaveScene(gallery, "Assets/00_Scenes/OH_ModelGallery.unity");
            AssetDatabase.SaveAssets();
            File.WriteAllLines("Docs/ModelCatalog.md", report);
            RenderCatalog(recipes);
            Debug.Log("PRODUCTION_MODELS_OK " + recipes.models.Length);
        }

        private static GameObject CreateModel(Model model)
        {
            var root = new GameObject("OH_" + model.name);
            var groups = new Dictionary<string, Transform> { [""] = root.transform };
            var partsByGroup = new Dictionary<string, List<Part>>();
            foreach (var part in model.parts)
            {
                if (!groups.ContainsKey(part.group))
                {
                    var joint = new GameObject(part.group).transform;
                    joint.SetParent(root.transform, false);
                    joint.localPosition = part.pivot;
                    groups[part.group] = joint;
                }
                if (!partsByGroup.ContainsKey(part.group)) partsByGroup[part.group] = new List<Part>();
                partsByGroup[part.group].Add(part);
            }
            foreach (var entry in partsByGroup)
            {
                Transform group = groups[entry.Key];
                var combinations = new List<CombineInstance>[materials.Length];
                for (int i = 0; i < combinations.Length; i++) combinations[i] = new List<CombineInstance>();
                foreach (var part in entry.Value)
                {
                    combinations[part.material].Add(new CombineInstance {
                        mesh = part.shape == 2 ? ingot : part.shape == 1 ? cylinder : cube,
                        transform = Matrix4x4.TRS(part.position - group.position, Quaternion.Euler(part.rotation), part.size)
                    });
                    if (model.category == "Architecture" || model.category == "Depot" || model.category == "Bank" || part.group == "Door")
                    {
                        var collision = group.gameObject.AddComponent<BoxCollider>();
                        collision.center = part.position - group.position;
                        var rotation = Quaternion.Euler(part.rotation);
                        var x = rotation * new Vector3(part.size.x, 0, 0);
                        var y = rotation * new Vector3(0, part.size.y, 0);
                        var z = rotation * new Vector3(0, 0, part.size.z);
                        collision.size = new Vector3(Mathf.Abs(x.x) + Mathf.Abs(y.x) + Mathf.Abs(z.x),
                            Mathf.Abs(x.y) + Mathf.Abs(y.y) + Mathf.Abs(z.y), Mathf.Abs(x.z) + Mathf.Abs(y.z) + Mathf.Abs(z.z));
                    }
                }
                var submeshes = new List<CombineInstance>();
                var usedMaterials = new List<Material>();
                foreach (var list in combinations)
                {
                    if (list.Count == 0) continue;
                    var submesh = new Mesh();
                    submesh.CombineMeshes(list.ToArray(), true, true);
                    submeshes.Add(new CombineInstance { mesh = submesh, transform = Matrix4x4.identity });
                    usedMaterials.Add(materials[Array.IndexOf(combinations, list)]);
                }
                var combined = new Mesh { name = "OH_" + model.name + "_" + (entry.Key == "" ? "Body" : entry.Key) };
                combined.CombineMeshes(submeshes.ToArray(), false, false);
                foreach (var submesh in submeshes) Object.DestroyImmediate(submesh.mesh);
                combined.RecalculateBounds();
                var saved = SaveMesh(combined, MeshFolder + "/" + combined.name + ".asset");
                group.gameObject.AddComponent<MeshFilter>().sharedMesh = saved;
                group.gameObject.AddComponent<MeshRenderer>().sharedMaterials = usedMaterials.ToArray();
            }
            if (model.category == "Characters")
            {
                foreach (string side in new[] { "Left", "Right" })
                {
                    groups[side + "Knee"].SetParent(groups[side + "Hip"], true);
                    groups[side + "Elbow"].SetParent(groups[side + "Shoulder"], true);
                    groups[side + "Shoulder"].SetParent(groups["Spine"], true);
                }
                groups["Head"].SetParent(groups["Spine"], true);
                var capsule = root.AddComponent<CapsuleCollider>();
                capsule.center = new Vector3(0, .93f, 0);
                capsule.height = 1.86f;
                capsule.radius = .26f;
            }
            else if (root.GetComponentsInChildren<Collider>().Length == 0)
            {
                var bounds = BoundsOf(root);
                var collider = root.AddComponent<BoxCollider>();
                collider.center = bounds.center;
                collider.size = bounds.size;
            }
            return root;
        }

        private static Mesh CreateCylinder()
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            const int segments = 12;
            void Triangle(Vector3 a, Vector3 b, Vector3 c)
            {
                int start = vertices.Count;
                vertices.Add(a); vertices.Add(b); vertices.Add(c);
                triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
            }
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments, b = (i + 1) * Mathf.PI * 2f / segments;
                Vector3 p = new(Mathf.Cos(a) * .5f, -.5f, Mathf.Sin(a) * .5f);
                Vector3 q = new(Mathf.Cos(b) * .5f, -.5f, Mathf.Sin(b) * .5f);
                Triangle(p, p + Vector3.up, q);
                Triangle(q, p + Vector3.up, q + Vector3.up);
                Triangle(Vector3.down * .5f, p, q);
                Triangle(Vector3.up * .5f, q + Vector3.up, p + Vector3.up);
            }
            var mesh = new Mesh { name = "OH_KitCylinder" };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh SaveMesh(Mesh mesh, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
            EditorUtility.CopySerialized(mesh, existing);
            Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static Bounds BoundsOf(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        private static void Lighting()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.55f, .57f, .6f);
            var light = new GameObject("Luz de catálogo").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 2.1f;
            light.transform.rotation = Quaternion.Euler(45, -35, 0);
        }

        private static void RenderCatalog(Recipes recipes)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                throw new InvalidOperationException("La revisión visual requiere GPU.");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Lighting();
            var camera = new GameObject("Cámara").AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.065f,.085f,.1f);
            if (RenderPipelineManager.currentPipeline == null)
            {
                var method = typeof(RenderPipelineManager).GetMethod("TryPrepareRenderPipeline", BindingFlags.Static | BindingFlags.NonPublic)
                    ?? typeof(RenderPipelineManager).GetMethod("PrepareRenderPipeline", BindingFlags.Static | BindingFlags.NonPublic);
                method.Invoke(null, new object[] { GraphicsSettings.currentRenderPipeline });
            }
            const int tile = 320, columns = 7;
            int rows = Mathf.CeilToInt(recipes.models.Length / (float)columns);
            var atlas = new Texture2D(tile * columns, tile * rows, TextureFormat.RGB24, false);
            var target = new RenderTexture(tile, tile, 24);
            bool oldAsync = ShaderUtil.allowAsyncCompilation;
            var oldTarget = RenderTexture.active;
            try
            {
                ShaderUtil.allowAsyncCompilation = false;
                for (int i = 0; i < recipes.models.Length; i++)
                {
                    var model = recipes.models[i];
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/" + model.category + "/OH_" + model.name + ".prefab");
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                    var bounds = BoundsOf(instance);
                    float radius = bounds.extents.magnitude;
                    camera.orthographicSize = radius * 1.15f;
                    camera.nearClipPlane = .001f;
                    camera.transform.position = bounds.center + new Vector3(1.2f,.85f,-1.6f).normalized * radius * 4f;
                    camera.transform.LookAt(bounds.center);
                    var label = new GameObject("Nombre").AddComponent<TextMesh>();
                    label.transform.SetParent(camera.transform, false);
                    label.transform.localPosition = new Vector3(0,-camera.orthographicSize*.88f,radius*2f);
                    label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    label.GetComponent<MeshRenderer>().sharedMaterial = label.font.material;
                    label.text = model.name;
                    label.anchor = TextAnchor.MiddleCenter;
                    label.characterSize = camera.orthographicSize*.065f;
                    label.fontSize = 32;
                    RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                    RenderTexture.active = target;
                    atlas.ReadPixels(new Rect(0,0,tile,tile), i % columns * tile, (rows - 1 - i / columns) * tile);
                    Object.DestroyImmediate(label.gameObject);
                    Object.DestroyImmediate(instance);
                }
                atlas.Apply();
                File.WriteAllBytes("Docs/QA/ProductionModels-preview.png", atlas.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldTarget;
                ShaderUtil.allowAsyncCompilation = oldAsync;
                target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(atlas);
            }
        }
    }
}
