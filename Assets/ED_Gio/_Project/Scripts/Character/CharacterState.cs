using UnityEngine;

namespace NABHI.Character
{
    /// <summary>
    /// Estados del personaje para animación y lógica
    /// </summary>
    public enum MovementState
    {
        Idle,
        Walking,
        Running,
        Jumping,
        Falling,
        WallSliding,
        WallJumping,
        Dashing,
        LedgeGrabbing,
        LedgeClimbing,
        Crouching,
        Swimming
    }

    /// <summary>
    /// Gestor de estados del personaje
    /// Maneja transiciones y lógica de estado
    /// </summary>
    public class CharacterState : MonoBehaviour
    {
        [Header("Estado Actual")]
        [SerializeField] private MovementState currentState = MovementState.Idle;
        [SerializeField] private MovementState previousState = MovementState.Idle;

        private CharacterController2D controller;

        public MovementState CurrentState => currentState;
        public MovementState PreviousState => previousState;

        private void Awake()
        {
            controller = GetComponent<CharacterController2D>();
        }

        private void Update()
        {
            UpdateState();
        }

        private void UpdateState()
        {
            MovementState newState = DetermineState();

            if (newState != currentState)
            {
                ChangeState(newState);
            }
        }

        private MovementState DetermineState()
        {
            // Prioridad de estados (de mayor a menor)

            // Dash tiene máxima prioridad
            if (controller.IsDashing)
                return MovementState.Dashing;

            // Wall slide
            if (controller.IsWallSliding)
                return MovementState.WallSliding;

            // En el aire
            if (!controller.IsGrounded)
            {
                if (controller.Velocity.y > 0.1f)
                    return MovementState.Jumping;
                else
                    return MovementState.Falling;
            }

            // En el suelo
            if (Mathf.Abs(controller.Velocity.x) > 0.1f)
            {
                // TODO: Agregar lógica de run vs walk
                return MovementState.Walking;
            }

            return MovementState.Idle;
        }

        private void ChangeState(MovementState newState)
        {
            // Guardar estado anterior
            previousState = currentState;
            currentState = newState;

            // Log para debug
            Debug.Log($"State Change: {previousState} -> {currentState}");

            // Callback para animación u otros sistemas
            OnStateChanged(previousState, currentState);
        }

        protected virtual void OnStateChanged(MovementState from, MovementState to)
        {
            // Override en clases derivadas para manejar cambios de estado
            // Por ejemplo: cambiar animaciones, reproducir sonidos, etc.
        }

        /// <summary>
        /// Forzar cambio de estado desde código externo
        /// </summary>
        public void ForceState(MovementState state)
        {
            ChangeState(state);
        }

        /// <summary>
        /// Verificar si está en un estado específico
        /// </summary>
        public bool IsInState(MovementState state)
        {
            return currentState == state;
        }

        /// <summary>
        /// Verificar si está en cualquiera de los estados proporcionados
        /// </summary>
        public bool IsInAnyState(params MovementState[] states)
        {
            foreach (var state in states)
            {
                if (currentState == state)
                    return true;
            }
            return false;
        }
    }
}
