using System;
using UnityEngine;

namespace NABHI.Chakras
{
    /// <summary>
    /// Sonido de los chakras, en un solo sitio.
    ///
    /// Se engancha a los eventos de ChakraSystem en lugar de tocar los ocho scripts
    /// de habilidad. Eso tiene una ventaja que importa: el loop se para porque el
    /// propio sistema avisa de la desactivación, así que no puede quedarse colgado
    /// aunque una habilidad concreta olvide llamarnos.
    ///
    /// ChakraSystem garantiza que solo hay un chakra activo a la vez (al activar otro
    /// desactiva el anterior), por eso basta con un único AudioSource para el bucle.
    /// </summary>
    public class ChakraSFX : MonoBehaviour
    {
        [Serializable]
        public class SonidoChakra
        {
            public ChakraType chakra;

            [Tooltip("Se oye una vez al activar.")]
            public AudioClip activar;

            [Tooltip("Se repite mientras el chakra está activo. Dejar vacío para los " +
                     "chakras instantáneos, que no tienen duración.")]
            public AudioClip bucle;

            [Tooltip("Se oye una vez al desactivar.")]
            public AudioClip desactivar;

            [Range(0f, 1f)] public float volumen = 1f;
        }

        [Header("Referencias")]
        [SerializeField] private ChakraSystem sistema;

        [Tooltip("AudioSource dedicado al bucle. Si se deja vacío se crea uno al arrancar, " +
                 "para no pelearse con el AudioSource que PlayerSFX usa con PlayOneShot.")]
        [SerializeField] private AudioSource fuenteBucle;

        [Header("Sonidos por chakra")]
        [SerializeField] private SonidoChakra[] sonidos = new SonidoChakra[0];

        [Header("Ajustes")]
        [Tooltip("Segundos de fundido al entrar y salir del bucle. 0 = corte seco.")]
        [SerializeField] private float fundido = 0.15f;

        [Range(0f, 1f)]
        [SerializeField] private float volumenGeneral = 1f;

        private AudioSource fuentePuntual;
        private ChakraType enBucle = ChakraType.None;
        private float volumenObjetivo;

        private void Awake()
        {
            if (sistema == null) sistema = GetComponent<ChakraSystem>();
            if (sistema == null) sistema = GetComponentInParent<ChakraSystem>();

            if (sistema == null)
            {
                Debug.LogError("[ChakraSFX] Sin ChakraSystem. Componente desactivado.", this);
                enabled = false;
                return;
            }

            fuentePuntual = gameObject.AddComponent<AudioSource>();
            fuentePuntual.playOnAwake = false;
            fuentePuntual.loop = false;

            if (fuenteBucle == null)
            {
                fuenteBucle = gameObject.AddComponent<AudioSource>();
                fuenteBucle.playOnAwake = false;
            }
            fuenteBucle.loop = true;
            fuenteBucle.Stop();
        }

        private void OnEnable()
        {
            if (sistema == null) return;
            sistema.OnChakraActivated += AlActivar;
            sistema.OnChakraDeactivated += AlDesactivar;
        }

        private void OnDisable()
        {
            if (sistema != null)
            {
                sistema.OnChakraActivated -= AlActivar;
                sistema.OnChakraDeactivated -= AlDesactivar;
            }
            // Si nos apagan con un chakra sonando, el bucle moriría reproduciéndose.
            PararBucle(true);
        }

        private void Update()
        {
            if (fuenteBucle == null || !fuenteBucle.isPlaying) return;

            if (fundido <= 0f)
            {
                fuenteBucle.volume = volumenObjetivo;
                return;
            }

            fuenteBucle.volume = Mathf.MoveTowards(fuenteBucle.volume, volumenObjetivo, Time.unscaledDeltaTime / fundido);

            // Al terminar el fundido de salida, paramos de verdad.
            if (volumenObjetivo <= 0f && fuenteBucle.volume <= 0.001f)
                PararBucle(true);
        }

        private void AlActivar(ChakraType tipo)
        {
            SonidoChakra s = Buscar(tipo);
            if (s == null) return;

            if (s.activar != null)
                fuentePuntual.PlayOneShot(s.activar, s.volumen * volumenGeneral);

            if (s.bucle == null) return;

            fuenteBucle.clip = s.bucle;
            fuenteBucle.volume = fundido > 0f ? 0f : s.volumen * volumenGeneral;
            volumenObjetivo = s.volumen * volumenGeneral;
            fuenteBucle.Play();
            enBucle = tipo;
        }

        private void AlDesactivar(ChakraType tipo)
        {
            SonidoChakra s = Buscar(tipo);
            if (s != null && s.desactivar != null)
                fuentePuntual.PlayOneShot(s.desactivar, s.volumen * volumenGeneral);

            // Solo paramos si el bucle es de este chakra: si ya cambió a otro, el
            // evento de desactivación del anterior no debe cortar el nuevo.
            if (enBucle != tipo) return;

            if (fundido > 0f) volumenObjetivo = 0f;   // Update remata el fundido
            else PararBucle(true);
        }

        private void PararBucle(bool inmediato)
        {
            enBucle = ChakraType.None;
            volumenObjetivo = 0f;
            if (fuenteBucle == null) return;
            if (inmediato)
            {
                fuenteBucle.Stop();
                fuenteBucle.clip = null;
            }
        }

        private SonidoChakra Buscar(ChakraType tipo)
        {
            for (int i = 0; i < sonidos.Length; i++)
                if (sonidos[i] != null && sonidos[i].chakra == tipo) return sonidos[i];
            return null;
        }

        /// <summary>
        /// Rellena la lista con una entrada por chakra, para no crearlas a mano.
        /// </summary>
        [ContextMenu("Crear una entrada por chakra")]
        private void CrearEntradas()
        {
            Array valores = Enum.GetValues(typeof(ChakraType));
            var lista = new System.Collections.Generic.List<SonidoChakra>();

            foreach (ChakraType t in valores)
            {
                if (t == ChakraType.None) continue;
                SonidoChakra ya = Buscar(t);
                lista.Add(ya ?? new SonidoChakra { chakra = t });
            }
            sonidos = lista.ToArray();
        }
    }
}
