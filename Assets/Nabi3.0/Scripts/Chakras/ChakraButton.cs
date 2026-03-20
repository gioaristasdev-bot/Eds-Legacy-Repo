using NABHI.Chakras;
using UnityEngine;

public class ChakraButton : MonoBehaviour
{
    public int chakraID;
    public ChakraSystem chakraSystem;

    public void Activate()
    {
        chakraSystem.ActivateChakra(chakraID);
    }
}
