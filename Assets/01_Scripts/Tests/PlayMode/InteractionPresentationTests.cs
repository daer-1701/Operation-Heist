using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using OperationHeist.Player;
using OperationHeist.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace OperationHeist.Tests
{
    public class PresentationTestInteractor : PlayerInteractor
    {
        protected override bool HasApplicationFocus => true;
    }

    public class InteractionPresentationTests : InputTestFixture
    {
        private GameObject _player, _prop;

        [TearDown]
        public override void TearDown()
        {
            if (_player != null) Object.DestroyImmediate(_player);
            if (_prop != null) Object.DestroyImmediate(_prop);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator ModelInspectionUpdatesPromptAndFeedback()
        {
            _player = new GameObject("PresentationPlayer");
            _player.SetActive(false);
            _player.transform.position = new Vector3(1000,0,1000);
            var view = new GameObject("View",typeof(Camera));
            view.transform.SetParent(_player.transform,false);
            view.transform.localPosition = new Vector3(0,1.65f,0);
            var camera = view.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.08f,.1f,.13f);
            var capsule = _player.AddComponent<CharacterController>();
            capsule.height = 1.8f; capsule.radius = .3f; capsule.center = new Vector3(0,.9f,0);
            var motor = _player.AddComponent<FpsController>();
            var actor = _player.AddComponent<PresentationTestInteractor>();
            _player.AddComponent<InteractionPromptPresenter>();
            _player.SetActive(true);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(FpsController).GetField("gravity",flags).SetValue(motor,0f);
            typeof(FpsController).GetMethod("SetCursorLocked",flags).Invoke(motor,new object[]{true});
#if UNITY_EDITOR
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/02_Prefabs/Props/OH_TransportCrate.prefab");
            _prop = Object.Instantiate(prefab, new Vector3(1000,0,1001.5f), Quaternion.identity);
#endif
            Assert.That(_prop,Is.Not.Null);
            camera.transform.LookAt(_prop.GetComponent<Collider>().bounds.center);
            typeof(FpsController).GetMethod("CachePitchFromCamera",flags).Invoke(motor,null);
            Physics.SyncTransforms();
            yield return null;
            yield return null;
            Assert.That(actor.RefreshTarget(),Is.True);
            var canvas = _player.GetComponentInChildren<Canvas>();
            var labels = _player.GetComponentsInChildren<Text>();
            var prompt = System.Array.Find(labels,label=>label.name=="Prompt");
            var feedback = System.Array.Find(labels,label=>label.name=="Feedback");
            Assert.That(canvas.enabled,Is.True);
            Assert.That(prompt.text,Does.Contain("[E]"));
            if(SystemInfo.graphicsDeviceType!=GraphicsDeviceType.Null)
            {
                canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=1;
                Capture(camera,1280,720,"Interaction-1280x720.png");
                Capture(camera,1920,1080,"Interaction-1920x1080.png");
            }
            Assert.That(actor.TryInteract(),Is.True);
            yield return null;
            Assert.That(prompt.text,Does.Contain("inspeccionado"));
            Assert.That(feedback.text,Does.Contain("Inspeccionado:"));
            Assert.That(actor.TryInteract(),Is.False);
            Assert.That(actor.CurrentTarget.InteractionCount,Is.EqualTo(1));
            actor.enabled=false;
            yield return null;
            Assert.That(canvas.enabled,Is.False);
        }

        private static void Capture(Camera camera,int width,int height,string fileName)
        {
            if(RenderPipelineManager.currentPipeline==null)
            {
                var prepare=typeof(RenderPipelineManager).GetMethod("TryPrepareRenderPipeline",BindingFlags.Static|BindingFlags.NonPublic)
                    ?? typeof(RenderPipelineManager).GetMethod("PrepareRenderPipeline",BindingFlags.Static|BindingFlags.NonPublic);
                Assert.That(prepare,Is.Not.Null);
                prepare.Invoke(null,new object[]{GraphicsSettings.currentRenderPipeline});
            }
            var target=new RenderTexture(width,height,24);
            var image=new Texture2D(width,height,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;
            var previousTarget=camera.targetTexture;
#if UNITY_EDITOR
            bool async=UnityEditor.ShaderUtil.allowAsyncCompilation;
            UnityEditor.ShaderUtil.allowAsyncCompilation=false;
#endif
            try
            {
                camera.targetTexture=target;
                Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
                RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,width,height),0,0); image.Apply();
                Directory.CreateDirectory("Docs/QA");
                File.WriteAllBytes("Docs/QA/"+fileName,image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture=previousTarget; RenderTexture.active=previous;
#if UNITY_EDITOR
                UnityEditor.ShaderUtil.allowAsyncCompilation=async;
#endif
                target.Release(); Object.Destroy(target); Object.Destroy(image);
            }
        }
    }
}
