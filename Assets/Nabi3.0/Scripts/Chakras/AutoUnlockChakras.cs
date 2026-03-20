using NABHI.Chakras.DebugTools;
using UnityEngine;

public class AutoUnlockChakras : MonoBehaviour
{
    void Start()
    {
        ChakraDebugController debug = FindObjectOfType<ChakraDebugController>();

        if (debug != null)
        {
            debug.SendMessage("UnlockAllChakras", SendMessageOptions.DontRequireReceiver);
        }
    }
}