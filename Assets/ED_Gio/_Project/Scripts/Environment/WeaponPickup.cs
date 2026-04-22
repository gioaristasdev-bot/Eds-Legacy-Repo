using UnityEngine;
using NABHI.Weapons;

namespace NABHI.Environment
{
    /// <summary>
    /// Coloca este script en un GameObject con Collider2D (Is Trigger = true).
    /// Cuando el jugador lo toca, recoge el arma automáticamente y el objeto desaparece.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class WeaponPickup : MonoBehaviour
    {
        [Header("Visual (opcional)")]
        [Tooltip("Objeto visual que gira o pulsa para indicar que es recogible")]
        [SerializeField] private Transform visual;

        [Header("Rotación")]
        [SerializeField] private float rotationSpeed = 90f;

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void Update()
        {
            if (visual != null)
                visual.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            WeaponStateManager wsm = other.GetComponent<WeaponStateManager>();
            if (wsm == null)
                wsm = other.GetComponentInParent<WeaponStateManager>();

            if (wsm == null) return;

            wsm.PickupWeapon();
            Destroy(gameObject);
        }
    }
}
