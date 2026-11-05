using System.Collections;
using NUnit.Framework;
using OperationHeist.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

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
        }

        private static float Pitch(Quaternion rotation)
        {
            return rotation.eulerAngles.x > 180f ? rotation.eulerAngles.x - 360f : rotation.eulerAngles.x;
        }
    }
}
