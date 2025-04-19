using System;
using System.Collections.Generic;
using System.Linq;
using UdonPoints.Examples;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace UdonPoints.Editor
{
    [CustomEditor(typeof(PointsApplicator), true)]
    public class ApplicatorEditor : UnityEditor.Editor
    {
        private PointsApplicator pointsApplicator;
        private Type pointsApplicatorType;
        private readonly Type interactType = typeof(InteractPointsApplicator);
        
        public SerializedProperty ApplicationEvent;
        public SerializedProperty ApplicationAction;
        public SerializedProperty AmountToApply;
        public SerializedProperty TimeApplication;
        public SerializedProperty ExcludeOwner;
        public SerializedProperty CoolDown;
        public SerializedProperty DenyIfNotEnough;
        public SerializedProperty HideAfterCollect;
        public SerializedProperty ShowAfterCollect;
        public SerializedProperty ObjectsToHide;
        public SerializedProperty ObjectsToShow;
        public SerializedProperty ReappearTime;
        public SerializedProperty NetworkFeatures;
        public SerializedProperty Persistence;
        public SerializedProperty PersistenceGUID;

        private Dictionary<UdonPointsBehaviour, bool> selectedBehaviours = new Dictionary<UdonPointsBehaviour, bool>();

        private bool DoesGuidExist(string guid) =>
            FindObjectsOfType<PointsApplicator>().Count(x => x.PersistenceGUID == guid) > 0;
        
        private string GenerateGuid()
        {
            string guid = String.Empty;
            PointsApplicator[] applicators = FindObjectsOfType<PointsApplicator>();
            while (guid == String.Empty)
            {
                Guid g = Guid.NewGuid();
                string gstring = g.ToString();
                if(applicators.Count(x => x.PersistenceGUID == gstring) > 0) continue;
                guid = gstring;
                break;
            }
            return guid;
        }
        
        private void OnEnable()
        {
            pointsApplicator = target as PointsApplicator;
            if (pointsApplicator == null)
                throw new Exception("Invalid Window");
            pointsApplicatorType = pointsApplicator.GetType();
            ApplicationEvent = serializedObject.FindProperty("ApplicationEvent");
            ApplicationAction = serializedObject.FindProperty("ApplicationAction");
            AmountToApply = serializedObject.FindProperty("AmountToApply");
            TimeApplication = serializedObject.FindProperty("TimeApplication");
            ExcludeOwner = serializedObject.FindProperty("ExcludeOwner");
            CoolDown = serializedObject.FindProperty("CoolDown");
            DenyIfNotEnough = serializedObject.FindProperty("DenyIfNotEnough");
            HideAfterCollect = serializedObject.FindProperty("HideAfterCollect");
            ShowAfterCollect = serializedObject.FindProperty("ShowAfterCollect");
            ObjectsToHide = serializedObject.FindProperty("ObjectsToHide");
            ObjectsToShow = serializedObject.FindProperty("ObjectsToShow");
            ReappearTime = serializedObject.FindProperty("ReappearTime");
            NetworkFeatures = serializedObject.FindProperty("NetworkFeatures");
            Persistence = serializedObject.FindProperty("Persistence");
            PersistenceGUID = serializedObject.FindProperty("PersistenceGUID");
            if (DoesGuidExist(PersistenceGUID.stringValue))
                PersistenceGUID.stringValue = GenerateGuid();
        }

        private void ValidateBehaviours()
        {
            bool isNull = false;
            foreach (UdonPointsBehaviour udonPointsBehaviour in pointsApplicator.TargetBehaviours)
            {
                if(udonPointsBehaviour != null) continue;
                isNull = true;
            }
            if(!isNull) return;
            pointsApplicator.TargetBehaviours = pointsApplicator.TargetBehaviours.Where(x => x != null).ToArray();
        }

        public override void OnInspectorGUI()
        {
            if (!Helper.IsInstancedInScene(pointsApplicator.gameObject))
            {
                GUILayout.Label("Please insert this prefab into your scene.");
                return;
            }
            if (Helper.IsPrefab(pointsApplicator.gameObject))
            {
                EditorGUILayout.HelpBox("Please unpack this prefab completely!", MessageType.Error);
                pointsApplicator.Manager = null;
                pointsApplicator.TargetBehaviours = Array.Empty<UdonPointsBehaviour>();
            }
            if (pointsApplicator.Manager == null)
            {
                UdonPointsManager[] managers = FindObjectsOfType<UdonPointsManager>();
                if (managers.Length < 1)
                {
                    EditorGUILayout.HelpBox("Please create an UdonPointsManager!", MessageType.Error);
                    return;
                }
                if (managers.Length == 1)
                {
                    pointsApplicator.Manager = managers[0];
                    EditorUtility.SetDirty(pointsApplicator.gameObject);
                    return;
                }
                GUILayout.Label("Please select a Manager to use before continuing.");
                foreach (UdonPointsManager udonPointsManager in managers)
                {
                    if (GUILayout.Button(udonPointsManager.gameObject.name))
                    {
                        pointsApplicator.Manager = udonPointsManager;
                        EditorUtility.SetDirty(pointsApplicator.gameObject);
                    }
                }
                return;
            }
            if (pointsApplicator.Manager.UdonPointsBehaviours == null ||
                pointsApplicator.Manager.UdonPointsBehaviours.Length < 1)
            {
                GUILayout.Label("Please create a points behaviour before continuing.");
                return;
            }
            if (pointsApplicator.TargetBehaviours == null || pointsApplicator.TargetBehaviours.Length < 1)
            {
                GUILayout.Label("Target Behaviour", EditorStyles.boldLabel);
                GUILayout.Label("Select which Behaviours you would like to target.", EditorStyles.miniBoldLabel);
                foreach (UdonPointsBehaviour udonPointsBehaviour in pointsApplicator.Manager.UdonPointsBehaviours)
                {
                    if(!selectedBehaviours.ContainsKey(udonPointsBehaviour))
                        selectedBehaviours.Add(udonPointsBehaviour, false);
                    selectedBehaviours[udonPointsBehaviour] = EditorGUILayout.Toggle(udonPointsBehaviour.MoneySafeName,
                        selectedBehaviours[udonPointsBehaviour]);
                }
                if (selectedBehaviours.Count(x => x.Value) > 0)
                {
                    if(GUILayout.Button("Continue"))
                    {
                        pointsApplicator.TargetBehaviours =
                            selectedBehaviours.Where(x => x.Value).Select(pair => pair.Key).ToArray();
                        EditorUtility.SetDirty(pointsApplicator.gameObject);
                    }
                }
                else
                    GUILayout.Label("Please select at least one behaviour.", EditorStyles.miniLabel);
                return;
            }
            ValidateBehaviours();
            GUILayout.Label("Points Applicator", EditorStyles.boldLabel);
            EditorGUILayout.Separator();
            if (pointsApplicatorType != interactType)
            {
                ApplicationEvent.enumValueIndex = Array.IndexOf(Enum.GetValues(typeof(ApplicationEvent)),
                    EditorGUILayout.EnumPopup("Application Event", (ApplicationEvent) ApplicationEvent.enumValueIndex));
                if (ApplicationEvent.enumValueIndex == (int) Examples.ApplicationEvent.Interact)
                {
                    ApplicationEvent.enumValueIndex = (int) Examples.ApplicationEvent.Trigger;
                    EditorUtility.DisplayDialog("PointsApplicator",
                        "You cannot use Touch with PointsApplicator! Please use InteractPointsApplicator instead.", "OK");
                }
            }
            else
            {
                ApplicationEvent.enumValueIndex = (int) Examples.ApplicationEvent.Interact;
                UdonSharpGUI.DrawInteractSettings(pointsApplicator);
                EditorGUILayout.Space();
            }
            ApplicationAction.enumValueIndex = Array.IndexOf(Enum.GetValues(typeof(MoneyAction)),
                EditorGUILayout.EnumPopup("Money Action", (MoneyAction) ApplicationAction.enumValueIndex));
            AmountToApply.floatValue = EditorGUILayout.FloatField("Amount to Apply", AmountToApply.floatValue);
            if (ApplicationEvent.enumValueIndex == (int) Examples.ApplicationEvent.Timed)
            {
                TimeApplication.floatValue = EditorGUILayout.FloatField("Money Cooldown", TimeApplication.floatValue);
                DenyIfNotEnough.boolValue = false;
                HideAfterCollect.boolValue = false;
                ShowAfterCollect.boolValue = false;
                NetworkFeatures.boolValue = false;
                Persistence.boolValue = false;
            }
            else
            {
                EditorGUILayout.Separator();
                GUILayout.Label("Features", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "DenyIfNotEnough will not apply the action unless the user's money is greater than or equal to the amount on ALL TargetBehaviours.",
                    MessageType.Info);
                DenyIfNotEnough.boolValue = EditorGUILayout.Toggle("Needs Amount", DenyIfNotEnough.boolValue);
                EditorGUILayout.Space();
                if (HideAfterCollect.boolValue && ShowAfterCollect.boolValue)
                {
                    HideAfterCollect.boolValue = false;
                    ShowAfterCollect.boolValue = false;
                }

                if (!ShowAfterCollect.boolValue)
                    HideAfterCollect.boolValue =
                        EditorGUILayout.Toggle("Hide After Collect", HideAfterCollect.boolValue);
                if (!HideAfterCollect.boolValue)
                    ShowAfterCollect.boolValue =
                        EditorGUILayout.Toggle("Show After Collect", ShowAfterCollect.boolValue);
                if (HideAfterCollect.boolValue)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(ObjectsToHide, new GUIContent("Objects to Hide"));
                    EditorGUILayout.HelpBox("Set Reappear Time to 0 to have the Object never appear again!",
                        MessageType.Info);
                    ReappearTime.floatValue = EditorGUILayout.FloatField("Reappear Time", ReappearTime.floatValue);
                    EditorGUILayout.Space();
                }
                else if (ShowAfterCollect.boolValue)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(ObjectsToHide, new GUIContent("Objects to Hide"));
                    EditorGUILayout.PropertyField(ObjectsToShow, new GUIContent("Objects to Show"));
                    EditorGUILayout.HelpBox("Set Disappear Time to 0 to have the Object never disappear again!",
                        MessageType.Info);
                    ReappearTime.floatValue = EditorGUILayout.FloatField("Disappear Time", ReappearTime.floatValue);
                    EditorGUILayout.Space();
                }
                Persistence.boolValue = EditorGUILayout.Toggle("Persistence", Persistence.boolValue);
                if (Persistence.boolValue)
                {
                    if (string.IsNullOrEmpty(PersistenceGUID.stringValue))
                        PersistenceGUID.stringValue = GenerateGuid();
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.TextField("PersistenceGUID", PersistenceGUID.stringValue);
                    EditorGUI.EndDisabledGroup();
                    NetworkFeatures.boolValue = false;
                }
                NetworkFeatures.boolValue = !Persistence.boolValue &&
                                            EditorGUILayout.Toggle("Network Features", NetworkFeatures.boolValue);
                if (NetworkFeatures.boolValue)
                    Persistence.boolValue = false;
                EditorGUILayout.Separator();
                if (pointsApplicator.ApplicationEvent == Examples.ApplicationEvent.Trigger ||
                    pointsApplicator.ApplicationEvent == Examples.ApplicationEvent.Collider)
                {
                    GUILayout.Label("Touch Features", EditorStyles.boldLabel);
                    ExcludeOwner.boolValue = EditorGUILayout.Toggle("Exclude Owner", ExcludeOwner.boolValue);
                    if (NetworkFeatures.boolValue)
                        EditorGUILayout.HelpBox("CoolDown is Local Only.", MessageType.Info);
                    if (CoolDown.floatValue < 0f)
                        EditorGUILayout.HelpBox("CoolDown cannot be less than 0! PointsApplicator will default to 5.",
                            MessageType.Warning);
                    CoolDown.floatValue = EditorGUILayout.FloatField("Cool Down", CoolDown.floatValue);
                }
            }
            if (GUILayout.Button("Redo Setup"))
            {
                pointsApplicator.Manager = null;
                pointsApplicator.TargetBehaviours = Array.Empty<UdonPointsBehaviour>();
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}