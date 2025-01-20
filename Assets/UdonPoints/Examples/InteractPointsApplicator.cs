using UnityEngine;

namespace UdonPoints.Examples
{
    [RequireComponent(typeof(Collider))]
    public class InteractPointsApplicator : PointsApplicator
    {
        public Collider InteractCollider;
        
        public override void Interact()
        {
            if(!IsEnough()) return;
            if(isHidden) return;
            Manager.EffectMoney(ApplicationAction, AmountToApply, TargetBehaviours);
            CallOnMoney();
        }

        internal override void UpdateState(bool state)
        {
            if (HideAfterCollect)
            {
                InteractCollider.enabled = state;
                DisableInteractive = !state;
            }
            else if(ShowAfterCollect)
            {
                InteractCollider.enabled = !state;
                DisableInteractive = state;
            }
            base.UpdateState(state);
        }

        internal override void Start()
        {
            InteractCollider = GetComponent<Collider>();
            base.Start();
        }
    }
}
