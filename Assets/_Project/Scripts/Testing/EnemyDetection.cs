using UnityEngine;

namespace NABHI.Testing
{
    /// <summary>
    /// Sistema de deteccion para enemigos.
    /// Respeta la invisibilidad del jugador verificando el layer "InvisiblePlayer".
    /// Usar con TestEnemy o ElectronicEnemy para probar ChakraInvisibility.
    /// </summary>
    public class EnemyDetection : MonoBehaviour
    {
        [Header("Detection Settings")]
        [SerializeField] private float detectionRadius = 5f;
        [SerializeField] private float detectionAngle = 90f;
        [SerializeField] private LayerMask playerLayer;
        [SerializeField] private LayerMask obstacleLayer;

        [Header("Invisible Layer")]
        [Tooltip("Nombre del layer que usa el jugador cuando es invisible")]
        [SerializeField] private string invisibleLayerName = "InvisiblePlayer";

        [Header("Behavior")]
        [SerializeField] private bool requireLineOfSight = true;
        [SerializeField] private float alertDuration = 3f;

        [Header("Visual Feedback")]
        [SerializeField] private Color idleColor = Color.green;
        [SerializeField] private Color alertColor = Color.red;
        [SerializeField] private Color searchingColor = Color.yellow;
        [SerializeField] private SpriteRenderer detectionIndicator;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool drawGizmos = true;

        private Transform detectedPlayer;
        private Vector2 lastKnownPosition;
        private float alertTimer;
        private int invisibleLayerIndex;

        public enum DetectionState { Idle, Alert, Searching }
        public DetectionState CurrentState { get; private set; } = DetectionState.Idle;

        public bool HasDetectedPlayer => detectedPlayer != null;
        public Transform DetectedPlayer => detectedPlayer;
        public Vector2 LastKnownPosition => lastKnownPosition;

        // Eventos
        public System.Action<Transform> OnPlayerDetected;
        public System.Action OnPlayerLost;
        public System.Action OnSearchEnded;

        private void Awake()
        {
            invisibleLayerIndex = LayerMask.NameToLayer(invisibleLayerName);

            if (invisibleLayerIndex == -1)
            {
                Debug.LogWarning($"[EnemyDetection] Layer '{invisibleLayerName}' no encontrado. " +
                    "Crea este layer para que la invisibilidad funcione correctamente.");
            }
        }

        private void Update()
        {
            switch (CurrentState)
            {
                case DetectionState.Idle:
                    ScanForPlayer();
                    break;

                case DetectionState.Alert:
                    TrackPlayer();
                    break;

                case DetectionState.Searching:
                    SearchForPlayer();
                    break;
            }

            UpdateVisual();
        }

        private void ScanForPlayer()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius, playerLayer);

            foreach (var hit in hits)
            {
                // Verificar si el jugador es invisible
                if (IsTargetInvisible(hit.gameObject))
                {
                    continue;
                }

                // Verificar angulo de vision
                if (!IsInFieldOfView(hit.transform.position))
                {
                    continue;
                }

                // Verificar linea de vision
                if (requireLineOfSight && !HasLineOfSight(hit.transform.position))
                {
                    continue;
                }

                // Jugador detectado!
                DetectPlayer(hit.transform);
                return;
            }
        }

        private void TrackPlayer()
        {
            if (detectedPlayer == null)
            {
                LosePlayer();
                return;
            }

            // Verificar si el jugador se volvio invisible
            if (IsTargetInvisible(detectedPlayer.gameObject))
            {
                LosePlayer();
                return;
            }

            // Verificar si salio del rango
            float distance = Vector2.Distance(transform.position, detectedPlayer.position);
            if (distance > detectionRadius)
            {
                LosePlayer();
                return;
            }

            // Verificar linea de vision
            if (requireLineOfSight && !HasLineOfSight(detectedPlayer.position))
            {
                LosePlayer();
                return;
            }

            // Actualizar ultima posicion conocida
            lastKnownPosition = detectedPlayer.position;
        }

        private void SearchForPlayer()
        {
            alertTimer -= Time.deltaTime;

            // Intentar re-detectar
            ScanForPlayer();

            if (alertTimer <= 0)
            {
                EndSearch();
            }
        }

        private bool IsTargetInvisible(GameObject target)
        {
            if (invisibleLayerIndex == -1) return false;
            return target.layer == invisibleLayerIndex;
        }

        private bool IsInFieldOfView(Vector2 targetPosition)
        {
            Vector2 directionToTarget = (targetPosition - (Vector2)transform.position).normalized;
            Vector2 forward = transform.right; // Asumiendo que "right" es la direccion de vision

            float angle = Vector2.Angle(forward, directionToTarget);
            return angle <= detectionAngle / 2f;
        }

        private bool HasLineOfSight(Vector2 targetPosition)
        {
            Vector2 direction = targetPosition - (Vector2)transform.position;
            float distance = direction.magnitude;

            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction.normalized, distance, obstacleLayer);

            return hit.collider == null;
        }

        private void DetectPlayer(Transform player)
        {
            detectedPlayer = player;
            lastKnownPosition = player.position;
            CurrentState = DetectionState.Alert;

            if (showDebugInfo)
            {
                Debug.Log($"[EnemyDetection] {gameObject.name} detecto al jugador!");
            }

            OnPlayerDetected?.Invoke(player);
        }

        private void LosePlayer()
        {
            lastKnownPosition = detectedPlayer != null ? (Vector2)detectedPlayer.position : lastKnownPosition;
            detectedPlayer = null;
            CurrentState = DetectionState.Searching;
            alertTimer = alertDuration;

            if (showDebugInfo)
            {
                Debug.Log($"[EnemyDetection] {gameObject.name} perdio al jugador. Buscando...");
            }

            OnPlayerLost?.Invoke();
        }

        private void EndSearch()
        {
            CurrentState = DetectionState.Idle;

            if (showDebugInfo)
            {
                Debug.Log($"[EnemyDetection] {gameObject.name} termino la busqueda.");
            }

            OnSearchEnded?.Invoke();
        }

        private void UpdateVisual()
        {
            if (detectionIndicator == null) return;

            switch (CurrentState)
            {
                case DetectionState.Idle:
                    detectionIndicator.color = idleColor;
                    break;
                case DetectionState.Alert:
                    detectionIndicator.color = alertColor;
                    break;
                case DetectionState.Searching:
                    // Parpadeo amarillo
                    float alpha = Mathf.PingPong(Time.time * 3f, 1f);
                    detectionIndicator.color = new Color(searchingColor.r, searchingColor.g, searchingColor.b, alpha);
                    break;
            }
        }

        /// <summary>
        /// Forzar deteccion del jugador (para testing)
        /// </summary>
        public void ForceDetect(Transform player)
        {
            DetectPlayer(player);
        }

        /// <summary>
        /// Resetear estado de deteccion
        /// </summary>
        public void ResetDetection()
        {
            detectedPlayer = null;
            CurrentState = DetectionState.Idle;
            alertTimer = 0;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos) return;

            // Dibujar radio de deteccion
            Gizmos.color = new Color(idleColor.r, idleColor.g, idleColor.b, 0.3f);
            Gizmos.DrawWireSphere(transform.position, detectionRadius);

            // Dibujar cono de vision
            Vector3 forward = transform.right;
            float halfAngle = detectionAngle / 2f;

            Vector3 leftBoundary = Quaternion.Euler(0, 0, halfAngle) * forward * detectionRadius;
            Vector3 rightBoundary = Quaternion.Euler(0, 0, -halfAngle) * forward * detectionRadius;

            Gizmos.color = alertColor;
            Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
            Gizmos.DrawLine(transform.position, transform.position + rightBoundary);

            // Dibujar arco
            int segments = 20;
            Vector3 previousPoint = transform.position + leftBoundary;
            for (int i = 1; i <= segments; i++)
            {
                float angle = halfAngle - (detectionAngle * i / segments);
                Vector3 point = transform.position + Quaternion.Euler(0, 0, angle) * forward * detectionRadius;
                Gizmos.DrawLine(previousPoint, point);
                previousPoint = point;
            }

            // Dibujar ultima posicion conocida
            if (Application.isPlaying && CurrentState == DetectionState.Searching)
            {
                Gizmos.color = searchingColor;
                Gizmos.DrawWireSphere(lastKnownPosition, 0.5f);
            }
        }
    }
}
