# SETUP INSTRUCTIONS - NABHI Character Controller

## ✅ COMPLETADO AUTOMÁTICAMENTE

Los siguientes elementos ya están configurados:

### 1. Scripts Copiados ✅
- `Assets/_Project/Scripts/Character/CharacterController2D.cs`
- `Assets/_Project/Scripts/Character/CharacterState.cs`

### 2. Estructura de Carpetas ✅
```
Assets/
└── _Project/
    ├── Scripts/
    │   └── Character/
    ├── Prefabs/
    ├── Scenes/
    └── Animations/
```

### 3. Layers Configurados ✅
- **Layer 8:** Player
- **Layer 9:** Ground
- **Layer 10:** Enemies
- **Layer 11:** Wall

### 4. Tags Configurados ✅
- Player
- Ground
- Wall

### 5. Input Configurado ✅
- **Dash:** Left Shift (teclado) / Joystick Button 2 (gamepad)

---

## 🎮 PRÓXIMOS PASOS EN UNITY

### Paso 1: Crear el GameObject Player

1. **Abrir Unity** (proyecto debe estar abierto)
2. **Crear Empty GameObject:**
   - Hierarchy → Right Click → Create Empty
   - Nombre: "Player"
   - Position: (0, 0, 0)
   - Layer: **Player**
   - Tag: **Player**

3. **Agregar Rigidbody2D:**
   - Inspector → Add Component → Rigidbody2D
   - **Body Type:** Dynamic
   - **Linear Drag:** 0
   - **Angular Drag:** 0.05
   - **Gravity Scale:** 2.5
   - **Collision Detection:** Continuous
   - **Sleeping Mode:** Never Sleep
   - **Interpolate:** Interpolate
   - **Constraints:** Freeze Rotation Z ✓

4. **Agregar CapsuleCollider2D:**
   - Inspector → Add Component → CapsuleCollider2D
   - **Size:** X: 0.5, Y: 1.0
   - **Offset:** X: 0, Y: 0

5. **Agregar CharacterController2D:**
   - Inspector → Add Component → Character Controller 2D
   - Configurar parámetros (ver abajo)

6. **Agregar CharacterState:**
   - Inspector → Add Component → Character State

7. **Agregar Sprite de ED90:**
   - Inspector → Add Component → Sprite Renderer
   - **Sprite:** Hacer clic en el círculo pequeño → Buscar "Face" o "TorsoUpper"
   - Los sprites están en: `Assets/images/Ed90.psb`
   - Sprites disponibles: Face, TorsoUpper, TorsoLower, Hair/Baird, Neck, etc.
   - **Recomendación temporal:** Usar "TorsoUpper" para ver el personaje completo
   - **Order in Layer:** 0
   - **Color:** White (255, 255, 255, 255)

   **NOTA:** Por ahora usa un solo sprite para probar el movimiento.
   Más adelante crearás un rig completo con todas las partes del cuerpo.

---

### Paso 2: Configurar CharacterController2D

En el Inspector del Player, configurar estos valores iniciales:

#### Movement Settings:
- **Move Speed:** 8.0
- **Acceleration Time Ground:** 0.1
- **Acceleration Time Air:** 0.2
- **Air Control Multiplier:** 0.8

#### Jump Settings:
- **Jump Force:** 12.0
- **Coyote Time:** 0.15
- **Jump Buffer Time:** 0.1
- **Variable Jump Height:** ✓ (checked)
- **Jump Release Force Multiplier:** 0.5

#### Dash Settings:
- **Can Dash:** ✓ (checked)
- **Dash Speed:** 20.0
- **Dash Duration:** 0.15
- **Dash Cooldown:** 0.5

#### Wall Mechanics:
- **Can Wall Slide:** ✓ (checked)
- **Wall Slide Speed:** 2.0
- **Can Wall Jump:** ✓ (checked)
- **Wall Jump Force:** X: 10, Y: 15

#### Ground Detection:
- **Ground Check Position:** (0, -0.5, 0)
- **Ground Check Size:** (0.4, 0.1, 0)
- **Ground Layer:** Ground (Layer 9)

#### Wall Detection:
- **Wall Check Distance:** 0.3
- **Wall Layer:** Wall (Layer 11)

#### Advanced:
- **Max Fall Speed:** 20.0

---

### Paso 3: Crear Escena de Prueba

1. **Guardar Escena Actual:**
   - File → Save As → `Assets/_Project/Scenes/TestScene_Movement.unity`

2. **Crear Suelo:**
   - Hierarchy → Right Click → 2D Object → Sprite → Square
   - Nombre: "Ground"
   - Position: (0, -3, 0)
   - Scale: (20, 1, 1)
   - Layer: **Ground**
   - Tag: **Ground**
   - Agregar Component → Box Collider 2D

3. **Crear Paredes (Opcional):**
   - Duplicate Ground (Ctrl+D)
   - Nombre: "Wall_Left"
   - Position: (-10, 0, 0)
   - Scale: (1, 10, 1)
   - Layer: **Wall**
   - Tag: **Wall**

   - Duplicate para "Wall_Right"
   - Position: (10, 0, 0)

4. **Configurar Camera:**
   - Seleccionar Main Camera
   - **Orthographic Size:** 5
   - Position: (0, 0, -10)

---

### Paso 4: ¡Probar!

1. **Press Play (▶)**

2. **Controles:**
   - **A / D** o **← / →**: Mover izquierda/derecha
   - **Space**: Saltar
   - **Left Shift**: Dash (mantener dirección)
   - **W / S**: Input vertical (para dash en diagonal)

3. **Verificar:**
   - ✓ El personaje se mueve suavemente
   - ✓ El salto responde bien
   - ✓ Puede hacer dash
   - ✓ No atraviesa el suelo
   - ✓ Puede saltar desde el suelo

---

## 🐛 Troubleshooting

### El personaje cae infinitamente:
- Verificar que Ground tenga **Layer: Ground**
- Verificar que CharacterController2D tenga **Ground Layer: Ground**
- Verificar que Ground tenga **Box Collider 2D**

### El personaje no se mueve:
- Verificar que Rigidbody2D no tenga **Body Type: Static**
- Verificar que **Freeze Position X/Y** no estén marcados

### El personaje se traba en paredes:
- Cambiar Physics Material del Collider2D
- O agregar Physics Material 2D con **Friction: 0**

### No se detecta input:
- Verificar que en Input Manager existan:
  - "Horizontal"
  - "Vertical"
  - "Jump"
  - "Dash"

---

## 📊 Valores de Referencia (Juegos Profesionales)

### Hollow Knight (Feel similar):
- Move Speed: 7.5
- Jump Force: 13.0
- Dash Speed: 22.0
- Coyote Time: 0.15

### Celeste (Más ágil):
- Move Speed: 9.0
- Jump Force: 14.0
- Dash Speed: 24.0
- Coyote Time: 0.1

### Metroid Dread (Pesado):
- Move Speed: 8.5
- Jump Force: 11.0
- Dash Speed: 18.0
- Coyote Time: 0.08

---

## 🎯 Siguiente Fase: Ajustar Feel

Una vez que el personaje se mueva, dedica tiempo a ajustar:

1. **Aceleración** - ¿Se siente resbaladizo o pegajoso?
2. **Altura del salto** - ¿Es satisfactorio?
3. **Velocidad de dash** - ¿Se siente poderoso?
4. **Coyote time** - ¿Puedes saltar justo después del borde?
5. **Air control** - ¿Tienes suficiente control en el aire?

**No hay valores "correctos"**, solo lo que se siente bien para tu juego.

---

## 📝 Notas

- Los scripts ya están compilados y listos para usar
- Si Unity muestra errores, verifica que ambos scripts estén en la carpeta correcta
- Puedes ajustar todos los valores en tiempo real (durante Play mode) para experimentar
- Los cambios en Play mode NO se guardan, anótalos antes de salir

---

**¡Listo para empezar! 🚀**
