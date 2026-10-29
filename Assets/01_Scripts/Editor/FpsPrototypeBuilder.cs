using System;
using System.IO;
using OperationHeist.Player;
using OperationHeist.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OperationHeist.Editor
{
    [InitializeOnLoad]
    public static class FpsPrototypeBuilder
    {
        private const string ScenePath = "Assets/00_Scenes/FPSPrototype.unity";
        private const string PrefabPath = "Assets/02_Prefabs/Player/FPSPlayer.prefab";
        private const string RequestPath = "Tools/FPSSetup.request";
        static FpsPrototypeBuilder() { EditorApplication.delayCall += ProcessRequest; }

        private static void ProcessRequest()
        {
            if (!File.Exists(RequestPath)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += ProcessRequest;
                return;
            }
            File.Delete(RequestPath);
            try { Build(); File.WriteAllText("Tools/FPSSetup.result", "OK"); }
            catch (Exception exception)
            {
                File.WriteAllText("Tools/FPSSetup.result", exception.ToString());
                Debug.LogException(exception);
            }
        }

        [MenuItem("Operation Heist/Crear prototipo FPS")]
        public static void Build()
        {
            Directory.CreateDirectory("Assets/02_Prefabs/Player");
            Directory.CreateDirectory("Assets/00_Scenes");
            Directory.CreateDirectory("Assets/03_Materials");
            Directory.CreateDirectory("Assets/04_Models");
            AssetDatabase.Refresh();
            Scene previousScene = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                var root = new GameObject("FPSPlayer");
                var character = root.AddComponent<CharacterController>();
                character.height = 1.8f;
                character.radius = 0.3f;
                character.center = new Vector3(0, 0.9f, 0);
                var cameraObject = new GameObject("PlayerCamera", typeof(Camera), typeof(AudioListener));
                cameraObject.transform.SetParent(root.transform, false);
                cameraObject.transform.localPosition = new Vector3(0, 1.65f, 0);
                cameraObject.tag = "MainCamera";
                var view = cameraObject.GetComponent<Camera>();
                view.clearFlags = CameraClearFlags.SolidColor;
                view.backgroundColor = new Color(0.08f, 0.1f, 0.13f);
                var controller = root.AddComponent<FpsController>();
                var serialized = new SerializedObject(controller);
                const string scriptPath = "Assets/01_Scripts/Player/Movement/FpsController.cs";
                AssetDatabase.ImportAsset(scriptPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
                if (script == null || script.GetClass() != typeof(FpsController))
                    throw new InvalidOperationException($"No se pudo resolver el MonoScript del controlador FPS. Asset={script}, class={script?.GetClass()}, expected={typeof(FpsController)}");
                serialized.FindProperty("m_Script").objectReferenceValue = script;
                serialized.FindProperty("playerCamera").objectReferenceValue = cameraObject.GetComponent<Camera>();
                serialized.ApplyModifiedPropertiesWithoutUndo();
                root.AddComponent<PlayerInteractor>();
                root.AddComponent<InteractionPromptPresenter>();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                root.transform.position = new Vector3(0, 0.1f, -6);
                Box("Ground", new Vector3(0, -0.5f, 0), new Vector3(24, 1, 24));
                Box("Wall_Left", new Vector3(-12, 1.5f, 0), new Vector3(0.5f, 3, 24));
                Box("Wall_Right", new Vector3(12, 1.5f, 0), new Vector3(0.5f, 3, 24));
                Box("Wall_Back", new Vector3(0, 1.5f, 12), new Vector3(24, 3, 0.5f));
                Box("Wall_Front", new Vector3(0, 1.5f, -12), new Vector3(24, 3, 0.5f));
                Box("LowTunnel_Ceiling", new Vector3(0, 1.3f, 2), new Vector3(3, 0.3f, 5));
                Box("Obstacle", new Vector3(5, 0.75f, 0), new Vector3(2, 1.5f, 2));
                var light = new GameObject("Sun", typeof(Light)).GetComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 2;
                light.transform.rotation = Quaternion.Euler(50, -30, 0);
                RenderSettings.ambientLight = Color.gray;
                RenderSettings.skybox = null;
                if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("No se pudo guardar la escena FPS.");
                AssetDatabase.SaveAssets();
                Debug.Log("Operation Heist: prefab FPS y escena de prueba creados.");
            }
            finally
            {
                if (!Application.isBatchMode && previousScene.IsValid())
                {
                    SceneManager.SetActiveScene(previousScene);
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static void Box(string name, Vector3 position, Vector3 scale)
        {
            // Geometría creada por el proyecto; no depende de meshes de packs o plantillas.
            var cube = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer), typeof(BoxCollider));
            cube.GetComponent<MeshFilter>().sharedMesh = OriginalCuboid();
            string materialName = name == "Ground" ? "OH_Floor" : name == "Obstacle" || name == "LowTunnel_Ceiling" ? "OH_Safety" : "OH_Wall";
            Color color = name == "Ground" ? new Color(0.18f, 0.2f, 0.22f) : name == "Obstacle" || name == "LowTunnel_Ceiling" ? new Color(0.7f, 0.36f, 0.08f) : new Color(0.42f, 0.46f, 0.5f);
            cube.GetComponent<MeshRenderer>().sharedMaterial = OriginalMaterial(materialName, color);
            cube.name = name;
            cube.transform.position = position;
            cube.transform.localScale = scale;
        }

        internal static Material OriginalMaterial(string name, Color color)
        {
            string path = $"Assets/03_Materials/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit no está disponible.");
            material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.15f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        internal static Mesh OriginalCuboid()
        {
            const string path = "Assets/04_Models/OH_Cuboid.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;
            var vertices = new Vector3[24];
            var triangles = new int[36];
            var normals = new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            for (int face = 0; face < normals.Length; face++)
            {
                Vector3 normal = normals[face];
                Vector3 reference = Mathf.Abs(normal.y) > 0.5f ? Vector3.right : Vector3.up;
                Vector3 u = Vector3.Cross(reference, normal).normalized * 0.5f;
                Vector3 v = Vector3.Cross(normal, u);
                Vector3 center = normal * 0.5f;
                int start = face * 4;
                vertices[start] = center - u - v;
                vertices[start + 1] = center + u - v;
                vertices[start + 2] = center + u + v;
                vertices[start + 3] = center - u + v;
                int index = face * 6;
                triangles[index] = start; triangles[index + 1] = start + 1; triangles[index + 2] = start + 2;
                triangles[index + 3] = start; triangles[index + 4] = start + 2; triangles[index + 5] = start + 3;
            }
            mesh = new Mesh { name = "OH_Cuboid", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }
    }
}
