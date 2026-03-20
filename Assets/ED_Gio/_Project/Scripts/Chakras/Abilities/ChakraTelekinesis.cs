using UnityEngine;
using System.Collections;
using Debug = UnityEngine.Debug;

namespace NABHI.Chakras.Abilities
{
    /// <summary>
    /// Chakra 7 (Opcion 1): Telecinesis
    /// Color: Rojo - Raiz (Confianza)
    ///
    /// Mueve objetos que bloqueen el camino o que puedan usarse como
    /// pesas en las plataformas de rompecabezas.
    /// </summary>
    public class ChakraTelekinesis : ChakraBase
    {
        [Header("Telekinesis Settings")]
        [Tooltip("Radio de deteccion de objetos movibles")]
        [SerializeField] private float grabRange = 5f;

        [Tooltip("Distancia a la que se mantiene el objeto agarrado")]
        [SerializeField] private float holdDistance = 3f;

        [Tooltip("Fuerza de movimiento del objeto")]
        [SerializeField] private float moveForce = 15f;

        [Tooltip("Velocidad de rotacion del objeto hacia el cursor")]
        [SerializeField] private float followSpeed = 10f;

        [Tooltip("Fuerza de lanzamiento")]
        [SerializeField] private float throwForce = 20f;

        [Header("Layers")]
        [SerializeField] private LayerMask movableObjectLayer;

        [Header("Visual")]
        [SerializeField] private LineRenderer telekinesisBeam;
        [SerializeField] private ParticleSystem grabParticles;
        [SerializeField] private ParticleSystem holdParticles;

        // Estado
        private TelekineticObject currentTarget;
        private TelekineticObject heldObject;
        private bool isHolding;
        private Vector2 targetPosition;

        // Propiedades
        public bool IsHolding => isHolding;
        public TelekineticObject HeldObject => heldObject;

        protected override void Awake()
        {
            base.Awake();

            // Configurar tipo
            chakraType = ChakraType.Telekinesis;
            chakraName = "Telecinesis";
            chakraColor = new Color(0.9f, 0.2f, 0.2f); // Rojo
            activationMode = ChakraActivationMode.Contextual;
            energyCostPerSecond = 8f;
            cooldown = 0.5f;

            if (telekinesisBeam != null)
                telekinesisBeam.enabled = false;
        }

        protected override void Update()
        {
            base.Update();

            // Detectar objetos cercanos
            DetectMovableObjects();

            // Actualizar posicion objetivo basada en mouse/cursor
            UpdateTargetPosition();

            // Si esta sosteniendo un objeto, moverlo
            if (isHolding && heldObject != null)
            {
                MoveHeldObject();
                UpdateBeam();
            }

            // Input para soltar/lanzar
            if (isHolding && Input.GetMouseButtonDown(1)) // Click derecho para lanzar
            {
                ThrowObject();
            }
        }

        private void DetectMovableObjects()
        {
            if (isHolding) return;

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, grabRange, movableObjectLayer);

            if (hits.Length > 0)
            {
                float closestDist = float.MaxValue;
                TelekineticObject closest = null;

                foreach (var hit in hits)
                {
                    var teleObj = hit.GetComponent<TelekineticObject>();
                    if (teleObj != null && teleObj.CanBeGrabbed)
                    {
                        float dist = Vector2.Distance(transform.position, hit.transform.position);
                        if (dist < closestDist)
                        {
                            closestDist = dist;
                            closest = teleObj;
                        }
                    }
                }

                currentTarget = closest;
            }
            else
            {
                currentTarget = null;
            }
        }

        private void UpdateTargetPosition()
        {
            // Calcular posicion objetivo basada en la direccion del mouse
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0;

            Vector2 direction = (mouseWorldPos - transform.position).normalized;
            targetPosition = (Vector2)transform.position + direction * holdDistance;
        }

        private void MoveHeldObject()
        {
            if (heldObject == null) return;

            // Mover el objeto hacia la posicion objetivo
            Vector2 currentPos = heldObject.transform.position;
            Vector2 direction = targetPosition - currentPos;

            if (heldObject.Rigidbody != null)
            {
                // Usar fisica suave
                heldObject.Rigidbody.velocity = direction * followSpeed;
            }
            else
            {
                // Mover directamente
                heldObject.transform.position = Vector2.Lerp(currentPos, targetPosition, followSpeed * Time.deltaTime);
            }
        }

        private void UpdateBeam()
        {
            if (telekinesisBeam == null || heldObject == null) return;

            telekinesisBeam.enabled = true;
            telekinesisBeam.SetPosition(0, transform.position);
            telekinesisBeam.SetPosition(1, heldObject.transform.position);
        }

        public override bool HasValidTarget()
        {
            return currentTarget != null || isHolding;
        }

        public override bool CanActivate()
        {
            if (isHolding) return true; // Puede desactivar para soltar

            if (!base.CanActivate())
                return false;

            return currentTarget != null;
        }

        protected override void OnActivate()
        {
            if (currentTarget != null)
            {
                GrabObject(currentTarget);
            }
        }

        private void GrabObject(TelekineticObject obj)
        {
            heldObject = obj;
            isHolding = true;

            obj.OnGrabbed();

            // Efecto visual
            if (grabParticles != null)
            {
                grabParticles.transform.position = obj.transform.position;
                grabParticles.Play();
            }

            if (holdParticles != null)
            {
                holdParticles.Play();
            }

            Debug.Log($"[ChakraTelekinesis] Agarrado: {obj.name}");
        }

        private void ReleaseObject()
        {
            if (heldObject != null)
            {
                heldObject.OnReleased();
                Debug.Log($"[ChakraTelekinesis] Soltado: {heldObject.name}");
            }

            heldObject = null;
            isHolding = false;

            if (telekinesisBeam != null)
                telekinesisBeam.enabled = false;

            if (holdParticles != null)
                holdParticles.Stop();
        }

        private void ThrowObject()
        {
            if (heldObject == null) return;

            // Calcular direccion de lanzamiento hacia el mouse
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0;

            Vector2 throwDirection = ((Vector2)mouseWorldPos - (Vector2)heldObject.transform.position).normalized;

            // Lanzar
            if (heldObject.Rigidbody != null)
            {
                heldObject.Rigidbody.velocity = throwDirection * throwForce;
            }

            heldObject.OnThrown(throwDirection * throwForce);

            Debug.Log($"[ChakraTelekinesis] Lanzado: {heldObject.name}");

            ReleaseObject();
            Deactivate();
        }

        protected override void OnDeactivate()
        {
            ReleaseObject();
        }

        protected override string GetChakraDescription()
        {
            return "Mueve objetos con la mente para resolver puzzles y desbloquear caminos.";
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = chakraColor;
            Gizmos.DrawWireSphere(transform.position, grabRange);

            if (currentTarget != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, currentTarget.transform.position);
            }

            if (isHolding)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(targetPosition, 0.3f);
            }
        }
    }

    /// <summary>
    /// Componente para objetos que pueden ser movidos con telecinesis
    /// </summary>
    public class TelekineticObject : MonoBehaviour
    {
        [Header("Configuracion")]
        [SerializeField] private float weight = 1f;
        [SerializeField] private bool canBeThrownAtEnemies = true;
        [SerializeField] private int throwDamage = 20;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color highlightColor = Color.yellow;

        private Rigidbody2D rb;
        private Color originalColor;
        private bool isBeingHeld;

        public bool CanBeGrabbed => !isBeingHeld;
        public float Weight => weight;
        public Rigidbody2D Rigidbody => rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (spriteRenderer != null)
                originalColor = spriteRenderer.color;
        }

        public void OnGrabbed()
        {
            isBeingHeld = true;

            if (rb != null)
            {
                rb.gravityScale = 0;
                rb.drag = 5f;
            }

            if (spriteRenderer != null)
                spriteRenderer.color = highlightColor;
        }

        public void OnReleased()
        {
            isBeingHeld = false;

            if (rb != null)
            {
                rb.gravityScale = 1;
                rb.drag = 0;
            }

            if (spriteRenderer != null)
                spriteRenderer.color = originalColor;
        }

        public void OnThrown(Vector2 velocity)
        {
            OnReleased();

            if (canBeThrownAtEnemies)
            {
                // Activar deteccion de colision para dano
                StartCoroutine(EnableThrowDamage());
            }
        }

        private IEnumerator EnableThrowDamage()
        {
            float damageTime = 0.5f;
            float elapsed = 0;

            while (elapsed < damageTime)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!canBeThrownAtEnemies) return;

            // Si esta en movimiento rapido, aplicar dano
            if (rb != null && rb.velocity.magnitude > 5f)
            {
                var damageable = collision.gameObject.GetComponent<Character.IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(throwDamage);
                    Debug.Log($"[TelekineticObject] Golpeo a {collision.gameObject.name} por {throwDamage} dano");
                }
            }
        }
    }
}
