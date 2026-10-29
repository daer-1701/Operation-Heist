using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using OperationHeist.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace OperationHeist.Tests
{
    /// <summary>
    /// Play Mode coverage for local raycast interaction. Uses a Focus override so
    /// batch/headless runs do not depend on OS window focus.
    /// </summary>
    public class PlayerInteractorTests : InputTestFixture
    {
        const float FarOriginX = 5000f;
        const float FarOriginZ = 5000f;

        static readonly string[] PropPrefabPaths =
        {
            "Assets/02_Prefabs/Props/OH_TransportCrate.prefab",
            "Assets/02_Prefabs/Props/OH_StorageShelf.prefab",
            "Assets/02_Prefabs/Props/OH_ServiceCounter.prefab",
            "Assets/02_Prefabs/Props/OH_CompactSafe.prefab"
        };

        GameObject _player;
        FpsController _controller;
        Camera _camera;
        TestablePlayerInteractor _interactor;
        Keyboard _keyboard;
        readonly List<GameObject> _spawned = new List<GameObject>();
        float _previousCaptureDelta;
        InputSettings.BackgroundBehavior _previousBackgroundBehavior;
        CursorLockMode _previousLockMode;
        bool _previousCursorVisible;
        MethodInfo _setCursorLocked;

        sealed class TestablePlayerInteractor : PlayerInteractor
        {
            public bool Focused = true;
            protected override bool HasApplicationFocus => Focused;
        }

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            _previousCaptureDelta = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60f;
            _previousBackgroundBehavior = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            _previousLockMode = Cursor.lockState;
            _previousCursorVisible = Cursor.visible;

            _setCursorLocked = typeof(FpsController).GetMethod(
                "SetCursorLocked",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(_setCursorLocked, Is.Not.Null, "FpsController.SetCursorLocked must remain available for fixtures.");

            // Far from FPSPrototype props; avoids depending on the currently open scene.
            _player = new GameObject("InteractorTestPlayer");
            _player.SetActive(false);
            _player.transform.position = new Vector3(FarOriginX, 0f, FarOriginZ);
            var capsule = _player.AddComponent<CharacterController>();
            capsule.height = 1.8f;
            capsule.radius = 0.3f;
            capsule.center = new Vector3(0f, 0.9f, 0f);
            var view = new GameObject("View", typeof(Camera), typeof(AudioListener));
            view.transform.SetParent(_player.transform);
            view.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            _camera = view.GetComponent<Camera>();
            _controller = _player.AddComponent<FpsController>();
            _interactor = _player.AddComponent<TestablePlayerInteractor>();
            _player.SetActive(true);
            // Geometry/input tests must not drift due to gravity while yielding frames.
            typeof(FpsController).GetField("gravity", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_controller, 0f);

            _controller.SetLocalControl(true);
            SetCursorCaptured(true);
            _interactor.Focused = true;
            _keyboard = InputSystem.AddDevice<Keyboard>();
            Physics.SyncTransforms();
        }

        [TearDown]
        public override void TearDown()
        {
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null)
                {
                    Object.DestroyImmediate(_spawned[i]);
                }
            }

            _spawned.Clear();

            if (_player != null)
            {
                Object.DestroyImmediate(_player);
            }

            if (_keyboard != null)
            {
                InputSystem.RemoveDevice(_keyboard);
                _keyboard = null;
            }

            Time.captureDeltaTime = _previousCaptureDelta;
            InputSystem.settings.backgroundBehavior = _previousBackgroundBehavior;
            Cursor.lockState = _previousLockMode;
            Cursor.visible = _previousCursorVisible;
            base.TearDown();
        }

        void SetCursorCaptured(bool locked)
        {
            _setCursorLocked.Invoke(_controller, new object[] { locked });
        }

        GameObject Track(GameObject go)
        {
            _spawned.Add(go);
            return go;
        }

        PrototypeInteractable SpawnProp(Vector3 worldPosition, Vector3? scale = null, string name = "Prop")
        {
            var go = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            go.name = name;
            go.transform.position = worldPosition;
            go.transform.localScale = scale ?? new Vector3(0.4f, 0.4f, 0.4f);
            var prop = go.AddComponent<PrototypeInteractable>();
            prop.Configure(name);
            Physics.SyncTransforms();
            return prop;
        }

        GameObject SpawnSolidObstacle(Vector3 worldPosition, Vector3 scale, string name = "Wall")
        {
            var go = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            go.name = name;
            go.transform.position = worldPosition;
            go.transform.localScale = scale;
            Physics.SyncTransforms();
            return go;
        }

        Vector3 EyePosition => _camera.transform.position;

        Vector3 Ahead(float distance) => EyePosition + _player.transform.forward * distance;

        [Test]
        public void DefaultMaxDistanceIsTwoPointFive()
        {
            Assert.That(_interactor.MaxDistance, Is.EqualTo(2.5f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator RefreshTarget_WithinRange_SetsCurrentTarget()
        {
            var prop = SpawnProp(Ahead(1.5f));
            yield return null;
            Assert.That(_interactor.RefreshTarget(), Is.True);
            Assert.That(_interactor.CurrentTarget, Is.SameAs(prop));
            Assert.That(_interactor.CanInteractWith(prop), Is.True);
        }

        [UnityTest]
        public IEnumerator RefreshTarget_OutsideMaxDistance_ClearsTarget()
        {
            _interactor.MaxDistance = 2.5f;
            var prop = SpawnProp(Ahead(3.2f));
            yield return null;
            Assert.That(_interactor.RefreshTarget(), Is.False);
            Assert.That(_interactor.CurrentTarget, Is.Null);
            Assert.That(_interactor.CanInteractWith(prop), Is.False);
            Assert.That(prop.TryInteract(_interactor), Is.False);
            Assert.That(prop.InteractionCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator SolidWallWithoutInteractable_BlocksPropBehind()
        {
            var prop = SpawnProp(Ahead(2.2f), name: "BehindWall");
            SpawnSolidObstacle(Ahead(1.0f), new Vector3(1.2f, 1.2f, 0.2f));
            yield return null;
            Assert.That(_interactor.RefreshTarget(), Is.False);
            Assert.That(_interactor.CurrentTarget, Is.Null);
            Assert.That(prop.TryInteract(_interactor), Is.False);
        }

        [UnityTest]
        public IEnumerator TriggerColliderInFront_IsIgnored_PropRemainsReachable()
        {
            var prop = SpawnProp(Ahead(2.0f), name: "BehindTrigger");
            var trigger = SpawnSolidObstacle(Ahead(0.8f), new Vector3(1f, 1f, 0.2f), "TriggerVolume");
            trigger.GetComponent<Collider>().isTrigger = true;
            Physics.SyncTransforms();
            yield return null;
            Assert.That(_interactor.RefreshTarget(), Is.True);
            Assert.That(_interactor.CurrentTarget, Is.SameAs(prop));
            Assert.That(_interactor.TryInteract(), Is.True);
            Assert.That(prop.InteractionCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator OwnColliderIgnored_AndChildColliderResolvesParentProp()
        {
            var root = Track(new GameObject("PropRoot"));
            root.transform.position = Ahead(1.6f);
            var prop = root.AddComponent<PrototypeInteractable>();
            prop.Configure("ChildColliderProp");
            var child = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            child.name = "PropChildCollider";
            child.transform.SetParent(root.transform, false);
            child.transform.localPosition = Vector3.zero;
            child.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            Object.DestroyImmediate(child.GetComponent<MeshRenderer>());
            Physics.SyncTransforms();
            yield return null;

            Assert.That(_interactor.RefreshTarget(), Is.True);
            Assert.That(_interactor.CurrentTarget, Is.SameAs(prop));
            Assert.That(_interactor.TryInteract(), Is.True);
            Assert.That(prop.IsInspected, Is.True);
        }

        [UnityTest]
        public IEnumerator WithoutLocalOwnership_RefreshClearsAndInteractFails()
        {
            var prop = SpawnProp(Ahead(1.4f));
            yield return null;
            Assert.That(_interactor.RefreshTarget(), Is.True);
            _controller.SetLocalControl(false);
            Assert.That(_interactor.RefreshTarget(), Is.False);
            Assert.That(_interactor.CurrentTarget, Is.Null);
            Assert.That(_interactor.TryInteract(), Is.False);
            Assert.That(prop.TryInteract(_interactor), Is.False);
            Assert.That(prop.InteractionCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator FreeCursor_BlocksTryInteract_ButRefreshTargetStillQueries()
        {
            var prop = SpawnProp(Ahead(1.4f));
            SetCursorCaptured(false);
            Assert.That(_controller.IsCursorCaptured, Is.False);
            yield return null;
            Assert.That(_interactor.RefreshTarget(), Is.True, "RefreshTarget ignores cursor capture.");
            Assert.That(_interactor.CurrentTarget, Is.SameAs(prop));
            Assert.That(_interactor.TryInteract(), Is.False);
            Assert.That(prop.TryInteract(_interactor), Is.False);
            Assert.That(prop.InteractionCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator MissingApplicationFocus_BlocksTryInteract_ButRefreshTargetStillQueries()
        {
            var prop = SpawnProp(Ahead(1.4f));
            _interactor.Focused = false;
            yield return null;
            Assert.That(_interactor.RefreshTarget(), Is.True, "RefreshTarget ignores OS/focus override.");
            Assert.That(_interactor.CurrentTarget, Is.SameAs(prop));
            Assert.That(_interactor.TryInteract(), Is.False);
            Assert.That(prop.InteractionCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator DisabledOrDestroyedProp_ClearsTargetAndRejectsInteract()
        {
            var prop = SpawnProp(Ahead(1.4f), name: "DisableMe");
            yield return null;
            Assert.That(_interactor.RefreshTarget(), Is.True);

            prop.enabled = false;
            Assert.That(_interactor.RefreshTarget(), Is.False);
            Assert.That(_interactor.CurrentTarget, Is.Null);
            prop.enabled = true;

            prop.gameObject.SetActive(false);
            Assert.That(_interactor.RefreshTarget(), Is.False);
            prop.gameObject.SetActive(true);
            Assert.That(_interactor.RefreshTarget(), Is.True);

            Object.DestroyImmediate(prop.gameObject);
            Physics.SyncTransforms();
            Assert.That(_interactor.RefreshTarget(), Is.False);
            Assert.That(_interactor.CurrentTarget, Is.Null);
            Assert.That(_interactor.TryInteract(), Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TargetChangedBeforeInteract_UsesFreshRaycast()
        {
            var first = SpawnProp(Ahead(1.3f), name: "First");
            yield return null;
            Assert.That(_interactor.RefreshTarget(), Is.True);
            Assert.That(_interactor.CurrentTarget, Is.SameAs(first));

            first.transform.position = Ahead(4.0f);
            var second = SpawnProp(Ahead(1.5f), name: "Second");
            Physics.SyncTransforms();
            Assert.That(_interactor.RefreshTarget(), Is.True);
            Assert.That(_interactor.CurrentTarget, Is.SameAs(second));
            Assert.That(_interactor.TryInteract(), Is.True);
            Assert.That(second.InteractionCount, Is.EqualTo(1));
            Assert.That(first.InteractionCount, Is.EqualTo(0));
            yield return null;
        }

        [UnityTest]
        public IEnumerator RepeatedInteract_DoesNotDuplicateInspection()
        {
            var prop = SpawnProp(Ahead(1.4f));
            yield return null;
            Assert.That(_interactor.TryInteract(), Is.True);
            Assert.That(prop.InteractionCount, Is.EqualTo(1));
            Assert.That(prop.IsInspected, Is.True);
            Assert.That(_interactor.TryInteract(), Is.False);
            Assert.That(prop.TryInteract(_interactor), Is.False);
            Assert.That(prop.InteractionCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DisabledInteractor_DoesNotInteract()
        {
            var prop = SpawnProp(Ahead(1.4f));
            yield return null;
            Assert.That(_interactor.RefreshTarget(), Is.True);
            _interactor.enabled = false;
            Assert.That(_interactor.CurrentTarget, Is.Null);
            Assert.That(_interactor.TryInteract(), Is.False);
            Assert.That(prop.TryInteract(_interactor), Is.False);
            Assert.That(prop.InteractionCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator DirectPropTryInteract_CannotBypassRangeOrOwnership()
        {
            var far = SpawnProp(Ahead(3.5f), name: "Far");
            yield return null;
            Assert.That(far.TryInteract(_interactor), Is.False);

            var near = SpawnProp(Ahead(1.2f), name: "Near");
            _controller.SetLocalControl(false);
            Assert.That(near.TryInteract(_interactor), Is.False);
            Assert.That(near.InteractionCount, Is.EqualTo(0));
            Assert.That(far.InteractionCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator InteractKey_PerformsSingleActionPerPress()
        {
            var prop = SpawnProp(Ahead(1.4f));
            yield return null;
            Assert.That(_interactor.RefreshTarget(), Is.True);

            Action beforeUpdate = null;
            beforeUpdate = () =>
            {
                InputSystem.onBeforeUpdate -= beforeUpdate;
                beforeUpdate = null;
                InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.E));
            };

            try
            {
                InputSystem.onBeforeUpdate += beforeUpdate;
                yield return null;
                Assert.That(prop.InteractionCount, Is.EqualTo(1), "E debe inspeccionar una vez por pulsación.");
                Assert.That(prop.IsInspected, Is.True);

                // Key still down another frame: no duplicate.
                yield return null;
                Assert.That(prop.InteractionCount, Is.EqualTo(1));

                InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
                InputSystem.Update();
                yield return null;

                beforeUpdate = () =>
                {
                    InputSystem.onBeforeUpdate -= beforeUpdate;
                    beforeUpdate = null;
                    InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.E));
                };
                InputSystem.onBeforeUpdate += beforeUpdate;
                yield return null;
                Assert.That(prop.InteractionCount, Is.EqualTo(1), "Repetir E sobre prop ya inspeccionado no duplica.");
            }
            finally
            {
                if (beforeUpdate != null)
                {
                    InputSystem.onBeforeUpdate -= beforeUpdate;
                }
            }
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator ExistingPropPrefabs_SupportPrototypeInteractionWithoutRegeneratingAssets()
        {
            Assert.That(PropPrefabPaths.Length, Is.EqualTo(4));
            float lateral = -1.5f;
            var props = new List<PrototypeInteractable>();

            foreach (string path in PropPrefabPaths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.That(prefab, Is.Not.Null, path);
                var instance = Track(Object.Instantiate(prefab, Ahead(1.6f) + _player.transform.right * lateral, Quaternion.identity));
                instance.name = prefab.name + "_TestInstance";
                var interactable = instance.GetComponent<PrototypeInteractable>();
                Assert.That(interactable, Is.Not.Null, "El prefab entregado debe incluir la interacción: " + path);

                interactable.Configure(prefab.name);
                Assert.That(instance.GetComponentInChildren<Collider>(), Is.Not.Null, path);
                props.Add(interactable);
                lateral += 1f;
            }

            Physics.SyncTransforms();
            yield return null;

            for (int selected = 0; selected < props.Count; selected++)
            {
                for (int i = 0; i < props.Count; i++)
                    props[i].transform.position = _player.transform.position + Vector3.forward * 1.6f + Vector3.right * (i == selected ? 0 : 10 + i * 3);
                Physics.SyncTransforms();
                _camera.transform.LookAt(props[selected].GetComponent<Collider>().bounds.center);
                Assert.That(_interactor.RefreshTarget(), Is.True, PropPrefabPaths[selected]);
                Assert.That(_interactor.CurrentTarget, Is.SameAs(props[selected]));
                Assert.That(_interactor.TryInteract(), Is.True);
                Assert.That(_interactor.TryInteract(), Is.False);
                Assert.That(props[selected].InteractionCount, Is.EqualTo(1));
            }
        }
#endif
    }
}
