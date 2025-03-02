using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace UdonPoints.Networking
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class UdonPointsNetworkBehaviour : UdonSharpBehaviour
    {
        public UdonPointsManager Manager;
        public double SyncTolerance = 0.001;

        // TODO: VRChat PlayerObject, if you can hear me, please add decimal support...
        [UdonSynced] [HideInInspector] public string[] MoneyNames;
        [UdonSynced] [HideInInspector] public double[] Money;
        
        [UdonSynced] [HideInInspector] public byte[] RawMoney;

        public decimal[] remoteMoney;
        private double[] lastMoney;
        private VRCPlayerApi lastOwner;

        public VRCPlayerApi GetOwner() => VRC.SDKBase.Networking.GetOwner(gameObject);

        public int GetIndexFromName(string n)
        {
            for (int i = 0; i < MoneyNames.Length; i++)
            {
                string moneyName = MoneyNames[i];
                if(moneyName != n) continue;
                return i;
            }
            return -1;
        }

        public decimal GetRawMoney(int index)
        {
            if (index < 0 || remoteMoney == null || index >= remoteMoney.Length)
                return 0;
            return remoteMoney[index];
        }

        public decimal GetRawMoney(string n) => GetRawMoney(GetIndexFromName(n));

        public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner)
        {
            if (!requestingPlayer.isMaster)
            {
                Manager.Logger.LogError("Could not transfer ownership from " + requestingPlayer.displayName + "(" + requestingPlayer.playerId + ")");
                return false;
            }
            return true;
        }

        public override void OnPlayerJoined(VRCPlayerApi player) => OnMoneyUpdate(true);

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            if(Manager.LocalPlayer == null || !Manager.LocalPlayer.isMaster) return;
            VRCPlayerApi owner = GetOwner();
            // Owner just left the instance
            if(owner == null) return;
            if(GetOwner().playerId != player.playerId) return;
            VRC.SDKBase.Networking.SetOwner(Manager.LocalPlayer, gameObject);
        }

        public override void OnPostSerialization(SerializationResult result)
        {
            if (result.success) return;
            Manager.Logger.LogError("Could not serialize!");
        }

        public override void OnDeserialization(DeserializationResult result)
        {
            if(RawMoney == null || Money == null)
            {
                Manager.Logger.Log("RawMoney or Money is null! Cannot deserialize.");
                return;
            }
            decimal[] rawMoney = RawMoney.ToDecimals();
            if (remoteMoney == null || remoteMoney.Length != MoneyNames.Length)
                remoteMoney = new decimal[MoneyNames.Length];
            Array.Copy(rawMoney, remoteMoney, rawMoney.Length);
        }

        internal void OnMoneyUpdate(bool force = false)
        {
            if(Manager.LocalPlayer.playerId != GetOwner().playerId) return;
            MoneyNames = Manager.GetAllMoneyNames();
            bool requestUpdate = false;
            if (Money == null || Money.Length != MoneyNames.Length)
            {
                Money = new double[MoneyNames.Length];
                requestUpdate = true;
            }
            decimal[] localMoney = new decimal[MoneyNames.Length];
            // Update all values
            for (int i = 0; i < MoneyNames.Length; i++)
            {
                UdonPointsBehaviour behaviour = Manager.GetBehaviourFromName(MoneyNames[i]);
                decimal money = Manager.GetMoney(behaviour);
                double moneyDouble = money.MoneyToDouble();
                Money[i] = moneyDouble;
                localMoney[i] = money;
                if (lastMoney != null && Math.Abs(moneyDouble - lastMoney[i]) > SyncTolerance) requestUpdate = true;
            }
            if (lastMoney == null)
            {
                lastMoney = new double[Money.Length];
                requestUpdate = true;
            }
            Array.Copy(Money, lastMoney, Money.Length);
            if(!requestUpdate && !force) return;
            RawMoney = localMoney.ToBytes();
            RequestSerialization();
        }

        private void LateUpdate()
        {
            VRCPlayerApi currentOwner = GetOwner();
            if(lastOwner != currentOwner) OnMoneyUpdate(true);
            lastOwner = currentOwner;
        }
    }
}
