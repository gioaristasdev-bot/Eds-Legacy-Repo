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
        [Range(0f, 1f)] public float deathVolume = 1f;

        [Header("Audio Espacial")]
        [Tooltip("Distancia minima desde la que se escucha al volumen maximo")]
        public float minDistance = 5f;
        [Tooltip("Distancia maxima a la que deja de escucharse")]
        public float maxDistance = 30f;

        // ─── Referencias internas ───────────────────────────────────────────

        AudioSource audioSource;
        EnemyBase enemy;
        float footstepTimer;

        // ─── Unity ──────────────────────────────────────────────────────────

        void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake  = false;
            audioSource.spatialBlend = 1f;
            audioSource.rolloffMode  = AudioRolloffMode.Linear;
            audioSource.minDistance  = minDistance;
            audioSource.maxDistance  = maxDistance;

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

        /// <summary>Al morir. Crea un AudioSource temporal para que el clip suene aunque el enemigo se destruya.</summary>
        public void PlayDeath()
        {
            if (deathClips == null || deathClips.Length == 0) return;
            var clip = deathClips[Random.Range(0, deathClips.Length)];
            if (clip == null) return;

            var go  = new GameObject("EnemyDeathSFX");
            go.transform.position = transform.position;
            var src = go.AddComponent<AudioSource>();
            src.spatialBlend = 1f;
            src.rolloffMode  = AudioRolloffMode.Linear;
            src.minDistance  = minDistance;
            src.maxDistance  = maxDistance;
            src.PlayOneShot(clip, deathVolume);
            Destroy(go, clip.length + 0.1f);
        }

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
