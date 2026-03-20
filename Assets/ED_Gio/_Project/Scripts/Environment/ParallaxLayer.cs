using UnityEngine;

namespace NABHI.Environment
{
    /// <summary>
    /// Aplica efecto parallax a objetos basado en su distancia Z de la camara.
    /// Objetos mas lejanos (Z mayor) se mueven mas lento.
    /// </summary>
    public class ParallaxLayer : MonoBehaviour
    {
        [Header("Configuracion")]
        [Tooltip("Referencia a la camara (si no se asigna, usa Camera.main)")]
        [SerializeField] private Transform cameraTransform;

        [Tooltip("Factor de parallax. 0 = no se mueve, 1 = se mueve con la camara")]
        [Range(0f, 1f)]
        [SerializeField] private float parallaxFactor = 0.5f;

        [Tooltip("Calcular factor automaticamente basado en posicion Z")]
        [SerializeField] private bool autoCalculateFactor = true;

        [Tooltip("Distancia Z de referencia para calculo automatico (objetos en este Z no tienen parallax)")]
        [SerializeField] private float referenceZ = 0f;

        [Tooltip("Distancia Z maxima para el calculo (parallax maximo)")]
        [SerializeField] private float maxZ = 20f;

        [Header("Ejes")]
        [SerializeField] private bool parallaxX = true;
        [SerializeField] private bool parallaxY = true;

        [Header("Repeticion Infinita (opcional)")]
        [Tooltip("Activar para fondos que se repiten infinitamente")]
        [SerializeField] private bool infiniteScrollX = false;
        [SerializeField] private float spriteWidth = 10f;

        private Vector3 lastCameraPosition;
        private Vector3 startPosition;

        private void Start()
        {
            if (cameraTransform == null)
            {
                cameraTransform = Camera.main?.transform;
            }

            if (cameraTransform == null)
            {
                Debug.LogError("[ParallaxLayer] No se encontro camara!");
                enabled = false;
                return;
            }

            lastCameraPosition = cameraTransform.position;
            startPosition = transform.position;

            // Calcular factor automaticamente basado en Z
            if (autoCalculateFactor)
            {
                CalculateParallaxFactor();
            }
        }

        private void CalculateParallaxFactor()
        {
            // Objetos en Z = referenceZ tienen parallax = 0 (se mueven con gameplay)
            // Objetos en Z = maxZ tienen parallax cercano a 1 (casi estaticos)
            float distanceFromReference = Mathf.Abs(transform.position.z - referenceZ);
            float normalizedDistance = Mathf.Clamp01(distanceFromReference / maxZ);

            // Invertir: mas lejos = factor mas alto = se mueve menos
            parallaxFactor = normalizedDistance;
        }

        private void LateUpdate()
        {
            if (cameraTransform == null) return;

            // Calcular movimiento de la camara desde el ultimo frame
            Vector3 cameraDelta = cameraTransform.position - lastCameraPosition;

            // Calcular nueva posicion con parallax
            // parallaxFactor = 0: objeto se mueve igual que camara (no hay efecto)
            // parallaxFactor = 1: objeto no se mueve (fondo estatico)
            float moveX = parallaxX ? cameraDelta.x * parallaxFactor : 0f;
            float moveY = parallaxY ? cameraDelta.y * parallaxFactor : 0f;

            // Mover en direccion opuesta al movimiento de camara para crear efecto
            transform.position += new Vector3(moveX, moveY, 0f);

            // Scroll infinito horizontal
            if (infiniteScrollX)
            {
                HandleInfiniteScroll();
            }

            lastCameraPosition = cameraTransform.position;
        }

        private void HandleInfiniteScroll()
        {
            float relativeX = cameraTransform.position.x - transform.position.x;

            if (relativeX > spriteWidth / 2f)
            {
                transform.position += Vector3.right * spriteWidth;
            }
            else if (relativeX < -spriteWidth / 2f)
            {
                transform.position -= Vector3.right * spriteWidth;
            }
        }

        /// <summary>
        /// Resetea la posicion a la inicial
        /// </summary>
        public void ResetPosition()
        {
            transform.position = startPosition;
            if (cameraTransform != null)
            {
                lastCameraPosition = cameraTransform.position;
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Mostrar info de parallax en el editor
            Gizmos.color = Color.cyan;
            Vector3 labelPos = transform.position + Vector3.up * 2f;

            #if UNITY_EDITOR
            UnityEditor.Handles.Label(labelPos, $"Parallax: {parallaxFactor:F2}\nZ: {transform.position.z}");
            #endif
        }
    }
}
