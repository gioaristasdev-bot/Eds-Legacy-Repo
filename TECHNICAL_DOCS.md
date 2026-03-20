# Documentación Técnica — Nabhi EDs Legacy

> Versión Unity: **2022.3.51f1** | Render Pipeline: **URP 14.0.12**

---

## Tabla de Contenidos

1. [Visión General del Proyecto](#1-visión-general-del-proyecto)
2. [Estructura de Carpetas](#2-estructura-de-carpetas)
3. [Arquitectura y Patrones de Diseño](#3-arquitectura-y-patrones-de-diseño)
4. [Sistema de Personaje](#4-sistema-de-personaje)
5. [Sistema de Chakras](#5-sistema-de-chakras)
6. [Sistema de Energía](#6-sistema-de-energía)
7. [Sistema de Enemigos](#7-sistema-de-enemigos)
8. [Sistema de Armas y Proyectiles](#8-sistema-de-armas-y-proyectiles)
9. [Sistema de UI](#9-sistema-de-ui)
10. [Sistema de Escenas y Transiciones](#10-sistema-de-escenas-y-transiciones)
11. [Entorno Interactivo](#11-entorno-interactivo)
12. [Dependencias y Paquetes](#12-dependencias-y-paquetes)
13. [Layers y Tags](#13-layers-y-tags)
14. [Controles del Jugador](#14-controles-del-jugador)
15. [Flujo de Juego](#15-flujo-de-juego)

---

## 1. Visión General del Proyecto

**Nabhi EDs Legacy** es un juego 2.5D tipo **Metroidvania** en Unity 2022.3. El jugador controla a **ED**, un personaje que desbloquea progresivamente 8 habilidades basadas en los **Chakras**, cada una con mecánicas únicas. El mundo combina sprites 2D con perspectiva 3D, enemigos con IA basada en estados, y una UI estilo Sci-Fi.

**Características principales:**
- Movimiento 2D avanzado (coyote time, jump buffer, wall slide, doble salto)
- 8 chakras jugables con sistemas propios
- 5+ tipos de enemigos con IA definida
- Sistema de energía para los chakras
- Transiciones de escenas con fade
- Soporte para teclado/mouse y gamepad

---

## 2. Estructura de Carpetas

```
Assets/
│
├── ED_Gio/                        # Contenido del colaborador ED Gio
│   ├── _Project/
│   │   ├── Animations/            # Animaciones del personaje ED
│   │   │   └── FrameByFrame/      # Animaciones frame-by-frame
│   │   ├── Editor/                # Scripts de herramientas del editor
│   │   ├── Prefabs/               # Prefabs del personaje, armas y enemigos
│   │   └── Scripts/
│   │       ├── Chakras/           # Sistema de chakras completo
│   │       │   └── Abilities/     # Cada habilidad chakra
│   │       ├── Character/         # Controlador de personaje
│   │       └── Environment/       # Scripts de entorno
│   ├── Sprits/FrameNabhi/         # Spritesheet del personaje
│   └── 2D Animation Starter Pack/ # Asset de terceros (animación 2D)
│
├── Nabi3.0/                       # Proyecto principal
│   ├── Animations/                # Animaciones de nivel
│   ├── Animators/                 # Animator Controllers
│   ├── Audio/                     # Efectos de sonido
│   ├── Escenas/                   # Escenas del juego
│   ├── Fuentes/                   # Tipografías
│   ├── Materiales/                # Materiales 2D/3D/UI
│   ├── Prefabs/                   # Prefabs de nivel (jefes, puertas, etc.)
│   ├── Scripts/
│   │   └── UI/                    # Scripts de interfaz
│   ├── Sprites/                   # Sprites de nivel y jefes
│   └── VideosUI/                  # Videos para la UI
│
├── _Project/                      # Testing y enemigos
│   └── Scripts/
│       ├── Enemies/               # Scripts de enemigos
│       └── Testing/               # Scripts de prueba
│
├── Monsters Creatures Fantasy/    # Asset: criaturas de fantasía
├── PolygonNature/                 # Asset: naturaleza poligonal
├── PolygonSciFiCity/              # Asset: ciudad sci-fi
├── PolygonSciFiSpace/             # Asset: espacio sci-fi
├── Quibli/                        # Asset: shaders artísticos / post-process
├── SCI-FI UI Pack Pro/            # Asset: UI futurista
├── Sprite Shaders Ultimate/       # Asset: shaders avanzados para sprites
├── Synty/                         # Asset: UI/Interface futurista
└── TextMesh Pro/                  # Texto mejorado
```

---

## 3. Arquitectura y Patrones de Diseño

### Namespaces

```
NABHI                         # Namespace raíz del proyecto
├── Chakras                   # Todo el sistema de chakras
│   └── Abilities             # Implementaciones individuales
├── Character                 # Movimiento, salud, animación
├── Weapons                   # Armas y proyectiles
├── Enemies                   # IA y tipos de enemigos
└── UI                        # Interfaz de usuario
```

### Patrones utilizados

| Patrón | Dónde se usa |
|---|---|
| **Singleton** | `SceneTransitionManager`, `FadeManager`, `ChakraSystem` |
| **Observer / Events** | Chakras, energía, salud del jugador |
| **State Machine** | Estados de movimiento del jugador, estados de enemigos |
| **Template Method** | `ChakraBase` (clase abstracta con hooks) |
| **Component** | Arquitectura estándar de Unity |

### Interfaces principales

| Interfaz | Propósito |
|---|---|
| `IDamageable` | Puede recibir daño |
| `IStunnable` | Puede ser aturdido (por Tremor) |
| `IHackable` | Puede ser hackeado (por Remote Hack) |
| `IEMPTarget` | Objetivo del EMP (drones) |

---

## 4. Sistema de Personaje

### CharacterController2D
`Assets/ED_Gio/_Project/Scripts/Character/CharacterController2D.cs`

Controlador de movimiento 2D basado en física (`Rigidbody2D`).

**Parámetros de movimiento:**

| Parámetro | Valor | Descripción |
|---|---|---|
| `moveSpeed` | 8f | Velocidad base |
| `runSpeedMultiplier` | 1.5f | Multiplicador al correr |
| `accelerationTime` | 0.1f | Tiempo para alcanzar velocidad máxima |
| `decelerationTime` | 0.1f | Tiempo para frenar |
| `airControlMultiplier` | 0.8f | Control reducido en el aire |
| `maxFallSpeed` | 20f | Velocidad máxima de caída |
| `jumpForce` | 12f | Fuerza del salto |
| `jumpCutMultiplier` | 0.5f | Reducción al soltar salto antes de tiempo |
| `fallGravityMultiplier` | 1.5f | Gravedad extra al caer |
| `coyoteTime` | 0.15f | Tiempo de gracia para saltar al borde |
| `jumpBufferTime` | 0.1f | Tiempo para recordar input de salto anticipado |
| `maxAirJumps` | 1 | Cantidad de saltos extra en el aire |

**Propiedades públicas:**
- `IsGrounded` — si está en el suelo
- `IsWallSliding` — si se desliza en una pared
- `IsDashing` — si está haciendo dash
- `FacingDirection` — dirección en la que mira (-1 o 1)
- `Velocity` — velocidad actual

**Depende de:** `Rigidbody2D`, `CapsuleCollider2D`, `CharacterState`, `PlayerHealth`, `ChakraFloat`, `ChakraTremor`

---

### CharacterState
`Assets/ED_Gio/_Project/Scripts/Character/CharacterState.cs`

Gestiona el estado de movimiento actual.

**Estados disponibles:**
```
Idle → Walking → Running
     ↓
Jumping → Falling
     ↓
WallSliding → WallJumping
     ↓
Dashing
LedgeGrabbing → LedgeClimbing
Crouching
Swimming
```

**Prioridad de estados (mayor a menor):**
1. `Dashing`
2. `WallSliding`
3. `Jumping` / `Falling`
4. `Walking` / `Idle`

**Evento:** `OnStateChanged(from, to)`

---

### PlayerHealth
`Assets/ED_Gio/_Project/Scripts/Character/PlayerHealth.cs`

Sistema de salud con knockback e invencibilidad (I-frames).

| Parámetro | Valor | Descripción |
|---|---|---|
| `maxHealth` | 100 | Salud máxima |
| `knockbackForce` | (10f, 8f) | Fuerza de empuje al recibir daño |
| `knockbackDuration` | 0.3f | Duración del knockback |
| `invincibilityDuration` | 1.5f | Tiempo invulnerable tras golpe |
| `flickerSpeed` | 0.1f | Velocidad de parpadeo durante I-frames |

**Métodos clave:**
- `TakeDamage(float damage)` — recibe daño
- `Heal(float amount)` — se cura
- `ApplyKnockback(Vector2 direction)` — knockback direccional
- `ForceInvincibility(float duration)` — invencibilidad forzada
- `Revive()` — revive al jugador

**Eventos:** `OnHealthChanged`, `OnDeath`, `OnHit`

**Implementa:** `IDamageable`

---

### Animación

| Script | Descripción |
|---|---|
| `CharacterAnimator.cs` | Animaciones básicas (hit, muerte) |
| `HybridAnimationController.cs` | Sistema híbrido frame-by-frame + rigging |
| `AimController.cs` | Dirección de puntería (mouse o analog stick) |

---

## 5. Sistema de Chakras

Los chakras son las habilidades principales del juego. Hay 8 tipos, cada uno con mecánicas únicas.

### ChakraBase (Clase Abstracta)
`Assets/ED_Gio/_Project/Scripts/Chakras/ChakraBase.cs`

Todos los chakras heredan de esta clase.

**Variables principales:**

| Variable | Tipo | Descripción |
|---|---|---|
| `chakraType` | `ChakraType` | Identificador del chakra |
| `chakraName` | `string` | Nombre |
| `chakraColor` | `Color` | Color asociado |
| `activationMode` | `ChakraActivationMode` | Instant / Continuous / Contextual |
| `energyCostPerSecond` | `float` | Energía por segundo (Continuous) |
| `energyCostPerUse` | `float` | Energía por uso (Instant) |
| `cooldown` | `float` | Tiempo de espera entre usos |
| `isUnlocked` | `bool` | Si está desbloqueado |

**Métodos:**
- `TryActivate()` — intenta activar verificando energía y cooldown
- `Activate()` / `Deactivate()` — activa/desactiva
- `Unlock()` / `Lock()` — desbloquea/bloquea

**Eventos:** `OnChakraActivated`, `OnChakraDeactivated`, `OnChakraUnlocked`

---

### Los 8 Chakras

#### Chakra 1 — Float (Levitación)
`Abilities/ChakraFloat.cs` | Color: Rosa | Modo: **Continuous**

Permite levitar mientras se mantiene el botón de salto.

| Parámetro | Valor |
|---|---|
| `floatForce` | 12f |
| `maxUpwardSpeed` | 5f |
| `floatGravityScale` | 0.3f |
| `maxDescentSpeed` | 1.5f |

- Mantener ESPACIO = ascender (consume energía)
- Soltar ESPACIO = descender lentamente
- Desactiva la gravedad al ascender

---

#### Chakra 2 — Invisibilidad
`Abilities/ChakraInvisibility.cs` | Color: Azul | Modo: **Continuous**

Vuelve invisible al personaje, cambiando su layer.

| Parámetro | Valor |
|---|---|
| `invisibleAlpha` | 0.2f |
| `fadeSpeed` | 5f |
| `cancelOnAttack` | true |
| `cancelOnDamage` | true |
| `invisibleLayerName` | "InvisiblePlayer" |

- Consumo: 30f energía por segundo
- Los enemigos no detectan al jugador en layer `InvisiblePlayer`

---

#### Chakra 3 — Tremor (Temblor)
`Abilities/ChakraTremor.cs` | Color: Verde | Modo: **Instant**

Golpe al suelo que aturde y empuja a los enemigos cercanos.

| Parámetro | Valor |
|---|---|
| `tremblRadius` | 5f |
| `stunDuration` | 2f |
| `knockbackForce` | 8f |
| `damage` | 10 |
| `structureDamage` | 50 |
| `animationWindupTime` | 0.4f |
| `postImpactLockDuration` | 0.75f |
| `tremorShakeForce` | 3.0f |

- Bloquea otras acciones durante su ejecución
- Destroza estructuras frágiles
- Genera camera shake con Cinemachine

---

#### Chakra 4 — Echo Sense (Eco Sensitivo)
`Abilities/ChakraEchoSense.cs` | Color: Naranja | Modo: **Contextual**

Desde puntos de eco especiales, revela zonas y caminos ocultos.

| Parámetro | Valor |
|---|---|
| `detectionRadius` | 3f |
| `revealDuration` | 10f |
| `revealRadius` | 15f |

- Solo se activa cerca de objetos `EchoPoint`
- Revela objetos `HiddenZone` por `revealDuration` segundos

---

#### Chakra 5 — Remote Hack (Hackeo Remoto)
`Abilities/ChakraRemoteHack.cs` | Color: Amarillo | Modo: **Contextual**

Controla terminales electrónicas a distancia.

| Parámetro | Valor |
|---|---|
| `hackRange` | 8f |
| `hackDuration` | 1.5f |
| `ignoreWalls` | true |

- Afecta objetos que implementan la interfaz `HackableTerminal`
- Puede atravesar paredes

---

#### Chakra 6 — EMP (Pulso Electromagnético)
`Abilities/ChakraEMP.cs` | Color: Azul Claro | Modo: **Instant**

Pulso que incapacita temporalmente a enemigos electrónicos.

| Parámetro | Valor |
|---|---|
| `empRadius` | 6f |
| `disableDuration` | 3f |
| `empDamage` | 15 |

- Solo afecta a objetos que implementan `IEMPTarget` (ej: drones)
- Parámetros `durationMultiplier` y `radiusMultiplier` para mejoras con amuletos

---

#### Chakra 7a — Telekinesis (Telecinesis)
`Abilities/ChakraTelekinesis.cs` | Color: Rojo | Modo: **Contextual**

Mueve objetos de entorno para puzles o para despejar el camino.

| Parámetro | Valor |
|---|---|
| `grabRange` | 5f |
| `holdDistance` | 3f |
| `moveForce` | 15f |
| `throwForce` | 20f |

- Solo afecta objetos con componente `TelekineticObject`

---

#### Chakra 7b — Gravity Pulse (Pulso Gravitacional)
Alternativa al chakra 7. Mecánica similar pero con un pulso gravitacional.

---

### ChakraSystem (Gestor)
`Assets/ED_Gio/_Project/Scripts/Chakras/ChakraSystem.cs`

Singleton que gestiona todos los chakras: selección, activación y UI.

**Variables clave:**
- `chakras: Dictionary<ChakraType, ChakraBase>` — diccionario con todos los chakras
- `selectedChakra` — chakra actualmente seleccionado
- `activeChakra` — chakra actualmente activo
- `wheelSlowMotionScale = 0.3f` — slow-motion al abrir la rueda

**Métodos principales:**
- `SelectChakra(ChakraType)` — selecciona un chakra
- `TryActivateChakra(ChakraType)` — intenta activar verificando condiciones
- `UnlockChakra(ChakraType)` — desbloquea un chakra
- `OpenWheel()` / `CloseWheel()` — abre/cierra la rueda de selección

**Eventos:** `OnChakraSelected`, `OnChakraActivated`, `OnChakraDeactivated`, `OnWheelToggled`, `OnChakraUnlocked`

---

## 6. Sistema de Energía

### EnergySystem
`Assets/ED_Gio/_Project/Scripts/Chakras/EnergySystem.cs`

Gestiona el recurso de energía que consumen los chakras.

| Parámetro | Valor | Descripción |
|---|---|---|
| `maxEnergy` | 100f | Energía máxima |
| `energyRegenRate` | 5f | Regeneración por segundo |
| `regenDelay` | 2f | Espera antes de empezar a regenerar |
| `regenWhileUsingChakra` | false | Regenera mientras usa chakra |

**Métodos:**
- `ConsumeEnergy(float amount)` — consume una cantidad fija
- `ConsumeEnergyPerSecond(float rate)` — consume por segundo (chakras continuos)
- `StopUsingEnergy()` — indica que se dejó de usar el chakra
- `AddEnergy(float amount)` / `RestoreFullEnergy()` — recupera energía
- `IncreaseMaxEnergy(float amount)` — mejora permanente

**Propiedades:** `EnergyPercent`, `HasEnergy`, `IsFullEnergy`

**Eventos:** `OnEnergyChanged(current, max)`, `OnEnergyDepleted`, `OnEnergyFull`

---

## 7. Sistema de Enemigos

### EnemyBase (Clase Abstracta)
`Assets/_Project/Scripts/Enemies/EnemyBase.cs`

Todos los enemigos heredan de esta clase.

**Estados disponibles:**

| Estado | ID | Descripción |
|---|---|---|
| `Idle` | 0 | Esperando |
| `Patrol` | 1 | Patrullando |
| `Chase` | 2 | Persiguiendo al jugador |
| `Attack` | 3 | Atacando |
| `Cooldown` | 4 | Esperando tras ataque |
| `Hit` | 5 | Recibiendo golpe |
| `Recover` | 6 | Recuperación post-ataque (Cyborg) |
| `Retreat` | 7 | Retroceso táctico (Drone) |
| `Stunned` | 8 | Aturdido por Tremor |
| `Hacked` | 9 | Controlado por Remote Hack |
| `Dead` | 10 | Muerto |

**Parámetros base:**

| Parámetro | Valor | Descripción |
|---|---|---|
| `maxHealth` | 50f | Salud base |
| `detectionRadius` | 8f | Radio de detección |
| `fieldOfView` | 120f | Campo de visión en grados |
| `use360Detection` | false | Detección completa (sin ángulo ciego) |
| `requireLineOfSight` | true | Requiere visión directa |

**Implementa:** `IDamageable`, `IStunnable`, `IHackable`

---

### EnemyCyborg
`Assets/_Project/Scripts/Enemies/EnemyCyborg.cs`

Enemigo terrestre cuerpo a cuerpo.

**Flujo de estados:** `Idle → Patrol → Chase → Attack → Recover → Hit → Dead`

| Parámetro | Valor |
|---|---|
| `contactDamage` | 10f |
| `meleeDamage` | 20f |
| `meleeRange` | 1.5f |
| `meleeCooldown` | 1.5f |
| `attackWindup` | 0.3f |
| `recoverDuration` | 0.6f |
| `meleeKnockback` | (8f, 4f) |

- Patrulla horizontal con ground check
- Daño continuo si toca al jugador
- Golpe fuerte con rango limitado
- Detectado por: Tremor (stunned), Remote Hack (hacked), Invisibilidad (no detecta)

---

### EnemyDrone
`Assets/_Project/Scripts/Enemies/EnemyDrone.cs`

Enemigo aéreo rápido, sin gravedad.

**Flujo de estados:** `Patrol → Chase → Attack → Retreat → Hit → Dead`

| Parámetro | Valor |
|---|---|
| `contactDamage` | 10f |
| `floatAmplitude` | 0.5f |
| `floatFrequency` | 2f |
| `retreatDuration` | 1.2f |
| `retreatSpeed` | 5f |

- Patrulla con movimiento sinusoidal
- Detección 360° sin line of sight
- Retroceso táctico después de impactar
- **Implementa `IEMPTarget`** — desactivado por EMP

---

### Otros tipos de enemigos

| Script | Descripción |
|---|---|
| `EnemyGuardian.cs` | Guardián resistente |
| `EnemyMechSoldier.cs` | Soldado mecánico |
| `EnemyTurret.cs` | Torreta estacionaria |
| `EnemyHurtbox.cs` | Zona de daño separada del cuerpo |
| `EnemyProjectile.cs` | Proyectil lanzado por enemigos |

---

## 8. Sistema de Armas y Proyectiles

### WeaponController
`Assets/ED_Gio/_Project/Scripts/Character/WeaponController.cs`

| Parámetro | Valor |
|---|---|
| `fireRate` | 5f |
| `projectileSpeed` | 20f |
| `projectileDamage` | 10f |
| `maxAmmo` | 30 |
| `reserveAmmo` | 120 |
| `reloadTime` | 1.5f |
| `recoilForce` | 0.5f |

**Depende de:** `AimController`, `Projectile` (prefab)

---

### Projectile
`Assets/ED_Gio/_Project/Scripts/Character/Projectile.cs`

Proyectil genérico reutilizable.

| Parámetro | Valor |
|---|---|
| `damage` | 10f |
| `speed` | 20f |
| `lifetime` | 3f |
| `piercing` | false |
| `affectedByGravity` | false |

- Al colisionar con un `IDamageable` aplica daño
- Detecta layers: Ground, Wall, Enemies, Player

---

## 9. Sistema de UI

### Managers

| Script | Descripción |
|---|---|
| `MainMenuManager.cs` | Menú principal (Jugar, Opciones, Salir) |
| `PauseMenu.cs` | Pausa (`Time.timeScale = 0`) con confirmación de salida |
| `GameOverMenu.cs` | Pantalla de Game Over |
| `OptionsManager.cs` | Pantalla de opciones |
| `BrightnessController.cs` | Control de brillo |

### UI del Jugador

| Script | Descripción |
|---|---|
| `ChakraMenuUI.cs` | Rueda de selección de chakras (pausa el tiempo) |
| `EnergyBarUI.cs` | Barra de energía en pantalla |

### Utilidades

| Script | Descripción |
|---|---|
| `UIButtonEffect.cs` | Efectos visuales en botones |
| `SandstormUI.cs` | Efecto visual de tormenta de arena |
| `SceneDatabase.cs` | ScriptableObject con info de escenas |
| `SceneMenuGenerator.cs` | Genera menú de escenas dinámicamente |

---

## 10. Sistema de Escenas y Transiciones

### Escenas del Juego

| Escena | Descripción |
|---|---|
| `Bootstrap.unity` | Inicialización del juego |
| `MainMenu.unity` | Menú principal |
| `LoadingScene.unity` | Pantalla de carga entre escenas |
| `Level1.unity` | Primer nivel |
| `Nivel-REINA.unity` | Nivel con jefe REINA |
| `Anumaciones.unity` | Demostración de animaciones |

### Flujo de Transición

```
Escena actual
    ↓
SceneTransitionManager.LoadScene("NombreEscena")
    ↓
FadeManager.FadeOut()
    ↓
LoadingManager.SceneToLoad = "NombreEscena"
    ↓
Cargar "LoadingScene"
    ↓
LoadSceneAsync() (carga asincrónica)
    ↓
FadeManager.FadeIn()
    ↓
Escena destino lista
```

### Scripts

| Script | Patrón | Descripción |
|---|---|---|
| `SceneTransitionManager.cs` | Singleton + DontDestroyOnLoad | Orquesta las transiciones |
| `LoadingManager.cs` | Static | Carga asincrónica |
| `FadeManager.cs` | Singleton + DontDestroyOnLoad | Fade in/out con imagen UI |
| `SceneTransitionTrigger.cs` | Trigger | Detecta al jugador y cambia de escena |

---

## 11. Entorno Interactivo

### DoorInteraction2_5D
`Assets/ED_Gio/_Project/Scripts/Environment/DoorInteraction2_5D.cs`

| Parámetro | Descripción |
|---|---|
| `messageUI` | Mensaje "Presiona Y" |
| `doorAnimator` | Animator de la puerta |
| `openTriggerName` | "OpenDoor" |
| `doorAudio` | Sonido de apertura |

- Muestra mensaje al entrar en trigger
- Botón Y / gamepad abre la puerta
- Solo se puede abrir una vez (`isOpen`)

---

### ElevatorController2D
`Assets/ED_Gio/_Project/Scripts/Environment/ElevatorController2D.cs`

| Parámetro | Valor |
|---|---|
| `speed` | 2f |

- Botón Y activa el elevador
- Bloquea el input del jugador mientras se mueve
- El jugador se adjunta como hijo (`SetParent`) para moverse junto al elevador
- Activa partículas y audio al moverse

---

### Otros scripts de entorno

| Script | Descripción |
|---|---|
| `DoorAnimator.cs` | Animación de puerta genérica |
| `AreaMessageTrigger2D.cs` | Muestra mensajes al entrar en zona |
| `ParallaxLayer3D.cs` | Efecto de parallax para el fondo |
| `Collider2DProxy.cs` | Proxy entre sistemas 2D y 3D |

---

## 12. Dependencias y Paquetes

### Paquetes de Unity (manifest.json)

| Paquete | Versión | Uso |
|---|---|---|
| Universal Render Pipeline | 14.0.12 | Renderizado moderno |
| Cinemachine | 2.10.5 | Cámara dinámica y camera shake |
| Input System | 1.14.0 | Soporte gamepad + teclado |
| TextMesh Pro | 3.0.7 | Texto de alta calidad |
| 2D PSD Importer | 8.1.0 | Importar PSBs/PSDs como sprites |
| Timeline | 1.7.7 | Cinemáticas y secuencias |
| Visual Scripting | 1.9.4 | Scripting visual |
| Test Framework | 1.1.33 | Tests automatizados |

### Assets de la Asset Store

| Asset | Uso en el proyecto |
|---|---|
| Quibli | Post-processing artístico / shaders |
| Sprite Shaders Ultimate | Efectos visuales en sprites |
| SCI-FI UI Pack Pro | UI futurista |
| Synty UI | Interfaz futurista adicional |
| Polygon Nature | Assets de naturaleza (3D) |
| Polygon Sci-Fi City | Assets de ciudad sci-fi (3D) |
| Polygon Sci-Fi Space | Assets de espacio sci-fi (3D) |
| Monsters Creatures Fantasy | Criaturas de fantasía |
| VolumetricLines | Líneas volumétricas (efectos de luz) |

---

## 13. Layers y Tags

### Layers relevantes

| Layer | Uso |
|---|---|
| `Ground` | Suelo — detección de `IsGrounded` |
| `Wall` | Paredes — detección de wall slide |
| `Player` | Jugador — colisiones y detección de enemigos |
| `Enemies` | Enemigos — hit detection de proyectiles |
| `InvisiblePlayer` | Jugador invisible — enemigos no detectan este layer |

### Prefabs principales

| Prefab | Descripción |
|---|---|
| `Player.prefab` | Personaje principal completo |
| `PlayerCamera.prefab` | Cámara con Cinemachine |
| `EdsGun.prefab` | Arma del jugador |
| `Bullet.prefab` | Proyectil básico |
| `Cyborg.prefab` / `Cyborg 1.prefab` | Variantes del cyborg |
| `Drone.prefab` / `Drone 1.prefab` | Variantes del dron |
| `Guardian.prefab` | Enemigo guardián |
| `Acorazado.prefab` | Enemigo con armadura |
| `BaseTorretaTecho.prefab` | Torreta de techo |
| `EnemyProjectile.prefab` | Proyectil de enemigos |
| `Puerta1.prefab` | Puerta interactiva |
| `ReinaBoss.prefab` | Jefe REINA |

---

## 14. Controles del Jugador

| Acción | Teclado | Gamepad |
|---|---|---|
| Mover | WASD | D-Pad / Joystick Izq. |
| Saltar | ESPACIO | A |
| Dash | SHIFT (probable) | X (probable) |
| Apuntar | Mouse | Joystick Der. |
| Disparar | Click Izq. | RB / RT |
| Abrir rueda de chakras | TAB (mantener) | SELECT (mantener) |
| Activar chakra | E | A |
| Cambio rápido de chakra | Alt Izq. | — |
| Selección rápida (rueda) | Números 1-9 | D-Pad |
| Interactuar (puertas, elevadores) | — | Y |
| Pausa | ESC | START |

---

## 15. Flujo de Juego

```
Bootstrap.unity
    │  Inicializa sistemas globales
    ▼
MainMenu.unity
    │  Jugar → LoadScene("LoadingScene")
    ▼
LoadingScene.unity
    │  Carga asincrónica del nivel
    ▼
Level1.unity / Nivel-REINA.unity
    │
    ├── Spawn del jugador
    ├── Spawn de enemigos
    ├── Sistema de chakras activo
    │
    ├── [Loop de juego]
    │       ├── Movimiento + salto
    │       ├── Combate (disparo, chakras)
    │       ├── Exploración (puertas, elevadores, zonas ocultas)
    │       └── Gestión de energía
    │
    ├── [Game Over] → GameOverMenu → MainMenu
    │
    └── [Completar nivel] → SceneTransitionTrigger → siguiente nivel
```

---

*Documentación generada para el equipo de Nabhi EDs Legacy.*
