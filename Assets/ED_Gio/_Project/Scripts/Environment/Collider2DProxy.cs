using UnityEngine;

namespace NABHI.Environment
{
    /// <summary>
    /// Agrega un BoxCollider2D a un objeto 3D para sistema hibrido 2D/3D.
    /// El personaje 2D puede detectar colisiones con objetos 3D a traves de este proxy.
    /// </summary>
    [ExecuteInEditMode]
    public class Collider2DProxy : MonoBehaviour
    {
        [Header("Configuracion")]
        [Tooltip("Tipo de collider 2D a usar")]
        [SerializeField] private ColliderType colliderType = ColliderType.Box;

        [Tooltip("Sincronizar automaticamente con el Renderer 3D")]
        [SerializeField] private bool autoSyncSize = true;

        [Tooltip("Offset adicional del collider")]
        [SerializeField] private Vector2 offset = Vector2.zero;

        [Tooltip("Ajuste de tamanio manual (si autoSyncSize esta desactivado)")]
        [SerializeField] private Vector2 manualSize = Vector2.one;

        [Header("Layer")]
        [Tooltip("Layer para el collider 2D (Ground, Wall, etc)")]
        [SerializeField] private string targetLayer = "Ground";

        [Header("Debug")]
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private Color gizmoColor = new Color(0, 1, 0, 0.5f);

        public enum ColliderType
        {
            Box,
            Circle,
            Edge
        }

        private BoxCollider2D boxCollider2D;
        private CircleCollider2D circleCollider2D;
        private EdgeCollider2D edgeCollider2D;
        private Renderer meshRenderer;

        private void OnEnable()
        {
            SetupCollider();
        }

        private void OnValidate()
        {
            // Actualizar cuando cambian valores en el inspector
            if (Application.isEditor)
            {
                SetupCollider();
            }
        }

        /// <summary>
        /// Configura el collider 2D basado en el tipo seleccionado
        /// </summary>
        [ContextMenu("Setup Collider")]
        public void SetupCollider()
        {
            meshRenderer = GetComponent<Renderer>();

            // Limpiar colliders existentes si cambia el tipo
            CleanupColliders();

            switch (colliderType)
            {
                case ColliderType.Box:
                    SetupBoxCollider();
                    break;
                case ColliderType.Circle:
                    SetupCircleCollider();
                    break;
                case ColliderType.Edge:
                    SetupEdgeCollider();
                    break;
            }

            // Asignar layer
            ApplyLayer();
        }

        private void CleanupColliders()
        {
            // Solo limpiar si existe y no es del tipo correcto
            if (colliderType != ColliderType.Box && boxCollider2D != null)
            {
                if (Application.isPlaying)
                    Destroy(boxCollider2D);
                else
                    DestroyImmediate(boxCollider2D);
                boxCollider2D = null;
            }

            if (colliderType != ColliderType.Circle && circleCollider2D != null)
            {
                if (Application.isPlaying)
                    Destroy(circleCollider2D);
                else
                    DestroyImmediate(circleCollider2D);
                circleCollider2D = null;
            }

            if (colliderType != ColliderType.Edge && edgeCollider2D != null)
            {
                if (Application.isPlaying)
                    Destroy(edgeCollider2D);
                else
                    DestroyImmediate(edgeCollider2D);
                edgeCollider2D = null;
            }
        }

        private void SetupBoxCollider()
        {
            boxCollider2D = GetComponent<BoxCollider2D>();
            if (boxCollider2D == null)
            {
                boxCollider2D = gameObject.AddComponent<BoxCollider2D>();
            }

            Vector2 size = GetSize();
            boxCollider2D.size = size;
            boxCollider2D.offset = offset;
        }

        private void SetupCircleCollider()
        {
            circleCollider2D = GetComponent<CircleCollider2D>();
            if (circleCollider2D == null)
            {
                circleCollider2D = gameObject.AddComponent<CircleCollider2D>();
            }

            Vector2 size = GetSize();
            circleCollider2D.radius = Mathf.Max(size.x, size.y) / 2f;
            circleCollider2D.offset = offset;
        }

        private void SetupEdgeCollider()
        {
            edgeCollider2D = GetComponent<EdgeCollider2D>();
            if (edgeCollider2D == null)
            {
                edgeCollider2D = gameObject.AddComponent<EdgeCollider2D>();
            }

            Vector2 size = GetSize();
            // Crear linea horizontal por defecto (para suelo)
            edgeCollider2D.points = new Vector2[]
            {
                new Vector2(-size.x / 2f, 0) + offset,
                new Vector2(size.x / 2f, 0) + offset
            };
        }

        private Vector2 GetSize()
        {
            if (autoSyncSize && meshRenderer != null)
            {
                // Obtener bounds del renderer y convertir a escala local
                Bounds bounds = meshRenderer.bounds;
                Vector3 localScale = transform.lossyScale;

                // El tamanio en 2D es X e Y del mesh 3D
                return new Vector2(
                    bounds.size.x,
                    bounds.size.y
                );
            }

            return manualSize;
        }

        private void ApplyLayer()
        {
            int layer = LayerMask.NameToLayer(targetLayer);
            if (layer != -1)
            {
                gameObject.layer = layer;
            }
            else
            {
                Debug.LogWarning($"[Collider2DProxy] Layer '{targetLayer}' no existe. Crealo en Edit > Project Settings > Tags and Layers");
            }
        }

        /// <summary>
        /// Desactiva el collider 3D si existe
        /// </summary>
        [ContextMenu("Disable 3D Collider")]
        public void Disable3DCollider()
        {
            Collider col3D = GetComponent<Collider>();
            if (col3D != null)
            {
                col3D.enabled = false;
                Debug.Log($"[Collider2DProxy] Collider 3D desactivado en {gameObject.name}");
            }
        }

        /// <summary>
        /// Configura todo automaticamente: desactiva 3D, crea 2D, asigna layer
        /// </summary>
        [ContextMenu("Auto Setup Complete")]
        public void AutoSetupComplete()
        {
            Disable3DCollider();
            SetupCollider();
            Debug.Log($"[Collider2DProxy] Setup completo en {gameObject.name}");
        }

        private void OnDrawGizmosSelected()
        {
            if (!showGizmos) return;

            Gizmos.color = gizmoColor;
            Vector2 size = GetSize();
            Vector3 center = transform.position + new Vector3(offset.x, offset.y, 0);

            switch (colliderType)
            {
                case ColliderType.Box:
                    Gizmos.DrawWireCube(center, new Vector3(size.x, size.y, 0.1f));
                    break;
                case ColliderType.Circle:
                    float radius = Mathf.Max(size.x, size.y) / 2f;
                    Gizmos.DrawWireSphere(center, radius);
                    break;
                case ColliderType.Edge:
                    Gizmos.DrawLine(
                        center + Vector3.left * size.x / 2f,
                        center + Vector3.right * size.x / 2f
                    );
                    break;
            }
        }
    }
}
