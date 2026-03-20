using UnityEngine;
using System.Collections;
using NABHI.Character;
using NABHI.Chakras.Abilities;

namespace NABHI.Testing
{
    /// <summary>
    /// Enemigo de prueba para testear los chakras.
    /// Implementa IDamageable, IStunnable para Tremor y otros efectos.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class TestEnemy : MonoBehaviour, IDamageable, IStunnable
    {
        [Header("Stats")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private bool patrolEnabled = true;
        [SerializeField] private float patrolDistance = 3f;

        [Header("Visual Feedback")]
        [SerializeField] private Color normalColor = Color.red;
        [SerializeField] private Color stunnedColor = Color.yellow;
        [SerializeField] private Color damagedColor = Color.white;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;

        private SpriteRenderer spriteRenderer;
        private Rigidbody2D rb;
        private Vector2 startPosition;
        private int patrolDirection = 1;

        private bool isStunned;
        private float stunTimer;
        private Coroutine damageFlashCoroutine;

        public bool IsStunned => isStunned;

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
        }

        private void Update()
        {
            // Actualizar timer de stun
            if (isStunned)
            {
                stunTimer -= Time.deltaTime;
                if (stunTimer <= 0)
                {
                    EndStun();
                }
            }
        }

        private void FixedUpdate()
        {
            if (!isStunned && patrolEnabled && IsAlive())
            {
                Patrol();
            }
        }

        private void Patrol()
        {
            // Movimiento de patrulla simple
            Vector2 movement = Vector2.right * patrolDirection * moveSpeed;
            rb.velocity = new Vector2(movement.x, rb.velocity.y);

            // Cambiar direccion al llegar al limite
            float distanceFromStart = transform.position.x - startPosition.x;
            if (Mathf.Abs(distanceFromStart) >= patrolDistance)
            {
                patrolDirection *= -1;

                // Voltear sprite
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

            currentHealth -= damage;

            if (showDebugInfo)
            {
                Debug.Log($"[TestEnemy] {gameObject.name} recibio {damage} de dano. Salud: {currentHealth}/{maxHealth}");
            }

            // Flash de dano
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
                Debug.Log($"[TestEnemy] {gameObject.name} curado. Salud: {currentHealth}/{maxHealth}");
            }
        }

        #endregion

        #region IStunnable Implementation

        public void ApplyStun(float duration)
        {
            isStunned = true;
            stunTimer = duration;

            // Detener movimiento
            rb.velocity = Vector2.zero;

            // Cambiar color
            if (spriteRenderer != null)
            {
                spriteRenderer.color = stunnedColor;
            }

            if (showDebugInfo)
            {
                Debug.Log($"[TestEnemy] {gameObject.name} aturdido por {duration} segundos!");
            }
        }

        private void EndStun()
        {
            isStunned = false;

            if (spriteRenderer != null && IsAlive())
            {
                spriteRenderer.color = normalColor;
            }

            if (showDebugInfo)
            {
                Debug.Log($"[TestEnemy] {gameObject.name} se recupero del aturdimiento.");
            }
        }

        #endregion

        private IEnumerator DamageFlash()
        {
            if (spriteRenderer != null)
            {
                Color originalColor = isStunned ? stunnedColor : normalColor;
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
                Debug.Log($"[TestEnemy] {gameObject.name} ha muerto!");
            }

            // Cambiar color a gris
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.gray;
            }

            // Desactivar fisicas
            rb.velocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Static;

            // Opcional: destruir despues de un tiempo
            Destroy(gameObject, 2f);
        }

        /// <summary>
        /// Reiniciar el enemigo (para testing)
        /// </summary>
        public void Reset()
        {
            currentHealth = maxHealth;
            isStunned = false;
            stunTimer = 0;
            transform.position = startPosition;
            rb.bodyType = RigidbodyType2D.Dynamic;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = normalColor;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector2 pos = Application.isPlaying ? startPosition : (Vector2)transform.position;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(pos + Vector2.left * patrolDistance, pos + Vector2.right * patrolDistance);
            Gizmos.DrawWireSphere(pos + Vector2.left * patrolDistance, 0.2f);
            Gizmos.DrawWireSphere(pos + Vector2.right * patrolDistance, 0.2f);
        }
    }
}
