using UnityEngine;

namespace NABHI.Chakras
{
    /// <summary>
    /// Zona que habilita chakras específicos mientras el jugador esté dentro.
    /// Añadir a un GameObject con Collider2D (trigger activado).
    /// Al entrar: habilita los chakras configurados.
    /// Al salir: los deshabilita y desactiva si estaban en uso.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ChakraZone : MonoBehaviour
    {
        [Header("Chakras de esta Zona")]
        [Tooltip("Chakras que el jugador puede usar mientras esté dentro de la zona")]
        [SerializeField] private ChakraType[] chakras;

        [Header("Configuración")]
        [SerializeField] private string playerTag = "Player";

        private ChakraSystem chakraSystem;

        private void Awake()
        {
            // Asegurar que el collider sea trigger
            var col = GetComponent<Collider2D>();
            if (!col.isTrigger)
            {
                col.isTrigger = true;
                Debug.LogWarning($"[ChakraZone] {gameObject.name}: Collider2D forzado a isTrigger=true");
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Debug.Log($"[ChakraZone] {gameObject.name}: Trigger Enter → {other.gameObject.name} (tag: {other.tag})");

            if (!other.CompareTag(playerTag)) return;

            if (chakraSystem == null)
                chakraSystem = other.GetComponentInParent<ChakraSystem>()
                            ?? FindObjectOfType<ChakraSystem>();

            if (chakraSystem == null)
            {
                Debug.LogWarning($"[ChakraZone] {gameObject.name}: No se encontró ChakraSystem en la escena");
                return;
            }

            Debug.Log($"[ChakraZone] {gameObject.name}: Habilitando {chakras.Length} chakra(s)");
            chakraSystem.EnableZoneChakras(chakras);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag(playerTag)) return;
            Debug.Log($"[ChakraZone] {gameObject.name}: Jugador salió, deshabilitando chakras");
            chakraSystem?.DisableZoneChakras(chakras);
        }

        #region Gizmos

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.6f, 0f, 1f, 0.15f);
            DrawZoneGizmo();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.6f, 0f, 1f, 0.4f);
            DrawZoneGizmo();

            // Etiqueta con chakras configurados
#if UNITY_EDITOR
            if (chakras != null && chakras.Length > 0)
            {
                string label = "Chakras:\n";
                foreach (var c in chakras)
                    label += $"  · {c}\n";

                UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, label);
            }
#endif
        }

        private void DrawZoneGizmo()
        {
            var col = GetComponent<Collider2D>();
            if (col == null) return;

            if (col is BoxCollider2D box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.offset, box.size);
                Gizmos.color *= new Color(1, 1, 1, 2.5f); // borde más opaco
                Gizmos.DrawWireCube(box.offset, box.size);
            }
            else if (col is CircleCollider2D circle)
            {
                Gizmos.DrawSphere(transform.position + (Vector3)(circle.offset), circle.radius);
            }
        }

        #endregion
    }
}
