using TMPro;
using UdonPoints.Networking;
using UdonSharp;
using VRC.SDKBase;

namespace UdonPoints.Debugging
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TextDebugger : UdonSharpBehaviour
    {
        private const string LINE_BREAK = "<br>";

        public UdonPointsManager Manager;
        public TMP_Text Text;

        private bool canRun;

        private void Start()
        {
            if (Text == null)
            {
                Manager.Logger.LogError("No text provided!");
                return;
            }
            Text.richText = true;
            canRun = true;
        }

        private double GetNetworkMoney(UdonPointsBehaviour behaviour, UdonPointsNetworkBehaviour networkBehaviour)
        {
            if (networkBehaviour.Money == null) return 0;
            int index = networkBehaviour.GetIndexFromName(behaviour.MoneySafeName);
            if (index < 0 || index >= networkBehaviour.Money.Length) return 0;
            return networkBehaviour.Money[index];
        }

        private void LateUpdate()
        {
            if (!canRun) return;
            string finalText = "";
            foreach (UdonPointsBehaviour pointsBehaviour in Manager.UdonPointsBehaviours)
            {
                finalText += $"PointsBehaviour: {pointsBehaviour.MoneySafeName}{LINE_BREAK}Money: {Manager.GetMoney(pointsBehaviour)}";
                foreach (UdonPointsNetworkBehaviour pointsNetworkBehaviour in Manager.UdonPointsNetworkBehaviours)
                {
                    VRCPlayerApi realOwner = pointsNetworkBehaviour.GetOwner();
                    finalText += $"{LINE_BREAK}NetworkBehaviour {pointsNetworkBehaviour.gameObject.name} : ";
                    finalText += $"Owner: {realOwner.displayName} ({realOwner.playerId})";
                    finalText +=
                        $" | Money: {Manager.GetMoney(realOwner, pointsBehaviour)} ({Manager.GetMoney(realOwner, pointsBehaviour, false)}) | N-{GetNetworkMoney(pointsBehaviour, pointsNetworkBehaviour)}";
                }
                finalText += LINE_BREAK;
            }
            Text.text = finalText;
        }
    }
}