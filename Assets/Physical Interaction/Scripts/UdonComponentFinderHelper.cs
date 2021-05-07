using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.Udon;

public class UdonComponentFinderHelper : MonoBehaviour
{
    public UdonBehaviour udonBehaviour;
    public string assigningArrayVariable = "";
    public string[] searchTypes;

    [Space(5)]
    [Tooltip("Check this to find the specified objects types in the scene and assign them to UdonBehaviour variable")]
    public bool checkThisToUpdateArray;

    #if UNITY_EDITOR

    void OnValidate()
    {
        if (!this.gameObject.activeSelf || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying)
            return;

        if (checkThisToUpdateArray) {
            UnityEditor.EditorApplication.delayCall += () => {
                UpdateButtons();
                checkThisToUpdateArray = false;
            };
        }
    }

    void UpdateButtons()
    {
        if (udonBehaviour == null || assigningArrayVariable.Length == 0 || searchTypes == null || searchTypes.Length == 0)
            return;

        var allSceneObjects = FindAllSceneObjects();
        var foundComponents = new List<Component>();
        
        foreach (var obj in allSceneObjects) {
            var udonBehaviours = obj.GetComponentsInChildren<UdonBehaviour>();
            foreach (var ub in udonBehaviours) {
                foreach (var typeName in searchTypes) {
                    if (ub.programSource.name == typeName)
                        foundComponents.Add(ub);
                }
            }
        }
        Debug.Log("Objects found and assigned: " + foundComponents.Count);
        udonBehaviour.publicVariables.TrySetVariableValue(assigningArrayVariable, foundComponents.ToArray());
    }

    public static List<GameObject> FindAllSceneObjects()
    {
        var allRootObjects = new List<GameObject>(1000);
        SceneManager.GetActiveScene().GetRootGameObjects(allRootObjects);
        return allRootObjects;
    }

    #endif
}
