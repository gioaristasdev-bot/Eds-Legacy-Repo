using UnityEngine;
using UnityEditor;
using NABHI.Chakras;
using NABHI.Chakras.DebugTools;

namespace NABHI.Editor
{
    [CustomEditor(typeof(ChakraDebugController))]
    public class ChakraDebugControllerEditor : UnityEditor.Editor
    {
        private SerializedProperty chakraSystemProp;
        private SerializedProperty energySystemProp;
        private SerializedProperty selectedChakraProp;

        // Chakra unlock properties
        private SerializedProperty chakra1Prop;
        private SerializedProperty chakra2Prop;
        private SerializedProperty chakra3Prop;
        private SerializedProperty chakra4Prop;
        private SerializedProperty chakra5Prop;
        private SerializedProperty chakra6Prop;
        private SerializedProperty chakra7aProp;
        private SerializedProperty chakra7bProp;

        // Read-only state
        private SerializedProperty currentlyActiveProp;
        private SerializedProperty currentEnergyProp;
        private SerializedProperty maxEnergyProp;

        private bool showChakraToggles = true;
        private bool showState = true;

        private void OnEnable()
        {
            chakraSystemProp = serializedObject.FindProperty("chakraSystem");
            energySystemProp = serializedObject.FindProperty("energySystem");
            selectedChakraProp = serializedObject.FindProperty("selectedChakra");

            chakra1Prop = serializedObject.FindProperty("chakra1_Float");
            chakra2Prop = serializedObject.FindProperty("chakra2_Invisibility");
            chakra3Prop = serializedObject.FindProperty("chakra3_Tremor");
            chakra4Prop = serializedObject.FindProperty("chakra4_EchoSense");
            chakra5Prop = serializedObject.FindProperty("chakra5_RemoteHack");
            chakra6Prop = serializedObject.FindProperty("chakra6_EMP");
            chakra7aProp = serializedObject.FindProperty("chakra7a_Telekinesis");
            chakra7bProp = serializedObject.FindProperty("chakra7b_GravityPulse");

            currentlyActiveProp = serializedObject.FindProperty("currentlyActive");
            currentEnergyProp = serializedObject.FindProperty("currentEnergy");
            maxEnergyProp = serializedObject.FindProperty("maxEnergy");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Header
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("CHAKRA DEBUG CONTROLLER", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Referencias
            EditorGUILayout.PropertyField(chakraSystemProp);
            EditorGUILayout.PropertyField(energySystemProp);

            EditorGUILayout.Space();
            DrawSeparator();

            // Seleccion de Chakra
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Chakra Seleccionado", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(selectedChakraProp, new GUIContent("Seleccionar"));

            EditorGUILayout.Space();
            DrawSeparator();

            // Chakras desbloqueados
            EditorGUILayout.Space();
            showChakraToggles = EditorGUILayout.Foldout(showChakraToggles, "Chakras Desbloqueados", true);

            if (showChakraToggles)
            {
                EditorGUI.indentLevel++;

                DrawChakraToggle(chakra1Prop, "1. Float (Levitacion)", new Color(1f, 0.4f, 0.7f));
                DrawChakraToggle(chakra2Prop, "2. Invisibilidad", new Color(0.2f, 0.4f, 1f));
                DrawChakraToggle(chakra3Prop, "3. Temblor", new Color(0.2f, 0.8f, 0.4f));
                DrawChakraToggle(chakra4Prop, "4. Eco Sensitivo", new Color(1f, 0.6f, 0.2f));
                DrawChakraToggle(chakra5Prop, "5. Hacker Remota", new Color(1f, 0.9f, 0.2f));
                DrawChakraToggle(chakra6Prop, "6. PEM", new Color(0.4f, 0.8f, 1f));

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Chakra 7 (Elegir una opcion para testear):", EditorStyles.miniLabel);
                DrawChakraToggle(chakra7aProp, "7a. Telecinesis", new Color(0.9f, 0.2f, 0.2f));
                DrawChakraToggle(chakra7bProp, "7b. Pulso Gravitacional", new Color(0.9f, 0.2f, 0.2f));

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            DrawSeparator();

            // Botones de accion
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Acciones Rapidas", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Desbloquear Todos", GUILayout.Height(30)))
            {
                ((ChakraDebugController)target).UnlockAll();
            }

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Bloquear Todos", GUILayout.Height(30)))
            {
                ((ChakraDebugController)target).LockAll();
            }

            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("Rellenar Energia", GUILayout.Height(30)))
            {
                ((ChakraDebugController)target).RefillEnergy();
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            DrawSeparator();

            // Estado actual (solo lectura)
            EditorGUILayout.Space();
            showState = EditorGUILayout.Foldout(showState, "Estado Actual (Runtime)", true);

            if (showState)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.PropertyField(currentlyActiveProp, new GUIContent("Chakra Activo"));

                // Barra de energia
                float energy = currentEnergyProp.floatValue;
                float maxEnergy = maxEnergyProp.floatValue;
                float percent = maxEnergy > 0 ? energy / maxEnergy : 0;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Energia:", GUILayout.Width(60));

                Rect rect = EditorGUILayout.GetControlRect(GUILayout.Height(20));
                EditorGUI.ProgressBar(rect, percent, $"{energy:F0} / {maxEnergy:F0}");

                EditorGUILayout.EndHorizontal();
                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.Space();
            DrawSeparator();

            // Controles info
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Controles", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "E = Activar/Desactivar chakra\n" +
                "Tab (Hold) = Abrir rueda de seleccion\n" +
                "Alt = Cambio rapido de chakra",
                MessageType.Info
            );

            serializedObject.ApplyModifiedProperties();

            // Repintar en play mode para actualizar estado
            if (Application.isPlaying)
            {
                Repaint();
            }
        }

        private void DrawChakraToggle(SerializedProperty prop, string label, Color color)
        {
            EditorGUILayout.BeginHorizontal();

            // Color indicator
            Color oldColor = GUI.backgroundColor;
            GUI.backgroundColor = prop.boolValue ? color : Color.gray;

            GUIStyle colorBox = new GUIStyle(GUI.skin.box);
            colorBox.fixedWidth = 20;
            colorBox.fixedHeight = 18;
            GUILayout.Box("", colorBox);

            GUI.backgroundColor = oldColor;

            // Toggle
            prop.boolValue = EditorGUILayout.Toggle(prop.boolValue, GUILayout.Width(20));
            EditorGUILayout.LabelField(label);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSeparator()
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        }
    }
}
