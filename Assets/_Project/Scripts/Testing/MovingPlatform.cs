using UnityEngine;

namespace NABHI.Testing
{
    /// <summary>
    /// Plataforma movil para testear ChakraGravityPulse.
    /// El componente SlowedObject de GravityPulse afectara automaticamente al Rigidbody2D y Animator.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class MovingPlatform : MonoBehaviour
    {
        public enum MovementType { Horizontal, Vertical, Circular, Patrol }

        [Header("Movement Type")]
        [SerializeField] private MovementType movementType = MovementType.Horizontal;

        [Header("Linear Movement")]
        [SerializeField] private float moveDistance = 3f;
        [SerializeField] private float moveSpeed = 2f;

        [Header("Circular Movement")]
        [SerializeField] private float circleRadius = 2f;
        [SerializeField] private float rotationSpeed = 1f;

        [Header("Patrol Points")]
        [SerializeField] private Transform[] patrolPoints;
        [SerializeField] private float waitTimeAtPoints = 0.5f;

        [Header("Visual")]
        [SerializeField] private Color normalColor = new Color(0.5f, 0.5f, 0.8f);
        [SerializeField] private Color slowedColor = new Color(0.3f, 0.3f, 0.5f);

        [Header("Player Transport")]
        [SerializeField] private bool transportPlayer = true;

        private Rigidbody2D rb;
        private SpriteRenderer spriteRenderer;
        private Vector2 startPosition;
        private float currentAngle;
        private int currentPatrolIndex;
        private float waitTimer;
        private float originalSpeed;

        // Para detectar si esta ralentizado
        private float lastVelocityMagnitude;
        private bool isSlowed;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            startPosition = transform.position;
            originalSpeed = moveSpeed;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = normalColor;
            }
        }

        private void FixedUpdate()
        {
            Vector2 targetVelocity = Vector2.zero;

            switch (movementType)
            {
                case MovementType.Horizontal:
                    targetVelocity = CalculateHorizontalMovement();
                    break;
                case MovementType.Vertical:
                    targetVelocity = CalculateVerticalMovement();
                    break;
                case MovementType.Circular:
                    targetVelocity = CalculateCircularMovement();
                    break;
                case MovementType.Patrol:
                    targetVelocity = CalculatePatrolMovement();
                    break;
            }

            rb.velocity = targetVelocity;

            // Detectar si esta siendo ralentizado por GravityPulse
            DetectSlowEffect();
        }

        private Vector2 CalculateHorizontalMovement()
        {
            float offset = Mathf.PingPong(Time.time * moveSpeed, moveDistance * 2) - moveDistance;
            Vector2 targetPos = startPosition + Vector2.right * offset;
            return (targetPos - (Vector2)transform.position).normalized * moveSpeed;
        }

        private Vector2 CalculateVerticalMovement()
        {
            float offset = Mathf.PingPong(Time.time * moveSpeed, moveDistance * 2) - moveDistance;
            Vector2 targetPos = startPosition + Vector2.up * offset;
            return (targetPos - (Vector2)transform.position).normalized * moveSpeed;
        }

        private Vector2 CalculateCircularMovement()
        {
            currentAngle += rotationSpeed * Time.fixedDeltaTime;
            Vector2 targetPos = startPosition + new Vector2(
                Mathf.Cos(currentAngle) * circleRadius,
                Mathf.Sin(currentAngle) * circleRadius
            );
            return (targetPos - (Vector2)transform.position) * moveSpeed;
        }

        private Vector2 CalculatePatrolMovement()
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                return Vector2.zero;
            }

            Transform targetPoint = patrolPoints[currentPatrolIndex];
            if (targetPoint == null) return Vector2.zero;

            Vector2 direction = (Vector2)targetPoint.position - (Vector2)transform.position;
            float distance = direction.magnitude;

            if (distance < 0.1f)
            {
                // Llegamos al punto, esperar y pasar al siguiente
                waitTimer += Time.fixedDeltaTime;
                if (waitTimer >= waitTimeAtPoints)
                {
                    waitTimer = 0;
                    currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                }
                return Vector2.zero;
            }

            return direction.normalized * moveSpeed;
        }

        private void DetectSlowEffect()
        {
            // Comparar velocidad actual con la esperada
            float currentMag = rb.velocity.magnitude;
            float expectedMag = moveSpeed;

            // Si la velocidad es significativamente menor, esta siendo ralentizado
            bool wasSlowed = isSlowed;
            isSlowed = currentMag < expectedMag * 0.8f && currentMag > 0.01f;

            if (isSlowed != wasSlowed)
            {
                UpdateVisual();
            }

            lastVelocityMagnitude = currentMag;
        }

        private void UpdateVisual()
        {
            if (spriteRenderer == null) return;

            spriteRenderer.color = isSlowed ? slowedColor : normalColor;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!transportPlayer) return;

            // Si el jugador aterriza encima, hacerlo hijo de la plataforma
            if (collision.gameObject.CompareTag("Player"))
            {
                // Verificar que esta encima (no al lado)
                Vector2 contactNormal = collision.GetContact(0).normal;
                if (contactNormal.y < -0.5f)
                {
                    collision.transform.SetParent(transform);
                }
            }
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            if (!transportPlayer) return;

            if (collision.gameObject.CompareTag("Player"))
            {
                collision.transform.SetParent(null);
            }
        }

        /// <summary>
        /// Cambiar velocidad de movimiento (usado para testing)
        /// </summary>
        public void SetSpeed(float newSpeed)
        {
            moveSpeed = newSpeed;
        }

        /// <summary>
        /// Resetear a velocidad original
        /// </summary>
        public void ResetSpeed()
        {
            moveSpeed = originalSpeed;
        }

        private void OnDrawGizmosSelected()
        {
            Vector2 pos = Application.isPlaying ? startPosition : (Vector2)transform.position;

            Gizmos.color = Color.cyan;

            switch (movementType)
            {
                case MovementType.Horizontal:
                    Gizmos.DrawLine(pos + Vector2.left * moveDistance, pos + Vector2.right * moveDistance);
                    Gizmos.DrawWireSphere(pos + Vector2.left * moveDistance, 0.2f);
                    Gizmos.DrawWireSphere(pos + Vector2.right * moveDistance, 0.2f);
                    break;

                case MovementType.Vertical:
                    Gizmos.DrawLine(pos + Vector2.down * moveDistance, pos + Vector2.up * moveDistance);
                    Gizmos.DrawWireSphere(pos + Vector2.down * moveDistance, 0.2f);
                    Gizmos.DrawWireSphere(pos + Vector2.up * moveDistance, 0.2f);
                    break;

                case MovementType.Circular:
                    // Dibujar circulo
                    int segments = 32;
                    Vector3 previousPoint = pos + new Vector2(circleRadius, 0);
                    for (int i = 1; i <= segments; i++)
                    {
                        float angle = (float)i / segments * Mathf.PI * 2;
                        Vector3 point = pos + new Vector2(Mathf.Cos(angle) * circleRadius, Mathf.Sin(angle) * circleRadius);
                        Gizmos.DrawLine(previousPoint, point);
                        previousPoint = point;
                    }
                    break;

                case MovementType.Patrol:
                    if (patrolPoints != null && patrolPoints.Length > 0)
                    {
                        for (int i = 0; i < patrolPoints.Length; i++)
                        {
                            if (patrolPoints[i] == null) continue;

                            Gizmos.DrawWireSphere(patrolPoints[i].position, 0.3f);

                            int nextIndex = (i + 1) % patrolPoints.Length;
                            if (patrolPoints[nextIndex] != null)
                            {
                                Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[nextIndex].position);
                            }
                        }
                    }
                    break;
            }
        }
    }
}
