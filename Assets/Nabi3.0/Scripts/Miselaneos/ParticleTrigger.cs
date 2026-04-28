using UnityEngine;

public class ParticleTrigger : MonoBehaviour
{
    public GameObject particleObject;

    public void ActivateParticles()
    {
        particleObject.SetActive(true);
    }
}
