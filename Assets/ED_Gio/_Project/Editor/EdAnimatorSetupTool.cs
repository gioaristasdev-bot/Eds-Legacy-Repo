using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Editor Tool para configurar el Animator Controller de Ed.
/// Menu: Nabhi / Ed Animator Setup Tool
///
/// Funciones:
///   1. Auto-asigna clips de animacion a estados por nombre.
///   2. Aplica configuracion precisa de transiciones (duracion, exit time, interrupciones).
///   3. Muestra un informe del estado del controller.
/// </summary>
public class EdAnimatorSetupTool : EditorWindow
{
    // -------------------------------------------------------------------------
    #region CONFIGURACION DE CLIPS

    /// <summary>
    /// Mapeo: nombre del estado en el controller → nombre del archivo .anim (sin extension).
    /// Basado en los clips en Assets/_Project/Animações finais/
    /// </summary>
    private static readonly Dictionary<string, string> StateToClipName = new Dictionary<string, string>
    {
        // --- CON ARMA ---
        { "ED_Idle",        "Idle"            },
        { "ED_Walk",        "Walk"            },
        { "ED_Walk_Shoot",  "Walk"            },
        { "ED_Shoot",       "Idle"            }, // Idle armado mientras dispara quieto
        { "ED_Shoot 0",     "Idle"            },
        { "ED_Dash",        "Dash"            },
        { "ED_Jump",        "Jump"            },
        { "ED_Jump2",       "Jump"            },
        { "ED_Fall",        "Jump"            }, // Reutiliza Jump en descenso hasta tener clip propio
        { "ED_WallSlide",   "Wall slide"      },
        { "ED_Hit",         "Hit Reaction"    },
        { "GroundPound",    "Tremor"          },

        // --- SIN ARMA (estados " 0") ---
        { "ED_Idle 0",      "Idle without gun"           },
        { "ED_Walk 0",      "Walk without gun"           },
        { "ED_Dash 0",      "Dash without gun"           },
        { "ED_Jump 0",      "Jump without gun"           },
        { "ED_Jump2 0",     "Jump without gun"           },
        { "ED_Fall 0",      "Jump without gun"           },
        { "ED_WallSlide 0", "Wall slide without gun"     },
        { "ED_Hit 0",       "Hit Reaction without gun"   },
        { "GroundPound 0",  "Tremor"                     },

        // --- LEVITACION ---
        { "Levitation Start",              "Levitation Start"              },
        { "Levitation Loop",               "Levitation Loop"               },
        { "Levitation End",                "Levitation End"                },
        { "Levitation loop start without gun", "Levitation loop start without gun" },
        { "Levitation loop without gun",   "Levitation loop without gun"   },
        { "Levitation End without gun",    "Levitation End without gun"    },

        // --- HACKEO ---
        { "Hack Start",   "Hack Start"   },
        { "Hacking end",  "Hacking end"  },
    };

    #endregion

    // -------------------------------------------------------------------------
    #region CONFIGURACION DE TRANSICIONES

    private enum TransitionProfile
    {
        Movement,   // Sin exit time, blending rapido — ideal para estados de movimiento
        OneShot,    // Con exit time al 85% — para Hit, Death, Tremor
        ChainStart, // Sin exit time (pasa al siguiente en la cadena por exit time)
        ChainLoop,  // Sin exit time (sale cuando parametro cambia)
        ChainEnd,   // Con exit time (reproduce hasta el final antes de volver a Idle)
    }

    /// <summary>
    /// Profile asignado a cada estado del controller.
    /// Los estados no listados aquí no se tocan.
    /// </summary>
    private static readonly Dictionary<string, TransitionProfile> StateProfiles =
        new Dictionary<string, TransitionProfile>
    {
        { "ED_Idle",         TransitionProfile.Movement   },
        { "ED_Idle 0",       TransitionProfile.Movement   },
        { "ED_Walk",         TransitionProfile.Movement   },
        { "ED_Walk 0",       TransitionProfile.Movement   },
        { "ED_Walk_Shoot",   TransitionProfile.Movement   },
        { "ED_Shoot",        TransitionProfile.Movement   },
        { "ED_Shoot 0",      TransitionProfile.Movement   },
        { "ED_Dash",         TransitionProfile.Movement   },
        { "ED_Dash 0",       TransitionProfile.Movement   },
        { "ED_Jump",         TransitionProfile.Movement   },
        { "ED_Jump 0",       TransitionProfile.Movement   },
        { "ED_Jump2",        TransitionProfile.Movement   },
        { "ED_Jump2 0",      TransitionProfile.Movement   },
        { "ED_Fall",         TransitionProfile.Movement   },
        { "ED_Fall 0",       TransitionProfile.Movement   },
        { "ED_WallSlide",    TransitionProfile.Movement   },
        { "ED_WallSlide 0",  TransitionProfile.Movement   },

        { "ED_Hit",          TransitionProfile.OneShot    },
        { "ED_Hit 0",        TransitionProfile.OneShot    },
        { "GroundPound",     TransitionProfile.OneShot    },
        { "GroundPound 0",   TransitionProfile.OneShot    },

        { "Levitation Start",              TransitionProfile.ChainStart },
        { "Levitation loop start without gun", TransitionProfile.ChainStart },
        { "Levitation Loop",               TransitionProfile.ChainLoop  },
        { "Levitation loop without gun",   TransitionProfile.ChainLoop  },
        { "Levitation End",                TransitionProfile.ChainEnd   },
        { "Levitation End without gun",    TransitionProfile.ChainEnd   },

        { "Hack Start",   TransitionProfile.ChainStart },
        { "Hacking end",  TransitionProfile.ChainEnd   },
    };

    // Duraciones (segundos)
    private const float DUR_MOVEMENT    = 0.05f;
    private const float DUR_ONESHOT_IN  = 0.05f;
    private const float DUR_ONESHOT_OUT = 0.1f;
    private const float DUR_CHAIN       = 0.08f;

    #endregion

    // -------------------------------------------------------------------------
    #region VENTANA

    private AnimatorController controller;
    private Vector2 scrollPos;
    private string log = "";
    private bool showLog = true;

    [MenuItem("Nabhi/Ed Animator Setup Tool")]
    public static void OpenWindow()
    {
        var win = GetWindow<EdAnimatorSetupTool>("Ed Animator Setup");
        win.minSize = new Vector2(480, 520);
    }

    private void OnGUI()
    {
        GUILayout.Label("Ed Animator Setup Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        controller = (AnimatorController)EditorGUILayout.ObjectField(
            "Animator Controller", controller, typeof(AnimatorController), false);

        if (controller == null)
        {
            EditorGUILayout.HelpBox(
                "Arrastra aquí el archivo Ed90_Rigged.controller\n" +
                "(Assets/ED_Gio/_Project/Animations/Ed90_Rigged.controller)",
                MessageType.Info);
            return;
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Operaciones", EditorStyles.boldLabel);

        if (GUILayout.Button("1. Auto-asignar clips a estados", GUILayout.Height(32)))
            AssignClips();

        EditorGUILayout.Space(2);

        if (GUILayout.Button("2. Aplicar configuración de transiciones", GUILayout.Height(32)))
            ApplyTransitionSettings();

        EditorGUILayout.Space(2);

        if (GUILayout.Button("3. Aplicar TODO (clips + transiciones)", GUILayout.Height(36)))
        {
            AssignClips();
            ApplyTransitionSettings();
        }

        EditorGUILayout.Space(8);

        if (GUILayout.Button("Generar informe del controller"))
            GenerateReport();

        EditorGUILayout.Space(8);

        showLog = EditorGUILayout.Foldout(showLog, "Log");
        if (showLog)
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(260));
            EditorGUILayout.TextArea(log, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        if (GUILayout.Button("Limpiar log"))
            log = "";
    }

    #endregion

    // -------------------------------------------------------------------------
    #region ASIGNAR CLIPS

    private void AssignClips()
    {
        if (controller == null) return;

        Log("=== AUTO-ASIGNAR CLIPS ===");

        // Buscar todos los clips .anim en el proyecto dentro de "Animações finais"
        string[] guids = AssetDatabase.FindAssets(
            "t:AnimationClip",
            new[] { "Assets/_Project/Animações finais" }
        );

        var clipsByName = new Dictionary<string, AnimationClip>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip != null && !clipsByName.ContainsKey(clip.name))
                clipsByName[clip.name] = clip;
        }

        Log($"Clips encontrados: {clipsByName.Count}");
        foreach (var k in clipsByName.Keys)
            Log($"  • {k}");

        // Recorrer estados del controller
        var sm = controller.layers[0].stateMachine;
        int assigned = 0, skipped = 0, notFound = 0;

        foreach (var childState in sm.states)
        {
            var state = childState.state;
            string stateName = state.name;

            if (!StateToClipName.TryGetValue(stateName, out string clipName))
            {
                skipped++;
                continue;
            }

            if (!clipsByName.TryGetValue(clipName, out AnimationClip clip))
            {
                Log($"  ⚠ Clip '{clipName}' no encontrado para estado '{stateName}'");
                notFound++;
                continue;
            }

            // Solo actualizar si el clip cambió
            if (state.motion == clip)
            {
                Log($"  — '{stateName}' ya tiene '{clipName}' (sin cambios)");
                skipped++;
                continue;
            }

            Undo.RecordObject(controller, "Assign Clip");
            state.motion = clip;
            EditorUtility.SetDirty(controller);
            Log($"  ✓ '{stateName}' → '{clipName}'");
            assigned++;
        }

        AssetDatabase.SaveAssets();
        Log($"\nResultado: {assigned} asignados, {skipped} sin cambios, {notFound} clips no encontrados.");
    }

    #endregion

    // -------------------------------------------------------------------------
    #region APLICAR TRANSICIONES

    private void ApplyTransitionSettings()
    {
        if (controller == null) return;

        Log("\n=== CONFIGURAR TRANSICIONES ===");

        var sm = controller.layers[0].stateMachine;
        int modified = 0;

        // --- Transiciones de AnyState ---
        foreach (var t in sm.anyStateTransitions)
        {
            string destName = t.destinationState != null ? t.destinationState.name : "(null)";
            if (!StateProfiles.TryGetValue(destName, out var profile)) continue;

            ApplyProfile(t, profile, incoming: true);
            modified++;
            Log($"  AnyState → '{destName}' : {profile}");
        }

        // --- Transiciones salientes de cada estado ---
        foreach (var childState in sm.states)
        {
            var state = childState.state;
            string stateName = state.name;
            bool hasProfile = StateProfiles.TryGetValue(stateName, out var profile);

            foreach (var t in state.transitions)
            {
                string destName = t.destinationState != null ? t.destinationState.name : "(exit)";

                // Las salidas de OneShot/ChainEnd usan perfil "salida"
                if (hasProfile && (profile == TransitionProfile.OneShot ||
                                   profile == TransitionProfile.ChainEnd))
                {
                    ApplyProfile(t, profile, incoming: false);
                    modified++;
                    Log($"  '{stateName}' → '{destName}' (salida {profile})");
                }
                else if (StateProfiles.TryGetValue(destName, out var destProfile))
                {
                    ApplyProfile(t, destProfile, incoming: true);
                    modified++;
                    Log($"  '{stateName}' → '{destName}' : {destProfile}");
                }
            }
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Log($"\nTransiciones modificadas: {modified}");
    }

    private void ApplyProfile(AnimatorStateTransition t, TransitionProfile profile, bool incoming)
    {
        Undo.RecordObject(controller, "Apply Transition Profile");

        switch (profile)
        {
            case TransitionProfile.Movement:
                t.hasExitTime            = false;
                t.duration               = DUR_MOVEMENT;
                t.offset                 = 0f;
                t.interruptionSource     = TransitionInterruptionSource.SourceThenDestination;
                t.orderedInterruption    = true;
                t.canTransitionToSelf    = false;
                break;

            case TransitionProfile.OneShot:
                if (incoming)
                {
                    // Entrada a OneShot: sin exit time, blending rápido
                    t.hasExitTime         = false;
                    t.duration            = DUR_ONESHOT_IN;
                    t.interruptionSource  = TransitionInterruptionSource.None;
                    t.orderedInterruption = true;
                    t.canTransitionToSelf = false;
                }
                else
                {
                    // Salida de OneShot: esperar al 85% del clip
                    t.hasExitTime         = true;
                    t.exitTime            = 0.85f;
                    t.duration            = DUR_ONESHOT_OUT;
                    t.interruptionSource  = TransitionInterruptionSource.None;
                    t.orderedInterruption = true;
                    t.canTransitionToSelf = false;
                }
                break;

            case TransitionProfile.ChainStart:
                // Entrada: sin exit time. Salida: al terminar el clip pasa al siguiente.
                if (incoming)
                {
                    t.hasExitTime         = false;
                    t.duration            = DUR_CHAIN;
                    t.interruptionSource  = TransitionInterruptionSource.None;
                    t.orderedInterruption = true;
                    t.canTransitionToSelf = false;
                }
                else
                {
                    t.hasExitTime         = true;
                    t.exitTime            = 0.9f;
                    t.duration            = DUR_CHAIN;
                    t.interruptionSource  = TransitionInterruptionSource.None;
                    t.orderedInterruption = true;
                    t.canTransitionToSelf = false;
                }
                break;

            case TransitionProfile.ChainLoop:
                // El loop no tiene exit time; sale cuando el bool cambia.
                t.hasExitTime         = false;
                t.duration            = DUR_CHAIN;
                t.interruptionSource  = TransitionInterruptionSource.Source;
                t.orderedInterruption = true;
                t.canTransitionToSelf = false;
                break;

            case TransitionProfile.ChainEnd:
                if (incoming)
                {
                    t.hasExitTime         = false;
                    t.duration            = DUR_CHAIN;
                    t.interruptionSource  = TransitionInterruptionSource.None;
                    t.orderedInterruption = true;
                    t.canTransitionToSelf = false;
                }
                else
                {
                    // Reproducir hasta el final antes de volver a Idle
                    t.hasExitTime         = true;
                    t.exitTime            = 0.9f;
                    t.duration            = DUR_CHAIN;
                    t.interruptionSource  = TransitionInterruptionSource.None;
                    t.orderedInterruption = true;
                    t.canTransitionToSelf = false;
                }
                break;
        }
    }

    #endregion

    // -------------------------------------------------------------------------
    #region INFORME

    private void GenerateReport()
    {
        if (controller == null) return;

        Log("\n=== INFORME DEL CONTROLLER ===");

        // Parámetros
        Log("Parámetros:");
        foreach (var p in controller.parameters)
            Log($"  [{p.type}] {p.name}");

        // Parámetros faltantes
        var required = new[] { "isMoving", "isGrounded", "isJumping", "isFalling",
                                "isDashing", "isWallSliding", "isArmed", "isShooting",
                                "isLevitating", "isHacking", "JumpCount", "Hit", "GroundPound" };
        var existing = controller.parameters.Select(p => p.name).ToHashSet();
        var missing = required.Where(r => !existing.Contains(r)).ToArray();
        if (missing.Length > 0)
            Log($"⚠ PARÁMETROS FALTANTES: {string.Join(", ", missing)}");
        else
            Log("✓ Todos los parámetros requeridos presentes.");

        // Estados y sus clips
        Log("\nEstados y clips asignados:");
        var sm = controller.layers[0].stateMachine;
        foreach (var childState in sm.states)
        {
            var state = childState.state;
            string clipName = state.motion != null ? state.motion.name : "⚠ SIN CLIP";
            string mappedClip = StateToClipName.ContainsKey(state.name)
                ? StateToClipName[state.name]
                : "—";
            bool match = state.motion != null && state.motion.name == mappedClip;
            string indicator = state.motion == null ? "❌" : (match ? "✓" : "△");
            Log($"  {indicator} [{state.name}] → {clipName}  (esperado: {mappedClip})");
        }

        // Transiciones Any State
        Log($"\nTransiciones AnyState: {sm.anyStateTransitions.Length}");
        foreach (var t in sm.anyStateTransitions)
        {
            string dest = t.destinationState != null ? t.destinationState.name : "null";
            string conds = string.Join(", ", t.conditions.Select(c =>
                $"{c.parameter}{(c.mode == AnimatorConditionMode.If ? "=T" : c.mode == AnimatorConditionMode.IfNot ? "=F" : "")}"));
            Log($"  → {dest} | {conds} | exitTime:{t.hasExitTime} dur:{t.duration:F2}");
        }
    }

    #endregion

    // -------------------------------------------------------------------------
    #region UTILIDAD

    private void Log(string msg)
    {
        log += msg + "\n";
        Repaint();
    }

    #endregion
}
