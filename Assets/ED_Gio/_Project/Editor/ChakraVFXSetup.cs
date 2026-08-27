using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Ventana de setup para los sistemas de partículas de los chakras.
///
/// Aplica de una vez el protocolo que estos efectos necesitan y que es fácil
/// olvidar a mano:
///   - Play On Awake OFF en todos los sistemas anidados (si no, se disparan al cargar).
///   - Stop Action None (con Destroy el objeto muere tras el primer uso y la
///     referencia del chakra se queda nula).
///   - Order in Layer por delante del sprite del personaje, o no se ve nada.
///   - Scaling Mode Hierarchy y normalización del tamaño, porque cada pack de VFX
///     viene autorado a una escala distinta.
///   - Asignación de la instancia de ESCENA al campo del chakra. Arrastrar el
///     prefab desde Project deja una referencia a un asset que nunca se reproduce.
/// </summary>
public class ChakraVFXSetup : EditorWindow
{
    private const string ContenedorNombre = "VFX_Chakras";

    private enum Altura { Pies, Centro, Pecho, Manual }

    private class Slot
    {
        public string campo;
        public string etiqueta;
        public bool loop;
        public float tamanoObjetivo;
        public Altura altura;
        public float alturaManual;
        public GameObject prefab;
        public string estado = "";
    }

    private List<Slot> slots;
    private Vector2 scroll;
    private int ordenEnCapa = 30;
    private string capa = "Default";

    [MenuItem("Nabhi/Chakras/Setup de VFX")]
    public static void Abrir()
    {
        GetWindow<ChakraVFXSetup>("VFX de Chakras").minSize = new Vector2(520f, 400f);
    }

    private void OnEnable()
    {
        slots = new List<Slot>
        {
            NuevoSlot("floatParticles",        "Float - aura continua", true,  0.8f, Altura.Pies),
            NuevoSlot("ascendParticles",       "Float - ascenso",       false, 0.8f, Altura.Centro),
            NuevoSlot("invisibilityParticles", "Invisibilidad",         true,  0.9f, Altura.Centro),
            NuevoSlot("hackParticles",         "Hackeo remoto",         true,  0.8f, Altura.Pecho),
            NuevoSlot("empPulseParticles",     "EMP - pulso",           false, 1.2f, Altura.Centro),
            NuevoSlot("pulseParticles",        "Pulso de gravedad",     false, 1.2f, Altura.Centro),
            NuevoSlot("echoWaveParticles",     "Eco - onda",            false, 1.5f, Altura.Centro),
            NuevoSlot("tremblParticles",       "Temblor - impacto",     false, 1.5f, Altura.Pies),
        };
        RefrescarEstados();
    }

    private static Slot NuevoSlot(string campo, string etiqueta, bool loop, float tamano, Altura altura)
    {
        return new Slot { campo = campo, etiqueta = etiqueta, loop = loop, tamanoObjetivo = tamano, altura = altura };
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Monta un prefab de partículas sobre un chakra aplicando toda la configuración necesaria.\n" +
            "Arrastra el prefab desde Project: la ventana crea la instancia en la escena y la asigna.",
            MessageType.Info);

        Transform chakras = BuscarChakras();
        if (chakras == null)
        {
            EditorGUILayout.HelpBox("No encuentro los componentes de chakra en la escena abierta.", MessageType.Error);
            return;
        }

        EditorGUILayout.LabelField("Contenedor", chakras.name + "/" + ContenedorNombre);

        using (new EditorGUILayout.HorizontalScope())
        {
            capa = EditorGUILayout.TextField("Sorting Layer", capa);
            ordenEnCapa = EditorGUILayout.IntField("Order", ordenEnCapa);
        }

        EditorGUILayout.LabelField("Altura del personaje", AlturaPersonaje(chakras).ToString("F2") + " unidades");
        EditorGUILayout.Space();

        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (Slot s in slots)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(s.etiqueta, EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Campo", s.campo + "   -   " + s.estado);

                s.prefab = (GameObject)EditorGUILayout.ObjectField("Prefab de VFX", s.prefab, typeof(GameObject), false);

                using (new EditorGUILayout.HorizontalScope())
                {
                    s.loop = EditorGUILayout.Toggle("Loop", s.loop, GUILayout.Width(120f));
                    s.tamanoObjetivo = EditorGUILayout.FloatField("Tamano", s.tamanoObjetivo);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    s.altura = (Altura)EditorGUILayout.EnumPopup("Altura", s.altura);
                    if (s.altura == Altura.Manual)
                        s.alturaManual = EditorGUILayout.FloatField(s.alturaManual);
                }

                using (new EditorGUI.DisabledScope(s.prefab == null))
                {
                    if (GUILayout.Button("Montar y asignar"))
                        Montar(chakras, s);
                }
            }
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.Space();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Montar todos los que tengan prefab"))
            {
                foreach (Slot s in slots)
                    if (s.prefab != null) Montar(chakras, s);
            }
            if (GUILayout.Button("Refrescar estado"))
                RefrescarEstados();
        }
    }

    private Transform BuscarChakras()
    {
        MonoBehaviour[] todos = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (MonoBehaviour mb in todos)
        {
            if (mb == null) continue;
            if (mb.GetType().Name == "ChakraFloat") return mb.transform;
        }
        return null;
    }

    private float AlturaPersonaje(Transform chakras)
    {
        Transform raiz = chakras;
        while (raiz.parent != null) raiz = raiz.parent;
        CapsuleCollider2D cap = raiz.GetComponent<CapsuleCollider2D>();
        return cap != null ? cap.size.y * raiz.lossyScale.y : 1f;
    }

    private float AlturaLocal(Transform chakras, Slot s)
    {
        if (s.altura == Altura.Manual) return s.alturaManual;

        Transform raiz = chakras;
        while (raiz.parent != null) raiz = raiz.parent;
        CapsuleCollider2D cap = raiz.GetComponent<CapsuleCollider2D>();
        if (cap == null) return 0f;

        float escala = Mathf.Approximately(chakras.lossyScale.y, 0f) ? 1f : chakras.lossyScale.y;
        float alturaMundo = cap.size.y * raiz.lossyScale.y;
        float centroY = raiz.position.y + cap.offset.y * raiz.lossyScale.y;

        float destinoY;
        if (s.altura == Altura.Pies) destinoY = centroY - alturaMundo * 0.5f;
        else if (s.altura == Altura.Pecho) destinoY = centroY + alturaMundo * 0.15f;
        else destinoY = centroY;

        return (destinoY - chakras.position.y) / escala;
    }

    private void Montar(Transform chakras, Slot s)
    {
        Transform contenedor = chakras.Find(ContenedorNombre);
        if (contenedor == null)
        {
            GameObject go = new GameObject(ContenedorNombre);
            Undo.RegisterCreatedObjectUndo(go, "Crear contenedor de VFX");
            go.transform.SetParent(chakras, false);
            contenedor = go.transform;
        }

        string nombre = "FX_" + s.campo;
        Transform existente = contenedor.Find(nombre);
        if (existente != null)
            Undo.DestroyObjectImmediate(existente.gameObject);

        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(s.prefab, contenedor);
        if (inst == null)
        {
            s.estado = "no pude instanciar el prefab";
            return;
        }
        Undo.RegisterCreatedObjectUndo(inst, "Montar VFX de chakra");
        inst.name = nombre;
        inst.transform.localPosition = new Vector3(0f, AlturaLocal(chakras, s), 0f);
        inst.transform.localScale = Vector3.one;

        ParticleSystem[] sistemas = inst.GetComponentsInChildren<ParticleSystem>(true);
        if (sistemas.Length == 0)
        {
            s.estado = "el prefab no tiene ParticleSystem";
            return;
        }

        foreach (ParticleSystem ps in sistemas)
        {
            ParticleSystem.MainModule m = ps.main;
            m.playOnAwake = false;
            m.stopAction = ParticleSystemStopAction.None;
            m.loop = s.loop;
            m.scalingMode = ParticleSystemScalingMode.Hierarchy;

            ParticleSystemRenderer r = ps.GetComponent<ParticleSystemRenderer>();
            if (r == null) continue;
            r.sortingLayerName = capa;
            r.sortingOrder = ordenEnCapa;
        }

        ParticleSystem raizPS = inst.GetComponent<ParticleSystem>();
        if (raizPS == null) raizPS = sistemas[0];

        // Normalizamos el tamaño: cada pack viene autorado a una escala distinta,
        // así que escalamos hasta que la partícula mida lo pedido en unidades de mundo.
        float baseSize = raizPS.main.startSize.constantMax;
        float actual = baseSize * raizPS.transform.lossyScale.y;
        if (baseSize > 0.0001f && actual > 0.0001f)
            inst.transform.localScale *= s.tamanoObjetivo / actual;

        raizPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        int asignados = Asignar(s.campo, raizPS);
        s.estado = asignados > 0
            ? "montado y asignado (" + sistemas.Length + " sistemas)"
            : "montado, pero no encontre el campo en ningun chakra";

        EditorSceneManager.MarkSceneDirty(chakras.gameObject.scene);
    }

    private int Asignar(string campo, ParticleSystem instancia)
    {
        int n = 0;
        MonoBehaviour[] todos = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (MonoBehaviour mb in todos)
        {
            if (mb == null || !mb.GetType().Name.StartsWith("Chakra")) continue;

            SerializedObject so = new SerializedObject(mb);
            SerializedProperty pr = so.FindProperty(campo);
            if (pr == null || pr.propertyType != SerializedPropertyType.ObjectReference) continue;

            pr.objectReferenceValue = instancia;
            so.ApplyModifiedProperties();
            n++;
        }
        return n;
    }

    private void RefrescarEstados()
    {
        MonoBehaviour[] todos = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Slot s in slots)
        {
            s.estado = "sin asignar";
            foreach (MonoBehaviour mb in todos)
            {
                if (mb == null || !mb.GetType().Name.StartsWith("Chakra")) continue;

                SerializedObject so = new SerializedObject(mb);
                SerializedProperty pr = so.FindProperty(s.campo);
                if (pr == null || pr.propertyType != SerializedPropertyType.ObjectReference) continue;

                Object v = pr.objectReferenceValue;
                if (v == null) s.estado = "VACIO";
                else if (EditorUtility.IsPersistent(v)) s.estado = "apunta a un ASSET (no se reproduce)";
                else s.estado = "OK - " + v.name;
            }
        }
        Repaint();
    }
}
