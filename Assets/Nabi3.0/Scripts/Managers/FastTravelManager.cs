using System.Collections.Generic;
using UnityEngine;

public class FastTravelManager : MonoBehaviour
{
    public static FastTravelManager Instance;

    private List<PortalFastTravel> unlockedPortals =
        new List<PortalFastTravel>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void UnlockPortal(PortalFastTravel portal)
    {
        if (!unlockedPortals.Contains(portal))
        {
            unlockedPortals.Add(portal);

            Debug.Log("Portal desbloqueado: " + portal.portalName);
        }
    }

    public List<PortalFastTravel> GetUnlockedPortals()
    {
        return unlockedPortals;
    }

    public void TeleportPlayer(Transform player, Transform destination)
    {
        player.position = destination.position;
    }
}
