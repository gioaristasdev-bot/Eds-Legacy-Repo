# SESIÓN 15 - CHECKLIST DE REVISIÓN DEL PROYECTO NABHI

**Fecha:** 2025-12-09
**Objetivo:** Documentar el estado actual del proyecto NABHI antes de la integración

**NOTA:** ✅ = Información confirmada del archivo CLAUDE.md | ⚠️ = Necesita verificación en Unity

---

## 📋 PARTE 1: INFORMACIÓN DEL PROYECTO

### 1.1 Información Básica

- **Versión Unity (documentada):** 2021.3.13f1 ✅
- **Versión Unity (actual según usuario):** 2022.3.62f2 ⚠️ (Verificar en Unity Hub)
- **Ubicación:** `C:\Users\PcVip\Documents\NABHI\Nabhi el legado de ED2-12-09-2024-Ver-2021-3-13f1` ✅
- **Usa Corgi Engine:** Sí ✅
- **Pipeline de Renderizado:** Universal Render Pipeline (URP) ✅
- **¿Abre sin errores?:** _____________ (Verificar al abrir)

### 1.2 Escenas del Proyecto

**Escenas identificadas en CLAUDE.md:** ✅

- **Menús:**
  - PantallaPresentacion.unity
  - MenuPrincipal.unity
  - LoadingScreen.unity
  - Agradecimientos.unity

- **Cinemáticas:**
  - CinematicaIntro.unity
  - CinematicaGamePlay.unity

- **Niveles Jugables:**
  - Nivel-0.unity (Tutorial/inicial)
  - Nivel-1.unity
  - Nivel-2.unity

- **Escena principal para testing:** Nivel-0.unity ✅
- **Cantidad total de escenas:** 9+ escenas ✅

---

## 🎮 PARTE 2: PLAYER ACTUAL

### 2.1 Identificación del Player

- **GameObject nombre:** _____________
- **Ubicación en Hierarchy:** _____________
- **Ubicación del prefab:** _____________
- **Tag del Player:** _____________
- **Layer del Player:** _____________

### 2.2 Componentes del Player

**Listar todos los componentes/scripts:**

```
GameObject: [Nombre del Player]
├── Transform
├── Rigidbody2D: _____________
├── Collider: _____________ (tipo)
├── [Componente 1]: _____________
├── [Componente 2]: _____________
├── [Componente 3]: _____________
├── [Componente 4]: _____________
└── (continuar...)
```

### 2.3 Scripts de Corgi en el Player

**Marcar cuáles tiene:**

- [ ] Character
- [ ] CharacterAbility
- [ ] CharacterHandleWeapon
- [ ] CharacterHorizontalMovement
- [ ] CharacterJump
- [ ] CharacterDash
- [ ] Otros: _____________

### 2.4 Posición Inicial del Player

- **Position X:** _____________
- **Position Y:** _____________
- **Position Z:** _____________
- **Escala (Scale):** _____________

---

## 👾 PARTE 3: ENEMIGOS

### 3.1 Tipos de Enemigos

**Listar todos los tipos de enemigos:**

1. **Enemigo Tipo 1:**
   - Nombre: _____________
   - Prefab ubicación: _____________
   - ¿Tiene sistema de salud?: Sí / No
   - Script de salud: _____________
   - Vida máxima: _____________
   - Layer: _____________

2. **Enemigo Tipo 2:**
   - Nombre: _____________
   - Prefab ubicación: _____________
   - ¿Tiene sistema de salud?: Sí / No
   - Script de salud: _____________
   - Vida máxima: _____________
   - Layer: _____________

3. **Enemigo Tipo 3:**
   - Nombre: _____________
   - Prefab ubicación: _____________
   - ¿Tiene sistema de salud?: Sí / No
   - Script de salud: _____________
   - Vida máxima: _____________
   - Layer: _____________

(Agregar más si es necesario)

### 3.2 Sistema de Salud de Enemigos

- **¿Los enemigos tienen salud?:** Sí ✅
- **Método para aplicar daño:** _____________
- **¿Implementan alguna interfaz?:** _____________
- **Efectos al recibir daño:** _____________
- **Comportamiento al morir:** _____________

---

## 🎨 PARTE 4: SISTEMAS EXISTENTES

### 4.1 UI (Interfaz de Usuario)

- [ ] **UI de Salud**
  - Ubicación: _____________
  - Tipo (barra, corazones, etc.): _____________
  - Script: _____________

- [ ] **UI de Munición**
  - Ubicación: _____________
  - Tipo: _____________
  - ¿Munición infinita?: Sí ✅

- [ ] **UI de Puntos**
  - Ubicación: _____________
  - Script: _____________

- [ ] **Otros elementos de UI:**
  - _____________
  - _____________

### 4.2 Sistemas de Juego

- [ ] **Audio Manager**
  - ¿Existe?: Sí / No
  - Ubicación: _____________

- [ ] **Game Manager**
  - ¿Existe?: Sí / No
  - Ubicación: _____________

- [ ] **Sistema de Checkpoints**
  - ¿Existe?: Sí / No

- [ ] **Sistema de Power-ups**
  - ¿Existe?: Sí / No

- [ ] **Sistema de Diálogos**
  - ¿Existe?: Sí / No

### 4.3 Cámara Actual

- **Tipo de cámara:** _____________
- **¿Usa Cinemachine?:** Sí / No
- **Configuración de seguimiento:** _____________
- **Smooth follow:** Sí / No

---

## ⚙️ PARTE 5: PROJECT SETTINGS

### 5.1 Tags

**Listar todos los tags personalizados:**

- Player: _____________
- Enemy: _____________
- Ground: _____________
- Wall: _____________
- Otros:
  - _____________
  - _____________
  - _____________

### 5.2 Layers

**Layers del proyecto NABHI actual:**

```
User Layer 8: _____________
User Layer 9: _____________
User Layer 10: _____________
User Layer 11: _____________
User Layer 12: _____________
User Layer 13: _____________
User Layer 14: _____________
User Layer 15: _____________
```

### 5.3 Input Manager

**Configuraciones de Input actuales:**

**Horizontal:**
- Positive Button: _____________
- Negative Button: _____________
- Alt Positive Button: _____________
- Alt Negative Button: _____________
- Type: _____________

**Vertical:**
- Positive Button: _____________
- Negative Button: _____________
- Alt Positive Button: _____________
- Alt Negative Button: _____________
- Type: _____________

**Jump:**
- Positive Button: _____________
- Alt Positive Button: _____________

**Fire1:**
- Positive Button: _____________
- Alt Positive Button: _____________

**Dash (si existe):**
- Positive Button: _____________
- Alt Positive Button: _____________

**Otros ejes personalizados:**
- _____________
- _____________

### 5.4 Physics 2D

**Layer Collision Matrix:**

(Tomar screenshot o anotar configuraciones importantes)

- Player colisiona con: _____________
- Enemies colisiona con: _____________
- Ground colisiona con: _____________
- Configuraciones especiales: _____________

---

## 🗺️ PARTE 6: ESTRUCTURA DEL MAPA

### 6.1 Plataformas y Suelos

- **GameObject de suelo principal:** _____________
- **Layer de suelos:** _____________
- **Cantidad de plataformas:** _____________
- **¿Hay plataformas móviles?:** Sí / No

### 6.2 Paredes

- **¿Hay paredes para wall jump?:** Sí / No
- **Layer de paredes:** _____________

### 6.3 Límites del Mapa

- **¿Hay bounds/límites configurados?:** Sí / No
- **Método usado:** _____________

### 6.4 Tamaño del Mapa

- **Ancho aproximado (unidades):** _____________
- **Alto aproximado (unidades):** _____________

---

## 📦 PARTE 7: PACKAGES INSTALADOS

**Window → Package Manager:**

Verificar cuáles están instalados:

- [ ] 2D Animation (versión: _____)
- [ ] 2D PSD Importer (versión: _____)
- [ ] 2D Sprite (versión: _____)
- [ ] Cinemachine (versión: _____)
- [ ] TextMeshPro (versión: _____)
- [ ] Corgi Engine (versión: _____)
- [ ] Otros packages importantes:
  - _____________
  - _____________

---

## 🔍 PARTE 8: ANÁLISIS DE COMPATIBILIDAD

### 8.1 Conflictos Potenciales

**Identificar posibles conflictos:**

- [ ] ¿Hay scripts con nombres que puedan chocar? _____________
- [ ] ¿Hay prefabs con nombres duplicados? _____________
- [ ] ¿Hay layers ya usados que necesitemos? _____________
- [ ] ¿Input Manager tiene configuraciones que conflictúen? _____________

### 8.2 Dependencias de Corgi

**¿Qué otros sistemas dependen de Corgi?**

- Enemigos: _____________
- UI: _____________
- Armas: _____________
- Power-ups: _____________
- Otros: _____________

---

## 📝 PARTE 9: NOTAS ADICIONALES

**Anotar cualquier observación importante:**

```
-
-
-
```

**Problemas encontrados al abrir el proyecto:**

```
-
-
-
```

**Características especiales del juego:**

```
-
-
-
```

---

## ✅ CHECKLIST DE COMPLETITUD

Marcar cuando hayas completado cada sección:

- [ ] Parte 1: Información del Proyecto
- [ ] Parte 2: Player Actual
- [ ] Parte 3: Enemigos
- [ ] Parte 4: Sistemas Existentes
- [ ] Parte 5: Project Settings
- [ ] Parte 6: Estructura del Mapa
- [ ] Parte 7: Packages Instalados
- [ ] Parte 8: Análisis de Compatibilidad
- [ ] Parte 9: Notas Adicionales

---

## 🎯 PRÓXIMO PASO

Una vez completado este checklist:

1. Guardar este documento
2. Cerrar Unity
3. Proceder con el **Backup del Proyecto NABHI** (Paso 1.2 del plan)

---

**Última actualización:** 2025-12-09
**Estado:** En progreso
