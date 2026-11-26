using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OperationHeist.Tests
{
    public class OriginalModelsTests
    {
        [UnityTest]
        public IEnumerator SceneLoadsFourOriginalPropsAndRenders()
        {
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/00_Scenes/FPSPrototype.unity", new LoadSceneParameters(LoadSceneMode.Single));
#else
            Assert.Ignore("El prototipo se comprueba desde el editor.");
            yield break;
#endif
            foreach (string name in new[] { "TransportCrate", "StorageShelf", "ServiceCounter", "CompactSafe" })
            {
                var prop = GameObject.Find("OH_" + name);
                Assert.That(prop, Is.Not.Null, name);
                Assert.That(prop.GetComponent<BoxCollider>(), Is.Not.Null);
                var mesh = prop.GetComponent<MeshFilter>().sharedMesh;
                Assert.That(mesh.vertexCount, Is.GreaterThan(0));
                Assert.That(mesh.bounds.min.y, Is.GreaterThanOrEqualTo(-0.001f));
                foreach (var material in prop.GetComponent<MeshRenderer>().sharedMaterials)
                {
                    Assert.That(material.shader.isSupported, Is.True);
                    Assert.That(material.GetTexture("_BaseMap"), Is.Null);
                }
            }
            var camera = Camera.main;
            Assert.That(camera, Is.Not.Null);
            camera.GetComponentInParent<OperationHeist.Player.FpsController>().enabled = false;
            camera.transform.position = new Vector3(14,12,-13);
            camera.transform.LookAt(new Vector3(0,.8f,3));
            yield return null;
            yield return null;
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
            {
                Assert.That(GraphicsSettings.currentRenderPipeline, Is.Not.Null, "Debe estar activo URP.");
                // Batch Mode has no Game View to initialize the pipeline before a render request.
                // Test-only compatibility hook for the project's pinned Unity editor.
                if (RenderPipelineManager.currentPipeline == null)
                {
                    var prepare = typeof(RenderPipelineManager).GetMethod("TryPrepareRenderPipeline", BindingFlags.Static | BindingFlags.NonPublic)
                        ?? typeof(RenderPipelineManager).GetMethod("PrepareRenderPipeline", BindingFlags.Static | BindingFlags.NonPublic);
                    Assert.That(prepare, Is.Not.Null, "Actualizar helper de pruebas al cambiar de versión de Unity.");
                    prepare.Invoke(null, new object[] { GraphicsSettings.currentRenderPipeline });
                }
                Assert.That(RenderPipelineManager.currentPipeline, Is.Not.Null, "Debe existir una instancia de pipeline.");
                Debug.Log("Preview pipeline: " + RenderPipelineManager.currentPipeline.GetType().FullName);
                var target = new RenderTexture(1400,850,24);
                var image = new Texture2D(1400,850,TextureFormat.RGB24,false);
                var previous = RenderTexture.active;
#if UNITY_EDITOR
                bool previousAsyncCompilation = UnityEditor.ShaderUtil.allowAsyncCompilation;
#endif
                try
                {
#if UNITY_EDITOR
                    UnityEditor.ShaderUtil.allowAsyncCompilation = false;
#endif
                    RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                    RenderTexture.active = target;
                    image.ReadPixels(new Rect(0,0,1400,850),0,0); image.Apply();
                    Directory.CreateDirectory("Docs/QA");
                    File.WriteAllBytes("Docs/QA/OriginalModels-preview.png",image.EncodeToPNG());
                }
                finally
                {
                    RenderTexture.active=previous; target.Release();
#if UNITY_EDITOR
                    UnityEditor.ShaderUtil.allowAsyncCompilation = previousAsyncCompilation;
#endif
                    Object.Destroy(target); Object.Destroy(image);
                }
            }
        }
    }
}
