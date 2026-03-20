using UnityEngine;
using System.Collections;
using Debug = UnityEngine.Debug;

namespace NABHI.Chakras.Abilities
{
    /// <summary>
    /// Chakra 5: Hacker Remota (de corto alcance)
    /// Color: Amarillo - Plexo Solar (Sabiduria y Poder)
    ///
    /// Controla terminales electronicas cercanas o a corta distancia.
    /// Puede interactuar incluso a traves de paredes si esta en la misma escena.
    /// </summary>
    public class ChakraRemoteHack : ChakraBase
    {
        [Header("Hack Settings")]
        [Tooltip("Radio de deteccion de terminales")]
        [SerializeField] private float hackRange = 8f;

        [Tooltip("Tiempo para completar el hackeo")]
        [SerializeField] private float hackDuration = 1.5f;

        [Tooltip("Ignora paredes para detectar terminales")]
        [SerializeField] private bool ignoreWalls = true;

        [Header("Layers")]
        [SerializeField] private LayerMask terminalLayer;

        [Header("Visual")]
        [SerializeField] private LineRenderer hackBeam;
        [SerializeField] private ParticleSystem hackParticles;
        [SerializeField] private Color hackingColor = Color.yellow;
        [SerializeField] private Color successColor = Color.green;

        // Estado
        private HackableTerminal currentTarget;
        private HackableTerminal nearestTerminal;
        private Coroutine hackCoroutine;
        private bool isHacking;
        private float hackProgress;

        // Propiedades
        public bool IsHacking => isHacking;
        public float HackProgress => hackProgress;
        public HackableTerminal CurrentTarget => currentTarget;

        protected override void Awake()
        {
            base.Awake();

            // Configurar tipo
            chakraType = ChakraType.RemoteHack;
            chakraName = "Hacker Remota";
            chakraColor = new Color(1f, 0.9f, 0.2f); // Amarillo
            activationMode = ChakraActivationMode.Contextual;
            cooldown = 0.5f;

            // Configurar beam
            if (hackBeam != null)
            {
                hackBeam.enabled = false;
            }
        }

        protected override void Update()
        {
            base.Update();

            // Buscar terminales cercanas
            DetectTerminals();

            // Actualizar visual del beam
            UpdateHackBeam();
        }

        private void DetectTerminals()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, hackRange, terminalLayer);

            if (hits.Length > 0)
            {
                float closestDist = float.MaxValue;
                HackableTerminal closest = null;

                foreach (var hit in hits)
                {
                    var terminal = hit.GetComponent<HackableTerminal>();
                    if (terminal != null && terminal.CanBeHacked)
                    {
                        // Verificar linea de vision si no ignora paredes
                        if (!ignoreWalls)
                        {
                            Vector2 direction = hit.transform.position - transform.position;
                            RaycastHit2D lineOfSight = Physics2D.Raycast(transform.position, direction, direction.magnitude);
                            if (lineOfSight.collider != null && lineOfSight.collider != hit)
                            {
                                continue; // Hay un obstaculo
                            }
                        }

                        float dist = Vector2.Distance(transform.position, hit.transform.position);
                        if (dist < closestDist)
                        {
                            closestDist = dist;
                            closest = terminal;
                        }
                    }
                }

                nearestTerminal = closest;
            }
            else
            {
                nearestTerminal = null;
            }
        }

        private void UpdateHackBeam()
        {
            if (hackBeam == null) return;

            if (isHacking && currentTarget != null)
            {
                hackBeam.enabled = true;
                hackBeam.SetPosition(0, transform.position);
                hackBeam.SetPosition(1, currentTarget.transform.position);

                // Color segun progreso
                Color beamColor = Color.Lerp(hackingColor, successColor, hackProgress);
                hackBeam.startColor = beamColor;
                hackBeam.endColor = beamColor;
            }
            else
            {
                hackBeam.enabled = false;
            }
        }

        public override bool HasValidTarget()
        {
            return nearestTerminal != null;
        }

        public override bool CanActivate()
        {
            if (!base.CanActivate())
                return false;

            return HasValidTarget();
        }

        protected override void OnActivate()
        {
            if (nearestTerminal == null)
            {
                Debug.LogWarning("[ChakraRemoteHack] No hay terminal valida!");
                return;
            }

            currentTarget = nearestTerminal;
            hackCoroutine = StartCoroutine(HackRoutine());
        }

        private IEnumerator HackRoutine()
        {
            isHacking = true;
            hackProgress = 0f;

            // Efectos
            if (hackParticles != null)
                hackParticles.Play();

            Debug.Log($"[ChakraRemoteHack] Iniciando hackeo de {currentTarget.name}...");

            // Proceso de hackeo
            float elapsed = 0f;
            while (elapsed < hackDuration)
            {
                // Verificar que el target siga siendo valido
                if (currentTarget == null || !currentTarget.CanBeHacked)
                {
                    Debug.Log("[ChakraRemoteHack] Target perdido!");
                    CancelHack();
                    yield break;
                }

                // Verificar que siga en rango
                float dist = Vector2.Distance(transform.position, currentTarget.transform.position);
                if (dist > hackRange * 1.2f) // Un poco de tolerancia
                {
                    Debug.Log("[ChakraRemoteHack] Fuera de rango!");
                    CancelHack();
                    yield break;
                }

                elapsed += Time.deltaTime;
                hackProgress = elapsed / hackDuration;
                yield return null;
            }

            // Hackeo completado
            CompleteHack();
        }

        private void CompleteHack()
        {
            if (currentTarget != null)
            {
                currentTarget.OnHacked();
                Debug.Log($"[ChakraRemoteHack] Terminal {currentTarget.name} hackeada!");
            }

            isHacking = false;
            hackProgress = 0f;
            currentTarget = null;

            if (hackParticles != null)
                hackParticles.Stop();

            // Desactivar chakra
            Deactivate();
        }

        private void CancelHack()
        {
            isHacking = false;
            hackProgress = 0f;
            currentTarget = null;

            if (hackParticles != null)
                hackParticles.Stop();

            Deactivate();
        }

        protected override void OnDeactivate()
        {
            if (hackCoroutine != null)
            {
                StopCoroutine(hackCoroutine);
                hackCoroutine = null;
            }

            isHacking = false;
            hackProgress = 0f;
            currentTarget = null;

            if (hackBeam != null)
                hackBeam.enabled = false;

            if (hackParticles != null)
                hackParticles.Stop();
        }

        protected override string GetChakraDescription()
        {
            return "Hackea terminales electronicas cercanas para activar mecanismos y abrir puertas.";
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = chakraColor;
            Gizmos.DrawWireSphere(transform.position, hackRange);

            if (nearestTerminal != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, nearestTerminal.transform.position);
            }
        }
    }

    /// <summary>
    /// Componente para terminales hackeables
    /// </summary>
    public class HackableTerminal : MonoBehaviour
    {
        [Header("Configuracion")]
        [SerializeField] private bool singleUse = true;
        [SerializeField] private UnityEngine.Events.UnityEvent onHacked;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer terminalSprite;
        [SerializeField] private Color activeColor = Color.green;
        [SerializeField] private Color hackedColor = Color.cyan;

        private bool isHacked = false;

        public bool CanBeHacked => !isHacked || !singleUse;
        public bool IsHacked => isHacked;

        public void OnHacked()
        {
            isHacked = true;

            // Cambiar color
            if (terminalSprite != null)
            {
                terminalSprite.color = hackedColor;
            }

            // Invocar eventos
            onHacked?.Invoke();

            Debug.Log($"[HackableTerminal] {name} fue hackeada!");
        }

        public void Reset()
        {
            isHacked = false;
            if (terminalSprite != null)
            {
                terminalSprite.color = activeColor;
            }
        }
    }

    /// <summary>
    /// Interface para enemigos y objetos hackeables por ChakraRemoteHack.
    /// Implementada en EnemyBase para todos los enemigos del demo.
    /// </summary>
    public interface IHackable
    {
        /// <summary>¿Puede ser hackeado ahora? (vivo, no ya hackeado, no stunneado)</summary>
        bool CanBeHacked { get; }

        /// <summary>Llamado cuando el jugador empieza a hackear (barra de progreso iniciada).</summary>
        void OnHackStart();

        /// <summary>Llamado cuando el hackeo se completa con éxito. Aplicar efecto aquí.</summary>
        void OnHackComplete();

        /// <summary>Llamado si el hackeo es interrumpido antes de completarse.</summary>
        void OnHackInterrupted();
    }
}
