#if RELOG
using ReLog;
#endif
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

#if RELOG
        public CoreLogger CoreLogger;
#endif
        
        public void Log(string message)
        {
#if UDONPOINTS_CHAMCHI
            foreach (LogPanel logPanel in LogPanels)
            {
                logPanel.Log(Manager, message);
            }
#endif
#if RELOG
            CoreLogger.Log(message);
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
#if RELOG
            CoreLogger.LogWarning(message);
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
#if RELOG
            CoreLogger.LogError(message);
#endif
            Debug.LogError(message);
        }

        private void Start() => Manager = GetComponent<UdonPointsManager>();
    }
}