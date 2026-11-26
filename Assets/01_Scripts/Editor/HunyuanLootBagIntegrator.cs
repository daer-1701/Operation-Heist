using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace OperationHeist.Editor
{
    public static class HunyuanLootBagIntegrator
    {
        private const string MaterialsFolder = "Assets/03_Materials/Hunyuan";
        private const string TexturesFolder = "Assets/05_Textures/Hunyuan";

        [MenuItem("Operation Heist/Integrar bolsa y expediente Hunyuan")]
        public static void IntegrateBatch()
        {
            if (!AssetDatabase.IsValidFolder(MaterialsFolder))
                AssetDatabase.CreateFolder("Assets/03_Materials", "Hunyuan");
            if (!AssetDatabase.IsValidFolder(TexturesFolder))
                AssetDatabase.CreateFolder("Assets/05_Textures", "Hunyuan");

            Integrate("Assets/04_Models/Hunyuan/LootBag/OH_LootBag.fbx", "OH_LootBag", false,
                new Color(.31f, .34f, .20f));
            Integrate("Assets/04_Models/nuevosmodelos/OH_Dossier.fbx", "OH_Dossier", true,
                new Color(.27f, .29f, .30f));
            AssetDatabase.SaveAssets();
            Debug.Log("HUNYUAN_LOOT_MODELS_OK");
        }

        public static void IntegrateBankBatch()
        {
            IntegrateBatch();
            MissionSetBuilder.BuildBank();
            NetworkLabBuilder.BuildBank();
        }

        private static void Integrate(string fbx, string name, bool layFlat, Color fallback)
        {
            if (!File.Exists(fbx)) throw new FileNotFoundException("No se encuentra el FBX original.", fbx);
            AssetDatabase.ImportAsset(fbx);
            if (AssetImporter.GetAtPath(fbx) is not ModelImporter importer)
                throw new InvalidOperationException("Importador de modelo ausente: " + fbx);
            importer.animationType = ModelImporterAnimationType.None;
            importer.importAnimation = false;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Medium;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.SaveAndReimport();
            string texturePath = $"{TexturesFolder}/{name}";
            if (!AssetDatabase.IsValidFolder(texturePath))
                AssetDatabase.CreateFolder(TexturesFolder, name);
            importer.ExtractTextures(texturePath);
            AssetDatabase.Refresh();
            var color = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath + "/texture_pbr_20250901.png");
            string normalPath = texturePath + "/texture_pbr_20250901_normal.png";
            if (AssetImporter.GetAtPath(normalPath) is TextureImporter normalImporter &&
                normalImporter.textureType != TextureImporterType.NormalMap)
            {
                normalImporter.textureType = TextureImporterType.NormalMap;
                normalImporter.SaveAndReimport();
            }
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            string prefabPath = $"Assets/02_Prefabs/Production/Loot/{name}.prefab";
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var original = root.transform.Find("Modelo Hunyuan");
                if (original != null) UnityEngine.Object.DestroyImmediate(original.gameObject);
                var collider = root.GetComponent<BoxCollider>();
                var visual = new GameObject("Modelo Hunyuan").transform;
                visual.SetParent(root.transform, false);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, visual);
                var renderers = instance.GetComponentsInChildren<MeshRenderer>();
                if (renderers.Length == 0) throw new InvalidOperationException("FBX sin renderizadores: " + fbx);

                Bounds bounds = GetBounds(renderers);
                if (layFlat && bounds.size.y > Mathf.Min(bounds.size.x, bounds.size.z))
                {
                    visual.localRotation = bounds.size.x < bounds.size.z
                        ? Quaternion.Euler(0f, 0f, 90f) : Quaternion.Euler(90f, 0f, 0f);
                    bounds = GetBounds(renderers);
                }
                if (bounds.size.x < bounds.size.z)
                {
                    visual.localRotation = Quaternion.Euler(0f, 90f, 0f) * visual.localRotation;
                    bounds = GetBounds(renderers);
                }
                var size = bounds.size;
                if (Mathf.Min(size.x, size.y, size.z) < .0001f)
                    throw new InvalidOperationException("El FBX tiene dimensiones degeneradas: " + fbx);
                float scale = Mathf.Min(collider.size.x / size.x, collider.size.y / size.y, collider.size.z / size.z);
                visual.localScale = Vector3.one * scale;
                bounds = GetBounds(renderers);
                Vector3 basePoint = bounds.center;
                basePoint.y = bounds.min.y;
                visual.localPosition = collider.center - Vector3.up * (collider.size.y * .5f)
                    - root.transform.InverseTransformPoint(basePoint);

                int materialIndex = 0;
                int textured = 0;
                long triangles = 0;
                foreach (var renderer in renderers)
                {
                    var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                    var materials = renderer.sharedMaterials;
                    Array.Resize(ref materials, mesh.subMeshCount);
                    for (int slot = 0; slot < materials.Length; slot++)
                    {
                        var material = ConvertMaterial(materials[slot], name, materialIndex++, fallback, color, normal);
                        if (material.GetTexture("_BaseMap") != null) textured++;
                        materials[slot] = material;
                        triangles += mesh.GetIndexCount(slot) / 3;
                    }
                    renderer.sharedMaterials = materials;
                }

                root.GetComponent<MeshFilter>().sharedMesh = null;
                root.GetComponent<MeshRenderer>().enabled = false;
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out bool saved);
                if (!saved) throw new IOException("No se pudo guardar " + prefabPath);
                Debug.Log($"HUNYUAN_MODEL_OK {name} tris={triangles} slots={materialIndex} conTextura={textured} tamaño={GetBounds(renderers).size}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Bounds GetBounds(MeshRenderer[] renderers)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static Material ConvertMaterial(Material source, string name, int slot, Color fallback,
            Texture generatedColor, Texture generatedNormal)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit no está disponible.");
            string path = $"{MaterialsFolder}/{name}_{slot:00}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            Texture texture = null;
            if (source != null && source.HasProperty("_BaseMap")) texture = source.GetTexture("_BaseMap");
            if (texture == null && source != null && source.HasProperty("_MainTex")) texture = source.GetTexture("_MainTex");
            if (texture == null) texture = generatedColor;
            material.SetTexture("_BaseMap", texture);
            material.SetTexture("_BumpMap", generatedNormal);
            if (generatedNormal != null) material.EnableKeyword("_NORMALMAP");
            else material.DisableKeyword("_NORMALMAP");
            material.SetColor("_BaseColor", texture == null ? fallback : Color.white);
            material.SetFloat("_Smoothness", .2f);
            material.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
