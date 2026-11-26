using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace OperationHeist.Editor
{
    public static class CampaignArtBuilder
    {
        private const string MeshDir = "Assets/04_Models/Codex/CampaignArt";
        private const string PrefabDir = "Assets/02_Prefabs/Art/CampaignArt";
        private const string MatDir = "Assets/03_Materials/CampaignArt";

        [Serializable] private sealed class Recipes { public Model[] models; }
        [Serializable] private sealed class Model
        {
            public string name, category, use, source, prefabSource, uniform;
            public Vector3 target;
            public Part[] parts;
            public Anchor[] anchors;
        }
        [Serializable] private sealed class Part
        {
            public string name, shape, material, group;
            public Vector3 position, size, pivot, rotation;
        }
        [Serializable] private sealed class Anchor { public string name, parent; public Vector3 position; }

        private static Dictionary<string, Mesh> meshes;
        private static Dictionary<string, Material> materials;

        public static void Build()
        {
            var recipes = JsonUtility.FromJson<Recipes>(File.ReadAllText("CampaignArtRecipes.json"));
            Directory.CreateDirectory(MeshDir);
            Directory.CreateDirectory(PrefabDir);
            Directory.CreateDirectory(MatDir);
            AssetDatabase.Refresh();
            meshes = new Dictionary<string, Mesh> {
                ["Box"] = Required<Mesh>("Assets/04_Models/OH_Cuboid.asset"),
                ["Cylinder"] = Required<Mesh>("Assets/04_Models/Codex/Production/OH_KitCylinder.asset"),
                ["Ingot"] = Required<Mesh>("Assets/04_Models/Codex/Production/OH_KitIngot.asset"),
                ["Sphere"] = Required<Mesh>("Assets/04_Models/Codex/Production/OH_KitSphere.asset")
            };
            materials = new[] { "Steel", "Dark", "Brass", "Ivory", "Wood", "Teal", "Red", "Cloth", "Skin", "Screen" }
                .ToDictionary(name => name, name => Required<Material>($"Assets/03_Materials/OH_Kit_{name}.mat"));
            materials["Glass"] = Glass();
            var report = new List<string> { "prefab\tuse\tsource\tmeshes\ttriangles\tbounds\tanchors" };
            foreach (var model in recipes.models)
            {
                if (!string.IsNullOrWhiteSpace(model.source) &&
                    AssetDatabase.LoadAssetAtPath<GameObject>($"{MeshDir}/{model.source}.obj") == null)
                    throw new FileNotFoundException("Falta fuente generada para " + model.name);
                var root = new GameObject(model.name);
                try
                {
                    if (!string.IsNullOrEmpty(model.prefabSource)) AddPrefabVariant(root, model);
                    else if (string.IsNullOrEmpty(model.source)) AddParts(root, model);
                    else
                    {
                        AddGeneratedMesh(root, model);
                        if (model.parts.Length > 0) AddParts(root, model);
                    }
                    foreach (var anchor in model.anchors)
                    {
                        Transform parent = string.IsNullOrEmpty(anchor.parent)
                            ? root.transform : root.transform.Find(anchor.parent);
                        if (parent == null) throw new InvalidOperationException(model.name + "/" + anchor.parent);
                        CreateChild(parent, anchor.name, parent.InverseTransformPoint(
                            root.transform.TransformPoint(anchor.position)));
                    }
                    var renderers = root.GetComponentsInChildren<MeshRenderer>();
                    if (renderers.Length == 0) throw new InvalidOperationException(model.name);
                    var filters = root.GetComponentsInChildren<MeshFilter>();
                    int triangles = 0;
                    foreach (var filter in filters)
                    {
                        var mesh = filter.sharedMesh;
                        if (mesh == null || !mesh.HasVertexAttribute(VertexAttribute.Normal) ||
                            !mesh.HasVertexAttribute(VertexAttribute.TexCoord0))
                            throw new InvalidOperationException(model.name + ": falta malla, normales o UV0");
                        triangles += Enumerable.Range(0, mesh.subMeshCount).Sum(i => (int)mesh.GetIndexCount(i) / 3);
                    }
                    Bounds bounds = renderers[0].bounds;
                    foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                    if (!float.IsFinite(bounds.size.sqrMagnitude) || bounds.min.y < -.004f)
                        throw new InvalidOperationException(model.name + ": límites " + bounds);
                    string dir = $"{PrefabDir}/{model.category}";
                    if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder(PrefabDir, model.category);
                    string path = $"{dir}/{model.name}.prefab";
                    if (PrefabUtility.SaveAsPrefabAsset(root, path) == null)
                        throw new InvalidOperationException("No se guardó " + path);
                    report.Add(string.Join("\t", path, model.use, model.source, filters.Length, triangles,
                        FormattableString.Invariant($"{bounds.size.x:F3},{bounds.size.y:F3},{bounds.size.z:F3}"),
                        string.Join(",", model.anchors.Select(a => a.name))));
                }
                finally { Object.DestroyImmediate(root); }
            }
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("CampaignArtEvidence");
            File.WriteAllLines("CampaignArtEvidence/build.tsv", report);
            Debug.Log($"CAMPAIGN_ART_BUILD_OK {report.Count - 1}/{recipes.models.Length}");
        }

        private static void AddParts(GameObject root, Model model)
        {
            string path = $"{MeshDir}/{model.name}.asset";
            var saved = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>()
                .ToDictionary(mesh => mesh.name);
            bool hasMainObject = saved.Count > 0;
            foreach (var group in model.parts.GroupBy(part => part.group))
            {
                string groupName = string.IsNullOrWhiteSpace(group.Key) ? "Body" : group.Key;
                Vector3 pivot = group.First().pivot;
                if (group.Any(part => part.pivot != pivot))
                    throw new InvalidOperationException(model.name + "/" + groupName + ": pivotes incompatibles");
                var character = root.transform.Find("Character");
                var joint = character == null ? null : character.GetComponentsInChildren<Transform>()
                    .FirstOrDefault(candidate => candidate.name == groupName);
                var node = joint == null
                    ? CreateChild(root.transform, groupName, pivot)
                    : CreateChild(joint, "Details", joint.InverseTransformPoint(root.transform.TransformPoint(pivot)));
                var temporary = new List<Mesh>();
                var palette = new List<Material>();
                foreach (var materialGroup in group.GroupBy(part => part.material))
                {
                    var combines = materialGroup.Select(part => new CombineInstance
                    {
                        mesh = meshes[part.shape],
                        transform = Matrix4x4.TRS(part.position - pivot, Quaternion.Euler(part.rotation), part.size)
                    }).ToArray();
                    var materialMesh = new Mesh();
                    materialMesh.CombineMeshes(combines, true);
                    temporary.Add(materialMesh);
                    palette.Add(materials[materialGroup.Key]);
                }
                string meshName = model.name + "_" + groupName;
                bool existing = saved.TryGetValue(meshName, out Mesh combined);
                if (!existing) combined = new Mesh { name = meshName };
                combined.CombineMeshes(temporary.Select(mesh => new CombineInstance { mesh = mesh }).ToArray(),
                    false, false);
                combined.RecalculateBounds();
                FillMeshAttributes(combined);
                foreach (var temp in temporary) Object.DestroyImmediate(temp);
                if (existing) EditorUtility.SetDirty(combined);
                else if (!hasMainObject)
                {
                    AssetDatabase.CreateAsset(combined, path);
                    hasMainObject = true;
                }
                else AssetDatabase.AddObjectToAsset(combined, path);
                node.gameObject.AddComponent<MeshFilter>().sharedMesh = combined;
                node.gameObject.AddComponent<MeshRenderer>().sharedMaterials = palette.ToArray();
            }
        }

        private static void AddGeneratedMesh(GameObject root, Model model)
        {
            string path = $"{MeshDir}/{model.source}.obj";
            var modelAsset = Required<GameObject>(path);
            var node = Object.Instantiate(modelAsset, root.transform);
            node.name = "Body";
            var renderers = node.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length == 0) throw new InvalidOperationException(path);
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            if (bounds.size.x <= 0 || bounds.size.y <= 0 || bounds.size.z <= 0)
                throw new InvalidOperationException("Dimensiones vacías: " + path);
            node.transform.localScale = new Vector3(model.target.x / bounds.size.x,
                model.target.y / bounds.size.y, model.target.z / bounds.size.z);
            var bottom = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            node.transform.localPosition = -Vector3.Scale(bottom, node.transform.localScale);
            foreach (var renderer in renderers)
                renderer.sharedMaterials = Enumerable.Repeat(
                    materials[model.category == "Museum" ? "Teal" : "Steel"],
                    renderer.sharedMaterials.Length).ToArray();
        }

        private static void AddPrefabVariant(GameObject root, Model model)
        {
            var source = Required<GameObject>(model.prefabSource);
            var body = Object.Instantiate(source, root.transform);
            body.name = "Character";
            if (!string.IsNullOrEmpty(model.uniform))
                foreach (var renderer in body.GetComponentsInChildren<MeshRenderer>())
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(mat =>
                        mat != null && (mat.name.Contains("Cloth") || mat.name == "OH_Guard_Mat5")
                            ? materials[model.uniform] : mat).ToArray();
            foreach (var collider in body.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(collider);
            Object.DestroyImmediate(body.GetComponent<MeshRenderer>());
            Object.DestroyImmediate(body.GetComponent<MeshFilter>());
            AddParts(root, model);
            AddMissingCharacterUvs(body, model.name);
            var inUse = root.GetComponentsInChildren<MeshFilter>().Select(filter => filter.sharedMesh).ToHashSet();
            foreach (var mesh in AssetDatabase.LoadAllAssetRepresentationsAtPath($"{MeshDir}/{model.name}.asset").OfType<Mesh>())
                if (!inUse.Contains(mesh)) Object.DestroyImmediate(mesh, true);
        }

        private static void AddMissingCharacterUvs(GameObject body, string modelName)
        {
            string path = $"{MeshDir}/{modelName}.asset";
            var saved = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().ToDictionary(mesh => mesh.name);
            bool fileExists = saved.Count > 0;
            var filters = body.GetComponentsInChildren<MeshFilter>();
            for (int index = 0; index < filters.Length; index++)
            {
                var filter = filters[index];
                var source = filter.sharedMesh;
                if (source.HasVertexAttribute(VertexAttribute.TexCoord0) &&
                    source.HasVertexAttribute(VertexAttribute.Normal)) continue;
                string name = $"{modelName}_Original_{index:D2}";
                var copy = Object.Instantiate(source);
                copy.name = name;
                FillMeshAttributes(copy);
                if (saved.TryGetValue(name, out var existing))
                {
                    EditorUtility.CopySerialized(copy, existing);
                    EditorUtility.SetDirty(existing);
                    Object.DestroyImmediate(copy);
                    filter.sharedMesh = existing;
                    continue;
                }
                if (fileExists) AssetDatabase.AddObjectToAsset(copy, path);
                else
                {
                    AssetDatabase.CreateAsset(copy, path);
                    fileExists = true;
                }
                filter.sharedMesh = copy;
            }
        }

        private static void FillMeshAttributes(Mesh mesh)
        {
            if (!mesh.HasVertexAttribute(VertexAttribute.Normal)) mesh.RecalculateNormals();
            if (mesh.HasVertexAttribute(VertexAttribute.TexCoord0)) return;
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var bounds = mesh.bounds;
            var size = Vector3.Max(bounds.size, Vector3.one * .0001f);
            var uv = new Vector2[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 point = vertices[i] - bounds.min;
                var normal = normals[i];
                float x = Mathf.Abs(normal.x), y = Mathf.Abs(normal.y), z = Mathf.Abs(normal.z);
                if (x >= y && x >= z) uv[i] = new Vector2(point.z / size.z, point.y / size.y);
                else if (y >= z) uv[i] = new Vector2(point.x / size.x, point.z / size.z);
                else uv[i] = new Vector2(point.x / size.x, point.y / size.y);
            }
            mesh.uv = uv;
        }

        private static Material Glass()
        {
            string path = $"{MatDir}/OH_DisplayGlass.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit no disponible.");
            bool create = material == null;
            if (create) material = new Material(shader) { name = "OH_DisplayGlass" };
            material.SetColor("_BaseColor", new Color(.35f, .70f, .73f, .16f));
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Smoothness", .82f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetShaderPassEnabled("ShadowCaster", false);
            material.SetShaderPassEnabled("DepthOnly", false);
            if (create) AssetDatabase.CreateAsset(material, path);
            else EditorUtility.SetDirty(material);
            return material;
        }

        private static Transform CreateChild(Transform parent, string name, Vector3 position)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            child.localPosition = position;
            return child;
        }

        private static T Required<T>(string path) where T : Object =>
            AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new FileNotFoundException(path);
    }
}
