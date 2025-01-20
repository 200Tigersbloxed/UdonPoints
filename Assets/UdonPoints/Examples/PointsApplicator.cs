using UdonSharp;
using UnityEngine;
using VRC.SDK3.Persistence;
using VRC.SDKBase;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;

namespace UdonPoints.Examples
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class PointsApplicator : UdonSharpBehaviour
    {
        public ApplicationEvent ApplicationEvent = ApplicationEvent.Trigger;
        public MoneyAction ApplicationAction;
        public float AmountToApply;
        public float TimeApplication;
        
        public UdonPointsManager Manager;
        public UdonPointsBehaviour[] TargetBehaviours;
        
        // Features
        public bool DenyIfNotEnough;
        public bool HideAfterCollect;
        public bool ShowAfterCollect;
        public GameObject[] ObjectsToHide = new GameObject[0];
        public GameObject[] ObjectsToShow = new GameObject[0];
        public float ReappearTime = 10f;
        public bool NetworkFeatures;
        
        public bool Persistence;
        public string PersistenceGUID;
        
        // Touch
        public bool ExcludeOwner;
        public float CoolDown = 5f;

        [UdonSynced] [HideInInspector] public bool isHidden;
        private bool inCooldown;
        private bool isRequestingControl;
        private bool sentTimer;
        private bool hasPickup;
        private bool firstJoinSerialize;
        private bool first = true;

        internal void CallOnMoney()
        {
            if(HideAfterCollect)
            {
                if (NetworkFeatures)
                {
                    VRCPlayerApi localPlayer = VRC.SDKBase.Networking.LocalPlayer;
                    VRCPlayerApi owner = VRC.SDKBase.Networking.GetOwner(gameObject);
                    if (localPlayer.playerId == owner.playerId)
                    {
                        // We own the object, send the message
                        SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Hide));
                        if(ReappearTime > 0)
                        {
                            // And also send the reappear message after a bit
                            StartNetworkTimer();
                        }
                    }
                    else
                    {
                        // We need ownership, take it
                        isRequestingControl = true;
                        VRC.SDKBase.Networking.SetOwner(localPlayer, gameObject);
                    }
                }
                else
                    Hide();
            }
            else if (ShowAfterCollect)
            {
                if (NetworkFeatures)
                {
                    VRCPlayerApi localPlayer = VRC.SDKBase.Networking.LocalPlayer;
                    VRCPlayerApi owner = VRC.SDKBase.Networking.GetOwner(gameObject);
                    if (localPlayer.playerId == owner.playerId)
                    {
                        // We own the object, send the message
                        SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Show));
                        if(ReappearTime > 0)
                        {
                            // And also send the reappear message after a bit
                            StartNetworkTimer();
                        }
                    }
                    else
                    {
                        // We need ownership, take it
                        isRequestingControl = true;
                        VRC.SDKBase.Networking.SetOwner(localPlayer, gameObject);
                    }
                }
                else
                    Show();
            }
        }

        public bool IsEnough()
        {
            if (!DenyIfNotEnough) return true;
            bool enough = true;
            foreach (UdonPointsBehaviour udonPointsBehaviour in TargetBehaviours)
            {
                if(Manager.GetMoney(udonPointsBehaviour) >= AmountToApply.FloatToMoney()) continue;
                enough = false;
            }
            return enough;
        }

        private void LoadData()
        {
            if(NetworkFeatures || !Persistence || string.IsNullOrEmpty(PersistenceGUID)) return;
            VRCPlayerApi localPlayer = VRC.SDKBase.Networking.LocalPlayer;
            bool hasData = PlayerData.HasKey(localPlayer, PersistenceGUID);
            if(!hasData) return;
            bool hidden = PlayerData.GetBool(localPlayer, PersistenceGUID);
            if (hidden)
                Hide();
            else
                Show();
            Manager.Logger.Log("Loaded PointsApplicator Data for " + PersistenceGUID);
        }

        private void SaveData()
        {
            if(NetworkFeatures || !Persistence || string.IsNullOrEmpty(PersistenceGUID)) return;
            PlayerData.SetBool(PersistenceGUID, isHidden);
            Manager.Logger.Log("Saved PointsApplicator Data for " + PersistenceGUID);
        }

        public void _Fire()
        {
            if(!IsEnough()) return;
            if(isHidden) return;
            if(ApplicationEvent != ApplicationEvent.Scripted) return;
            Manager.EffectMoney(ApplicationAction, AmountToApply, TargetBehaviours);
            CallOnMoney();
        }

        private void HandleTouchCollide(VRCPlayerApi player)
        {
            if(!IsEnough()) return;
            if(isHidden) return;
            if(inCooldown || !player.isLocal) return;
            if (ExcludeOwner && player.playerId == VRC.SDKBase.Networking.GetOwner(gameObject).playerId) return;
            Manager.EffectMoney(ApplicationAction, AmountToApply, TargetBehaviours);
            inCooldown = true;
            SendCustomEventDelayedSeconds(nameof(_EndCooldown), CoolDown < 0f ? 5f : CoolDown);
            CallOnMoney();
        }

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (ApplicationEvent != ApplicationEvent.Trigger) return;
            HandleTouchCollide(player);
        }

        public override void OnPlayerCollisionEnter(VRCPlayerApi player)
        {
            if (ApplicationEvent != ApplicationEvent.Collider) return;
            HandleTouchCollide(player);
        }

        public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner) =>
            NetworkFeatures || hasPickup;

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            if (!player.isLocal || !isRequestingControl) return;
            // We are now owner, lets hide it
            if(HideAfterCollect)
                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Hide));
            else if(ShowAfterCollect)
                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Show));
            if(ReappearTime > 0)
            {
                // And also send the reappear message after a bit
                StartNetworkTimer();
            }
            isRequestingControl = false;
        }
        
        public override void OnPlayerRestored(VRCPlayerApi player)
        {
            if(!player.isLocal) return;
            LoadData();
        }

        internal virtual void UpdateState(bool state)
        {
            if(HideAfterCollect)
            {
                foreach (GameObject o in ObjectsToHide)
                    o.SetActive(state);
            }
            else if (ShowAfterCollect)
            {
                foreach (GameObject o in ObjectsToHide)
                    o.SetActive(!state);
                foreach (GameObject o in ObjectsToShow)
                    o.SetActive(state);
            }
        }

        public override void OnDeserialization(DeserializationResult result)
        {
            if(firstJoinSerialize) return;
            if(HideAfterCollect)
                UpdateState(!isHidden);
            else if(ShowAfterCollect)
                UpdateState(isHidden);
            if(sentTimer || ReappearTime <= 0)
            {
                firstJoinSerialize = true;
                return;
            }
            if((HideAfterCollect || ShowAfterCollect) && isHidden)
            {
                Manager.Logger.Log("Started late timer for points applicator " + gameObject.name);
                StartNetworkTimer();
            }
            firstJoinSerialize = true;
        }

        public void _EndCooldown() => inCooldown = false;

        public void _TimedEvent()
        {
            if (!first && ApplicationEvent == ApplicationEvent.Timed)
            {
                Manager.EffectMoney(ApplicationAction, AmountToApply, TargetBehaviours);
                CallOnMoney();
            }
            else if (first)
                first = false;
            SendCustomEventDelayedSeconds(nameof(_TimedEvent), TimeApplication);
        }

        public void _ShowAfterSeconds()
        {
            VRCPlayerApi localPlayer = VRC.SDKBase.Networking.LocalPlayer;
            VRCPlayerApi owner = VRC.SDKBase.Networking.GetOwner(gameObject);
            if(localPlayer.playerId != owner.playerId) return;
            if(NetworkFeatures)
                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Show));
            Show();
            sentTimer = false;
        }
        
        public void _HideAfterSeconds()
        {
            VRCPlayerApi localPlayer = VRC.SDKBase.Networking.LocalPlayer;
            VRCPlayerApi owner = VRC.SDKBase.Networking.GetOwner(gameObject);
            if(localPlayer.playerId != owner.playerId) return;
            if(NetworkFeatures)
                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Hide));
            Hide();
            sentTimer = false;
        }

        private void StartNetworkTimer()
        {
            if(HideAfterCollect)
                SendCustomEventDelayedSeconds(nameof(_ShowAfterSeconds), ReappearTime);
            else if (ShowAfterCollect)
                SendCustomEventDelayedSeconds(nameof(_HideAfterSeconds), ReappearTime);
            sentTimer = true;
        }

        public void Show()
        {
            UpdateState(true);
            if(HideAfterCollect)
                isHidden = false;
            else if (ShowAfterCollect)
                isHidden = true;
            if (ShowAfterCollect && ReappearTime > 0)
            {
                StartNetworkTimer();
            }
            SaveData();
        }

        public void Hide()
        {
            UpdateState(false);
            if(HideAfterCollect)
                isHidden = true;
            else if (ShowAfterCollect)
                isHidden = false;
            if(HideAfterCollect && ReappearTime > 0)
            {
                // Every player should check for this
                StartNetworkTimer();
            }
            SaveData();
        }

        internal virtual void Start()
        {
            hasPickup = GetComponent<VRC_Pickup>() != null;
            if (!Persistence)
                UpdateState(HideAfterCollect);
            _TimedEvent();
        }
    }

    public enum ApplicationEvent
    {
        Interact,
        Trigger,
        Collider,
        Timed,
        Scripted
    }
}
