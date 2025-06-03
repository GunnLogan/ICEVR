using UnityEngine;
using System.Collections.Generic;

public class ToggleManager : MonoBehaviour
{
    [Tooltip("Define one entry per 'mode' or 'screen'.")]
    public List<ToggleGroup> groups;

    /// <summary>
    /// Hook this up to your Button.OnClick(int) and pass in the index of the group
    /// you want to activate.
    /// </summary>
    public void ActivateGroup(int groupIndex)
    {
        if (groupIndex < 0 || groupIndex >= groups.Count)
        {
            Debug.LogWarning($"ToggleManager: invalid index {groupIndex}");
            return;
        }

        for (int i = 0; i < groups.Count; i++)
        {
            var g = groups[i];

            bool isActiveGroup = (i == groupIndex);

            // Turn on/turn off each list exactly as configured
            if (isActiveGroup)
            {
                foreach (var go in g.turnOn)
                    go.SetActive(true);
                foreach (var go in g.turnOff)
                    go.SetActive(false);
            }
            else
            {
                // Optional: if you want non-selected groups to fully reset:
                foreach (var go in g.turnOn)
                    go.SetActive(false);
                foreach (var go in g.turnOff)
                    go.SetActive(true);
            }
        }
    }
}
