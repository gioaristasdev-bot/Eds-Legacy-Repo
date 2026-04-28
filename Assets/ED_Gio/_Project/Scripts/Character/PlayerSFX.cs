using UnityEngine;

namespace NABHI.Character
{
    /// <summary>
    /// Maneja todos los efectos de sonido de ED (jugador).
    /// Agregar al mismo GameObject que CharacterController2D y PlayerHealth.
    /// Asignar clips en el Inspector — los arrays de pasos tienen variaciones
    /// que se reproducen de forma aleatoria para evitar la repetición.
    ///
    /// Los clips de EDFX/Disparos son usados por WeaponController via GetComponentInParent.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class PlayerSFX : MonoBehaviour
    {
        [Header("Salto / Aterrizaje")]
        [Tooltip("EDFX/Salto — Male Jump *.wav")]
        public AudioClip[] jumpClips;

        [Tooltip("Sonido al aterrizar (puede reutilizar alguno de Pasos)")]
        public AudioClip[] landClips;

        [Header("Dash")]
        [Tooltip("EDFX/Dash — Buff 2.wav")]
        public AudioClip[] dashClips;

        [Header("Pasos")]
        [Tooltip("EDFX/Pasos/Concrete — usar segun superficie actual")]
        public AudioClip[] footstepClips;

        [Tooltip("Intervalo entre pasos (segundos). Ajustar segun velocidad de animacion")]
        [Range(0.1f, 0.8f)]
        public float footstepInterval = 0.32f;

        [Tooltip("Velocidad minima del personaje para reproducir pasos")]
        [Range(0f, 3f)]
        public float footstepMinSpeed = 0.5f;

        [Header("Disparos")]
        [Tooltip("EDFX/Disparos — Bio gun Shot *.wav o Sci Fi Pistol *.wav")]
        public AudioClip[] shotClips;

        [Header("Daño / Muerte")]
        [Tooltip("EDFX/Daño — Game Punch *.wav")]
        public AudioClip[] damageClips;

        [Tooltip("EDFX/Muerte — Male Death.wav")]
        public AudioClip[] deathClips;

        [Header("Items")]
        [Tooltip("EDFX/Items/Weapon Equipped — Weapon Equipped *.wav")]
        public AudioClip[] weaponEquipClips;

        [Tooltip("EDFX/Items/Health Pickup — Health Pickup *.wav")]
        public AudioClip[] healthPickupClips;

        [Header("Chakra Float")]
        [Tooltip("Sonido al activar la levitacion")]
        public AudioClip[] floatActivateClips;

        [Tooltip("Sonido al desactivar la levitacion")]
        public AudioClip[] floatDeactivateClips;

        [Tooltip("Sonido al iniciar el ascenso (mantener salto)")]
        public AudioClip[] floatAscendClips;

        [Header("Chakra Hack")]
        [Tooltip("Sonido al ejecutar el hackeo")]
        public AudioClip[] hackClips;

        [Header("Volumenes")]
        [Range(0f, 1f)] public float footstepVolume = 0.45f;
        [Range(0f, 1f)] public float sfxVolume = 1f;

        // ─── Referencias internas ───────────────────────────────────────────

        AudioSource audioSource;
        CharacterController2D controller;
        float footstepTimer;

        // ─── Unity ──────────────────────────────────────────────────────────

        void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;

            controller = GetComponent<CharacterController2D>();
        }

        void Start()
        {
            // Conectar eventos de PlayerHealth
            var health = GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.OnHit.AddListener(PlayDamage);
                health.OnDeath.AddListener(PlayDeath);
            }
        }

        void Update()
        {
            HandleFootsteps();
        }

        // ─── Pasos (gestionados aqui para no tocar CharacterController2D) ──

        void HandleFootsteps()
        {
            if (footstepClips == null || footstepClips.Length == 0) return;
            if (controller == null) return;

            bool moving   = Mathf.Abs(controller.Velocity.x) >= footstepMinSpeed;
            bool grounded = controller.IsGrounded;
            bool dashing  = controller.IsDashing;

            if (!grounded || !moving || dashing)
            {
                footstepTimer = 0f; // reiniciar para que el primer paso suene inmediato al retomar
                return;
            }

            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0f)
            {
                PlayRandom(footstepClips, footstepVolume);
                footstepTimer = footstepInterval;
            }
        }

        // ─── API pública (llamada desde CharacterController2D y WeaponController) ─

        /// <summary>Salto normal y double jump.</summary>
        public void PlayJump() => PlayRandom(jumpClips);

        /// <summary>Wall jump — reutiliza el mismo clip de salto.</summary>
        public void PlayWallJump() => PlayRandom(jumpClips);

        /// <summary>Al aterrizar desde el aire.</summary>
        public void PlayLand() => PlayRandom(landClips);

        /// <summary>Al iniciar el dash.</summary>
        public void PlayDash() => PlayRandom(dashClips);

        /// <summary>Al disparar (llamado por WeaponController).</summary>
        public void PlayShot() => PlayRandom(shotClips);

        /// <summary>Al recibir daño (conectado a PlayerHealth.OnHit).</summary>
        public void PlayDamage() => PlayRandom(damageClips);

        /// <summary>Al morir (conectado a PlayerHealth.OnDeath).</summary>
        public void PlayDeath() => PlayRandom(deathClips);

        /// <summary>Al recoger o equipar un arma.</summary>
        public void PlayWeaponEquip() => PlayRandom(weaponEquipClips);

        /// <summary>Al recoger un item de salud.</summary>
        public void PlayHealthPickup() => PlayRandom(healthPickupClips);

        /// <summary>Al activar el chakra Float (levitación).</summary>
        public void PlayFloatActivate() => PlayRandom(floatActivateClips);

        /// <summary>Al desactivar el chakra Float.</summary>
        public void PlayFloatDeactivate() => PlayRandom(floatDeactivateClips);

        /// <summary>Al iniciar el ascenso activo (mantener salto en Float).</summary>
        public void PlayFloatAscend() => PlayRandom(floatAscendClips);

        /// <summary>Al ejecutar el hackeo remoto.</summary>
        public void PlayHack() => PlayRandom(hackClips);

        // ─── Interno ─────────────────────────────────────────────────────────

        void PlayRandom(AudioClip[] clips, float volume = -1f)
        {
            if (clips == null || clips.Length == 0) return;
            var clip = clips[Random.Range(0, clips.Length)];
            if (clip == null) return;
            audioSource.PlayOneShot(clip, volume < 0 ? sfxVolume : volume);
        }
    }
}
