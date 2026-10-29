using System.Collections;
using System.Reflection;
using NUnit.Framework;
using OperationHeist.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace OperationHeist.Tests
{
    public class FpsControllerTests : InputTestFixture
    {
        private GameObject player;
        private FpsController controller;
        private Camera camera;
        private GameObject ceiling;
        private Keyboard keyboard;
        private float previousCaptureDelta;
        private InputSettings.BackgroundBehavior previousBackgroundBehavior;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            previousCaptureDelta = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60f;
            previousBackgroundBehavior = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            player = new GameObject("TestPlayer");
            player.SetActive(false);
            var capsule = player.AddComponent<CharacterController>();
            capsule.height = 1.8f;
            capsule.radius = 0.3f;
            capsule.center = new Vector3(0, 0.9f, 0);
            var view = new GameObject("View", typeof(Camera), typeof(AudioListener));
            view.transform.SetParent(player.transform);
            view.transform.localPosition = new Vector3(0, 1.65f, 0);
            camera = view.GetComponent<Camera>();
            controller = player.AddComponent<FpsController>();
            player.SetActive(true);
        }

        [TearDown]
        public override void TearDown()
        {
            Object.DestroyImmediate(player);
            if (ceiling != null) Object.DestroyImmediate(ceiling);
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            Time.captureDeltaTime = previousCaptureDelta;
            InputSystem.settings.backgroundBehavior = previousBackgroundBehavior;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            base.TearDown();
        }

        [Test]
        public void ViewSettingsApplyAndClamp()
        {
            Assert.That(controller.PlayerCamera, Is.SameAs(camera));
            controller.FieldOfView = 85;
            Assert.That(camera.fieldOfView, Is.EqualTo(85).Within(0.01f));
            controller.FieldOfView = -100;
            Assert.That(camera.fieldOfView, Is.GreaterThan(0));
            controller.FieldOfView = 999;
            Assert.That(camera.fieldOfView, Is.LessThan(180));
            controller.MouseSensitivity = -1;
            Assert.That(controller.MouseSensitivity, Is.GreaterThan(0));
        }

#if UNITY_EDITOR
        [Test]
        public void PrefabKeepsControllerReference()
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/02_Prefabs/Player/FPSPlayer.prefab");
            Assert.That(prefab, Is.Not.Null);
            var component = prefab.GetComponent<FpsController>();
            Assert.That(component, Is.Not.Null);
            var serialized = new UnityEditor.SerializedObject(component);
            Assert.That(serialized.FindProperty("m_Script").objectReferenceValue, Is.Not.Null);
        }
#endif

        [UnityTest]
        public IEnumerator RemoteControlDisablesLocalPresentationAndMotion()
        {
            controller.SetLocalControl(false);
            Assert.That(controller.HasLocalControl, Is.False);
            Assert.That(camera.enabled, Is.False);
            Assert.That(camera.GetComponent<AudioListener>().enabled, Is.False);
            Vector3 start = player.transform.position;
            yield return null;
            yield return null;
            Assert.That(player.transform.position, Is.EqualTo(start));
            controller.SetLocalControl(true);
            Assert.That(controller.HasLocalControl, Is.True);
            Assert.That(camera.enabled, Is.True);
            Assert.That(camera.GetComponent<AudioListener>().enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator CrouchLowersViewAndCannotStandThroughCeiling()
        {
            // Exercise the physical crouch transition independently of OS window focus.
            var transition = typeof(FpsController).GetMethod("UpdateCrouchHeight", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(transition, Is.Not.Null);
            var capsule = player.GetComponent<CharacterController>();
            controller.enabled = false;
            float standingEyes = camera.transform.localPosition.y;
            float feet = player.transform.position.y + capsule.center.y - capsule.height * 0.5f;
            for (int i = 0; i < 30; i++)
            {
                transition.Invoke(controller, new object[] { true });
                yield return null;
            }
            Assert.That(controller.IsCrouching, Is.True);
            Assert.That(camera.transform.localPosition.y, Is.LessThan(standingEyes - 0.3f));
            Assert.That(player.transform.position.y + capsule.center.y - capsule.height * 0.5f, Is.EqualTo(feet).Within(0.01f));
            ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.transform.position = new Vector3(0, feet + 1.4f, 0);
            ceiling.transform.localScale = new Vector3(3, 0.2f, 3);
            Physics.SyncTransforms();
            for (int i = 0; i < 10; i++)
            {
                transition.Invoke(controller, new object[] { false });
                yield return null;
            }
            Assert.That(capsule.height, Is.LessThan(1.3f));
            Object.DestroyImmediate(ceiling);
            Physics.SyncTransforms();
            for (int i = 0; i < 30; i++)
            {
                transition.Invoke(controller, new object[] { false });
                yield return null;
            }
            Assert.That(controller.IsCrouching, Is.False);
            Assert.That(camera.transform.localPosition.y, Is.EqualTo(standingEyes).Within(0.02f));
        }

        [UnityTest]
        public IEnumerator InputNormalizesDiagonalAndSprintIsFaster()
        {
            controller.enabled = false;
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var move = typeof(FpsController).GetMethod("HandleMove", flags);
            keyboard = InputSystem.AddDevice<Keyboard>();
            var map = (InputActionMap)typeof(FpsController).GetField("_actionMap", flags).GetValue(controller);
            map.devices = new InputDevice[] { keyboard };
            typeof(FpsController).GetMethod("EnableActions", flags).Invoke(controller, null);
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            InputSystem.Update();
            Assert.That(keyboard.wKey.isPressed, Is.True, "El dispositivo simulado debe recibir el input.");
            Assert.That(map.FindAction("Move").enabled, Is.True);
            Assert.That(map.FindAction("Move").ReadValue<Vector2>().y, Is.GreaterThan(0), "Move debe resolver el teclado simulado.");
            Assert.That(Time.deltaTime, Is.GreaterThan(0));
            Vector3 start = player.transform.position;
            move.Invoke(controller, null);
            float walk = Vector3.ProjectOnPlane(player.transform.position - start, Vector3.up).magnitude;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.D));
            InputSystem.Update();
            start = player.transform.position;
            move.Invoke(controller, null);
            float diagonal = Vector3.ProjectOnPlane(player.transform.position - start, Vector3.up).magnitude;
            Assert.That(walk, Is.GreaterThan(0));
            Assert.That(diagonal, Is.EqualTo(walk).Within(0.001f));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.LeftShift));
            InputSystem.Update();
            start = player.transform.position;
            move.Invoke(controller, null);
            float sprint = Vector3.ProjectOnPlane(player.transform.position - start, Vector3.up).magnitude;
            Assert.That(sprint, Is.GreaterThan(walk * 1.2f));
        }
    }
}
