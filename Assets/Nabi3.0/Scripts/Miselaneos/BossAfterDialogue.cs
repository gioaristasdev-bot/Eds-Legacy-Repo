using UnityEngine;

public class BossAfterDialogue : MonoBehaviour
{
    [Header("Referencia al sistema de diálogo")]
    [SerializeField] private GameObject[] dialogueUI;

    [Header("Boss")]
    [SerializeField] private GameObject boss;

    [Header("Puerta")]
    [SerializeField] private GameObject door; // 👈 nueva referencia

    private bool bossSpawned = false;
    private bool dialogueStarted = false;

    void Update()
    {
        if (bossSpawned) return;

        // Detectar si algún diálogo estuvo activo
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

        // 👇 Aparece el boss
        if (boss != null)
            boss.SetActive(true);

        // 👇 Cerrar puerta
        Animator anim = door.GetComponent<Animator>();

        if (anim != null)
        {
            anim.SetTrigger("Close");
        }
        Debug.Log("🔥 Boss aparece y la puerta se cierra");
    }
}
