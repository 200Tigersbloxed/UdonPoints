using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UdonPoints.Editor
{
    [InitializeOnLoad]
    internal static class Helper
    {
        private const string SDFILE = "sd.txt";

        public static readonly string[] RegisteredDefinitions =
        {
            "REVERSE_UC",
            "UDONPOINTS_CHAMCHI"
        };
        
        /// <summary>
        /// Renames a folder name
        /// </summary>
        /// <param name="directory">The full directory of the folder</param>
        /// <param name="newFolderName">New name of the folder</param>
        /// <returns>Returns true if rename is successful</returns>
        public static bool RenameFolder(string directory, string newFolderName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(directory) ||
                    string.IsNullOrWhiteSpace(newFolderName))
                {
                    return false;
                }


                var oldDirectory = new DirectoryInfo(directory);

                if (!oldDirectory.Exists)
                {
                    return false;
                }

                if (string.Equals(oldDirectory.Name, newFolderName, StringComparison.OrdinalIgnoreCase))
                {
                    //new folder name is the same with the old one.
                    return false;
                }

                string newDirectory;

                if (oldDirectory.Parent == null)
                {
                    //root directory
                    newDirectory = Path.Combine(directory, newFolderName);
                }
                else
                {
                    newDirectory = Path.Combine(oldDirectory.Parent.FullName, newFolderName);
                }

                if (Directory.Exists(newDirectory))
                {
                    //target directory already exists
                    return false;
                }

                oldDirectory.MoveTo(newDirectory);

                return true;
            }
            catch
            {
                //ignored
                return false;
            }
        }
        
        private static string GetGameObjectPath(GameObject obj)
        {
            string path = "/" + obj.name;
            while (obj.transform.parent != null)
            {
                obj = obj.transform.parent.gameObject;
                path = "/" + obj.name + path;
            }
            return path;
        }

        public static bool IsInstancedInScene(GameObject obj)
        {
            string path = GetGameObjectPath(obj);
            if (path != "/" + obj.name)
                return true;
            foreach (GameObject rootGameObject in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (rootGameObject == obj.gameObject)
                    return true;
            }
            return false;
        }

        public static bool IsPrefab(GameObject obj) => PrefabUtility.GetPrefabInstanceHandle(obj) != null;
        
        public static void AddScriptingDefineSymbol(string define)
        {
            BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            string[] defines;
            PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup, out defines);
            List<string> clone = new List<string>(defines);
            if(!clone.Contains(define))
                clone.Add(define);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, clone.ToArray());
            AddDefinition(define);
        }
    
        public static void RemoveScriptingDefineSymbol(string define)
        {
            BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            string[] defines;
            PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup, out defines);
            List<string> clone = new List<string>(defines);
            if(clone.Contains(define))
                clone.Remove(define);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, clone.ToArray());
            RemoveDefinition(define);
        }

        public static void EnsureDefinitions()
        {
            string[] literalDefinitions = GetSavedDefinitions();
            if(literalDefinitions.Length < 1) return;
            string[] defines;
            PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, out defines);
            List<string> definitionsToAdd = new List<string>();
            List<string> definitionsToRemove = new List<string>();
            foreach (string literalDefinition in literalDefinitions)
            {
                if(defines.Contains(literalDefinition)) continue;
                definitionsToAdd.Add(literalDefinition);
            }
            foreach (string definition in defines)
            {
                if(literalDefinitions.Contains(definition)) continue;
                if(!RegisteredDefinitions.Contains(definition)) continue;
                definitionsToRemove.Add(definition);
            }
            definitionsToAdd.ForEach(AddScriptingDefineSymbol);
            definitionsToRemove.ForEach(RemoveScriptingDefineSymbol);
        }

        static Helper() => EnsureDefinitions();

        private static string[] GetSavedDefinitions()
        {
            if (!File.Exists(SDFILE)) return Array.Empty<string>();
            return File.ReadAllLines(SDFILE);
        }

        private static void AddDefinition(string newLine)
        {
            if (!File.Exists(SDFILE))
            {
                File.WriteAllLines(SDFILE, new string[1]{ newLine });
                return;
            }
            List<string> currentLines = File.ReadAllLines(SDFILE).ToList();
            if(currentLines.Contains(newLine)) return;
            currentLines.Add(newLine);
            File.WriteAllLines(SDFILE, currentLines.ToArray());
        }
        
        private static void RemoveDefinition(string newLine)
        {
            if (!File.Exists(SDFILE)) return;
            List<string> currentLines = File.ReadAllLines(SDFILE).ToList();
            if(!currentLines.Contains(newLine)) return;
            currentLines.Remove(newLine);
            File.WriteAllLines(SDFILE, currentLines.ToArray());
        }
    }
}