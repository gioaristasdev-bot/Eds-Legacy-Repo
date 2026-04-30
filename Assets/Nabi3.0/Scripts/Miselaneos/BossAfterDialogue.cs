using UnityEngine;

public class BossAfterDialogue : MonoBehaviour
{
    [Header("Referencia al sistema de diálogo")]
    [SerializeField] private GameObject[] dialogueUI; // los mismos que usas

    [Header("Boss")]
    [SerializeField] private GameObject boss;

    private bool bossSpawned = false;
    private bool dialogueStarted = false;

    void Update()
    {
        if (bossSpawned) return;

        // Detectar si algún diálogo estuvo activo alguna vez
        foreach (GameObject ui in dialogueUI)
        {
            if (ui != null && ui.activeSelf)
            {
                dialogueStarted = true;
                return;
            }
        }

        // Si ya empezó y ahora todos están apagados → terminó
        if (dialogueStarted && AllDialoguesOff())
        {
            SpawnBoss();
        }
    }

    bool AllDialoguesOff()
    {
        foreach (GameObject ui in dialogueUI)
        {
            if (ui != null && ui.activeSelf)
                return false;
        }
        return true;
    }

    void SpawnBoss()
    {
        bossSpawned = true;

        if (boss != null)
            boss.SetActive(true);

        Debug.Log("🔥 Boss aparece después del diálogo");
    }
}
