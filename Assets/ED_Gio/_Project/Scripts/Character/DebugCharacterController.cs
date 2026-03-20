using UnityEngine;

namespace NABHI.Character
{
    /// <summary>
    /// Script de debug para diagnosticar problemas con el CharacterController2D
    /// Agregar al GameObject Player para ver información en pantalla
    /// </summary>
    [RequireComponent(typeof(CharacterController2D))]
    public class DebugCharacterController : MonoBehaviour
    {
        private CharacterController2D controller;
        private GUIStyle style;

        private void Awake()
        {
            controller = GetComponent<CharacterController2D>();
        }

        private void OnGUI()
        {
            // Configurar estilo de texto
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label);
                style.fontSize = 20;
                style.normal.textColor = Color.white;
                style.alignment = TextAnchor.UpperLeft;
            }

            // Crear background semi-transparente
            GUI.Box(new Rect(10, 10, 300, 200), "");

            // Mostrar información de debug
            string debugInfo = "=== CHARACTER DEBUG ===\n";
            debugInfo += $"Is Grounded: {controller.IsGrounded}\n";
            debugInfo += $"Is Dashing: {controller.IsDashing}\n";
            debugInfo += $"Is Wall Sliding: {controller.IsWallSliding}\n";
            debugInfo += $"Velocity: {controller.Velocity}\n";
            debugInfo += $"Facing Dir: {controller.FacingDirection}\n";
            debugInfo += $"\nPress SPACE to Jump";

            GUI.Label(new Rect(20, 20, 280, 180), debugInfo, style);
        }

        private void OnDrawGizmos()
        {
            // Dibujar indicador de grounded
            if (controller != null)
            {
                Gizmos.color = controller.IsGrounded ? Color.green : Color.red;
                Gizmos.DrawWireSphere(transform.position, 0.3f);
            }
        }
    }
}
