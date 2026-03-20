using UnityEngine;
using System.Collections;
using NABHI.Character;
using NABHI.Chakras.Abilities;

namespace NABHI.Testing
{
    /// <summary>
    /// Enemigo electronico para testear el chakra EMP.
    /// Implementa IDamageable, IStunnable e IEMPTarget.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class ElectronicEnemy : MonoBehaviour, IDamageable, IStunnable, IEMPTarget
    {
        [Header("Stats")]
        [SerializeField] private float maxHealth = 80f;
        [SerializeField] private float currentHealth;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private bool patrolEnabled = true;
        [SerializeField] private float patrolDistance = 4f;

        [Header("Visual Feedback")]
        [SerializeField] private Color normalColor = new Color(0.2f, 0.6f, 1f); // Azul electrico
        [SerializeField] private Color stunnedColor = Color.yellow;
        [SerializeField] private Color empDisabledColor = new Color(0.3f, 0.3f, 0.3f); // Gris apagado
        [SerializeField] private Color damagedColor = Color.white;

        [Header("EMP Effects")]
        [SerializeField] private ParticleSystem sparkParticles;
        [SerializeField] private GameObject electricAuraEffect;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;

        private SpriteRenderer spriteRenderer;
        private Rigidbody2D rb;
        private Vector2 startPosition;
        private int patrolDirection = 1;

        private bool isStunned;
        private float stunTimer;
        private bool isDisabledByEMP;
        private float empTimer;
        private Coroutine damageFlashCoroutine;

        public bool IsStunned => isStunned;
        public bool IsDisabledByEMP => isDisabledByEMP;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            currentHealth = maxHealth;
            startPosition = transform.position;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = normalColor;
            }

            // Activar aura electrica si existe
            if (electricAuraEffect != null)
            {
                electricAuraEffect.SetActive(true);
            }
        }

        private void Update()
        {
            // Actualizar timer de stun
            if (isStunned && !isDisabledByEMP)
            {
                stunTimer -= Time.deltaTime;
                if (stunTimer <= 0)
                {
                    EndStun();
                }
            }

            // Actualizar timer de EMP
            if (isDisabledByEMP)
            {
                empTimer -= Time.deltaTime;
                if (empTimer <= 0)
                {
                    EndEMPEffect();
                }
            }
        }

        private void FixedUpdate()
        {
            if (!isStunned && !isDisabledByEMP && patrolEnabled && IsAlive())
            {
                Patrol();
            }
        }

        private void Patrol()
        {
            Vector2 movement = Vector2.right * patrolDirection * moveSpeed;
            rb.velocity = new Vector2(movement.x, rb.velocity.y);

            float distanceFromStart = transform.position.x - startPosition.x;
            if (Mathf.Abs(distanceFromStart) >= patrolDistance)
            {
                patrolDirection *= -1;

                if (spriteRenderer != null)
                {
                    spriteRenderer.flipX = patrolDirection < 0;
                }
            }
        }

        #region IDamageable Implementation

        public void TakeDamage(float damage)
        {
            if (!IsAlive()) return;

            // Los enemigos electronicos reciben mas dano mientras estan afectados por EMP
            if (isDisabledByEMP)
            {
                damage *= 1.5f;
            }

            currentHealth -= damage;

            if (showDebugInfo)
            {
                Debug.Log($"[ElectronicEnemy] {gameObject.name} recibio {damage} de dano. Salud: {currentHealth}/{maxHealth}");
            }

            if (damageFlashCoroutine != null)
            {
                StopCoroutine(damageFlashCoroutine);
            }
            damageFlashCoroutine = StartCoroutine(DamageFlash());

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        public bool IsAlive()
        {
            return currentHealth > 0;
        }

        public void Heal(float amount)
        {
            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);

            if (showDebugInfo)
            {
                Debug.Log($"[ElectronicEnemy] {gameObject.name} reparado. Salud: {currentHealth}/{maxHealth}");
            }
        }

        #endregion

        #region IStunnable Implementation

        public void ApplyStun(float duration)
        {
            if (isDisabledByEMP) return; // EMP tiene prioridad

            isStunned = true;
            stunTimer = duration;

            rb.velocity = Vector2.zero;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = stunnedColor;
            }

            if (showDebugInfo)
            {
                Debug.Log($"[ElectronicEnemy] {gameObject.name} aturdido por {duration} segundos!");
            }
        }

        private void EndStun()
        {
            isStunned = false;

            if (spriteRenderer != null && IsAlive() && !isDisabledByEMP)
            {
                spriteRenderer.color = normalColor;
            }

            if (showDebugInfo)
            {
                Debug.Log($"[ElectronicEnemy] {gameObject.name} se recupero del aturdimiento.");
            }
        }

        #endregion

        #region IEMPTarget Implementation

        public void ApplyEMPEffect(float duration)
        {
            isDisabledByEMP = true;
            isStunned = true; // EMP tambien causa stun
            empTimer = duration;

            rb.velocity = Vector2.zero;

            // Cambiar visual a "apagado"
            if (spriteRenderer != null)
            {
                spriteRenderer.color = empDisabledColor;
            }

            // Desactivar aura electrica
            if (electricAuraEffect != null)
            {
                electricAuraEffect.SetActive(false);
            }

            // Mostrar chispas
            if (sparkParticles != null)
            {
                sparkParticles.Play();
            }

            if (showDebugInfo)
            {
                Debug.Log($"[ElectronicEnemy] {gameObject.name} deshabilitado por EMP durante {duration} segundos!");
            }
        }

        private void EndEMPEffect()
        {
            isDisabledByEMP = false;
            isStunned = false;

            if (spriteRenderer != null && IsAlive())
            {
                spriteRenderer.color = normalColor;
            }

            // Reactivar aura electrica
            if (electricAuraEffect != null)
            {
                electricAuraEffect.SetActive(true);
            }

            if (showDebugInfo)
            {
                Debug.Log($"[ElectronicEnemy] {gameObject.name} se recupero del efecto EMP.");
            }
        }

        #endregion

        private IEnumerator DamageFlash()
        {
            if (spriteRenderer != null)
            {
                Color originalColor = isDisabledByEMP ? empDisabledColor : (isStunned ? stunnedColor : normalColor);
                spriteRenderer.color = damagedColor;
                yield return new WaitForSeconds(0.1f);

                if (IsAlive())
                {
                    spriteRenderer.color = originalColor;
                }
            }
        }

        private void Die()
        {
            if (showDebugInfo)
            {
                Debug.Log($"[ElectronicEnemy] {gameObject.name} ha sido destruido!");
            }

            // Explosion de chispas
            if (sparkParticles != null)
            {
                var emission = sparkParticles.emission;
                emission.rateOverTime = 50;
                sparkParticles.Play();
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.gray;
            }

            if (electricAuraEffect != null)
            {
                electricAuraEffect.SetActive(false);
            }

            rb.velocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Static;

            Destroy(gameObject, 2f);
        }

        public void Reset()
        {
            currentHealth = maxHealth;
            isStunned = false;
            isDisabledByEMP = false;
            stunTimer = 0;
            empTimer = 0;
            transform.position = startPosition;
            rb.bodyType = RigidbodyType2D.Dynamic;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = normalColor;
            }

            if (electricAuraEffect != null)
            {
                electricAuraEffect.SetActive(true);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector2 pos = Application.isPlaying ? startPosition : (Vector2)transform.position;

            Gizmos.color = new Color(0.2f, 0.6f, 1f);
            Gizmos.DrawLine(pos + Vector2.left * patrolDistance, pos + Vector2.right * patrolDistance);
            Gizmos.DrawWireSphere(pos + Vector2.left * patrolDistance, 0.2f);
            Gizmos.DrawWireSphere(pos + Vector2.right * patrolDistance, 0.2f);
        }
    }
}
