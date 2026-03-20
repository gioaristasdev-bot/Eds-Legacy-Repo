using UnityEngine;
using NABHI.Character;

public class PlayerHealthAnimator : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Animator animator;

    void Start()
    {
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        if (animator == null)
        {
            // Buscar el CharacterAnimator para obtener el animator correcto
            CharacterAnimator charAnimator = GetComponent<CharacterAnimator>();
            if (charAnimator != null)
            {
                // No hacer nada, CharacterAnimator ya maneja esto
                Debug.LogWarning("[PlayerHealthAnimator] CharacterAnimator detectado. Este script no es necesario.");
                enabled = false;
                return;
            }
            else
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        // Suscribirse al evento de Hit
        if (playerHealth != null)
            playerHealth.OnHit.AddListener(OnPlayerHit);
    }

    void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnHit.RemoveListener(OnPlayerHit);
    }

    private void OnPlayerHit()
    {
        // Activar animación de Hit
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            Debug.Log("[PlayerHealthAnimator] Triggering Hit animation.");

            // Usar trigger en lugar de bool (más compatible)
            animator.SetTrigger("Hit");
        }
        else
        {
            Debug.LogWarning("[PlayerHealthAnimator] Animator no tiene AnimatorController asignado.");
        }
    }
}
