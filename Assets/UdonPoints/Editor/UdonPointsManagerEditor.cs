using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UdonPoints.Networking;
#if REVERSE_UC
using UCS;
#endif
using UnityEditor;
using UnityEngine;
using Logger = UdonPoints.Logging.Logger;

namespace UdonPoints.Editor
{
    [CustomEditor(typeof(UdonPointsManager))]
    public class UdonPointsManagerEditor : UnityEditor.Editor
    {
#if REVERSE_UC
        private const string UDONCHIPS_NAME = "UdonChips";
#endif
        
        private static GUIStyle Header;
        private static GUIStyle Subheader;
        
        private UdonPointsManager udonPointsManager;
        private SerializedProperty udonPoints;
        private SerializedProperty udonNetworkers;
        private SerializedProperty logger;

        private bool rawMode;
        private bool creatingPoints;
        private bool creatingNetworkers;
        private bool initUC;
        
        private string pointsName = "points";
        private float defaultMoney;
        private bool usePersistence = true;

        private int maxPlayers = 32;
        private double syncTolerance = 0.001;

#if REVERSE_UC
        private UdonChips udonChips;
#endif
        
        private bool showNetworkers;

        private void EnsureLogger()
        {
            if(rawMode || udonPointsManager._Logger != null) return;
            Transform t = udonPointsManager.transform;
            Logger l = null;
            for (int i = 0; i < t.childCount; i++)
            {
                Transform x = t.GetChild(i);
                l = x.GetComponent<Logger>();
                if (l != null) break;
            }
            if (l == null)
            {
                GameObject loggerObject = new GameObject("Logger");
                l = loggerObject.AddComponent<Logger>();
                loggerObject.transform.SetParent(t);
            }
            l.Manager = udonPointsManager;
            udonPointsManager._Logger = l;
            EditorUtility.SetDirty(l.gameObject);
            EditorUtility.SetDirty(udonPointsManager.gameObject);
        }

        private void CreateNewPoints()
        {
            GameObject gameObject = new GameObject(pointsName);
            UdonPointsBehaviour behaviour = gameObject.AddComponent<UdonPointsBehaviour>();
            behaviour.Manager = udonPointsManager;
            behaviour.MoneySafeName = pointsName;
            behaviour.DefaultMoney = defaultMoney;
            behaviour.DisablePersistence = !usePersistence;
            gameObject.transform.SetParent(udonPointsManager.transform);
            List<UdonPointsBehaviour> b = udonPointsManager.UdonPointsBehaviours.ToList();
            b.Add(behaviour);
            udonPointsManager.UdonPointsBehaviours = b.ToArray();
            EditorUtility.SetDirty(udonPointsManager.gameObject);
            EditorUtility.SetDirty(gameObject);
        }

        private void DeletePoints(UdonPointsBehaviour udonPointsBehaviour)
        {
            udonPointsManager.UdonPointsBehaviours =
                udonPointsManager.UdonPointsBehaviours.Where(x => x != udonPointsBehaviour).ToArray();
            DestroyImmediate(udonPointsBehaviour.gameObject);
            EditorUtility.SetDirty(udonPointsManager.gameObject);
        }

        private void FixPoints()
        {
            if (udonPointsManager.UdonPointsBehaviours == null)
                udonPointsManager.UdonPointsBehaviours = new UdonPointsBehaviour[0];
            udonPointsManager.UdonPointsBehaviours = udonPointsManager.UdonPointsBehaviours.Where(x => x != null).ToArray();
            EditorUtility.SetDirty(udonPointsManager.gameObject);
            Debug.Log("Fixed Points in Manager.");
        }

        private bool PointsNameExists()
        {
            foreach (UdonPointsBehaviour udonPointsBehaviour in udonPointsManager.UdonPointsBehaviours!)
            {
                if(udonPointsBehaviour.MoneySafeName.ToLower() != pointsName.ToLower()) continue;
                return true;
            }
            return false;
        }
        
        private void FixNetworkers()
        {
            if (udonPointsManager.UdonPointsNetworkBehaviours == null)
                udonPointsManager.UdonPointsNetworkBehaviours = new UdonPointsNetworkBehaviour[0];
            udonPointsManager.UdonPointsNetworkBehaviours = udonPointsManager.UdonPointsNetworkBehaviours.Where(x => x != null).ToArray();
            EditorUtility.SetDirty(udonPointsManager.gameObject);
            Debug.Log("Fixed Networkers in Manager.");
        }

        private void CreateNetworkers()
        {
            if (udonPointsManager.UdonPointsNetworkBehaviours == null)
                udonPointsManager.UdonPointsNetworkBehaviours = new UdonPointsNetworkBehaviour[0];
            bool isMalformed = udonPointsManager.UdonPointsNetworkBehaviours.Count(x => x == null) > 0;
            if(isMalformed) FixNetworkers();
            foreach (UdonPointsNetworkBehaviour networkBehaviour in udonPointsManager.UdonPointsNetworkBehaviours)
                DestroyImmediate(networkBehaviour.gameObject);
            Transform networkingContainer = udonPointsManager.transform.Find("Networkers");
            if (networkingContainer == null)
            {
                networkingContainer = new GameObject("Networkers").transform;
                networkingContainer.SetParent(udonPointsManager.transform);
            }
            UdonPointsNetworkBehaviour[] networkBehaviours = new UdonPointsNetworkBehaviour[maxPlayers];
            for (int i = 0; i < maxPlayers; i++)
            {
                Transform newNetworker = new GameObject($"Player ({i + 1})").transform;
                newNetworker.SetParent(networkingContainer);
                UdonPointsNetworkBehaviour networkBehaviour =
                    newNetworker.gameObject.AddComponent<UdonPointsNetworkBehaviour>();
                networkBehaviour.Manager = udonPointsManager;
                networkBehaviour.SyncTolerance = syncTolerance;
                networkBehaviours[i] = networkBehaviour;
                EditorUtility.SetDirty(networkBehaviour.gameObject);
            }
            udonPointsManager.UdonPointsNetworkBehaviours = networkBehaviours;
            EditorUtility.SetDirty(udonPointsManager.gameObject);
        }

        private void DrawPoints()
        {
            bool needsReset = false;
            if (udonPointsManager.UdonPointsBehaviours == null)
                udonPointsManager.UdonPointsBehaviours = new UdonPointsBehaviour[0];
            foreach (UdonPointsBehaviour udonPointsBehaviour in udonPointsManager.UdonPointsBehaviours)
            {
                if (udonPointsBehaviour == null)
                {
                    needsReset = true;
                    continue;
                }
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical();
                GUILayout.Label(udonPointsBehaviour.MoneySafeName);
                GUILayout.Label("Default Money: " + udonPointsBehaviour.DefaultMoney);
                GUILayout.Label("Persistence: " + (udonPointsBehaviour.DisablePersistence ? "Disabled" : "Enabled"));
                EditorGUILayout.EndVertical();
                if (GUILayout.Button("Edit"))
                    Selection.activeGameObject = udonPointsBehaviour.gameObject;
                if (GUILayout.Button("Delete"))
                    DeletePoints(udonPointsBehaviour);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();
            }
            if(!needsReset) return;
            FixPoints();
        }

#if REVERSE_UC
        private bool AreUdonChipsSetup()
        {
            if (udonChips == null)
            {
                GameObject g = GameObject.Find(UDONCHIPS_NAME);
                if (g == null) return false;
                UdonChips u = g.GetComponent<UdonChips>();
                if (u == null) return false;
                udonChips = u;
            }
            return udonChips != null && udonChips.Manager == udonPointsManager;
        }
        
        private UdonChips GetUdonChips()
        {
            if(udonChips != null) return udonChips;
            GameObject g = GameObject.Find(UDONCHIPS_NAME);
            if (g == null)
            {
                g = new GameObject(UDONCHIPS_NAME);
                g.transform.SetParent(udonPointsManager.transform);
            }
            UdonChips u = g.GetComponent<UdonChips>();
            if (u != null)
            {
                udonChips = u;
                return udonChips;
            }
            udonChips = g.AddComponent<UdonChips>();
            return udonChips;
        }

        private string GetUdonChipsLocation()
        {
            const string LOCATION1 = "Assets/UdonChips/00_UdonChips/.SCRIPT";
            if (Directory.Exists(LOCATION1))
                return LOCATION1;
            return null;
        }

        private void DisableUdonChipsIntegration()
        {
            bool c = EditorUtility.DisplayDialog("UdonPointsManager",
                "Disabling UdonChips Integration will edit files and may cause damage. Please create a backup before continuing. Would you like to continue?",
                "Yes", "No");
            if(!c) return;
            EditorUtility.DisplayDialog("UdonPointsManager",
                "This process will recompile your project. If you see errors before then, simply unfocus and refocus Unity. If you switch BuildTargets, simply selecting the Manager should fix any errors. If you still experience issues, try restarting Unity.",
                "OK");
            string location = GetUdonChipsLocation();
            if (location == null)
            {
                Debug.LogWarning("UdonChips not found! Did you re-import the package?");
                return;
            }
            Helper.RenameFolder(location, "SCRIPT");
            Helper.RemoveScriptingDefineSymbol("REVERSE_UC");
            Selection.activeGameObject = null;
        }
#else
        private string GetUdonChipsLocation()
        {
            const string LOCATION1 = "Assets/UdonChips/00_UdonChips/SCRIPT";
            if (Directory.Exists(LOCATION1))
                return LOCATION1;
            return null;
        }

        private void EnableUdonChipsIntegration()
        {
            bool c = EditorUtility.DisplayDialog("UdonPointsManager",
                "Enabling UdonChips Integration will edit files and may cause damage. Please create a backup before continuing. Would you like to continue?",
                "Yes", "No");
            if(!c) return;
            EditorUtility.DisplayDialog("UdonPointsManager",
                "This process will recompile your project. If you see errors before then, simply unfocus and refocus Unity. If you switch BuildTargets, simply selecting the Manager should fix any errors. If you still experience issues, try restarting Unity.",
                "OK");
            string location = GetUdonChipsLocation();
            if (location == null)
            {
                Debug.LogWarning("UdonChips not found! Did you import the package?");
                return;
            }
            Helper.RenameFolder(location, ".SCRIPT");
            string meta = location + ".meta";
            if(File.Exists(meta))
            {
                File.Delete(meta);
                Debug.Log("Deleted meta file.");
            }
            Helper.AddScriptingDefineSymbol("REVERSE_UC");
            AssetDatabase.Refresh(ImportAssetOptions.ImportRecursive);
            Selection.activeGameObject = null;
        }
#endif
        
        private string GetChamchiLocation()
        {
            const string LOCATION1 = "Assets/Chamchi";
            if (Directory.Exists(LOCATION1))
                return LOCATION1;
            return null;
        }

        private void DrawNetworkers()
        {
            if (udonPointsManager.UdonPointsNetworkBehaviours == null)
                udonPointsManager.UdonPointsNetworkBehaviours = new UdonPointsNetworkBehaviour[0];
            GUILayout.Label("Networker Count: " + udonPointsManager.UdonPointsNetworkBehaviours.Length);
            if (udonPointsManager.UdonPointsNetworkBehaviours.Length < 1)
            {
                return;
            }
            showNetworkers = EditorGUILayout.Foldout(showNetworkers, "<b>Networkers</b>",
                new GUIStyle(EditorStyles.foldout) {richText = true});
            if (showNetworkers)
            {
                bool needsReset = false;
                foreach (UdonPointsNetworkBehaviour networkBehaviour in udonPointsManager.UdonPointsNetworkBehaviours)
                {
                    if (networkBehaviour == null)
                    {
                        needsReset = true;
                        continue;
                    }
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.BeginVertical();
                    GUILayout.Label(networkBehaviour.gameObject.name);
                    GUILayout.Label("Sync Tolerance: " + networkBehaviour.SyncTolerance);
                    EditorGUILayout.EndVertical();
                    if (GUILayout.Button("Edit"))
                        Selection.activeGameObject = networkBehaviour.gameObject;
                    EditorGUILayout.EndHorizontal();
                }
                if(needsReset) FixNetworkers();
            }
        }

        private void OnEnable()
        {
            Helper.EnsureDefinitions();
            udonPointsManager = target as UdonPointsManager;
            if (udonPointsManager == null)
                throw new Exception("Invalid Window");
            udonPoints = serializedObject.FindProperty("UdonPointsBehaviours");
            udonNetworkers = serializedObject.FindProperty("UdonPointsNetworkBehaviours");
            logger = serializedObject.FindProperty("_Logger");
        }

        public override void OnInspectorGUI()
        {
            if (!Helper.IsInstancedInScene(udonPointsManager.gameObject))
            {
                GUILayout.Label("Please insert this prefab into your scene.");
                return;
            }
            if (Helper.IsPrefab(udonPointsManager.gameObject))
            {
                EditorGUILayout.HelpBox("Please unpack this prefab completely!", MessageType.Error);
                udonPointsManager._Logger = null;
                return;
            }
            Header ??= new GUIStyle(EditorStyles.largeLabel);
            Subheader ??= new GUIStyle(EditorStyles.miniLabel);
            if(creatingPoints)
            {
                // ??? unity why do you make this null
                if (pointsName == null) pointsName = String.Empty;
                bool pointsNameExists = PointsNameExists();
                bool pointsEmpty = string.IsNullOrEmpty(pointsName);
                GUILayout.Label("Points Name", Header);
                GUILayout.Label("An identifier and short-hand name used for your points behaviour.", Subheader);
                if(pointsEmpty)
                    EditorGUILayout.HelpBox("Points Name cannot be empty!", MessageType.Error);
                else if (pointsNameExists)
                    EditorGUILayout.HelpBox("A points behaviour by the name of " + pointsName.ToLower() + " already exists!",
                        MessageType.Error);
                pointsName = EditorGUILayout.TextField("Points Name", pointsName);
                GUILayout.Label("Default Money", Header);
                GUILayout.Label("The amount of money a player starts with.", Subheader);
                if (defaultMoney < 0)
                    EditorGUILayout.HelpBox("Your starting money is below 0. Is this intentional?",
                        MessageType.Warning);
                defaultMoney = EditorGUILayout.FloatField("Starting Money", defaultMoney);
                GUILayout.Label("Persistence", Header);
                GUILayout.Label("Whether or not to save PlayerData.", Subheader);
                usePersistence = EditorGUILayout.Toggle("Use Persistence", usePersistence);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Cancel"))
                    creatingPoints = false;
                if (!pointsNameExists && !pointsEmpty)
                {
                    if (GUILayout.Button("Create"))
                    {
                        CreateNewPoints();
                        creatingPoints = false;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            else if (creatingNetworkers)
            {
                GUILayout.Label("Maximum Amount of Players", Header);
                GUILayout.Label("The maximum amount of players that can be networked in your world.", Subheader);
                if(maxPlayers <= 1)
                    EditorGUILayout.HelpBox("You must have more than 1 Networker!", MessageType.Error);
                EditorGUILayout.HelpBox(
                    "Reduce the Max Player Count as low as possible! More players means more data has to sync.",
                    MessageType.Info);
                maxPlayers = EditorGUILayout.IntField("Max Player Count", maxPlayers);
                GUILayout.Label("Sync Tolerance", Header);
                GUILayout.Label(
                    "How much of a difference between the previous and new money value is needed\nto update values over the network.",
                    Subheader);
                if(syncTolerance <= 0)
                    EditorGUILayout.HelpBox("Sync Tolerance must be greater than 0! Try 0.001?", MessageType.Error);
                syncTolerance = EditorGUILayout.DoubleField("Tolerance", syncTolerance);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Cancel"))
                    creatingNetworkers = false;
                if(maxPlayers > 1 && syncTolerance > 0)
                {
                    if (GUILayout.Button($"Create {maxPlayers} Networkers"))
                    {
                        CreateNetworkers();
                        creatingNetworkers = false;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
#if REVERSE_UC
            else if (initUC)
            {
                GUILayout.Label("UdonChips Integration", Header);
                GUILayout.Label("Select the Points Behaviour you'd like to integrate with UdonChips.", Subheader);
                foreach (UdonPointsBehaviour udonPointsBehaviour in udonPointsManager.UdonPointsBehaviours)
                {
                    if (GUILayout.Button(udonPointsBehaviour.MoneySafeName))
                    {
                        UdonChips u = GetUdonChips();
                        u.Manager = udonPointsManager;
                        u.Target = udonPointsBehaviour;
                        initUC = false;
                        EditorGUILayout.Space();
                        break;
                    }
                }
                if (GUILayout.Button("Cancel"))
                    initUC = false;
            }
#endif
            else if (!rawMode)
            {
                GUILayout.Label("Points", Header);
                GUILayout.Label("Where all the money magic happens.", Subheader);
                DrawPoints();
                if (GUILayout.Button("Create a new Points Behaviour"))
                    creatingPoints = true;
                EditorGUILayout.Separator();
                GUILayout.Label("Networking", Header);
                GUILayout.Label("Configure NetworkSync for all Points Behaviours.", Subheader);
                DrawNetworkers();
                if (GUILayout.Button(udonPointsManager.UdonPointsNetworkBehaviours.Length > 0 ? "Redo Networking" : "Setup Networking"))
                    creatingNetworkers = true;
#if REVERSE_UC
                GUILayout.Label("UdonChips", Header);
                GUILayout.Label("Optionally create an integration between UdonChips and UdonPoints.", Subheader);
                bool chipsSetup = AreUdonChipsSetup();
                if (chipsSetup)
                {
                    UdonChips chips = GetUdonChips();
                    if (chips == null || chips.Target == null)
                        GUILayout.Label("Connected UdonPoints: None");
                    else
                        GUILayout.Label($"Connected UdonPoints: {chips.Target.MoneySafeName}");
                }
                else
                    GUILayout.Label("Connected UdonPoints: None");
                if (GUILayout.Button($"{(chipsSetup ? "Redo" : "Setup")} UdonChips"))
                    initUC = true;
                if (GetUdonChipsLocation() != null)
                {
                    if(GUILayout.Button("Disable UdonChips Integration"))
                        DisableUdonChipsIntegration();
                }
#else
                if (GetUdonChipsLocation() != null)
                {
                    GUILayout.Label("UdonChips", Header);
                    GUILayout.Label("Optionally create an integration between UdonChips and UdonPoints.", Subheader);
                    if(GUILayout.Button("Enable UdonChips Integration"))
                        EnableUdonChipsIntegration();
                }
#endif
            }
            else
            {
                EditorGUILayout.PropertyField(udonPoints, new GUIContent("UdonPoints"));
                EditorGUILayout.PropertyField(udonNetworkers, new GUIContent("Networkers"));
                EditorGUILayout.PropertyField(logger, new GUIContent("Logger"));
            }
            EditorGUILayout.Separator();
            if(!creatingPoints && !creatingNetworkers && !initUC)
            {
                GUILayout.Label("Advanced", Header);
                if (!rawMode && !string.IsNullOrEmpty(GetChamchiLocation()))
                {
                    GUILayout.Label("Chamchi Logger Integration");
                    GUILayout.Label("Optionally create an integration between Chamchi and UdonPoints.", Subheader);
                    Rect r = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight * 1.6f);
                    EditorGUI.HelpBox(r, "UdonPoints recommends using ReLog for stability and improvements.", MessageType.Warning);
                    r.x = r.width - 85;
                    r.width = 100;
                    r.height -= 10;
                    r.y += 5;
                    if(GUI.Button(r, "Open ReLog"))
                        Application.OpenURL("https://github.com/200Tigersbloxed/ReLog");
#if UDONPOINTS_CHAMCHI
                    if(GUILayout.Button("Disable Chamchi Integration"))
                        Helper.RemoveScriptingDefineSymbol("UDONPOINTS_CHAMCHI");
#else
                    if(GUILayout.Button("Enable Chamchi Integration"))
                        Helper.AddScriptingDefineSymbol("UDONPOINTS_CHAMCHI");
#endif
                }
                if (!rawMode && udonPointsManager._Logger != null)
                {
                    if (GUILayout.Button("Edit Current Logger"))
                        Selection.activeGameObject = udonPointsManager._Logger.gameObject;
                }
                if (rawMode)
                    EditorGUILayout.HelpBox(
                        "Raw Mode is only meant to be used by experienced developers! Please don't use this if you don't know what you're doing.",
                        MessageType.Warning);
                rawMode = EditorGUILayout.Toggle("Raw View", rawMode);
            }
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Made by 200Tigersbloxed", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField($"Version {UdonPointsManager.VERSION}", EditorStyles.miniBoldLabel);
            EnsureLogger();
            serializedObject.ApplyModifiedProperties();
        }
    }
}