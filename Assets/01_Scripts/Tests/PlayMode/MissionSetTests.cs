using System.Collections;
using System.Reflection;
using NUnit.Framework;
using OperationHeist.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace OperationHeist.Tests
{
    public class MissionSetTests
    {
        [Test]
        public void WalkCycleSwingsHipsInOppositeDirections()
        {
            var root = new GameObject("OH_Guard");
            var left = new GameObject("LeftHip").transform;
            var right = new GameObject("RightHip").transform;
            left.SetParent(root.transform, false);
            right.SetParent(root.transform, false);
            var cycle = root.AddComponent<RigidCharacterCycle>();
            cycle.Bind();
            cycle.Step(1.4f, 0.35f);
            float leftPitch = Pitch(left.localRotation);
            float rightPitch = Pitch(right.localRotation);
            Assert.That(leftPitch * rightPitch, Is.LessThan(0f), "Las caderas deben oponerse al caminar");
            Object.DestroyImmediate(root);
        }

        [UnityTest]
        public IEnumerator DepotUsesProductionGuardAndDossiers()
        {
            yield return SceneManager.LoadSceneAsync("Assets/00_Scenes/Heist_Depot.unity");
            yield return null;
            var guard = Object.FindAnyObjectByType<PatrolGuard>();
            Assert.That(guard.GetComponentInChildren<RigidCharacterCycle>(true), Is.Not.Null);
            Assert.That(GameObject.Find("Kit de produccion"), Is.Not.Null);
            foreach (var loot in Object.FindObjectsByType<LootItem>())
                Assert.That(loot.transform.Find("OH_Dossier"), Is.Not.Null, loot.name);
        }

        [UnityTest]
        public IEnumerator BankSetContainsTellerClerkAndWalkableFloor()
        {
            yield return SceneManager.LoadSceneAsync("Assets/00_Scenes/Heist_Bank.unity");
            yield return null;
            Assert.That(GameObject.Find("OH_TellerCounter"), Is.Not.Null);
            Assert.That(GameObject.Find("OH_BankClerk"), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<RigidCharacterCycle>(), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<FpsController>(), Is.Not.Null);
            Assert.That(GameObject.Find("OH_FloorTile"), Is.Not.Null);
            var bag = GameObject.Find("OH_LootBag");
            Assert.That(bag, Is.Not.Null);
            Assert.That(bag.transform.Find("Modelo Hunyuan").GetComponentInChildren<MeshRenderer>(), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator BankContractOpensVaultAndExtractsThroughRealScene()
        {
            yield return SceneManager.LoadSceneAsync("Heist_Depot");
            yield return null;
            GameObject.Find("OtherContract").GetComponent<Button>().onClick.Invoke();
            yield return null;
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Heist_Bank"));
            var mission = Object.FindAnyObjectByType<LocalMission>();
            Assert.That(mission.Phase, Is.EqualTo(MissionPhase.Briefing));
            Assert.That(mission.Player.GameplayInputEnabled, Is.False);
            var start = GameObject.Find("PrimaryAction").GetComponent<Button>();
            var spawn = mission.Player.transform.position;
            var door = Object.FindAnyObjectByType<BankVaultAccess>();
            var loot = Object.FindAnyObjectByType<LootItem>(FindObjectsInactive.Include);
            Assert.That(loot.gameObject.activeSelf, Is.False);
            Object.FindAnyObjectByType<PatrolGuard>().enabled = false;

            // Substitute OS focus only. Use the scene's geometry and normal raycast interaction.
            var agent = new GameObject("Agente de prueba");
            agent.SetActive(false);
            agent.transform.position = spawn;
            var capsule = agent.AddComponent<CharacterController>();
            capsule.height = 1.8f;
            capsule.radius = .3f;
            capsule.center = Vector3.up * .9f;
            var camera = new GameObject("Cámara", typeof(Camera)).transform;
            camera.SetParent(agent.transform, false);
            camera.localPosition = Vector3.up * 1.65f;
            var motor = agent.AddComponent<FpsController>();
            var actor = agent.AddComponent<MissionTestInteractor>();
            var carrier = agent.AddComponent<LootCarrier>();
            agent.SetActive(true);
            start.onClick.Invoke();
            Assert.That(mission.Phase, Is.EqualTo(MissionPhase.Playing));
            Assert.That(mission.Player.GameplayInputEnabled, Is.True);
            mission.Player.gameObject.SetActive(false);
            mission.Configure(motor, carrier, 240f, 1);
            typeof(FpsController).GetMethod("SetCursorLocked", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(motor, new object[] { true });
            Assert.That(mission.Objectives.RequiredCount, Is.EqualTo(6));

            var terminal = Object.FindAnyObjectByType<ExtractionPoint>();
            Aim(camera, terminal.transform.position + Vector3.up * 1.4f);
            Assert.That(actor.TryInteract(), Is.False, "Evacuación bloqueada antes del objetivo");
            Move(capsule, new Vector3(5.55f, spawn.y, -4.5f));
            Move(capsule, new Vector3(5.55f, spawn.y, .85f));
            var security = Object.FindAnyObjectByType<BankSecurity>();
            var switches = Object.FindObjectsByType<BankSecuritySwitch>();
            Assert.That(security.Select(4, out _), Is.False, "El panel de cámaras exige acceso");
            SelectSwitch(actor, camera, switches, 0);
            Assert.That(mission.Alarm, Is.GreaterThan(0f), "El cable incorrecto eleva la alarma");
            foreach (int channel in new[] { 1, 3, 0, 2 }) SelectSwitch(actor, camera, switches, channel);
            Assert.That(mission.Objectives.IsComplete("security-access"), Is.True);
            Physics.SyncTransforms();
            Move(capsule, new Vector3(5.55f, spawn.y, 3.65f));
            Aim(camera, door.transform.position + Vector3.up * 1.3f);
            Assert.That(security.Detects(agent.transform, camera.position, false), Is.True, "Cámara activa y con visión");
            Assert.That(actor.TryInteract(), Is.False, "La bóveda exige desconectar cámaras");
            SelectSwitch(actor, camera, switches, 4);
            Assert.That(security.CamerasOffline, Is.False);
            SelectSwitch(actor, camera, switches, 5);
            Assert.That(mission.Objectives.IsComplete("security-cameras"), Is.True);
            Assert.That(security.Detects(agent.transform, camera.position, false), Is.False, "La cámara deja de detectar");
            capsule.Move(Vector3.forward * 1.5f);
            Assert.That(agent.transform.position.z, Is.LessThan(4.65f), "La puerta cerrada bloquea al agente");
            Move(capsule, new Vector3(5.55f, spawn.y, 3.65f));
            Aim(camera, door.transform.position + Vector3.up * 1.3f);
            Assert.That(actor.TryInteract(), Is.True, "Apertura por raycast");
            Assert.That(mission.Objectives.IsComplete("vault-access"), Is.True);
            Assert.That(loot.gameObject.activeSelf, Is.True);
            Assert.That(loot.transform.Find("Modelo Hunyuan").GetComponentInChildren<MeshRenderer>(), Is.Not.Null);
            Physics.SyncTransforms();
            Move(capsule, new Vector3(5.55f, spawn.y, 5.75f));
            Aim(camera, loot.GetComponent<Collider>().bounds.center);
            Assert.That(actor.TryInteract(), Is.True, "Recogida en la bóveda");
            Move(capsule, new Vector3(5.55f, spawn.y, -4.5f));
            Move(capsule, spawn);
            Aim(camera, terminal.transform.position + Vector3.up * 1.4f);
            Assert.That(actor.TryInteract(), Is.True, "Entrega en el terminal");
            Assert.That(actor.TryInteract(), Is.True, "Evacuación");
            Assert.That(mission.Phase, Is.EqualTo(MissionPhase.Won));
            Assert.That(mission.Objectives.RequiredComplete, Is.True);
        }

        [UnityTest]
        public IEnumerator BankTimeoutAndReturnToDepot()
        {
            yield return SceneManager.LoadSceneAsync("Heist_Bank");
            yield return null;
            GameObject.Find("PrimaryAction").GetComponent<Button>().onClick.Invoke();
            var mission = Object.FindAnyObjectByType<LocalMission>();
            mission.Tick(mission.RemainingSeconds);
            Assert.That(mission.Phase, Is.EqualTo(MissionPhase.Lost));
            GameObject.Find("OtherContract").GetComponent<Button>().onClick.Invoke();
            yield return null;
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Heist_Depot"));
            Assert.That(GameObject.Find("Coop"), Is.Not.Null);
        }

        private static void Move(CharacterController capsule, Vector3 destination)
        {
            Vector3 displacement = destination - capsule.transform.position;
            capsule.Move(displacement);
            Assert.That(Vector3.Distance(capsule.transform.position, destination), Is.LessThan(.15f),
                "Ruta bloqueada hacia " + destination);
        }

        private static void Aim(Transform camera, Vector3 target)
        {
            camera.LookAt(target);
            Physics.SyncTransforms();
        }

        private static void SelectSwitch(PlayerInteractor actor, Transform camera, BankSecuritySwitch[] switches, int channel)
        {
            var control = System.Array.Find(switches, item => item.Channel == channel);
            Aim(camera, control.GetComponent<Collider>().bounds.center);
            Assert.That(actor.TryInteract(), Is.True, "Control del panel " + channel);
        }

        private static float Pitch(Quaternion rotation)
        {
            return rotation.eulerAngles.x > 180f ? rotation.eulerAngles.x - 360f : rotation.eulerAngles.x;
        }
    }
}
