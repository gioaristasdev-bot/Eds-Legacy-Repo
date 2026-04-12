using NABHI.Chakras;
using UnityEngine;

public class ChakraButton : MonoBehaviour
{
    public int chakraID;
    public ChakraSystem chakraSystem;

    public void Activate()
    {
        // Si chakraSystem no esta asignado, ChakraMenuUI maneja la activacion.
        if (chakraSystem == null) return;
        chakraSystem.TryActivateChakra((ChakraType)chakraID);
    }
}
