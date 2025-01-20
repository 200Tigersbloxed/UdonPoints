using UdonSharp;
using UnityEngine;

namespace UdonPoints.Logging
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class Logger : UdonSharpBehaviour
    {
        public UdonPointsManager Manager;

#if UDONPOINTS_CHAMCHI
        public LogPanel[] LogPanels;
#endif
        
        public void Log(string message)
        {
#if UDONPOINTS_CHAMCHI
            foreach (LogPanel logPanel in LogPanels)
            {
                logPanel.Log(Manager, message);
            }
#endif
            Debug.Log(message);
        }

        public void LogWarn(string message)
        {
#if UDONPOINTS_CHAMCHI
            foreach (LogPanel logPanel in LogPanels)
            {
                logPanel.LogWarn(Manager, message);
            }
#endif
            Debug.LogWarning(message);
        }

        public void LogError(string message)
        {
#if UDONPOINTS_CHAMCHI
            foreach (LogPanel logPanel in LogPanels)
            {
                logPanel.LogError(Manager, message);
            }
#endif
            Debug.LogError(message);
        }

        private void Start() => Manager = GetComponent<UdonPointsManager>();
    }
}