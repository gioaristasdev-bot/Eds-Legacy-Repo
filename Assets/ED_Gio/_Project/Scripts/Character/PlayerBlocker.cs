using UnityEngine;
using System.Collections.Generic;

public class PlayerBlocker : MonoBehaviour
{
    [SerializeField] private GameObject player;

    private List<MonoBehaviour> disabledScripts = new List<MonoBehaviour>();

    public void BlockPlayer()
    {
        disabledScripts.Clear();

        MonoBehaviour[] scripts = player.GetComponentsInChildren<MonoBehaviour>();

        foreach (MonoBehaviour script in scripts)
        {
            if (script.enabled && script != this)
            {
                disabledScripts.Add(script);
                script.enabled = false;
            }
        }
    }

    public void UnblockPlayer()
    {
        foreach (MonoBehaviour script in disabledScripts)
        {
            if (script != null)
                script.enabled = true;
        }

        disabledScripts.Clear();
    }
}