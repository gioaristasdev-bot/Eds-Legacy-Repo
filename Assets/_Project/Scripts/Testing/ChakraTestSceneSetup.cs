using UnityEngine;

namespace NABHI.Testing
{
    /// <summary>
    /// Utilidad para crear elementos de testing en la escena desde el Editor.
    /// Agrega este script a un GameObject vacio y usa el menu contextual para crear elementos.
    /// </summary>
    public class ChakraTestSceneSetup : MonoBehaviour
    {
        [Header("Layers Requeridos")]
        [Tooltip("Verifica que estos layers esten configurados en Project Settings > Tags and Layers")]
        [SerializeField] private string[] requiredLayers = new string[]
        {
            "Player",
            "InvisiblePlayer",
            "Enemy",
            "ElectronicEnemy",
            "Destructible",
            "Mechanism",
            "Terminal",
            "EchoPoint",
            "HiddenZone",
            "MovableObject"
        };

        [Header("Referencias de Prefabs (Opcional)")]
        [SerializeField] private GameObject testEnemyPrefab;
        [SerializeField] private GameObject electronicEnemyPrefab;
        [SerializeField] private GameObject destructiblePrefab;
        [SerializeField] private GameObject movingPlatformPrefab;
        [SerializeField] private GameObject telekineticObjectPrefab;

        [Header("Configuracion de Spawn")]
        [SerializeField] private float spawnSpacing = 3f;

        private void OnValidate()
        {
            // Verificar layers en el editor
            foreach (string layerName in requiredLayers)
            {
                int layer = LayerMask.NameToLayer(layerName);
                if (layer == -1)
                {
                    Debug.LogWarning($"[ChakraTestSceneSetup] Layer '{layerName}' no existe. Crealo en Project Settings > Tags and Layers");
                }
            }
        }

#if UNITY_EDITOR
        [ContextMenu("1. Crear TestEnemy Basico")]
        private void CreateTestEnemy()
        {
            GameObject enemy = CreateBasicObject("TestEnemy", new Vector2(5, 0));
            enemy.AddComponent<TestEnemy>();

            // Configurar componentes basicos
            var sr = enemy.GetComponent<SpriteRenderer>();
            sr.color = Color.red;

            var collider = enemy.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1, 1);

            // Configurar layer
            SetLayerIfExists(enemy, "Enemy");

            Debug.Log("[ChakraTestSceneSetup] TestEnemy creado. Configura el layer 'Enemy' en el Inspector si es necesario.");
        }

        [ContextMenu("2. Crear ElectronicEnemy")]
        private void CreateElectronicEnemy()
        {
            GameObject enemy = CreateBasicObject("ElectronicEnemy", new Vector2(8, 0));
            enemy.AddComponent<ElectronicEnemy>();

            var sr = enemy.GetComponent<SpriteRenderer>();
            sr.color = new Color(0.2f, 0.6f, 1f);

            var collider = enemy.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1, 1);

            SetLayerIfExists(enemy, "ElectronicEnemy");

            Debug.Log("[ChakraTestSceneSetup] ElectronicEnemy creado. Configura el layer 'ElectronicEnemy' en el Inspector.");
        }

        [ContextMenu("3. Crear DestructibleStructure")]
        private void CreateDestructibleStructure()
        {
            GameObject structure = CreateBasicObject("DestructibleStructure", new Vector2(3, 2));

            // Remover Rigidbody (las estructuras son estaticas)
            var rb = structure.GetComponent<Rigidbody2D>();
            if (rb != null) DestroyImmediate(rb);

            structure.AddComponent<DestructibleStructure>();

            var sr = structure.GetComponent<SpriteRenderer>();
            sr.color = new Color(0.6f, 0.4f, 0.2f);

            var collider = structure.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(2, 2);

            SetLayerIfExists(structure, "Destructible");

            Debug.Log("[ChakraTestSceneSetup] DestructibleStructure creada. Configura el layer 'Destructible' en el Inspector.");
        }

        [ContextMenu("4. Crear Enemy con Deteccion (para Invisibility)")]
        private void CreateEnemyWithDetection()
        {
            GameObject enemy = CreateBasicObject("EnemyWithDetection", new Vector2(10, 0));
            enemy.AddComponent<TestEnemy>();
            enemy.AddComponent<EnemyDetection>();

            var sr = enemy.GetComponent<SpriteRenderer>();
            sr.color = Color.red;

            var collider = enemy.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1, 1);

            // Crear indicador de deteccion
            GameObject indicator = new GameObject("DetectionIndicator");
            indicator.transform.SetParent(enemy.transform);
            indicator.transform.localPosition = new Vector3(0, 1.2f, 0);
            var indicatorSr = indicator.AddComponent<SpriteRenderer>();
            indicatorSr.color = Color.green;

            // Configurar referencia
            var detection = enemy.GetComponent<EnemyDetection>();
            var detectionSerialized = new UnityEditor.SerializedObject(detection);
            detectionSerialized.FindProperty("detectionIndicator").objectReferenceValue = indicatorSr;
            detectionSerialized.ApplyModifiedProperties();

            SetLayerIfExists(enemy, "Enemy");

            Debug.Log("[ChakraTestSceneSetup] Enemy con deteccion creado. Configura playerLayer en EnemyDetection.");
        }

        [ContextMenu("5. Crear MovingPlatform")]
        private void CreateMovingPlatform()
        {
            GameObject platform = CreateBasicObject("MovingPlatform", new Vector2(0, 3));
            platform.AddComponent<MovingPlatform>();

            var sr = platform.GetComponent<SpriteRenderer>();
            sr.color = new Color(0.5f, 0.5f, 0.8f);

            var collider = platform.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(3, 0.5f);

            // Configurar como kinematic
            var rb = platform.GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            Debug.Log("[ChakraTestSceneSetup] MovingPlatform creada.");
        }

        [ContextMenu("6. Crear TelekineticObject")]
        private void CreateTelekineticObject()
        {
            GameObject obj = CreateBasicObject("TelekineticObject", new Vector2(-3, 1));

            // El componente TelekineticObject esta definido en ChakraTelekinesis.cs
            // Agregamos el namespace correcto
            var telekineticType = System.Type.GetType("NABHI.Chakras.Abilities.TelekineticObject, Assembly-CSharp");
            if (telekineticType != null)
            {
                obj.AddComponent(telekineticType);
            }
            else
            {
                Debug.LogWarning("TelekineticObject no encontrado. Asegurate de que ChakraTelekinesis.cs este compilado.");
            }

            var sr = obj.GetComponent<SpriteRenderer>();
            sr.color = new Color(0.8f, 0.6f, 0.2f);

            var collider = obj.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.8f, 0.8f);

            SetLayerIfExists(obj, "MovableObject");

            Debug.Log("[ChakraTestSceneSetup] TelekineticObject creado. Configura el layer 'MovableObject' en el Inspector.");
        }

        [ContextMenu("7. Crear EchoPoint")]
        private void CreateEchoPoint()
        {
            GameObject point = CreateBasicObject("EchoPoint", new Vector2(-5, 0));

            // Remover Rigidbody
            var rb = point.GetComponent<Rigidbody2D>();
            if (rb != null) DestroyImmediate(rb);

            // El componente EchoPoint esta definido en ChakraEchoSense.cs
            var echoPointType = System.Type.GetType("NABHI.Chakras.Abilities.EchoPoint, Assembly-CSharp");
            if (echoPointType != null)
            {
                point.AddComponent(echoPointType);
            }
            else
            {
                Debug.LogWarning("EchoPoint no encontrado. Asegurate de que ChakraEchoSense.cs este compilado.");
            }

            var sr = point.GetComponent<SpriteRenderer>();
            sr.color = new Color(1f, 0.6f, 0.2f, 0.7f);

            var collider = point.AddComponent<CircleCollider2D>();
            collider.radius = 0.5f;
            collider.isTrigger = true;

            SetLayerIfExists(point, "EchoPoint");

            Debug.Log("[ChakraTestSceneSetup] EchoPoint creado. Configura el layer 'EchoPoint' en el Inspector.");
        }

        [ContextMenu("8. Crear HackableTerminal")]
        private void CreateHackableTerminal()
        {
            GameObject terminal = CreateBasicObject("HackableTerminal", new Vector2(-8, 0));

            // Remover Rigidbody
            var rb = terminal.GetComponent<Rigidbody2D>();
            if (rb != null) DestroyImmediate(rb);

            // El componente HackableTerminal esta definido en ChakraRemoteHack.cs
            var terminalType = System.Type.GetType("NABHI.Chakras.Abilities.HackableTerminal, Assembly-CSharp");
            if (terminalType != null)
            {
                terminal.AddComponent(terminalType);
            }
            else
            {
                Debug.LogWarning("HackableTerminal no encontrado. Asegurate de que ChakraRemoteHack.cs este compilado.");
            }

            var sr = terminal.GetComponent<SpriteRenderer>();
            sr.color = new Color(0.2f, 0.8f, 0.2f);

            var collider = terminal.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1, 1.5f);

            SetLayerIfExists(terminal, "Terminal");

            Debug.Log("[ChakraTestSceneSetup] HackableTerminal creada. Configura el layer 'Terminal' en el Inspector.");
        }

        [ContextMenu("9. Crear HiddenZone")]
        private void CreateHiddenZone()
        {
            GameObject zone = new GameObject("HiddenZone");
            zone.transform.position = new Vector3(-10, 2, 0);

            // El componente HiddenZone esta definido en ChakraEchoSense.cs
            var hiddenZoneType = System.Type.GetType("NABHI.Chakras.Abilities.HiddenZone, Assembly-CSharp");
            if (hiddenZoneType != null)
            {
                zone.AddComponent(hiddenZoneType);
            }
            else
            {
                Debug.LogWarning("HiddenZone no encontrado. Asegurate de que ChakraEchoSense.cs este compilado.");
            }

            // Crear sprite hijo (la plataforma oculta)
            GameObject hiddenPlatform = new GameObject("HiddenPlatform");
            hiddenPlatform.transform.SetParent(zone.transform);
            hiddenPlatform.transform.localPosition = Vector3.zero;

            var sr = hiddenPlatform.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);

            var collider = hiddenPlatform.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(3, 0.5f);
            collider.enabled = false; // Empieza desactivado

            SetLayerIfExists(zone, "HiddenZone");

            Debug.Log("[ChakraTestSceneSetup] HiddenZone creada con plataforma oculta. Configura el layer 'HiddenZone'.");
        }

        [ContextMenu("=== CREAR ESCENA COMPLETA DE TESTING ===")]
        private void CreateFullTestScene()
        {
            // Crear contenedor
            GameObject container = new GameObject("=== CHAKRA TEST ELEMENTS ===");
            container.transform.position = Vector3.zero;

            // Crear todos los elementos
            CreateTestEnemy();
            CreateElectronicEnemy();
            CreateDestructibleStructure();
            CreateEnemyWithDetection();
            CreateMovingPlatform();
            CreateTelekineticObject();
            CreateEchoPoint();
            CreateHackableTerminal();
            CreateHiddenZone();

            // Mover todo al contenedor
            foreach (var go in FindObjectsByType<TestEnemy>(FindObjectsSortMode.None))
                go.transform.SetParent(container.transform);
            foreach (var go in FindObjectsByType<ElectronicEnemy>(FindObjectsSortMode.None))
                go.transform.SetParent(container.transform);
            foreach (var go in FindObjectsByType<DestructibleStructure>(FindObjectsSortMode.None))
                go.transform.SetParent(container.transform);
            foreach (var go in FindObjectsByType<MovingPlatform>(FindObjectsSortMode.None))
                go.transform.SetParent(container.transform);

            Debug.Log("[ChakraTestSceneSetup] Escena de testing completa creada!");
            Debug.Log("IMPORTANTE: Configura los Layers en Project Settings > Tags and Layers");
        }

        private GameObject CreateBasicObject(string name, Vector2 position)
        {
            GameObject obj = new GameObject(name);
            obj.transform.position = position;

            // Agregar componentes basicos
            obj.AddComponent<SpriteRenderer>();
            obj.AddComponent<Rigidbody2D>();

            return obj;
        }

        private void SetLayerIfExists(GameObject obj, string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer != -1)
            {
                obj.layer = layer;
            }
        }
#endif
    }
}
