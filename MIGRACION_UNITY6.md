# Plan de migración a Unity 6 — Nabhi: Ed's Legacy

**Fecha de auditoría:** 2026-08-10
**Rama auditada:** `merge-fase-2` @ `11be5fa4` (post-merge `nivel-reina-merge` + `animations-daniel`)
**Versión actual:** Unity **2022.3.62f2** (LTS) · URP **14.0.12** · Color Space: Linear

---

## 1. Resumen ejecutivo

La migración es **viable**, pero **no es un "cambiar de versión y listo"**. El bloqueante real no es
el código propio del juego — que es casi todo compatible — sino **dos dependencias de terceros**:

1. **Quibli** (shaders toon + post-proceso). Está en el *núcleo* del render: sus shaders pintan
   **~233 materiales** y su Renderer Feature usa la API de render **obsoleta** en URP 17.
2. **Archivos `.psb` fuente que no están en el repositorio** (incluido el rig de Ed y el de la Reina).
   Sin ellos, la actualización del PSD Importer no se puede completar de forma reproducible.

**Recomendación:** migrar a **Unity 6 LTS** (rama `6000.x` LTS), **no** a la última versión Tech Stream.
Este proyecto está en producción y no necesita features de punta; necesita estabilidad.

> ⚠️ **Confirmar la versión exacta en Unity Hub antes de empezar.** No tengo Unity 6 instalado en
> esta máquina (solo 2022.3.51f1 y 2022.3.62f2), así que no puedo verificar cuál es el LTS vigente
> hoy. Instalar el LTS más alto disponible en el canal LTS.

---

## 2. Auditoría de compatibilidad

### 2.1 Paquetes de Unity

| Paquete | Actual | En Unity 6 | Riesgo | Nota |
|---|---|---|---|---|
| `render-pipelines.universal` | 14.0.12 | 17.x | 🔴 **Alto** | Render Graph. Ver §3.1 |
| `shadergraph` | 14.0.12 | 17.x | 🟡 Medio | Sube con URP; subgrafos suelen migrar solos |
| `cinemachine` | 2.10.5 | 3.x (o 2.10 legacy) | 🟢 **Bajo** | **No se usa en escenas.** Ver §3.4 |
| `2d.psdimporter` | 8.1.0 | 10.x/11.x | 🔴 **Alto** | Rig de Ed. Ver §3.2 |
| `2d.animation` | 9.2.0 (transitivo) | 10.x/11.x | 🔴 **Alto** | Skinning de personajes |
| `2d.common` | 8.1.0 | — | 🟡 Medio | Sube con los anteriores |
| `inputsystem` | 1.14.0 | 1.14+ | 🟢 Bajo | Ya en versión moderna |
| `textmeshpro` | 3.0.7 | ➡️ absorbido por `ugui` 2.x | 🟡 Medio | Ver §3.3 |
| `timeline` | 1.7.7 | 1.8.x | 🟢 Bajo | — |
| `visualscripting` | 1.9.4 | 1.9.x | 🟢 Bajo | — |
| `test-framework` | 1.1.33 | 1.4.x | 🟢 Bajo | — |
| `collab-proxy` | 2.11.2 | — | 🟢 Bajo | Candidato a eliminar (se usa Git) |

### 2.2 Assets de terceros

| Asset | Uso real medido | Riesgo |
|---|---|---|
| **Quibli** | 152 mats `StylizedLit` · 40 `Foliage` · 18 `Cloud2D` · 10 `Skybox` · 6 `Grass` · 5 `LightBeam` · 2 `Cloud3D` · **+ es el URP Config activo del proyecto** | 🔴 **Crítico** |
| **Hovl Studio** (VFX) | 15 shaders | 🟡 Medio |
| **Sprite Shaders Ultimate** | 11 shaders + `ShaderGUI` custom | 🟡 Medio |
| **Synty / Polygon** (SciFiCity, SciFiSpace, Nature) | 8 shaders, resto son mallas + mats | 🟢 Bajo |
| **SCI-FI UI Pack Pro** | UI, sin shaders custom detectados | 🟢 Bajo |
| **Monsters Creatures Fantasy** | sprites | 🟢 Bajo |

### 2.3 Código propio

**124 scripts propios** (307 en total contando terceros). Estado: **muy limpio para migrar.**

- ✅ **Cero** `ScriptableRendererFeature` / `ScriptableRenderPass` propios — todo el riesgo de
  Render Graph está en Quibli, no en nosotros.
- ✅ Cero `WWW`, cero `Application.LoadLevel`, cero `FindObjectsOfType`.
- 🟡 **9 usos de `FindObjectOfType`** → obsoleto (warning) en Unity 6. Migrar a
  `FindFirstObjectByType` / `FindAnyObjectByType`. Cambio mecánico, sin riesgo.
- 🟡 **7 usos de `OnGUI`** → sigue funcionando; revisar si son debug residual.
- ⚠️ Prácticamente **no hay `.asmdef`** propios (los 2 existentes son de Quibli): todo compila en
  `Assembly-CSharp`. No bloquea la migración, pero hace que **cada cambio recompile todo** — el
  ciclo de iteración durante la migración va a ser lento.

---

## 3. Riesgos detallados (ordenados por gravedad)

### 3.1 🔴 Quibli + Render Graph — *el bloqueante principal*

`Assets/Quibli/Post Process/Scripts/CompoundPass.cs:200` implementa:

```csharp
public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
```

Esta firma está **obsoleta en URP 17**. El propio archivo ya documenta el warning `CS0672` en sus
comentarios de cabecera (líneas 6-8), o sea que el vendor ya lo tenía identificado en URP 14.

Agrava el problema que **el URP Asset activo de todo el proyecto es un asset de ejemplo de Quibli**:

```
ProjectSettings/GraphicsSettings.asset → guid 524f0a41e9b5f451b98453c3fad05721
  = Assets/Quibli/Example URP Settings/Quibli URP Config.asset
```

Es decir: el pipeline de producción del juego apunta a un archivo dentro de la carpeta de demos de
un asset de terceros. Si Quibli se actualiza o se reimporta, ese archivo se puede sobrescribir.

**Opciones:**

| Opción | Coste | Resultado |
|---|---|---|
| **A. Actualizar Quibli** a su versión para Unity 6 | Bajo, *si el vendor la publicó* | Solución limpia. **Verificar primero en el Asset Store.** |
| **B. Compatibility Mode** de URP 17 | Muy bajo | Funciona pero es un parche: Unity lo va retirando. Solo como puente. |
| **C. Portar `CompoundPass` a Render Graph** | Alto | Solo si el vendor abandonó el asset. |
| **D. Sacar Quibli** | Muy alto | Habría que repintar 233 materiales. Descartado. |

➡️ **Acción previa a todo lo demás: confirmar si Quibli tiene versión compatible con Unity 6.**
Esta única respuesta decide si la migración dura ~1 semana (opción A) o varias (opción C).

### 3.2 🔴 Archivos `.psb` fuera del repositorio

`.gitignore:41` excluye `*.psb`. Consecuencia: hay `.meta` **huérfanos** — el `.meta` (con los datos
de rig, bones y skinning) está versionado, pero **el archivo fuente no**:

- `Assets/ED_Gio/_Project/Rig/Ed'sPose1Animation grande.psb` ← **el rig de Ed**
- `Assets/Nabi3.0/Sprites/ReinaBoss.psb` y `Assets/_Project/Inimigos/Boss/ReinaBoss.psb`
- `.psb` de Cyborg, Guardian y BreastplateArmor en `Assets/_Project/Inimigos/`

(Sí están versionados 5 `.psb`, añadidos antes de esa regla: `Ed90.psb`, `Guardian.psb`,
`Test_cybo.psb` y dos `BreastplateArmor.psb`.)

Al subir PSD Importer de 8.x a 10.x/11.x, Unity **reimporta** los `.psb`. Si el archivo no está,
el resultado depende de qué tenga cada quien en su disco local → migración no reproducible y
riesgo de perder el rig.

➡️ **Acción previa: recuperar todos los `.psb` y meterlos en el repo** (con Git LFS, que para eso
está) **antes** de tocar la versión de Unity.

### 3.3 🟡 TextMeshPro se fusiona con uGUI

En Unity 6, TMP deja de ser `com.unity.textmeshpro` y pasa a formar parte de `com.unity.ugui` 2.x.
El namespace `TMPro` se mantiene, así que **el código no se toca**. Lo que sí se regenera son los
assets de `Assets/TextMesh Pro/`.

Ojo con un detalle ya visto en este repo: en el commit de la ruleta, los `SDF.asset` de fuentes
(Anton, Bangers, Oswald, Roboto, LiberationSans…) aparecieron con **cientos de líneas eliminadas**
— son atlas dinámicos que Unity reescribe solo. Durante la migración van a volver a moverse; es
ruido esperado en el diff, no una pérdida.

### 3.4 🟢 Cinemachine: dependencia efectivamente muerta

Hallazgo de la auditoría: **no hay ni una sola `CinemachineVirtualCamera` ni `CinemachineBrain`**
en ninguna escena (`grep -i cinemachine` = 0 en las 7 escenas de build) ni en ningún prefab.

Lo único que existe son dos scripts que crean un `CinemachineImpulseSource` en runtime:
- `Assets/ED_Gio/_Project/Scripts/Character/CameraShake.cs:28`
- `Assets/ED_Gio/_Project/Scripts/Chakras/Abilities/ChakraTremor.cs:113`

⚠️ **Esto significa que el camera shake probablemente no se ve en el juego**: un Impulse Source sin
un Brain que lo escuche no produce movimiento. Es un bug **preexistente**, no causado por la
migración — pero conviene decidirlo ahora, porque cambia la estrategia:

- Si el shake **debe** funcionar → hay que montar Cinemachine de verdad → migrar a **Cinemachine 3.x**
  (API muy distinta: `CinemachineVirtualCamera` → `CinemachineCamera`, namespace `Unity.Cinemachine`).
- Si el shake se va a resolver de otra forma → **quitar Cinemachine del manifest** y borrar esas
  dependencias. Una dependencia pesada menos que migrar.

### 3.5 🟡 Otros

- **URP Renderers duplicados**: hay `URP-Balanced`, `URP-HighFidelity`, `URP-Performant` en
  `Assets/Settings/` que **no son los activos** (el activo es el de Quibli). Limpiar reduce
  superficie de migración.
- **`Assets/ED_Gio.unitypackage`** versionado dentro de `Assets/` — 
  Unity lo trata como asset. Sacarlo del repo.
- **Escenas residuales**: `Anumaciones.unity`, `Level1_Proyectiles_TEMP.unity` no están en build.

---

## 4. Plan de migración por fases

### Fase 0 — Prerrequisitos (bloqueantes, *antes* de instalar nada)

| # | Tarea | Responsable |
|---|---|---|
| 0.1 | **Verificar en el Asset Store si Quibli tiene versión Unity 6.** Decide todo lo demás. | — |
| 0.2 | Recuperar los `.psb` huérfanos y versionarlos con **Git LFS** (`.gitattributes`) | — |
| 0.3 | Confirmar versión LTS de Unity 6 vigente en Unity Hub e instalarla | — |
| 0.4 | Decidir el destino de Cinemachine (§3.4): migrar a 3.x o eliminar | — |
| 0.5 | Rama `migracion/unity6` desde `merge-fase-2` + tag de respaldo | — |
| 0.6 | Congelar merges de feature durante la migración | equipo |

### Fase 1 — Higiene previa (aún en 2022.3, todo verificable)

Trabajo de bajo riesgo que reduce ruido después. **Se hace y se valida en la versión actual.**

- 1.1 Mover el URP Config activo fuera de `Assets/Quibli/` → `Assets/Settings/` y reapuntar
  `GraphicsSettings`.
- 1.2 Sustituir los 9 `FindObjectOfType` por `FindFirstObjectByType`.
- 1.3 Borrar escenas y renderers no usados; sacar `ED_Gio.unitypackage` del repo.
- 1.4 (Opcional, alto valor) Introducir `.asmdef` para el código propio. Acelera mucho la
  iteración de las fases 2-4.
- ✅ **Checkpoint: el juego compila y corre igual en 2022.3.62f2.** Commit.

### Fase 2 — Salto de versión

- 2.1 Backup completo + verificar que la rama está limpia.
- 2.2 Abrir el proyecto con Unity 6 LTS. **Dejar que termine el reimport completo** (con ~840
  materiales y los atlas de TMP, esto tarda).
- 2.3 Guardar el log de consola íntegro → es el inventario real de trabajo pendiente.
- 2.4 Aplicar Render Pipeline Converter solo si hace falta (URP ya está en uso; no es una
  conversión Built-in→URP).
- ✅ **Checkpoint: el proyecto abre.** Se esperan errores; el objetivo es *catalogarlos*.

### Fase 3 — Resolver el render (el grueso del trabajo)

- 3.1 Actualizar Quibli a su versión Unity 6 (opción A de §3.1). Si no existe → activar
  **Compatibility Mode** en el URP Asset como puente y abrir la decisión de portar/reemplazar.
- 3.2 Verificar los 233 materiales Quibli: buscar magenta (shader roto) escena por escena.
- 3.3 Revisar Hovl Studio (VFX de chakras/proyectiles) y Sprite Shaders Ultimate.
- 3.4 Comparar contra capturas de referencia tomadas en Fase 1.
- ✅ **Checkpoint: ninguna superficie magenta, VFX correctos.**

### Fase 4 — 2D, animación y gameplay

- 4.1 Reimportar `.psb` con el PSD Importer nuevo; verificar bones y skin weights de **Ed**, Reina,
  Cyborg, Guardian y Acorazado.
- 4.2 Validar `Ed90_Rigged.controller` y los ~50 clips recién integrados de `animations-daniel`.
- 4.3 Validar el Input System: gamepad Xbox, y en particular **la ruleta de chakras (LB + stick
  derecho)** — es lo último que se integró y lo que más caro sale perder.
- 4.4 Validar TMP en los 7 escenarios de build.
- ✅ **Checkpoint: partida completa jugable de principio a fin.**

### Fase 5 — Validación y cierre

- 5.1 Recorrido completo por las 7 escenas de build (ver checklist §5).
- 5.2 Build de Windows y prueba en build (no solo en Editor).
- 5.3 Comparativa de rendimiento contra 2022.3 (Unity 6 suele mejorar por GPU Resident Drawer).
- 5.4 Merge a `master` + tag de versión.

---

## 5. Checklist de validación funcional

Ejecutar **en build**, no solo en Editor.

**Render**
- [ ] Ninguna superficie magenta en las 7 escenas de build
- [ ] Post-proceso Quibli (color grading, stylized detail) idéntico a la referencia
- [ ] Skybox, nubes, follaje y grass correctos
- [ ] VFX Hovl (chakras, proyectiles, portal) correctos

**Personaje Ed**
- [ ] Rig 2D completo sin deformaciones
- [ ] Flip por rotación Y (no scale) sigue correcto
- [ ] Todos los clips: Idle, Run, Walk, Jump, Fall, Dash, Combat1-3, Shootgun, Hit Reaction, Levitation, Hack, Tremor
- [ ] Transición con/sin arma (`isArmed`) y pickup de arma

**Chakras / ruleta** ⚠️ *lo más reciente — máxima prioridad*
- [ ] Rueda radial: LB mantenido + stick derecho
- [ ] Slow-motion al abrir la rueda
- [ ] Bloqueo de input mientras la rueda está abierta
- [ ] Float, Hack, Tremor, EMP, EchoSense, RemoteHack, Telekinesis, Invisibility
- [ ] Funciona en **Level1** *y* en **Nivel-REINA**
- [ ] Botones azul/gris según unlock

**Enemigos y jefe**
- [ ] Cyborg (ranged), Acorazado, Drone, Guardian, Turret
- [ ] BossReina: fade-in, Slam, DobleAgarre, Rayos; sincronización daño/animación

**Sistemas**
- [ ] Portales / FastTravel (recién integrado de `nivel-reina-merge`)
- [ ] Level2 carga correctamente
- [ ] SFX de player y enemigos
- [ ] Flujo Bootstrap → MainMenu → LoadingScene → VideoIntro → Level1 → Nivel-REINA → Créditos
- [ ] Guardado / SceneDatabase

---

## 6. Rollback

La migración vive en `migracion/unity6`. `merge-fase-2` **no se toca** hasta que el checklist §5
pase entero.

```bash
# Respaldo antes de empezar
git tag backup-pre-unity6 merge-fase-2
git switch -c migracion/unity6 merge-fase-2
```

Si hay que abortar: `git switch merge-fase-2` y **borrar `Library/`** (contiene la caché de import
de Unity 6, que 2022.3 no entiende).

⚠️ **Un proyecto abierto en Unity 6 no se puede volver a abrir en 2022.3.** Los `.unity`, `.prefab`
y `.asset` se reescriben con serialización nueva. Por eso la rama separada no es opcional.

---

## 7. Decisiones pendientes

1. **¿Quibli tiene versión para Unity 6?** — determina si el esfuerzo es de días o de semanas.
2. **¿Cinemachine se mantiene o se elimina?** — hoy está pagando el coste sin dar el beneficio.
3. **¿Se adopta Git LFS?** — necesario para versionar los `.psb` y saneable de paso para los
   `.unitypackage` y texturas grandes.
4. **¿Se meten `.asmdef` en Fase 1?** — coste inicial a cambio de iteración mucho más rápida durante
   toda la migración.
5. **¿Objetivo Unity 6 LTS o última Tech Stream?** — recomendación: **LTS**.

---

## Anexo — Comandos de auditoría usados

```bash
cat ProjectSettings/ProjectVersion.txt
cat Packages/manifest.json
grep -n "m_CustomRenderPipeline" -A3 ProjectSettings/GraphicsSettings.asset
git grep -l "ScriptableRenderPass" -- "*.cs"
git grep -c "FindObjectOfType" -- "Assets/ED_Gio/*.cs" "Assets/Nabi3.0/*.cs" "Assets/_Project/*.cs"
git ls-files "*.psb"          # comparar contra git ls-files "*.psb.meta"
```
