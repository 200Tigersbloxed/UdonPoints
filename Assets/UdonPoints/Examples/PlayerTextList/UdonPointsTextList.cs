using UdonPoints;
using TMPro;
using UdonSharp;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class UdonPointsTextList : UdonSharpBehaviour
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

    private void LateUpdate()
    {
        if(!canRun) return;
        string finalText = $"Player Money{LINE_BREAK}Player Count: {Manager.PlayerList.Length}{LINE_BREAK}{LINE_BREAK}";
        foreach (UdonPointsBehaviour behaviour in Manager.UdonPointsBehaviours)
        {
            string moneyName = behaviour.MoneySafeName;
            finalText += moneyName + LINE_BREAK;
            foreach (VRCPlayerApi player in Manager.PlayerList)
                finalText += $"{player.displayName}: {Manager.GetMoney(player, behaviour)} {moneyName}{LINE_BREAK}";
        }
        Text.text = finalText;
    }
}
