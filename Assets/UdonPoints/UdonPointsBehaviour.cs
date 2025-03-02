using UCS;
using UdonPoints.Networking;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Persistence;
using VRC.SDKBase;

namespace UdonPoints
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class UdonPointsBehaviour : UdonSharpBehaviour
    {
        private const int READY_COUNT = 3;

        public UdonPointsManager Manager;
        public string MoneySafeName = "points";
        public float DefaultMoney;
        public bool DisablePersistence;

        internal decimal CurrentMoney;
        internal bool IsReady => ready >= READY_COUNT;
        internal bool HasPlayerData { get; private set; }

        private int ready;
#if REVERSE_UC
        private UdonChips chipsObject;
#endif
        private decimal lastMoney;

        public void ResetMoney() => CurrentMoney = DefaultMoney.FloatToMoney();

        private void NetworkCheck()
        {
            if(CurrentMoney == lastMoney) return;
            // TODO: VRChat Persistence, if you can hear me, please add decimal support...
            if(!DisablePersistence)
                PlayerData.SetBytes(MoneySafeName, CurrentMoney.ToBytes());
            lastMoney = CurrentMoney;
            Manager.Logger.Log($"Updated money to {CurrentMoney}");
            foreach (UdonPointsNetworkBehaviour networkBehaviour in Manager.UdonPointsNetworkBehaviours)
            {
                if(networkBehaviour.GetOwner() != Manager.LocalPlayer) continue;
                networkBehaviour.OnMoneyUpdate();
            }
        }

        public override void OnPlayerRestored(VRCPlayerApi player)
        {
            if(IsReady || !player.isLocal || DisablePersistence) return;
            HasPlayerData = PlayerData.HasKey(player, MoneySafeName);
            if (HasPlayerData)
            {
                decimal m = PlayerData.GetBytes(player, MoneySafeName).ToDecimal();
                CurrentMoney = m;
                lastMoney = m;
            }
            ready++;
            Manager.Logger.Log($"Finished Stage {ready} (OnPlayerRestored) Initialization.");
        }

        // BUG: Using Awake does not store variables correctly (ClientSim)
        private void Start()
        {
            // Sanity Check
            if(IsReady) return;
#if REVERSE_UC
            GameObject c = GameObject.Find("UdonChips");
            if (c != null)
            {
                chipsObject = c.GetComponent<UdonChips>();
                chipsObject.money = 0;
            }
#endif
            if (string.IsNullOrEmpty(MoneySafeName))
            {
                Manager.Logger.LogError("MoneySafeName is invalid! Please set this to something unique!");
                return;
            }
            ready++;
            Manager.Logger.Log($"Finished Stage {ready} (Start) Initialization.");
            if (DisablePersistence)
            {
                ready++;
                Manager.Logger.Log($"Skipped Stage {ready} of Initialization.");
            }
        }

        private void Update()
        {
            if (ready == 2)
            {
                if(!HasPlayerData) ResetMoney();
                ready++;
                Manager.Logger.Log($"Finished Stage {ready} (Update) Initialization.");
                Manager.Logger.Log(HasPlayerData
                    ? $"UdonPointsBehaviour {MoneySafeName} is ready and loaded {CurrentMoney} money!"
                    : $"UdonPointsBehaviour {MoneySafeName} is ready!");
                foreach (UdonPointsNetworkBehaviour networkBehaviour in Manager.UdonPointsNetworkBehaviours)
                {
                    if(networkBehaviour.GetOwner() != Manager.LocalPlayer) continue;
                    networkBehaviour.OnMoneyUpdate(true);
                }
            }
            if(!IsReady) return;
            NetworkCheck();
        }
    }
}