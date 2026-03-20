# 🎨 TUTORIAL COMPLETO: Rigging 2D en Unity - Para Principiantes

## 📖 TABLA DE CONTENIDOS

1. [¿Qué es el Rigging 2D?](#qué-es-el-rigging-2d)
2. [Conceptos Básicos](#conceptos-básicos)
3. [Instalación de Paquetes](#instalación-de-paquetes)
4. [Preparación del PSB](#preparación-del-psb)
5. [Creación del Skeleton (Esqueleto)](#creación-del-skeleton)
6. [Skinning (Asignar Sprites a Bones)](#skinning)
7. [Crear Primera Animación](#crear-primera-animación)
8. [Configurar Animator](#configurar-animator)
9. [Troubleshooting](#troubleshooting)

---

## 🤔 ¿QUÉ ES EL RIGGING 2D?

### Analogía Simple

Imagina que tu personaje es como una **marioneta**:

- **Sprites** = Las piezas de tela/madera de la marioneta (TorsoUpper, Face, RightArm, etc.)
- **Bones (Huesos)** = Los hilos y palos que controlan la marioneta
- **Rigging** = El proceso de atar los hilos a las piezas
- **Animation** = Mover los hilos para hacer que la marioneta se mueva

### ¿Por qué Rigging?

**SIN Rigging:**
- Tienes que mover CADA sprite manualmente
- 100 sprites = 100 objetos que mover
- Animaciones toman MUCHO tiempo

**CON Rigging:**
- Mueves un "bone" (hueso) y todos los sprites conectados se mueven juntos
- Ejemplo: Mueves el bone del brazo → Hombro, antebrazo, mano se mueven automáticamente
- Animaciones se crean 10x más rápido

---

## 📚 CONCEPTOS BÁSICOS

### 1. Bones (Huesos)

**¿Qué son?**
- Puntos de control invisibles que mueven los sprites
- Como los huesos de tu cuerpo mueven tu piel

**Ejemplo:**
```
Brazo Humano:
- Bone del hombro → Conecta torso con brazo
- Bone del brazo superior → Mueve bíceps
- Bone del antebrazo → Mueve antebrazo
- Bone de la mano → Mueve mano

Cuando mueves el bone del hombro, TODOS los bones conectados se mueven.
```

### 2. Hierarchy (Jerarquía de Bones)

Los bones tienen relación padre-hijo:

```
Root (Pelvis/Torso) ← Bone principal
├── Spine (Columna)
│   ├── Chest (Pecho)
│   │   ├── RightShoulder
│   │   │   ├── RightUpperArm
│   │   │   │   ├── RightLowerArm
│   │   │   │   │   └── RightHand
│   │   │
│   │   ├── LeftShoulder
│   │   │   └── [similar...]
│   │   │
│   │   └── Neck
│   │       └── Head
│
├── RightHip
│   ├── RightUpperLeg
│   │   └── RightLowerLeg
│   │       └── RightFoot
│
└── LeftHip
    └── [similar...]
```

**Regla importante:**
- Cuando mueves un bone padre → Todos los hijos se mueven
- Ejemplo: Mover "Spine" → Mueve pecho, brazos, cabeza

### 3. Skinning (Peso de Influencia)

**¿Qué es?**
- Decirle a Unity: "Este sprite debe seguir a ESTE bone"
- También se llama "Weight Painting" (pintar pesos)

**Ejemplo:**
```
Sprite "RightUpperArm" está conectado a:
- 100% Bone "RightUpperArm"
- 0% otros bones

Sprite "RightElbow" podría estar conectado a:
- 50% Bone "RightUpperArm" (se mueve un poco con brazo superior)
- 50% Bone "RightLowerArm" (se mueve un poco con antebrazo)
```

### 4. IK (Inverse Kinematics)

**FK (Forward Kinematics) - Normal:**
- Mueves el hombro → El brazo sigue
- Como mover títeres tradicionales

**IK (Inverse Kinematics) - Inverso:**
- Mueves la MANO → El brazo se ajusta automáticamente
- Unity calcula cómo doblar el codo
- Más natural y fácil de animar

**Usaremos IK para piernas y brazos.**

---

## 📦 INSTALACIÓN DE PAQUETES

### PASO 1: Abrir Package Manager

1. **Unity → Top Menu → Window → Package Manager**
2. Espera que cargue (puede tardar unos segundos)

### PASO 2: Verificar/Instalar Paquetes Necesarios

**En Package Manager:**

1. **Cambiar filtro a "Unity Registry"** (dropdown arriba izquierda)

2. **Buscar e instalar estos paquetes:**

#### Paquete 1: 2D Animation

**Buscar:** "2D Animation"

**Verificar:**
- Si dice **"Installed"** (verde) → Ya está instalado ✓
- Si dice **"Install"** (botón azul) → Click en Install

**Esperar que termine de instalar**

#### Paquete 2: 2D PSD Importer

**Buscar:** "2D PSD Importer"

**Verificar e instalar** (igual que antes)

#### Paquete 3: 2D Sprite

**Buscar:** "2D Sprite"

**Verificar e instalar**

#### Paquete 4 (Opcional): 2D IK

**Buscar:** "2D IK"

**Este es OPCIONAL** - Permite IK avanzado
- Para empezar, NO es necesario
- Puedes instalarlo después si quieres IK

### PASO 3: Verificar que todo está instalado

**En Package Manager → Filtro: "In Project"**

Deberías ver:
- ✓ 2D Animation
- ✓ 2D PSD Importer
- ✓ 2D Sprite

**Cerrar Package Manager**

---

## 🎨 PREPARACIÓN DEL PSB

### PASO 1: Seleccionar Ed90.psb

1. **Project view** → Navegar a `Assets/images/`
2. **Click en "Ed90.psb"**
3. **Inspector** debe mostrar configuración del PSB

### PASO 2: Configurar para Character Rig

**En Inspector del Ed90.psb:**

1. **Texture Type:** Debe decir "Sprite (2D and UI)" ✓

2. **Scroll down hasta encontrar "Character Rig"**
   - Puede estar colapsado (flecha a la izquierda)
   - Click en la flecha para expandir

3. **Configurar:**
   - **Character Rig:** ✓ (checkbox MARCADO)
   - **Main Skeleton:** (Dejar vacío por ahora)
   - **Mosaic:** Unchecked
   - **Reslice:** Unchecked

4. **Click en botón "Apply"** (abajo del Inspector)
   - Unity reimportará el archivo
   - Puede tardar 5-10 segundos

### PASO 3: Verificar sprites

**Click en la flecha junto a "Ed90.psb"** en Project view (▶)

Deberías ver una lista de sprites:
- Face
- Hair/Baird
- TorsoUpper
- TorsoLower
- RightUpperLeg
- ... etc.

**Si NO ves sprites:**
- Verificar que Character Rig está checked
- Click Apply de nuevo
- Reabrir Unity si es necesario

---

## 🦴 CREACIÓN DEL SKELETON (ESQUELETO)

### CONCEPTO

Vamos a crear un "esqueleto" de bones (huesos) que controlarán los sprites.

### PASO 1: Abrir Skinning Editor

1. **Selecciona Ed90.psb** en Project view
2. **Inspector → Botón "Skinning Editor"** (botón grande cerca del botón Sprite Editor)
3. Se abre ventana **Skinning Editor**

**Si no ves botón "Skinning Editor":**
- Verificar que Character Rig está enabled
- Verificar que 2D Animation package está instalado

### PASO 2: Entender la Interfaz del Skinning Editor

La ventana tiene varias secciones:

```
┌─────────────────────────────────────────┐
│  [Visibility] [Bone Tools] [Geometry]   │ ← Toolbar superior
├─────────────────────────────────────────┤
│                                         │
│         [Vista del Personaje]           │ ← Centro: Ves los sprites
│              (Canvas)                   │
│                                         │
├─────────────────────────────────────────┤
│  [Sprite List]  │  [Bone Hierarchy]     │ ← Paneles laterales
│                 │                       │
└─────────────────────────────────────────┘
```

**Paneles importantes:**
- **Centro:** Aquí ves los sprites y crearás los bones
- **Sprite List (izquierda):** Lista de todos los sprites
- **Bone Hierarchy (derecha):** Lista de bones que crees

### PASO 3: Crear el Primer Bone (Root - Raíz)

Este será el bone principal que controla todo.

1. **En Toolbar superior → Click en "Create Bone"** (ícono de hueso)
   - El cursor cambia a modo crear bone

2. **En el canvas (centro), sobre el sprite del torso:**
   - **Click una vez** en el centro del pelvis/cintura (abajo del torso)
   - **Mueve el mouse hacia arriba** (hacia el pecho)
   - **Click de nuevo** en el centro del pecho

**Resultado:**
- Verás una **línea con círculos** en los extremos
- Esto es tu primer bone!
- Círculo de abajo = "Joint" (articulación inicial)
- Círculo de arriba = "Tip" (punta del bone)

3. **Presionar ESC** para salir del modo crear bone

**Nombrar el bone:**
- En panel **Bone Hierarchy (derecha)** → Verás "Bone_1"
- **Double-click en "Bone_1"** → Renombrar a: **"Root"** o **"Pelvis"**
- Presionar Enter

### PASO 4: Crear Bone de la Columna (Spine)

Vamos a crear un bone hijo del Root:

1. **Selecciona "Root"** en Bone Hierarchy (debe quedar highlighted)

2. **Click en "Create Bone"** de nuevo

3. **En canvas:**
   - **Click en el círculo superior del bone Root** (donde termina)
   - **Mueve hacia arriba** (hacia el cuello)
   - **Click** en la base del cuello

**Resultado:**
- Nuevo bone conectado al Root
- En Bone Hierarchy verás:
  ```
  Root
  └── Bone_2
  ```

4. **Renombrar "Bone_2" a "Spine"**

### PASO 5: Crear Bone del Cuello y Cabeza

**Bone del Cuello:**
1. **Selecciona "Spine"** en Bone Hierarchy
2. **Create Bone**
3. **Click en punta de Spine → Click en centro de la cabeza**
4. **Renombrar a "Neck"**

**Bone de la Cabeza:**
1. **Selecciona "Neck"**
2. **Create Bone**
3. **Click en punta de Neck → Click encima de la cabeza** (arriba del Hair)
4. **Renombrar a "Head"**

**Jerarquía hasta ahora:**
```
Root
└── Spine
    └── Neck
        └── Head
```

### PASO 6: Crear Bones del Brazo Derecho

Vamos a crear la cadena de bones para el brazo derecho:

**Hombro Derecho:**
1. **Selecciona "Spine"** (NO Neck - los brazos salen del pecho)
2. **Create Bone**
3. **Desde punta de Spine → Hacia el hombro derecho**
4. Click en posición del hombro derecho
5. **Renombrar a "RightShoulder"**

**Brazo Superior Derecho:**
1. **Selecciona "RightShoulder"**
2. **Create Bone**
3. **Desde punta de RightShoulder → Hacia el codo**
4. Click en posición del codo
5. **Renombrar a "RightUpperArm"**

**Antebrazo Derecho:**
1. **Selecciona "RightUpperArm"**
2. **Create Bone**
3. **Desde codo → Hacia la muñeca**
4. Click en muñeca
5. **Renombrar a "RightLowerArm"**

**Mano Derecha:**
1. **Selecciona "RightLowerArm"**
2. **Create Bone**
3. **Desde muñeca → Hacia punta de los dedos**
4. Click en punta de la mano
5. **Renombrar a "RightHand"**

**Jerarquía del brazo derecho:**
```
Root
└── Spine
    ├── Neck
    │   └── Head
    └── RightShoulder
        └── RightUpperArm
            └── RightLowerArm
                └── RightHand
```

### PASO 7: Crear Bones del Brazo Izquierdo

Repite el PASO 6, pero para el lado izquierdo:

- LeftShoulder (desde Spine)
- LeftUpperArm (desde LeftShoulder)
- LeftLowerArm (desde LeftUpperArm)
- LeftHand (desde LeftLowerArm)

**TIPS:**
- Los bones deben seguir la forma natural del brazo
- Posiciona las articulaciones (joints) en hombro, codo, muñeca

### PASO 8: Crear Bones de la Pierna Derecha

**Cadera Derecha:**
1. **Selecciona "Root"** (las piernas salen del Root/pelvis)
2. **Create Bone**
3. **Desde Root → Hacia cadera derecha**
4. **Renombrar a "RightHip"**

**Muslo Derecho:**
1. **Selecciona "RightHip"**
2. **Create Bone**
3. **Desde cadera → Hacia rodilla**
4. **Renombrar a "RightUpperLeg"**

**Pantorrilla Derecha:**
1. **Selecciona "RightUpperLeg"**
2. **Create Bone**
3. **Desde rodilla → Hacia tobillo**
4. **Renombrar a "RightLowerLeg"**

**Pie Derecho:**
1. **Selecciona "RightLowerLeg"**
2. **Create Bone**
3. **Desde tobillo → Hacia punta del pie**
4. **Renombrar a "RightFoot"**

### PASO 9: Crear Bones de la Pierna Izquierda

Repite PASO 8 para lado izquierdo:
- LeftHip
- LeftUpperLeg
- LeftLowerLeg
- LeftFoot

### PASO 10: Verificar Skeleton Completo

**En panel Bone Hierarchy, deberías ver:**

```
Root
├── Spine
│   ├── Neck
│   │   └── Head
│   ├── RightShoulder
│   │   └── RightUpperArm
│   │       └── RightLowerArm
│   │           └── RightHand
│   └── LeftShoulder
│       └── LeftUpperArm
│           └── LeftLowerArm
│               └── LeftHand
├── RightHip
│   └── RightUpperLeg
│       └── RightLowerLeg
│           └── RightFoot
└── LeftHip
    └── LeftUpperLeg
        └── LeftLowerLeg
            └── LeftFoot
```

**Total:** Aproximadamente 15-16 bones

**Si algo se ve mal:**
- Puedes mover bones con herramienta "Move" (M)
- Puedes borrar bones: Select bone → Delete key

---

## 🎨 SKINNING (ASIGNAR SPRITES A BONES)

### CONCEPTO

Ahora que tienes el esqueleto, debes decirle a Unity:
- "Este sprite debe seguir a ESTE bone"

### PASO 1: Cambiar a Modo "Auto Geometry"

**En Skinning Editor:**

1. **Toolbar superior → Click en "Auto Geometry"**
2. **Click en botón "Generate All"**
3. Espera unos segundos

**¿Qué hace esto?**
- Unity automáticamente detecta qué sprite va con qué bone
- Genera "geometry" (malla invisible) alrededor de cada sprite

### PASO 2: Cambiar a Modo "Weights" (Pesos)

1. **Toolbar superior → Click en "Weights"**

Ahora puedes ver y editar qué bones controlan cada sprite.

### PASO 3: Auto-Assign Weights (Asignación Automática)

**La forma MÁS FÁCIL:**

1. **Panel "Sprite List" (izquierda) → Select All** (Ctrl+A para seleccionar todos los sprites)

2. **Toolbar superior → Buscar "Auto"**
   - Verás botones: "Auto Weights", "Generate", etc.

3. **Click en "Auto Weights"**
   - Unity automáticamente asigna cada sprite al bone más cercano
   - Espera que termine (puede tardar 10-20 segundos)

**Resultado:**
- Cada sprite ahora está "conectado" a sus bones correspondientes
- Por ejemplo: Sprite "RightUpperArm" está conectado al bone "RightUpperArm"

### PASO 4: Verificar Weights (Opcional pero Recomendado)

**Para ver si funcionó bien:**

1. **Selecciona un bone en Bone Hierarchy** (ejemplo: "RightUpperArm")

2. **En canvas (centro):**
   - Los sprites influenciados por ese bone se **iluminan con color**
   - Rojo/Amarillo = 100% influencia
   - Azul/Verde = Menos influencia
   - Sin color = 0% influencia

3. **Probar con varios bones** para verificar

**Si algo se ve mal (sprite no iluminado):**
- Selecciona ese sprite en Sprite List
- Selecciona el bone correcto en Bone Hierarchy
- Toolbar → "Weight Brush" → Pinta sobre el sprite (como Photoshop)
- Aumenta weight a 100%

### PASO 5: Preview del Rig (Probar el Skeleton)

**Para ver si el rig funciona:**

1. **Toolbar superior → Click en "Preview Pose"**
   - Esto activa modo preview
   - Ahora puedes MOVER bones y ver cómo se mueven los sprites

2. **Herramienta "Free Move" (F)**
   - Selecciona un bone (ejemplo: RightUpperArm)
   - Muévelo con mouse
   - Los sprites conectados deberían moverse!

3. **Herramienta "Rotate" (R)**
   - Rota bones
   - Los sprites deben rotar con ellos

**TIPS:**
- Mueve el bone "Root" → TODO el personaje se mueve
- Mueve "Spine" → Torso, brazos, cabeza se mueven
- Mueve "RightUpperArm" → Solo el brazo derecho se mueve

4. **Para volver a editar:**
   - Click en "Preview Pose" de nuevo para desactivar
   - O click en "Edit Joints" para volver a editar bones

### PASO 6: Guardar el Rig

**MUY IMPORTANTE:**

1. **Skinning Editor → Top menu → Character → Apply**
   - Esto guarda todos los cambios al PSB
   - **SI NO HACES ESTO, PERDERÁS TODO EL TRABAJO**

2. Verifica que dice "Applied" o no hay cambios pendientes

3. **Cerrar Skinning Editor**

---

## 🎮 CREAR PREFAB DEL PERSONAJE

### PASO 1: Generar Prefab con Rig

1. **Project view → Ed90.psb → Click en flecha (▶)**
2. Deberías ver un nuevo item: **"Ed90 Sprite Lib"** o similar

3. **Buscar el Prefab generado:**
   - Puede estar en la misma carpeta
   - O en `Assets/images/`
   - Busca algo como "Ed90_Rig" o un GameObject con ícono de personaje

**Si NO ves prefab generado:**
- Hacer esto manualmente:

### PASO 2: Crear Prefab Manualmente (si es necesario)

1. **Project view → Busca "Ed90.psb"**
2. **Drag Ed90.psb** desde Project view → Scene view (no Hierarchy)
3. Unity crea un GameObject con todo el rig incluido

**Deberías ver en Hierarchy:**
- GameObject "Ed90" (o similar)
  - Con componente "Sprite Skin"
  - Con todos los sprites como hijos

4. **Drag este GameObject** desde Hierarchy → Project view (carpeta `Assets/_Project/Prefabs/`)
5. **Nombre:** "ED_Rigged"

### PASO 3: Reemplazar Player Actual

Ahora vamos a usar este rig en tu Player:

1. **Selecciona "Player"** en Hierarchy

2. **Si tiene un hijo "Visual" con sprites sueltos:**
   - Delete "Visual" (lo reemplazaremos)

3. **Eliminar SpriteRenderer del Player** (si tiene uno)

4. **Drag prefab "ED_Rigged"** (desde Project) → Sobre "Player" en Hierarchy
   - Esto lo hace hijo de Player

5. **Renombrar a "Visual"**

6. **Position de "Visual":**
   - X: 0
   - Y: 0
   - Z: 0
   - Scale: (1, 1, 1) o ajustar según tamaño

**Estructura final:**
```
Player
├── Rigidbody2D
├── CapsuleCollider2D
├── CharacterController2D
├── CharacterState
├── GroundCheck
└── Visual (ED_Rigged prefab)
    ├── Root (bone)
    ├── [Todos los sprites con Sprite Skin]
    └── [Jerarquía de bones y sprites]
```

### PASO 4: Verificar que Funciona

1. **Press Play** ▶
2. **Mover con A/D**
3. **El personaje completo debe moverse** (no solo un sprite)

**Si no se mueve o se ve raro:**
- Verificar Scale de "Visual"
- Ajustar Position Y de "Visual" si está muy alto/bajo

---

## 🎬 CREAR PRIMERA ANIMACIÓN

### PASO 1: Abrir Animation Window

1. **Window → Animation → Animation**
2. **Dock la ventana** donde sea cómodo (abajo)

### PASO 2: Seleccionar GameObject a Animar

1. **Hierarchy → Selecciona "Visual"** (el prefab con el rig)
   - NO selecciones "Player" (queremos animar solo el visual)

### PASO 3: Crear Animation Clip

1. **Animation window → Click "Create"**
2. **Guardar en:** `Assets/_Project/Animations/ED/`
   - Crear carpeta "ED" si no existe
3. **Nombre:** `ED_Idle.anim`
4. **Save**

**Se crea automáticamente:**
- Animation Clip: `ED_Idle.anim`
- Animator Controller: `Visual.controller` (o similar)

### PASO 4: Entender Animation Window

```
┌─────────────────────────────────────────┐
│ [ED_Idle] ▼  [:00] [Record]  [Play] >  │ ← Controles
├─────────────────────────────────────────┤
│  Property         │  0:00   0:30   1:00 │ ← Timeline
│  ─────────────────┼─────────────────────│
│  > Root           │    ◆                │
│  > RightUpperArm  │    ◆                │
│  > LeftUpperLeg   │                     │
└─────────────────────────────────────────┘
```

**Elementos:**
- **Clip dropdown:** Cambiar entre animaciones
- **Timeline:** Línea de tiempo (frames)
- **Record button (rojo):** Activar grabación automática
- **Play button:** Reproducir animación preview
- **Property list:** Bones/objetos que estás animando

### PASO 5: Crear Animación Idle Simple (Respiración)

**Concepto:**
- Idle = Personaje quieto, respirando
- Vamos a animar el Spine para simular respiración

**Paso a paso:**

1. **Click en botón RECORD (círculo rojo)**
   - Se pone rojo cuando está activo
   - Ahora TODO lo que hagas se graba

2. **En Scene view:**
   - Asegúrate que estás en frame 0:00 (inicio del timeline)
   - **Selecciona el bone "Spine"** (click en el bone en Scene view)
     - O encuentra "Spine" en Hierarchy bajo Visual

3. **No mover nada aún** - Solo para crear keyframe inicial
   - Ya está grabado automáticamente en 0:00

4. **En Animation window:**
   - **Click en timeline en frame 0:30** (medio segundo)
   - La línea roja se mueve a 0:30

5. **En Scene view:**
   - **Con Spine seleccionado → Herramienta Scale (R)**
   - **Scale Y:** Aumentar ligeramente (ejemplo: 1.0 → 1.05)
   - Esto simula "inhalar"
   - Se crea keyframe automáticamente

6. **En Animation window:**
   - **Click en frame 1:00** (un segundo)

7. **En Scene view:**
   - **Spine → Scale Y:** Volver a 1.0 (valor original)
   - Esto simula "exhalar"

8. **Click RECORD de nuevo** para desactivar grabación

**Resultado:**
- Frame 0:00: Spine normal (Scale Y = 1.0)
- Frame 0:30: Spine más alto (Scale Y = 1.05) - Inhala
- Frame 1:00: Spine normal (Scale Y = 1.0) - Exhala

### PASO 6: Configurar Loop

1. **Project view → Selecciona "ED_Idle.anim"**
2. **Inspector:**
   - **Loop Time:** ✓ (checked)
   - **Loop Pose:** ✓ (checked, opcional)

### PASO 7: Preview la Animación

**En Animation window:**
- **Click en Play (▶)**
- Deberías ver el personaje "respirando" suavemente

**En Game view:**
- **Press Play en Unity** ▶
- El personaje debería respirar automáticamente (idle)

---

## 🎬 CREAR ANIMACIÓN WALK (CAMINAR)

### PASO 1: Crear Nuevo Clip

1. **Animation window → Dropdown "ED_Idle" → Create New Clip**
2. **Nombre:** `ED_Walk.anim`
3. **Guardar en:** `Assets/_Project/Animations/ED/`

### PASO 2: Planificar Walk Cycle

Un walk cycle típico tiene estos frames:

```
Frame 0:   Pierna derecha adelante, izquierda atrás
Frame 5:   Ambas piernas juntas (centro)
Frame 10:  Pierna izquierda adelante, derecha atrás
Frame 15:  Ambas piernas juntas (centro)
Frame 20:  Volver a Frame 0 (loop)
```

### PASO 3: Animar Walk (Simplificado)

**TIPS ANTES DE EMPEZAR:**
- Usa herramienta Rotate (R) para rotar bones
- Los brazos se balancean OPUESTO a las piernas
  - Pierna derecha adelante → Brazo izquierdo adelante
  - Pierna izquierda adelante → Brazo derecho adelante

**Animación básica:**

1. **Record ON**

2. **Frame 0:00:**
   - RightUpperLeg: Rotar hacia adelante (Z: -20°)
   - LeftUpperLeg: Rotar hacia atrás (Z: 20°)
   - RightUpperArm: Rotar hacia atrás (Z: 15°)
   - LeftUpperArm: Rotar hacia adelante (Z: -15°)

3. **Frame 0:10 (medio):**
   - Todas las piernas/brazos en posición neutral (Z: 0°)

4. **Frame 0:20 (final - opuesto a inicio):**
   - RightUpperLeg: Rotar hacia atrás (Z: 20°)
   - LeftUpperLeg: Rotar hacia adelante (Z: -20°)
   - RightUpperArm: Rotar hacia adelante (Z: -15°)
   - LeftUpperArm: Rotar hacia atrás (Z: 15°)

5. **Record OFF**

6. **Configurar Loop:** Loop Time = checked

7. **Preview:** Click Play en Animation window

**Ajustar:**
- Si se ve muy rápido/lento: Ajustar FPS o duración
- Si no se ve natural: Ajustar ángulos de rotación

---

## 🎮 CONFIGURAR ANIMATOR CONTROLLER

### PASO 1: Abrir Animator Window

1. **Window → Animation → Animator**
2. Dock donde sea cómodo

### PASO 2: Seleccionar Animator Controller

1. **Hierarchy → Selecciona "Visual"**
2. **Inspector → Component "Animator"**
   - Controller: Debería tener asignado "Visual.controller" (creado automáticamente)

3. **Project view → Double-click en "Visual.controller"**
   - Se abre en Animator window

### PASO 3: Organizar Estados

**Deberías ver:**
- Entry (naranja)
- ED_Idle (naranja/gris)
- ED_Walk (gris)

**Si ED_Idle NO es el default:**
- Right click en "ED_Idle" → "Set as Layer Default State"
- Debe volverse naranja

### PASO 4: Crear Parámetros

**En Animator window → Panel "Parameters" (izquierda):**

1. **Click en "+" → Bool**
   - Nombre: **isMoving**

2. **Click en "+" → Bool**
   - Nombre: **isGrounded**

3. **(Opcional) Otros parámetros:**
   - isDashing (Bool)
   - isWallSliding (Bool)
   - velocityY (Float)

### PASO 5: Crear Transiciones

**Idle → Walk:**

1. **Right click en "ED_Idle" → Make Transition**
2. **Click en "ED_Walk"** (crea flecha)
3. **Click en la flecha** (transición)
4. **Inspector:**
   - **Has Exit Time:** ❌ (uncheck)
   - **Transition Duration:** 0.1
   - **Conditions:** Click "+"
     - **isMoving = true**

**Walk → Idle:**

1. **Right click en "ED_Walk" → Make Transition**
2. **Click en "ED_Idle"**
3. **Inspector:**
   - **Has Exit Time:** ❌
   - **Conditions:**
     - **isMoving = false**

### PASO 6: Conectar con CharacterAnimator

1. **Selecciona "Player"** (NO Visual)
2. **Add Component → Character Animator** (el script que creé antes)
3. **Inspector:**
   - **Auto Flip Sprite:** ✓
   - **Visual Transform:** Asignar "Visual" (drag desde Hierarchy)

**Este script automáticamente:**
- Lee el estado del CharacterController2D
- Actualiza el parámetro "isMoving" del Animator
- Hace flip del personaje al cambiar dirección

### PASO 7: Probar Todo Junto

1. **Press Play** ▶
2. **Presionar A/D para mover:**
   - Idle cuando no te mueves
   - Walk cuando te mueves
   - Flip automático al cambiar dirección
3. **Space para saltar** (aún usa animación Idle por ahora)

---

## 🐛 TROUBLESHOOTING

### "No veo el botón Skinning Editor"
- Verificar que 2D Animation está instalado
- Verificar que Ed90.psb tiene Character Rig = checked
- Reiniciar Unity

### "Los sprites no se mueven con los bones"
- No se aplicó Auto Weights
- En Skinning Editor → Weights → Auto Weights → Generate All
- Verificar que cada sprite tiene weight > 0 en algún bone

### "El personaje se ve estirado/deformado"
- Bones mal posicionados
- Volver a Skinning Editor → Edit Joints
- Reposicionar bones
- Apply cambios

### "La animación no se reproduce"
- Verificar que Animator tiene Controller asignado
- Verificar que ED_Idle es Default State (naranja)
- Verificar que Animation window muestra el clip

### "El personaje no se voltea (flip)"
- Verificar que CharacterAnimator está en "Player"
- Verificar que Visual Transform está asignado
- Verificar Auto Flip Sprite = checked

### "Al hacer Play, el personaje desaparece"
- Visual está muy lejos (Position incorrecta)
- Ajustar Position de Visual a (0, 0, 0)
- Verificar Scale no es 0

---

## 📚 PRÓXIMOS PASOS

Después de completar este tutorial, deberías tener:

✅ Personaje ED con rig completo
✅ Bones creados y conectados
✅ Sprites asignados a bones (skinning)
✅ Animación Idle (respiración)
✅ Animación Walk (caminar)
✅ Animator configurado con transiciones
✅ Integración con CharacterController2D

**Siguiente fase:**
1. Crear animaciones Jump, Fall, Dash
2. Configurar transiciones para estos estados
3. Ajustar y pulir animaciones
4. Agregar animaciones de combate (Attack, Hurt, etc.)

---

## ✅ CHECKLIST FINAL

Verifica que completaste:

- [ ] Package 2D Animation instalado
- [ ] Ed90.psb configurado con Character Rig
- [ ] Skeleton creado en Skinning Editor (15-16 bones)
- [ ] Auto Weights aplicado (skinning completo)
- [ ] Prefab ED_Rigged creado
- [ ] Prefab como hijo de Player (renombrado a "Visual")
- [ ] Animación ED_Idle creada y funciona
- [ ] Animación ED_Walk creada y funciona
- [ ] Animator Controller configurado
- [ ] Transiciones Idle ↔ Walk funcionan
- [ ] CharacterAnimator agregado a Player
- [ ] Flip automático funciona
- [ ] Todo funciona al presionar Play

---

**¡FELICIDADES!** 🎉

Has completado el rigging 2D básico de tu personaje. Esto es una habilidad muy valiosa que puedes usar en cualquier juego 2D.

**¿Listo para continuar con más animaciones?**
