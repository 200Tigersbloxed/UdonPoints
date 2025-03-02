using System;
using UdonPoints.Networking;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using Logger = UdonPoints.Logging.Logger;

namespace UdonPoints
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class UdonPointsManager : UdonSharpBehaviour
    {
        public const string VERSION = "v1.0.0";
        
        public UdonPointsBehaviour[] UdonPointsBehaviours;
        public UdonPointsNetworkBehaviour[] UdonPointsNetworkBehaviours;
        [HideInInspector] public Logger _Logger;

        public Logger Logger
        {
            get
            {
                EnsureLogger();
                return _Logger;
            }
        }
        public VRCPlayerApi[] PlayerList = Array.Empty<VRCPlayerApi>();
        public VRCPlayerApi LocalPlayer;
        
        private string[] moneyNames = new string[0];

        public string[] GetAllMoneyNames()
        {
            if (moneyNames.Length == UdonPointsBehaviours.Length) return moneyNames;
            moneyNames = new string[UdonPointsBehaviours.Length];
            for (int i = 0; i < moneyNames.Length; i++)
                moneyNames[i] = UdonPointsBehaviours[i].MoneySafeName;
            return moneyNames;
        }

        public UdonPointsBehaviour GetBehaviourFromName(string moneyName)
        {
            foreach (UdonPointsBehaviour udonPointsBehaviour in UdonPointsBehaviours)
            {
                if(udonPointsBehaviour.MoneySafeName != moneyName) continue;
                return udonPointsBehaviour;
            }
            return null;
        }
        
        private UdonPointsNetworkBehaviour GetNetworkedMoney(VRCPlayerApi player)
        {
            foreach (UdonPointsNetworkBehaviour networkBehaviour in UdonPointsNetworkBehaviours)
            {
                if(networkBehaviour.GetOwner().playerId != player.playerId) continue;
                return networkBehaviour;
            }
            return null;
        }
        
        public decimal GetMoney(UdonPointsBehaviour pointsBehaviour) => pointsBehaviour.CurrentMoney;
        public decimal GetMoney(VRCPlayerApi player, UdonPointsBehaviour pointsBehaviour, bool raw = true)
        {
            if (player.isLocal)
                return pointsBehaviour.CurrentMoney;
            UdonPointsNetworkBehaviour networkBehaviour = GetNetworkedMoney(player);
            if (networkBehaviour == null) return 0;
            if (networkBehaviour.MoneyNames == null || networkBehaviour.Money == null) return 0;
            if (networkBehaviour.MoneyNames.Length != networkBehaviour.Money.Length) return 0;
            for (int i = 0; i < networkBehaviour.MoneyNames.Length; i++)
            {
                string moneyName = networkBehaviour.MoneyNames[i];
                if(moneyName != pointsBehaviour.MoneySafeName) continue;
                return raw ? networkBehaviour.GetRawMoney(i) : networkBehaviour.Money[i].DoubleToMoney();
            }
            return 0;
        }

        public void EffectMoney(MoneyAction action, decimal amount, params UdonPointsBehaviour[] behaviours)
        {
            amount = amount.ClampToMoneyRange();
            foreach (UdonPointsBehaviour behaviour in behaviours)
            {
                switch (action)
                {
                    case MoneyAction.Add:
                        behaviour.CurrentMoney = NumericExtensions.SafeAdd(behaviour.CurrentMoney, amount);
                        break;
                    case MoneyAction.Subtract:
                        behaviour.CurrentMoney = NumericExtensions.SafeSubtract(behaviour.CurrentMoney, amount);
                        break;
                    case MoneyAction.Multiply:
                        behaviour.CurrentMoney = NumericExtensions.SafeMultiply(behaviour.CurrentMoney, amount);
                        break;
                    case MoneyAction.Divide:
                        // Cannot divide by zero
                        if (amount == 0) break;
                        behaviour.CurrentMoney = NumericExtensions.SafeDivide(behaviour.CurrentMoney, amount);
                        break;
                    case MoneyAction.Set:
                        behaviour.CurrentMoney = amount;
                        break;
                }
            }
        }
        public void EffectMoney(MoneyAction action, double amount, params UdonPointsBehaviour[] behaviours) =>
            EffectMoney(action, amount.DoubleToMoney(), behaviours);
        public void EffectMoney(MoneyAction action, float amount, params UdonPointsBehaviour[] behaviours) =>
            EffectMoney(action, amount.FloatToMoney(), behaviours);
        public void EffectMoney(MoneyAction action, decimal amount) => EffectMoney(action, amount, UdonPointsBehaviours);
        public void EffectMoney(MoneyAction action, double amount) => EffectMoney(action, amount.DoubleToMoney(), UdonPointsBehaviours);
        public void EffectMoney(MoneyAction action, float amount) => EffectMoney(action, amount.FloatToMoney(), UdonPointsBehaviours);
        
        private void EnsureLogger()
        {
            if (_Logger == null) return;
            _Logger.Manager = this;
        }
        
        private VRCPlayerApi[] PlayersWithoutNetworking(VRCPlayerApi[] allPlayers, int length = -1)
        {
            int i = 0;
            VRCPlayerApi[] players = null;
            if (length > -1) players = new VRCPlayerApi[length];
            foreach (VRCPlayerApi vrcPlayerApi in allPlayers)
            {
                bool ownsOne = false;
                foreach (UdonPointsNetworkBehaviour udonPointsNetworkBehaviour in UdonPointsNetworkBehaviours)
                {
                    if(udonPointsNetworkBehaviour.GetOwner().playerId != vrcPlayerApi.playerId) continue;
                    ownsOne = true;
                    break;
                }
                if(ownsOne) continue;
                if(players != null)
                    players[i] = vrcPlayerApi;
                i++;
            }
            if (length < 0) return PlayersWithoutNetworking(allPlayers, i);
            return players;
        }

        private UdonPointsNetworkBehaviour GetFirstUnowned()
        {
            bool masterHasOne = false;
            foreach (UdonPointsNetworkBehaviour udonPointsNetworkBehaviour in UdonPointsNetworkBehaviours)
            {
                VRCPlayerApi owner = udonPointsNetworkBehaviour.GetOwner();
                if(!owner.isMaster) continue;
                if (!masterHasOne)
                {
                    masterHasOne = true;
                    continue;
                }
                return udonPointsNetworkBehaviour;
            }
            Logger.LogError("No valid UdonPointsNetworkBehaviour found to claim! Are there enough GameObjects?");
            return null;
        }

        private void NetworkUpdate(VRCPlayerApi localMasterPlayer, VRCPlayerApi[] allPlayers)
        {
            if(!localMasterPlayer.isLocal) return;
            VRCPlayerApi[] playersWithoutNetworking = PlayersWithoutNetworking(allPlayers);
            foreach (VRCPlayerApi playerNoNetwork in playersWithoutNetworking)
            {
                // Find an object and assign it
                UdonPointsNetworkBehaviour networkBehaviour = GetFirstUnowned();
                if (networkBehaviour == null)
                {
                    Logger.LogError("Could not get NetworkBehaviour for player! Are there enough NetworkBehaviours?");
                    continue;
                }
                networkBehaviour.MoneyNames = null;
                networkBehaviour.Money = null;
                networkBehaviour.RawMoney = null;
                VRC.SDKBase.Networking.SetOwner(playerNoNetwork, networkBehaviour.gameObject);
                Logger.Log($"Assigned NetworkBehaviour ({networkBehaviour.name}) to {playerNoNetwork.displayName} ({playerNoNetwork.playerId})");
            }
        }

        private void Start() => LocalPlayer = VRC.SDKBase.Networking.LocalPlayer;

        private void Update()
        {
            EnsureLogger();
            int playerCount = VRCPlayerApi.GetPlayerCount();
            if(playerCount != PlayerList.Length)
            {
                PlayerList = new VRCPlayerApi[playerCount];
                VRCPlayerApi.GetPlayers(PlayerList);
                Logger.Log("Refresh PlayerList.");
                // You have to be the local player and master for this
                if(!LocalPlayer.isMaster || !LocalPlayer.isLocal) return;
                NetworkUpdate(LocalPlayer, PlayerList);
            }
        }
    }
}