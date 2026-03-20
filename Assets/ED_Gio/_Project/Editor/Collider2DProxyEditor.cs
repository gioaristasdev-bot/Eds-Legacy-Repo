using UnityEngine;
using UnityEditor;
using NABHI.Environment;

namespace NABHI.Editor
{
    /// <summary>
    /// Herramientas de editor para configurar sistema hibrido 2D/3D
    /// </summary>
    public class Collider2DProxyEditor : EditorWindow
    {
        private string targetLayer = "Ground";
        private Collider2DProxy.ColliderType colliderType = Collider2DProxy.ColliderType.Box;
        private bool disableCollider3D = true;

        [MenuItem("NABHI/Hybrid 2D-3D Setup")]
        public static void ShowWindow()
        {
            GetWindow<Collider2DProxyEditor>("Hybrid 2D/3D Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Sistema Hibrido 2D/3D", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "Selecciona objetos 3D en la Hierarchy y usa las opciones para agregar colliders 2D.",
                MessageType.Info
            );

            EditorGUILayout.Space();
            GUILayout.Label("Configuracion", EditorStyles.boldLabel);

            targetLayer = EditorGUILayout.TextField("Target Layer", targetLayer);
            colliderType = (Collider2DProxy.ColliderType)EditorGUILayout.EnumPopup("Tipo de Collider", colliderType);
            disableCollider3D = EditorGUILayout.Toggle("Desactivar Collider 3D", disableCollider3D);

            EditorGUILayout.Space();
            GUILayout.Label($"Objetos seleccionados: {Selection.gameObjects.Length}", EditorStyles.miniLabel);

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(Selection.gameObjects.Length == 0))
            {
                if (GUILayout.Button("Agregar Collider2DProxy a Seleccion", GUILayout.Height(30)))
                {
                    AddProxyToSelection();
                }

                EditorGUILayout.Space();

                if (GUILayout.Button("Solo Agregar BoxCollider2D (sin script)", GUILayout.Height(25)))
                {
                    AddBoxCollider2DOnly();
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            GUILayout.Label("Utilidades", EditorStyles.boldLabel);

            if (GUILayout.Button("Crear Layers (Ground, Wall, Enemy)"))
            {
                CreateRequiredLayers();
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Workflow recomendado:\n" +
                "1. Crear tus objetos 3D (cubos, meshes)\n" +
                "2. Seleccionarlos en la Hierarchy\n" +
                "3. Asignar Layer correcto (Ground/Wall)\n" +
                "4. Hacer click en 'Agregar Collider2DProxy'\n" +
                "5. El personaje 2D detectara los objetos",
                MessageType.None
            );
        }

        private void AddProxyToSelection()
        {
            int count = 0;
            foreach (GameObject obj in Selection.gameObjects)
            {
                Undo.RecordObject(obj, "Add Collider2DProxy");

                Collider2DProxy proxy = obj.GetComponent<Collider2DProxy>();
                if (proxy == null)
                {
                    proxy = Undo.AddComponent<Collider2DProxy>(obj);
                }

                // Usar SerializedObject para modificar propiedades privadas
                SerializedObject serializedProxy = new SerializedObject(proxy);
                serializedProxy.FindProperty("targetLayer").stringValue = targetLayer;
                serializedProxy.FindProperty("colliderType").enumValueIndex = (int)colliderType;
                serializedProxy.ApplyModifiedProperties();

                // Desactivar collider 3D si se solicito
                if (disableCollider3D)
                {
                    Collider col3D = obj.GetComponent<Collider>();
                    if (col3D != null)
                    {
                        Undo.RecordObject(col3D, "Disable 3D Collider");
                        col3D.enabled = false;
                    }
                }

                // Llamar setup
                proxy.SetupCollider();

                count++;
                EditorUtility.SetDirty(obj);
            }

            Debug.Log($"[Hybrid 2D/3D] Collider2DProxy agregado a {count} objetos");
        }

        private void AddBoxCollider2DOnly()
        {
            int count = 0;
            foreach (GameObject obj in Selection.gameObjects)
            {
                Undo.RecordObject(obj, "Add BoxCollider2D");

                BoxCollider2D col2D = obj.GetComponent<BoxCollider2D>();
                if (col2D == null)
                {
                    col2D = Undo.AddComponent<BoxCollider2D>(obj);

                    // Intentar obtener tamanio del renderer
                    Renderer renderer = obj.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        col2D.size = new Vector2(renderer.bounds.size.x, renderer.bounds.size.y);
                    }
                }

                // Asignar layer
                int layer = LayerMask.NameToLayer(targetLayer);
                if (layer != -1)
                {
                    obj.layer = layer;
                }

                // Desactivar collider 3D
                if (disableCollider3D)
                {
                    Collider col3D = obj.GetComponent<Collider>();
                    if (col3D != null)
                    {
                        Undo.RecordObject(col3D, "Disable 3D Collider");
                        col3D.enabled = false;
                    }
                }

                count++;
                EditorUtility.SetDirty(obj);
            }

            Debug.Log($"[Hybrid 2D/3D] BoxCollider2D agregado a {count} objetos");
        }

        private void CreateRequiredLayers()
        {
            CreateLayer("Ground");
            CreateLayer("Wall");
            CreateLayer("Enemy");
            CreateLayer("Player");

            Debug.Log("[Hybrid 2D/3D] Layers creados: Ground, Wall, Enemy, Player");
        }

        private void CreateLayer(string layerName)
        {
            // Verificar si ya existe
            if (LayerMask.NameToLayer(layerName) != -1)
            {
                return;
            }

            // Abrir TagManager
            SerializedObject tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]
            );

            SerializedProperty layersProp = tagManager.FindProperty("layers");

            // Buscar slot vacio (layers 8-31 son user layers)
            for (int i = 8; i < 32; i++)
            {
                SerializedProperty layerProp = layersProp.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(layerProp.stringValue))
                {
                    layerProp.stringValue = layerName;
                    tagManager.ApplyModifiedProperties();
                    Debug.Log($"[Hybrid 2D/3D] Layer '{layerName}' creado en slot {i}");
                    return;
                }
            }

            Debug.LogWarning($"[Hybrid 2D/3D] No hay slots disponibles para crear layer '{layerName}'");
        }
    }
}
