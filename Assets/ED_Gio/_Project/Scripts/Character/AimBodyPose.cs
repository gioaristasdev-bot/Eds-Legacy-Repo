using UnityEngine;

namespace NABHI.Weapons
{
    /// <summary>
    /// Ajusta la pose del cuerpo segun la direccion de apuntado, de forma procedural
    /// y sin clips nuevos. Tres efectos, todos proporcionales al aim:
    ///
    ///  - Inclinacion de torso y cabeza segun la componente vertical.
    ///  - Agachado en tierra al apuntar abajo (direcciones 6 y 8 de la referencia):
    ///    baja la cadera y, como las piernas van por IK con los pies clavados en sus
    ///    targets, las rodillas se doblan solas.
    ///  - Recogida en el aire al apuntar abajo (direccion 7): sube los targets de pie
    ///    hacia la cadera, encogiendo las piernas.
    ///
    /// Corre en LateUpdate con orden -50: DESPUES del Animator, que ya escribio la
    /// pose del clip, y ANTES de IKManager2D (orden -10), para que brazos y piernas
    /// resuelvan contra el cuerpo ya ajustado y no contra el del frame anterior.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class AimBodyPose : MonoBehaviour
    {
        /// <summary>
        /// Desplaza el eje Y local de un hueso sumando un offset, deshaciendo antes el
        /// del frame anterior si detecta que el Animator no reescribio el hueso.
        ///
        /// Sin esto, un hueso que algun clip no anime acumularia el offset frame a
        /// frame hasta deformar al personaje. Con esto da igual la cobertura del clip.
        /// </summary>
        private sealed class SafeOffsetY
        {
            private float lastWritten;
            private float lastOffset;
            private bool has;

            public void Apply(Transform t, float offset)
            {
                if (t == null) return;

                Vector3 local = t.localPosition;
                float baseY = local.y;

                // Si el valor sigue siendo exactamente el que dejamos, nadie lo toco.
                if (has && baseY == lastWritten)
                    baseY -= lastOffset;

                float newY = baseY + offset;
                t.localPosition = new Vector3(local.x, newY, local.z);

                lastWritten = newY;
                lastOffset = offset;
                has = true;
            }
        }

        #region REFERENCIAS

        [Header("Referencias")]
        [SerializeField] private AimController aimController;
        [SerializeField] private NABHI.Character.CharacterController2D characterController;

        [Tooltip("Hueso Dorsal (columna alta). Animado en los 39 clips.")]
        [SerializeField] private Transform dorsal;

        [Tooltip("Hueso Head. Animado en los 39 clips.")]
        [SerializeField] private Transform head;

        [Tooltip("Hueso Weist (cadera). Se baja para agacharse.")]
        [SerializeField] private Transform hips;

        [Header("Targets IK de las piernas")]
        [Tooltip("IK L leg/New LimbSolver2D (1)_Target")]
        [SerializeField] private Transform footTargetL;

        [Tooltip("IK R leg/IK R leg_Target")]
        [SerializeField] private Transform footTargetR;

        #endregion

        #region INCLINACION

        [Header("Inclinacion del torso")]
        [Range(0f, 40f)] [SerializeField] private float leanUpDegrees = 14f;
        [Range(0f, 40f)] [SerializeField] private float leanDownDegrees = 10f;
        [Range(0f, 1f)]  [SerializeField] private float dorsalShare = 0.65f;
        [Range(0f, 1f)]  [SerializeField] private float headShare = 0.35f;
        [Range(1f, 40f)] [SerializeField] private float leanSmoothing = 14f;
        [SerializeField] private bool enableLean = true;

        #endregion

        #region AGACHADO EN TIERRA

        [Header("Agachado (en tierra, apuntando abajo)")]
        [Range(-1f, 0f)] [SerializeField] private float crouchAimThreshold = -0.35f;
        [Range(0f, 5f)]  [SerializeField] private float crouchDropUnits = 2f;
        [Range(1f, 40f)] [SerializeField] private float crouchSmoothing = 12f;
        [SerializeField] private bool enableCrouch = true;

        #endregion

        #region RECOGIDA EN EL AIRE

        [Header("Recogida (en el aire, apuntando abajo)")]
        [Tooltip("Por debajo de este aim.y empiezan a encogerse las piernas.")]
        [Range(-1f, 0f)] [SerializeField] private float tuckAimThreshold = -0.35f;

        [Tooltip("Cuanto suben los targets de pie hacia la cadera, en unidades de rig.")]
        [Range(0f, 6f)] [SerializeField] private float tuckRiseUnits = 2.5f;

        [Tooltip("Diferencia entre las dos piernas, para que no se recojan como un bloque.")]
        [Range(0f, 1f)] [SerializeField] private float tuckAsymmetry = 0.3f;

        [Tooltip("Grados extra de curvatura del torso al recogerse en el aire.")]
        [Range(0f, 30f)] [SerializeField] private float tuckExtraCurlDegrees = 8f;

        [Range(1f, 40f)] [SerializeField] private float tuckSmoothing = 12f;
        [SerializeField] private bool enableAirTuck = true;

        #endregion

        private float currentLean;
        private float currentDrop;
        private float currentTuck;

        private readonly SafeOffsetY hipOffset = new SafeOffsetY();
        private readonly SafeOffsetY footOffsetL = new SafeOffsetY();
        private readonly SafeOffsetY footOffsetR = new SafeOffsetY();

        private void Reset()
        {
            aimController = GetComponent<AimController>();
            characterController = GetComponent<NABHI.Character.CharacterController2D>();
        }

        private void Awake()
        {
            if (aimController == null) aimController = GetComponent<AimController>();
            if (characterController == null)
                characterController = GetComponent<NABHI.Character.CharacterController2D>();

            if (aimController == null)
                Debug.LogWarning("[AimBodyPose] Sin AimController: pose de apuntado desactivada.");
        }

        private void LateUpdate()
        {
            if (aimController == null) return;

            // aim.y esta en espacio de mundo: vale igual mirando a izquierda o derecha.
            float vertical = Mathf.Clamp(aimController.AimDirection.y, -1f, 1f);
            bool grounded = characterController == null || characterController.IsGrounded;

            UpdateTargets(vertical, grounded);
            ApplyLean();
            ApplyCrouch();
            ApplyAirTuck();
        }

        private void UpdateTargets(float vertical, bool grounded)
        {
            float dt = Time.deltaTime;

            // --- inclinacion ---
            float leanTarget = 0f;
            if (enableLean)
                leanTarget = vertical >= 0f ? vertical * leanUpDegrees : vertical * leanDownDegrees;
            currentLean = Mathf.Lerp(currentLean, leanTarget, 1f - Mathf.Exp(-leanSmoothing * dt));

            // --- agachado: solo en tierra ---
            float dropTarget = 0f;
            if (enableCrouch && grounded && vertical < crouchAimThreshold)
                dropTarget = Mathf.InverseLerp(crouchAimThreshold, -1f, vertical) * crouchDropUnits;
            currentDrop = Mathf.Lerp(currentDrop, dropTarget, 1f - Mathf.Exp(-crouchSmoothing * dt));

            // --- recogida: solo en el aire ---
            float tuckTarget = 0f;
            if (enableAirTuck && !grounded && vertical < tuckAimThreshold)
                tuckTarget = Mathf.InverseLerp(tuckAimThreshold, -1f, vertical);
            currentTuck = Mathf.Lerp(currentTuck, tuckTarget, 1f - Mathf.Exp(-tuckSmoothing * dt));
        }

        private void ApplyLean()
        {
            // Al recogerse en el aire el torso se curva algo mas hacia el objetivo.
            float total = currentLean - currentTuck * tuckExtraCurlDegrees;

            RotateBone(dorsal, total * dorsalShare);
            RotateBone(head, total * headShare);
        }

        private void RotateBone(Transform bone, float degrees)
        {
            if (bone == null) return;
            if (Mathf.Abs(degrees) < 0.01f) return;

            // Dorsal y Head los reescribe el Animator cada frame: sumar no acumula.
            bone.localRotation *= Quaternion.Euler(0f, 0f, degrees);
        }

        private void ApplyCrouch()
        {
            hipOffset.Apply(hips, -currentDrop);
        }

        private void ApplyAirTuck()
        {
            float rise = currentTuck * tuckRiseUnits;

            // Una pierna se recoge algo mas que la otra para que no parezca un bloque.
            footOffsetL.Apply(footTargetL, rise);
            footOffsetR.Apply(footTargetR, rise * (1f - tuckAsymmetry));
        }

        public float CurrentLean => currentLean;
        public float CurrentCrouchDrop => currentDrop;
        public float CurrentAirTuck => currentTuck;
    }
}
