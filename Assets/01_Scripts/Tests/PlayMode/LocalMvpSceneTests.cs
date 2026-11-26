using System.Collections;
using NUnit.Framework;
using OperationHeist.Player;
using OperationHeist.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace OperationHeist.Tests
{
    public class LocalMvpSceneTests
    {
        [UnityTest]
        public IEnumerator GuardRaisesAlarmOnlyWithClearLineOfSight()
        {
            yield return SceneManager.LoadSceneAsync("Assets/00_Scenes/Heist_Depot.unity");
            yield return null;
            var mission=Object.FindFirstObjectByType<LocalMission>();
            var guard=Object.FindFirstObjectByType<PatrolGuard>();guard.enabled=false;
            mission.StartMission();mission.Player.enabled=false;
            var cc=mission.Player.GetComponent<CharacterController>();cc.enabled=false;
            mission.Player.transform.position=guard.transform.position+Vector3.forward*4;cc.enabled=true;
            Physics.SyncTransforms();
            var targets = new[] { new GuardTarget(mission.Player.transform, mission.Player.PlayerCamera.transform.position,
                false, false, mission.Health) };
            if (guard.TickSecurity(.1f, targets, 0)) mission.RaiseAlarm(.0195f);
            Assert.That(mission.Alarm,Is.GreaterThan(0));
            var wall=new GameObject("Vision blocker",typeof(BoxCollider));wall.transform.position=guard.transform.position+new Vector3(0,1.5f,2);wall.transform.localScale=new Vector3(3,3,.3f);
            Physics.SyncTransforms();float before=mission.Alarm;
            Assert.That(guard.TickSecurity(.1f, targets, 0), Is.False);
            Assert.That(mission.Alarm,Is.EqualTo(before),"Un muro sólido debe bloquear detección");
            Object.Destroy(wall);
        }

        [UnityTest]
        public IEnumerator DepotRealLootCanAllBeDeliveredAndExtracted()
        {
            yield return SceneManager.LoadSceneAsync("Assets/00_Scenes/Heist_Depot.unity");
            yield return null;
            var mission=Object.FindFirstObjectByType<LocalMission>();
            mission.Player.gameObject.SetActive(false);
            Object.FindFirstObjectByType<PatrolGuard>().enabled=false;
            // Replace only OS-focus-dependent input for this deterministic scene integration.
            var player=new GameObject("SceneIntegrationPlayer"); player.SetActive(false);
            var cc=player.AddComponent<CharacterController>(); cc.height=1.8f;cc.radius=.3f;cc.center=new Vector3(0,.9f,0);
            var view=new GameObject("View",typeof(Camera));view.transform.SetParent(player.transform,false);view.transform.localPosition=new Vector3(0,1.65f,0);
            var motor=player.AddComponent<FpsController>();
            var actor=player.AddComponent<MissionTestInteractor>();
            var carrier=player.AddComponent<LootCarrier>();player.SetActive(true);
            mission.Configure(motor,carrier,300,3); mission.StartMission();
            typeof(FpsController).GetMethod("SetCursorLocked",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(motor,new object[]{true});
            var point=Object.FindFirstObjectByType<ExtractionPoint>();
            foreach(var loot in Object.FindObjectsByType<LootItem>(FindObjectsSortMode.None))
            {
                cc.enabled=false;player.transform.position=loot.transform.position+Vector3.back*1.3f;cc.enabled=true;
                view.transform.LookAt(loot.GetComponent<Collider>().bounds.center);Physics.SyncTransforms();
                Assert.That(actor.TryInteract(),Is.True,"Recogida del recurso real: "+loot.name);
                Assert.That(carrier.CarriedItem,Is.SameAs(loot));
                cc.enabled=false;player.transform.position=new Vector3(0,.05f,-12.4f);cc.enabled=true;
                view.transform.LookAt(point.GetComponent<Collider>().bounds.center);Physics.SyncTransforms();
                Assert.That(actor.TryInteract(),Is.True,"Depósito del recurso real: "+loot.name);
                Assert.That(carrier.IsCarrying,Is.False);
                Assert.That(loot.IsDeposited,Is.True);
            }
            Assert.That(mission.DepositedCount,Is.EqualTo(3));
            Assert.That(actor.TryInteract(),Is.True);
            Assert.That(mission.Phase,Is.EqualTo(MissionPhase.Won));
            Assert.That(mission.Player.GameplayInputEnabled,Is.False);
            Object.Destroy(player);
        }

        [UnityTest]
        public IEnumerator DepotBootsPausesFailsAndRestartsWithRealUi()
        {
            yield return SceneManager.LoadSceneAsync("Assets/00_Scenes/Heist_Depot.unity");
            yield return null;
            var mission=Object.FindFirstObjectByType<LocalMission>();
            Assert.That(mission,Is.Not.Null);
            Assert.That(mission.Player.PlayerCamera.enabled,Is.True);
            Assert.That(mission.Phase,Is.EqualTo(MissionPhase.Briefing));
            Assert.That(mission.Player.GameplayInputEnabled,Is.False);
            Assert.That(Object.FindObjectsByType<LootItem>(FindObjectsSortMode.None).Length,Is.EqualTo(3));
            Assert.That(Object.FindFirstObjectByType<ExtractionPoint>(),Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<PatrolGuard>(),Is.Not.Null);
            var ui=Object.FindFirstObjectByType<LocalMissionPresenter>();
            Assert.That(ui,Is.Not.Null);
            var button=System.Array.Find(ui.GetComponentsInChildren<Button>(true),b=>b.name=="PrimaryAction");
            Assert.That(button,Is.Not.Null);
            button.onClick.Invoke();
            Assert.That(mission.Phase,Is.EqualTo(MissionPhase.Playing));
            mission.TogglePause();
            float seconds=mission.RemainingSeconds;
            var guard=Object.FindFirstObjectByType<PatrolGuard>();
            Vector3 position=guard.transform.position;
            yield return null; yield return null;
            Assert.That(mission.RemainingSeconds,Is.EqualTo(seconds));
            Assert.That(guard.transform.position,Is.EqualTo(position));
            Assert.That(mission.Player.PlayerCamera.enabled,Is.True);
            button.onClick.Invoke();
            Assert.That(mission.Phase,Is.EqualTo(MissionPhase.Playing));
            mission.Tick(301);
            Assert.That(mission.Phase,Is.EqualTo(MissionPhase.Lost));
            Assert.That(button.gameObject.activeInHierarchy,Is.True);
            button.onClick.Invoke();
            yield return null; yield return null;
            mission=Object.FindFirstObjectByType<LocalMission>();
            Assert.That(mission.Phase,Is.EqualTo(MissionPhase.Briefing));
            Assert.That(mission.DepositedCount,Is.Zero);
            Assert.That(mission.Alarm,Is.Zero);
            Assert.That(Object.FindObjectsByType<LootItem>(FindObjectsSortMode.None).Length,Is.EqualTo(3));
        }
    }
}
