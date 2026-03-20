using UnityEngine;
using NABHI.Character;

public class DamageDealer : MonoBehaviour
{
    [SerializeField] private float damage = 10f;
    [SerializeField] private float cooldown = 1f;

    private float lastDamageTime = 0f;

    private void OnTriggerStay2D(Collider2D other)
    {
        if (Time.time - lastDamageTime < cooldown) return;

        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null && damageable.IsAlive())
        {
            damageable.TakeDamage(damage);
            lastDamageTime = Time.time;
            Debug.Log($"[DamageDealer] Causó {damage} de daño a {other.name}");
        }
    }
}
