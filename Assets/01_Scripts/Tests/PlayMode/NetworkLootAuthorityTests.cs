using NUnit.Framework;
using OperationHeist.Network;

namespace OperationHeist.Tests
{
    /// <summary>
    /// Host assignment rules without spinning NetworkManager (NGO host bootstrap is validated in Heist_NetworkLab playtest).
    /// </summary>
    public class NetworkLootAuthorityTests
    {
        [Test]
        public void AvailableLootCanBeAssignedToFreeCarrier()
        {
            Assert.That(NetworkLootRules.CanAssign(NetworkLootState.Available, carrierAlreadyBusy: false), Is.True);
        }

        [Test]
        public void CarriedLootRejectsSecondAssignment()
        {
            Assert.That(NetworkLootRules.CanAssign(NetworkLootState.Carried, carrierAlreadyBusy: false), Is.False);
            Assert.That(NetworkLootRules.CanAssign(NetworkLootState.Deposited, carrierAlreadyBusy: false), Is.False);
        }

        [Test]
        public void BusyCarrierCannotTakeAnotherAvailableLoot()
        {
            Assert.That(NetworkLootRules.CanAssign(NetworkLootState.Available, carrierAlreadyBusy: true), Is.False);
        }
    }
}
