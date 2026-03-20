# SESIÓN 18 - Sistema de Chakras (Core del Personaje)

**Fecha:** 2026-01-26
**Proyecto:** NABHI Character Controller Prototype
**Enfoque:** Implementación completa del Sistema de Chakras

---

## 1. RESUMEN EJECUTIVO

Esta sesión se centró en la implementación del **Sistema de Chakras**, el sistema de habilidades principal del personaje Ed según el GDD (sección 4.2). Se implementaron las 8 habilidades especiales que definen la jugabilidad única de NABHI.

### Logros Principales:
- Sistema completo de 8 Chakras implementado
- Sistema de energía con regeneración
- Controlador de debug para testing desde Inspector
- Arquitectura extensible basada en herencia y interfaces
- Sistema de activación Toggle (press to activate/deactivate)

---

## 2. CONTEXTO DEL PRODUCTO

### 2.1 ¿Qué son los Chakras en NABHI?

Los Chakras son habilidades especiales que Ed (el protagonista) desbloquea a lo largo del juego. Cada Chakra está asociado a:
- Un **color** específico (basado en los chakras tradicionales)
- Una **ubicación corporal** (Corona, Tercer Ojo, Garganta, etc.)
- Una **función única** que ayuda a superar obstáculos o enfrentar enemigos

### 2.2 Los 8 Chakras Implementados

| # | Chakra | Color | Ubicación | Función |
|---|--------|-------|-----------|---------|
| 1 | **Float** | Rosa | Corona | Levitación mientras se mantiene activo |
| 2 | **Invisibility** | Azul | Tercer Ojo | Invisibilidad (3x consumo de energía) |
| 3 | **Tremor** | Verde | Corazón | AOE: aturde enemigos y destruye estructuras |
| 4 | **EchoSense** | Naranja | Sacro | Revela zonas ocultas desde puntos específicos |
| 5 | **RemoteHack** | Amarillo | Plexo Solar | Hackea terminales electrónicas cercanas |
| 6 | **EMP** | Cian | Garganta | Desactiva enemigos electrónicos/mecánicos |
| 7a | **Telekinesis** | Rojo | Raíz | Mueve objetos con control del mouse |
| 7b | **GravityPulse** | Rojo | Raíz | Ralentiza el tiempo en un área local |

> **Nota:** Chakra 7 tiene dos opciones (7a y 7b). Ambas están implementadas para testing, pero en el juego final el jugador elegirá una.

### 2.3 Modos de Activación

| Modo | Comportamiento | Chakras |
|------|----------------|---------|
| **Continuous** | Toggle on/off, consume energía por segundo | Float, Invisibility, Telekinesis, GravityPulse |
| **Instant** | Efecto inmediato, cooldown después | Tremor, EMP |
| **Contextual** | Requiere contexto específico (punto de eco, terminal) | EchoSense, RemoteHack |

---

## 3. ARQUITECTURA DEL SISTEMA

### 3.1 Diagrama de Componentes

```
Player (GameObject)
├── EnergySystem          → Gestión de energía
├── ChakraSystem          → Manager principal de chakras
├── ChakraDebugController → Testing desde Inspector
│
└── [Chakras] (pueden ser hijos o en el mismo GO)
    ├── ChakraFloat
    ├── ChakraInvisibility
    ├── ChakraTremor
    ├── ChakraEchoSense
    ├── ChakraRemoteHack
    ├── ChakraEMP
    ├── ChakraTelekinesis
    └── ChakraGravityPulse
```

### 3.2 Flujo de Datos

```
[Input del Jugador]
        ↓
[ChakraSystem] ←→ [EnergySystem]
        ↓
[ChakraBase (seleccionado)]
        ↓
[Efecto en el mundo del juego]
```

### 3.3 Estructura de Archivos

```
Assets/_Project/
├── Scripts/
│   └── Chakras/
│       ├── ChakraBase.cs           → Clase base abstracta
│       ├── ChakraSystem.cs         → Manager principal
│       ├── EnergySystem.cs         → Sistema de energía
│       │
│       ├── Abilities/
│       │   ├── ChakraFloat.cs
│       │   ├── ChakraInvisibility.cs
│       │   ├── ChakraTremor.cs
│       │   ├── ChakraEchoSense.cs
│       │   ├── ChakraRemoteHack.cs
│       │   ├── ChakraEMP.cs
│       │   ├── ChakraTelekinesis.cs
│       │   └── ChakraGravityPulse.cs
│       │
│       ├── DebugTools/
│       │   └── ChakraDebugController.cs
│       │
│       └── UI/
│           ├── ChakraWheelUI.cs    → (Preparado para futuro)
│           └── EnergyBarUI.cs      → (Preparado para futuro)
│
└── Editor/
    └── ChakraDebugControllerEditor.cs → Inspector personalizado
```

---

## 4. PATRONES DE DISEÑO UTILIZADOS

### 4.1 Template Method Pattern (Patrón Plantilla)

**Ubicación:** `ChakraBase.cs`

```csharp
public abstract class ChakraBase : MonoBehaviour
{
    // Método plantilla - define el algoritmo
    public bool TryActivate()
    {
        if (!CanActivate()) return false;

        isActive = true;
        OnActivate();  // Hook para subclases
        return true;
    }

    // Hooks abstractos que las subclases implementan
    protected abstract void OnActivate();
    protected abstract void OnDeactivate();
    protected abstract string GetChakraDescription();
}
```

**Beneficio:** Cada chakra solo implementa su comportamiento específico, la lógica común está centralizada.

### 4.2 Observer Pattern (Patrón Observador)

**Ubicación:** `ChakraSystem.cs`, `EnergySystem.cs`

```csharp
// Eventos en ChakraSystem
public event Action<ChakraType> OnChakraSelected;
public event Action<ChakraType> OnChakraActivated;
public event Action<ChakraType> OnChakraDeactivated;
public event Action<bool> OnWheelToggled;
public event Action<ChakraType> OnChakraUnlocked;

// Eventos en EnergySystem
public event Action<float, float> OnEnergyChanged;
```

**Beneficio:** La UI y otros sistemas pueden reaccionar a cambios sin acoplamiento directo.

### 4.3 Strategy Pattern (Patrón Estrategia)

**Ubicación:** Cada `ChakraXXX.cs` es una estrategia diferente

```csharp
// ChakraSystem mantiene un diccionario de estrategias
private Dictionary<ChakraType, ChakraBase> chakras;

// Activar la estrategia seleccionada
public bool TryActivateChakra(ChakraType type)
{
    var chakra = chakras[type];
    return chakra.TryActivate();
}
```

**Beneficio:** Fácil agregar nuevos chakras sin modificar el sistema existente.

### 4.4 Component Pattern (Unity)

**Ubicación:** Todo el sistema

Cada chakra es un `MonoBehaviour` independiente que puede agregarse o removerse del Player. El `ChakraSystem` los descubre automáticamente con `GetComponentsInChildren<ChakraBase>()`.

**Beneficio:** Configuración flexible desde el Editor de Unity.

### 4.5 Interface Segregation

**Ubicación:** Interfaces en `ChakraTremor.cs`, `ChakraEMP.cs`

```csharp
public interface IStunnable
{
    void ApplyStun(float duration);
    bool IsStunned { get; }
}

public interface IDestructible
{
    void TakeDamage(int damage);
    bool IsDestroyed { get; }
}

public interface IEMPTarget
{
    void ApplyEMPEffect(float duration);
    bool IsDisabled { get; }
}
```

**Beneficio:** Los objetos del mundo implementan solo las interfaces que necesitan.

---

## 5. PARADIGMAS DE PROGRAMACIÓN

### 5.1 Programación Orientada a Objetos (OOP)

- **Herencia:** `ChakraBase` → `ChakraFloat`, `ChakraInvisibility`, etc.
- **Encapsulamiento:** Campos privados con propiedades públicas de solo lectura
- **Polimorfismo:** `ChakraSystem` trabaja con `ChakraBase`, no con tipos concretos
- **Abstracción:** Interfaces para interacción con el mundo del juego

### 5.2 Programación Orientada a Eventos

```csharp
// Suscripción a eventos
energySystem.OnEnergyChanged += OnEnergyChanged;
chakraSystem.OnChakraActivated += HandleChakraActivated;

// Emisión de eventos
OnChakraActivated?.Invoke(type);
```

### 5.3 Programación Basada en Componentes

Cada funcionalidad es un componente separado que puede combinarse:
- `EnergySystem` - Solo gestiona energía
- `ChakraSystem` - Solo gestiona selección/activación
- `ChakraXXX` - Solo implementa una habilidad específica

---

## 6. SISTEMA DE ENERGÍA

### 6.1 Configuración

```csharp
[Header("Energy Settings")]
[SerializeField] private float maxEnergy = 100f;
[SerializeField] private float energyRegenRate = 5f;      // Por segundo
[SerializeField] private float regenDelay = 2f;           // Después de usar
```

### 6.2 Consumo por Chakra

| Chakra | Tipo de Consumo | Valor Base |
|--------|-----------------|------------|
| Float | Por segundo | `energyCostPerSecond` (configurable) |
| Invisibility | Por segundo | 3x el costo normal |
| Tremor | Instantáneo | `energyCost` fijo |
| EchoSense | Instantáneo | `energyCost` fijo |
| RemoteHack | Durante hackeo | Costo por segundo mientras hackea |
| EMP | Instantáneo | `energyCost` fijo |
| Telekinesis | Por segundo | Mientras sostiene objeto |
| GravityPulse | Por segundo | Mientras está activo |

### 6.3 API del EnergySystem

```csharp
// Propiedades
public float CurrentEnergy { get; }
public float MaxEnergy { get; }
public float EnergyPercent { get; }
public bool IsRegenerating { get; }

// Métodos
public bool HasEnoughEnergy(float amount);
public bool TryConsumeEnergy(float amount);
public bool ConsumeEnergyPerSecond(float ratePerSecond);
public void StopUsingEnergy();
public void RestoreEnergy(float amount);
public void RestoreFullEnergy();
```

---

## 7. CONTROLES DEL SISTEMA

### 7.1 Input Configurado

| Tecla | Acción |
|-------|--------|
| **E** | Activar/Desactivar chakra seleccionado (Toggle) |
| **Tab** (mantener) | Abrir rueda de selección |
| **Alt** | Cambio rápido al siguiente chakra |
| **1-8** | Selección directa por número (cuando la rueda está abierta) |

### 7.2 Sistema Toggle

Decisión de diseño importante: Los chakras continuos usan **Toggle** en lugar de **Hold**.

**Razón:** El usuario indicó que mantener presionado E mientras maniobra es tedioso. Con Toggle:
- Press E → Activa el chakra
- Press E de nuevo → Desactiva el chakra
- Se desactiva automáticamente si la energía llega a 0

```csharp
private void ToggleOrActivateChakra()
{
    if (activeChakra != ChakraType.None)
    {
        var currentChakra = GetChakra(activeChakra);
        if (currentChakra.ActivationMode == ChakraActivationMode.Continuous)
        {
            if (activeChakra == selectedChakra)
            {
                DeactivateCurrentChakra();  // Toggle OFF
                return;
            }
        }
    }
    TryActivateSelectedChakra();  // Toggle ON
}
```

---

## 8. DETALLE DE CADA CHAKRA

### 8.1 ChakraFloat (Levitación)

**Archivo:** `Abilities/ChakraFloat.cs`

**Mecánica:**
- Reduce la gravedad del personaje
- Aplica fuerza hacia arriba
- Limita velocidad de caída

**Parámetros configurables:**
```csharp
[SerializeField] private float floatForce = 8f;
[SerializeField] private float maxUpwardSpeed = 4f;
[SerializeField] private float floatGravityScale = 0.2f;
[SerializeField] private float floatMaxFallSpeed = 2f;
```

**Física:**
```csharp
private void ApplyFloatPhysics()
{
    rb.gravityScale = floatGravityScale;

    if (rb.velocity.y < maxUpwardSpeed)
        rb.AddForce(Vector2.up * floatForce, ForceMode2D.Force);

    if (rb.velocity.y < -floatMaxFallSpeed)
        rb.velocity = new Vector2(rb.velocity.x, -floatMaxFallSpeed);
}
```

---

### 8.2 ChakraInvisibility (Invisibilidad)

**Archivo:** `Abilities/ChakraInvisibility.cs`

**Mecánica:**
- Reduce el alpha del sprite del personaje
- Desactiva detección por enemigos (layer de detección)
- Consume 3x energía normal

**Parámetros configurables:**
```csharp
[SerializeField] private float invisibleAlpha = 0.2f;
[SerializeField] private float fadeSpeed = 5f;
[SerializeField] private float energyMultiplier = 3f;
```

**Integración con enemigos:**
Los enemigos deben verificar si el jugador es visible antes de detectarlo.

---

### 8.3 ChakraTremor (Temblor)

**Archivo:** `Abilities/ChakraTremor.cs`

**Mecánica:**
- Efecto AOE instantáneo
- Aturde enemigos (IStunnable)
- Destruye estructuras frágiles (IDestructible)
- Aplica knockback
- Screen shake

**Parámetros configurables:**
```csharp
[SerializeField] private float tremblRadius = 5f;
[SerializeField] private float stunDuration = 2f;
[SerializeField] private float knockbackForce = 8f;
[SerializeField] private int damage = 10;
[SerializeField] private int structureDamage = 50;
```

**Detección:**
```csharp
Collider2D[] enemies = Physics2D.OverlapCircleAll(origin, tremblRadius, enemyLayer);
Collider2D[] destructibles = Physics2D.OverlapCircleAll(origin, tremblRadius, destructibleLayer);
```

---

### 8.4 ChakraEchoSense (Eco Sensitivo)

**Archivo:** `Abilities/ChakraEchoSense.cs`

**Mecánica:**
- Solo funciona cerca de "Puntos de Eco" (orbes etéreos)
- Revela zonas y caminos ocultos
- Efecto visual de expansión

**Parámetros configurables:**
```csharp
[SerializeField] private float detectionRadius = 3f;
[SerializeField] private float revealDuration = 5f;
[SerializeField] private float revealRadius = 10f;
```

**Requiere:** Objetos con tag "EchoPoint" o layer específico en la escena.

---

### 8.5 ChakraRemoteHack (Hackeo Remoto)

**Archivo:** `Abilities/ChakraRemoteHack.cs`

**Mecánica:**
- Detecta terminales cercanas (IHackable)
- Proceso de hackeo con barra de progreso
- Puede hackear a través de paredes

**Parámetros configurables:**
```csharp
[SerializeField] private float hackRange = 8f;
[SerializeField] private float hackDuration = 1.5f;
[SerializeField] private bool canHackThroughWalls = true;
```

**Interface requerida:**
```csharp
public interface IHackable
{
    bool CanBeHacked { get; }
    void OnHackStart();
    void OnHackComplete();
    void OnHackInterrupted();
}
```

---

### 8.6 ChakraEMP (Pulso Electromagnético)

**Archivo:** `Abilities/ChakraEMP.cs`

**Mecánica:**
- Pulso AOE que desactiva enemigos electrónicos
- Efecto visual de onda expansiva
- Puede mejorarse con amuletos (sistema futuro)

**Parámetros configurables:**
```csharp
[SerializeField] private float empRadius = 6f;
[SerializeField] private float disableDuration = 3f;
[SerializeField] private int empDamage = 15;
```

**Interface requerida:**
```csharp
public interface IEMPTarget
{
    void ApplyEMPEffect(float duration);
    bool IsDisabled { get; }
}
```

---

### 8.7 ChakraTelekinesis (Telecinesis)

**Archivo:** `Abilities/ChakraTelekinesis.cs`

**Mecánica:**
- Detecta objetos movibles cercanos (ITelekinetic)
- Control con mouse para mover objetos
- Puede lanzar objetos contra enemigos

**Parámetros configurables:**
```csharp
[SerializeField] private float grabRange = 5f;
[SerializeField] private float holdDistance = 3f;
[SerializeField] private float moveSpeed = 10f;
[SerializeField] private float throwForce = 15f;
[SerializeField] private int throwDamage = 20;
```

**Componente auxiliar:** `TelekineticObject` - Se agrega a objetos que pueden ser movidos.

---

### 8.8 ChakraGravityPulse (Pulso Gravitacional)

**Archivo:** `Abilities/ChakraGravityPulse.cs`

**Mecánica:**
- Crea un área de "slow motion" local
- Afecta enemigos, proyectiles, plataformas
- El jugador NO es afectado
- Efecto visual de distorsión

**Parámetros configurables:**
```csharp
[SerializeField] private float pulseRadius = 8f;
[SerializeField] private float slowFactor = 0.3f;
[SerializeField] private float maxDuration = 5f;
```

**Implementación del slow local:**
```csharp
// En lugar de Time.timeScale, modifica la velocidad de objetos individuales
foreach (var affected in affectedObjects)
{
    affected.rb.velocity *= slowFactor;
    affected.animator?.speed = slowFactor;
}
```

---

## 9. SISTEMA DE DEBUG (ChakraDebugController)

### 9.1 Propósito

Permite probar todos los chakras desde el Inspector de Unity sin necesidad de UI completa.

### 9.2 Funcionalidades

- **Toggles individuales:** Desbloquear/bloquear cada chakra
- **Dropdown de selección:** Elegir chakra activo
- **Botones rápidos:**
  - "Desbloquear Todos"
  - "Bloquear Todos"
  - "Rellenar Energía"
- **Estado en tiempo real:** Muestra chakra activo y barra de energía

### 9.3 Editor Personalizado

**Archivo:** `Editor/ChakraDebugControllerEditor.cs`

Características:
- Indicadores de color por chakra
- Barra de progreso para energía
- Sección de controles como referencia
- Repintado automático en Play Mode

---

## 10. ERRORES CORREGIDOS DURANTE LA SESIÓN

### 10.1 Conflicto de Namespace "Debug"

**Problema:** El namespace `NABHI.Chakras.Debug` conflictuaba con `UnityEngine.Debug`.

**Solución:**
1. Renombrar namespace a `NABHI.Chakras.DebugTools`
2. Agregar `using Debug = UnityEngine.Debug;` en archivos afectados

### 10.2 CameraShake no existía

**Problema:** `ChakraTremor` referenciaba `Character.CameraShake` que no existía.

**Solución:** Implementar screen shake simple con coroutine:
```csharp
private IEnumerator ShakeCamera()
{
    Vector3 originalPos = camTransform.localPosition;
    while (elapsed < screenShakeDuration)
    {
        float x = Random.Range(-1f, 1f) * screenShakeIntensity;
        float y = Random.Range(-1f, 1f) * screenShakeIntensity;
        camTransform.localPosition = originalPos + new Vector3(x, y, 0);
        elapsed += Time.deltaTime;
        yield return null;
    }
    camTransform.localPosition = originalPos;
}
```

### 10.3 TakeDamage con argumentos incorrectos

**Problema:** Se llamaba `TakeDamage(damage, origin)` pero la interface solo acepta `TakeDamage(float damage)`.

**Solución:** Remover el segundo argumento en ChakraTremor, ChakraEMP y ChakraTelekinesis.

---

## 11. PREPARACIÓN PARA TESTING

### 11.1 Configuración Mínima del Player

```
Player (GameObject)
├── Rigidbody2D
├── Collider2D
├── SpriteRenderer
├── EnergySystem
├── ChakraSystem
├── ChakraDebugController
├── ChakraFloat
├── ChakraInvisibility
├── ChakraTremor
├── ChakraEchoSense
├── ChakraRemoteHack
├── ChakraEMP
├── ChakraTelekinesis
└── ChakraGravityPulse
```

### 11.2 Elementos de Escena Necesarios para Testing

| Chakra | Elementos Requeridos |
|--------|---------------------|
| Float | Plataformas elevadas, espacios verticales |
| Invisibility | Enemigos con detección visual |
| Tremor | Enemigos (IStunnable), estructuras (IDestructible) |
| EchoSense | Puntos de Eco, zonas ocultas |
| RemoteHack | Terminales (IHackable) |
| EMP | Enemigos electrónicos (IEMPTarget) |
| Telekinesis | Objetos movibles (ITelekinetic/TelekineticObject) |
| GravityPulse | Enemigos, proyectiles, plataformas móviles |

### 11.3 Layers Recomendados

```
Layer 8:  Ground
Layer 9:  Player
Layer 10: Enemy
Layer 11: Destructible
Layer 12: Interactable (terminales, puntos de eco)
Layer 13: TelekineticObject
Layer 14: Projectile
```

---

## 12. PRÓXIMOS PASOS

### 12.1 Inmediatos (Testing)
- [ ] Crear escena de pruebas con elementos para cada chakra
- [ ] Implementar enemigos básicos con interfaces (IStunnable, IEMPTarget, IDamageable)
- [ ] Crear objetos de prueba (terminales, estructuras destruibles, objetos telekinéticos)
- [ ] Probar cada chakra individualmente

### 12.2 Corto Plazo
- [ ] Implementar UI de rueda de selección (ChakraWheelUI ya preparado)
- [ ] Implementar barra de energía visual (EnergyBarUI ya preparado)
- [ ] Balanceo de costos de energía y cooldowns
- [ ] Efectos visuales y de audio

### 12.3 Futuro
- [ ] Sistema de amuletos (mejoras para chakras)
- [ ] Sistema de desbloqueo progresivo
- [ ] Integración con sistema de guardado

---

## 13. NOTAS DE INTEGRACIÓN CON PROYECTO PRINCIPAL

### 13.1 Dependencias Externas

El sistema de Chakras depende de:
- `NABHI.Character.IDamageable` - Ya existe en el proyecto
- Layers configurados correctamente
- Rigidbody2D en el Player

### 13.2 Interfaces a Implementar en Otros Sistemas

Los siguientes sistemas del juego deben implementar interfaces:

**Enemigos:**
```csharp
public class Enemy : MonoBehaviour, IDamageable, IStunnable, IEMPTarget
{
    // Implementación...
}
```

**Terminales:**
```csharp
public class Terminal : MonoBehaviour, IHackable
{
    // Implementación...
}
```

**Estructuras Destruibles:**
```csharp
public class DestructibleWall : MonoBehaviour, IDestructible
{
    // Implementación...
}
```

### 13.3 Migración al Proyecto Principal

1. Copiar carpeta `Scripts/Chakras/` completa
2. Copiar `Editor/ChakraDebugControllerEditor.cs`
3. Configurar layers en el proyecto principal
4. Agregar componentes al Player
5. Implementar interfaces en objetos del mundo

---

## 14. REFERENCIAS

- **GDD:** Sección 4.2 - Sistema de Chakras
- **Sesión anterior:** RESUMEN_SESION_17.md (Controller 2D, sistema híbrido 2D/3D)
- **Namespaces principales:**
  - `NABHI.Chakras`
  - `NABHI.Chakras.Abilities`
  - `NABHI.Chakras.DebugTools`
  - `NABHI.Chakras.UI`

---

*Documento generado: Sesión 18 - Sistema de Chakras*
*Próxima sesión: Escenas de prueba y testing de chakras*
