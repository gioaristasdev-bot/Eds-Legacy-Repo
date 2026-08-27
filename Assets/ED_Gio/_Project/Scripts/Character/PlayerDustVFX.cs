using UnityEngine;
using NABHI.Character;

/// <summary>
/// Polvillo de carrera y de aterrizaje.
///
/// Lee solo la API pública de CharacterController2D (IsGrounded, Velocity), así que
/// no toca el controlador ni depende de eventos suyos: se puede quitar del Player
/// sin efectos colaterales.
///
/// Los dos sistemas de partículas deben tener Play On Awake OFF y Stop Action None,
/// igual que los VFX de los chakras. El de carrera además debe estar en loop.
/// </summary>
public class PlayerDustVFX : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private CharacterController2D controller;

    [Tooltip("Sistema en loop que emite mientras corre. Colocarlo a la altura de los pies.")]
    [SerializeField] private ParticleSystem runDust;

    [Tooltip("Sistema de un disparo para el golpe al aterrizar.")]
    [SerializeField] private ParticleSystem landDust;

    [Header("Polvo al correr")]
    [Tooltip("Velocidad horizontal a partir de la cual empieza a levantar polvo. " +
             "Por encima del ritmo de caminar, para que solo salga corriendo.")]
    [SerializeField] private float velocidadMinima = 9f;

    [Tooltip("Velocidad a la que el polvo alcanza su intensidad máxima.")]
    [SerializeField] private float velocidadMaxima = 12f;

    [Tooltip("Partículas por segundo a intensidad máxima.")]
    [SerializeField] private float emisionMaxima = 25f;

    [Header("Polvo al aterrizar")]
    [Tooltip("Velocidad de caída mínima para que el aterrizaje levante polvo. " +
             "Evita que una caída de un escalón haga la nube completa.")]
    [SerializeField] private float caidaMinima = 4f;

    [Tooltip("Velocidad de caída a la que el aterrizaje es máximo.")]
    [SerializeField] private float caidaMaxima = 18f;

    [Tooltip("Partículas del golpe a intensidad máxima.")]
    [SerializeField] private int particulasAterrizaje = 18;

    // Estado interno
    private bool estabaEnSuelo = true;
    private float velocidadCaida;      // última velocidad de caída antes de tocar suelo
    private bool corriendo;

    private void Awake()
    {
        if (controller == null)
            controller = GetComponent<CharacterController2D>();

        if (controller == null)
        {
            Debug.LogError("[PlayerDustVFX] Sin CharacterController2D. Componente desactivado.", this);
            enabled = false;
            return;
        }

        PrepararSistema(runDust, true);
        PrepararSistema(landDust, false);
    }

    /// <summary>
    /// Deja el sistema en el estado que este componente espera, por si se olvidó
    /// configurarlo en el Inspector. Stop Action distinto de None destruiría el
    /// objeto tras el primer uso y dejaría la referencia nula.
    /// </summary>
    private void PrepararSistema(ParticleSystem ps, bool enLoop)
    {
        if (ps == null) return;

        ParticleSystem.MainModule m = ps.main;
        m.playOnAwake = false;
        m.stopAction = ParticleSystemStopAction.None;
        m.loop = enLoop;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void Update()
    {
        Vector2 v = controller.Velocity;
        bool enSuelo = controller.IsGrounded;

        // Mientras cae, guardamos a qué velocidad lo hace: al tocar suelo ya es 0
        // y no habría forma de saber la fuerza del impacto.
        if (!enSuelo && v.y < 0f)
            velocidadCaida = -v.y;

        if (enSuelo && !estabaEnSuelo)
            Aterrizar();

        if (enSuelo)
            ActualizarPolvoCarrera(Mathf.Abs(v.x));
        else if (corriendo)
            DetenerPolvoCarrera();

        estabaEnSuelo = enSuelo;
    }

    private void ActualizarPolvoCarrera(float velocidad)
    {
        if (runDust == null) return;

        if (velocidad < velocidadMinima)
        {
            if (corriendo) DetenerPolvoCarrera();
            return;
        }

        float intensidad = Mathf.InverseLerp(velocidadMinima, velocidadMaxima, velocidad);

        ParticleSystem.EmissionModule em = runDust.emission;
        em.rateOverTimeMultiplier = Mathf.Lerp(emisionMaxima * 0.35f, emisionMaxima, intensidad);

        if (!corriendo)
        {
            runDust.Play(true);
            corriendo = true;
        }
    }

    private void DetenerPolvoCarrera()
    {
        corriendo = false;
        if (runDust == null) return;

        // StopEmitting, no Clear: las partículas ya emitidas terminan su vida en vez
        // de desaparecer de golpe.
        runDust.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    private void Aterrizar()
    {
        if (landDust == null || velocidadCaida < caidaMinima)
        {
            velocidadCaida = 0f;
            return;
        }

        float intensidad = Mathf.InverseLerp(caidaMinima, caidaMaxima, velocidadCaida);
        int cantidad = Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(particulasAterrizaje * 0.4f, particulasAterrizaje, intensidad)));

        landDust.Emit(cantidad);
        velocidadCaida = 0f;
    }

    private void OnDisable()
    {
        DetenerPolvoCarrera();
    }
}
