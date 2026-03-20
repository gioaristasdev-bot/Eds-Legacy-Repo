using UnityEngine;

namespace NABHI.Enemies
{
    /// <summary>
    /// Hurtbox separada del collider físico del enemigo (Sistema Unificado de Daño).
    ///
    /// CONFIGURACIÓN EN UNITY:
    ///   1. Crear child GameObject llamado "Hurtbox" dentro del enemigo.
    ///   2. Agregar CircleCollider2D (o CapsuleCollider2D). Is Trigger = ON.
    ///   3. Agregar este script al mismo child.
    ///   4. Asignar el mismo layer que el enemigo padre ("Enemies").
    ///   5. En el EnemyBase padre, arrastrar este collider al campo "hurtboxCollider".
    ///
    /// El proyectil del jugador (Projectile.cs) detecta la capa "Enemies" en este
    /// collider trigger y llama TakeDamage al IDamageable padre vía GetComponentInParent.
    ///
    /// BENEFICIOS:
    ///   - El collider principal maneja la física (suelo, plataformas) sin ser trigger.
    ///   - La hurtbox puede tener tamaño/forma distinto al collider de física.
    ///   - Permite hitbox justa e independiente del cuerpo de colisión.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class EnemyHurtbox : MonoBehaviour
    {
        private Collider2D col;

        private void Awake()
        {
            col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        #region GIZMOS

        private void OnDrawGizmosSelected()
        {
            if (col == null)
                col = GetComponent<Collider2D>();
            if (col == null) return;

            Gizmos.color = new Color(0f, 1f, 0.4f, 0.35f);

            if (col is CircleCollider2D circle)
            {
                Gizmos.DrawWireSphere(transform.position, circle.radius * transform.lossyScale.x);
            }
            else if (col is CapsuleCollider2D capsule)
            {
                Vector2 size = capsule.size * (Vector2)transform.lossyScale;
                // Dibujar bounds aproximados con cubo
                Gizmos.DrawWireCube(transform.position + (Vector3)capsule.offset, size);
            }
            else
            {
                Gizmos.DrawWireCube(transform.position, col.bounds.size);
            }
        }

        #endregion
    }
}
