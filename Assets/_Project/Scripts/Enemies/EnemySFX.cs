using UnityEngine;

namespace NABHI.Enemies
{
    /// <summary>
    /// Maneja todos los efectos de sonido de un enemigo.
    /// Agregar al mismo GameObject que EnemyBase (o subclase).
    /// Asignar clips en el Inspector segun el tipo de enemigo:
    ///   AcorazadoFX → EnemyMechSoldier
    ///   CyborgFX    → EnemyCyborg
    ///   DronFX      → EnemyDrone
    ///   GuardianFX  → EnemyGuardian
    ///   TorretaFX   → EnemyTurret
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class EnemySFX : MonoBehaviour
    {
        [Header("Daño")]
        [Tooltip("*FX/Daño — se reproduce al recibir un golpe")]
        public AudioClip[] damageClips;

        [Header("Muerte")]
        [Tooltip("*FX/Muerte — se reproduce al morir (si existe la carpeta)")]
        public AudioClip[] deathClips;

        [Header("Disparos")]
        [Tooltip("*FX/Disparos — se reproduce al disparar un proyectil")]
        public AudioClip[] shotClips;

        [Header("Pasos")]
        [Tooltip("*FX/Pasos — se reproduce mientras patrulla o persigue al jugador")]
        public AudioClip[] footstepClips;

        [Tooltip("Intervalo entre pasos (segundos). Ajustar segun velocidad del enemigo")]
        [Range(0.1f, 1f)]
        public float footstepInterval = 0.48f;

        [Header("Volumenes")]
        [Range(0f, 1f)] public float footstepVolume = 0.35f;
        [Range(0f, 1f)] public float sfxVolume = 0.9f;

        // ─── Referencias internas ───────────────────────────────────────────

        AudioSource audioSource;
        EnemyBase enemy;
        float footstepTimer;

        // ─── Unity ──────────────────────────────────────────────────────────

        void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // sonido 3D (se atenua con la distancia)

            enemy = GetComponent<EnemyBase>();
        }

        void Update()
        {
            HandleFootsteps();
        }

        // ─── Pasos ───────────────────────────────────────────────────────────

        void HandleFootsteps()
        {
            if (footstepClips == null || footstepClips.Length == 0 || enemy == null) return;

            var state = enemy.CurrentState;
            bool isMoving = state == EnemyBase.EnemyState.Patrol
                         || state == EnemyBase.EnemyState.Chase;

            if (!isMoving)
            {
                footstepTimer = 0f;
                return;
            }

            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0f)
            {
                PlayRandom(footstepClips, footstepVolume);
                footstepTimer = footstepInterval;
            }
        }

        // ─── API pública (llamada desde EnemyBase) ───────────────────────────

        /// <summary>Al recibir daño.</summary>
        public void PlayDamage() => PlayRandom(damageClips);

        /// <summary>Al morir.</summary>
        public void PlayDeath() => PlayRandom(deathClips);

        /// <summary>Al disparar un proyectil.</summary>
        public void PlayShot() => PlayRandom(shotClips);

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
