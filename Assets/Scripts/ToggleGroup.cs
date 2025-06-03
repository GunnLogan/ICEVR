using UnityEngine;                     // for Tooltip, GameObject, Serializable
using System.Collections.Generic; 

[System.Serializable]
public class ToggleGroup
{
    [Tooltip("Friendly name for you to see in the inspector")]
    public string label;

    [Tooltip("These will be SetActive(true) when this group is selected")]
    public List<GameObject> turnOn;

    [Tooltip("These will be SetActive(false) when this group is selected")]
    public List<GameObject> turnOff;
}