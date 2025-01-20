using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace UdonPoints.Examples
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class Leaderboard : UdonSharpBehaviour
    {
        private const string ALIGN_LEFT = "<align=\"left\">";
        private const string ALIGN_RIGHT = "<align=\"right\">";
        private const string LINE_HEIGHT_0 = "<line-height=0>";
        private const string LINE_HEIGHT_1EM = "<line-height=1em>";
        private const string LINE_BREAK = "<br>";
        
        public UdonPointsBehaviour TargetBehaviour;
        public TMP_Text Text;
        public Color FirstColor = new Color(1, 0.788f, 0);
        public Color SecondColor = new Color(0.71f, 0.71f, 0.71f);
        public Color ThirdColor = new Color(0.69f, 0.361f, 0.192f);

        private UdonPointsManager manager;
        private bool canRun;

        private VRCPlayerApi[] GetSortedArray()
        {
            VRCPlayerApi[] playerValue = new VRCPlayerApi[manager.PlayerList.Length];
            decimal[] moneyValue = new decimal[manager.PlayerList.Length];
            for (int i = 0; i < manager.PlayerList.Length; i++)
            {
                VRCPlayerApi player = manager.PlayerList[i];
                playerValue[i] = player;
                moneyValue[i] = manager.GetMoney(player, TargetBehaviour);
            }
            int n = moneyValue.Length;
            bool swapped;
            for (int i = 0; i < n - 1; i++)
            {
                swapped = false;
                for (int j = 0; j < n - 1 - i; j++)
                {
                    if (moneyValue[j] < moneyValue[j + 1]) 
                    {
                        decimal tempMoney = moneyValue[j];
                        moneyValue[j] = moneyValue[j + 1];
                        moneyValue[j + 1] = tempMoney;
                        VRCPlayerApi tempPlayer = playerValue[j];
                        playerValue[j] = playerValue[j + 1];
                        playerValue[j + 1] = tempPlayer;
                        swapped = true;
                    }
                }
                if (!swapped)
                    break;
            }
            return playerValue;
        }

        private void Start()
        {
            manager = TargetBehaviour.Manager;
            if (manager == null || Text == null)
            {
                manager.Logger.LogError("Misconfiguration! Cannot continue.");
                return;
            }
            Text.richText = true;
            canRun = true;
        }

        private void Update()
        {
            if(!canRun) return;
            VRCPlayerApi[] sortedPlayers = GetSortedArray();
            string t = "";
            for (int i = 0; i < sortedPlayers.Length; i++)
            {
                VRCPlayerApi player = sortedPlayers[i];
                string beginColor;
                switch (i)
                {
                    case 0:
                        beginColor = $"<color={FirstColor.ConvertToHex()}>";
                        break;
                    case 1:
                        beginColor = $"<color={SecondColor.ConvertToHex()}>";
                        break;
                    case 2:
                        beginColor = $"<color={ThirdColor.ConvertToHex()}>";
                        break;
                    default:
                        beginColor = String.Empty;
                        break;
                }
                t += beginColor + ALIGN_LEFT + player.displayName + LINE_HEIGHT_0 + LINE_BREAK + ALIGN_RIGHT +
                     manager.GetMoney(player, TargetBehaviour) + " " + TargetBehaviour.MoneySafeName + LINE_HEIGHT_1EM;
                if(!string.IsNullOrEmpty(beginColor))
                    t += "</color>";
                t += LINE_BREAK;
            }
            Text.text = t;
        }
    }
}