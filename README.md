# Nabhi EDs Legacy — Guía de configuración

Guía para clonar el proyecto y empezar a trabajar desde cero.

---

## Requisitos previos

Antes de clonar el repo, asegúrate de tener instalado:

- **Unity Hub** — [descargar aquí](https://unity.com/download)
- **Unity 2022.3.51f1** — se instala desde Unity Hub (ver paso 2)
- **Git** — [descargar aquí](https://git-scm.com/downloads)

---

## Paso 1 — Clonar el repositorio

Abre una terminal (CMD o Git Bash) en la carpeta donde quieras guardar el proyecto y ejecuta:

```bash
git clone https://github.com/gioaristasdev-bot/Eds-Legacy-Repo.git
```

Esto creará una carpeta llamada `Eds-Legacy-Repo` con todo el proyecto dentro.

---

## Paso 2 — Instalar la versión correcta de Unity

Es **importante** usar exactamente la versión **2022.3.51f1** para evitar problemas de compatibilidad.

1. Abre **Unity Hub**
2. Ve a **Installs → Install Editor**
3. Busca la versión `2022.3.51f1` y haz clic en **Install**
4. En módulos, asegúrate de incluir:
   - **Windows Build Support (IL2CPP)**
   - **Android Build Support** *(si lo necesitas)*

---

## Paso 3 — Abrir el proyecto

1. Abre **Unity Hub**
2. Ve a **Projects → Add project from disk**
3. Selecciona la carpeta `Eds-Legacy-Repo` que clonaste
4. Unity cargará el proyecto (la primera vez puede tardar varios minutos mientras importa los assets)

> La carpeta `Library/` se regenera automáticamente al abrir el proyecto por primera vez. Es normal que tarde.

---

## Paso 4 — Flujo de trabajo con Git

Para mantener el repositorio ordenado, sigue estos pasos al trabajar:

### Antes de empezar a trabajar — actualiza tu copia local
```bash
git pull
```

### Cuando quieras guardar tus cambios
```bash
git add .
git commit -m "Descripción breve de lo que hiciste"
git push
```

### Ver el estado de tus archivos modificados
```bash
git status
```

---

## Archivos excluidos del repositorio

Los siguientes archivos **no están en el repo** pero estarán en tu disco al abrir Unity por primera vez:

| Carpeta / Archivo | Razón |
|---|---|
| `Library/` | Generada automáticamente por Unity |
| `Temp/` | Archivos temporales de compilación |
| `Logs/` | Logs locales |
| `obj/` | Archivos de compilación de VS |
| `.vs/` | Configuración local de Visual Studio |
| `*.psb` / `SourceFiles/*.zip` | Source files de assets comprados, no necesarios para el proyecto |

---

## Problemas comunes

**Unity no encuentra la versión del proyecto**
→ Instala la versión `2022.3.51f1` desde Unity Hub (ver Paso 2).

**El proyecto tarda mucho en abrir la primera vez**
→ Es normal. Unity está regenerando la carpeta `Library/`. No cierres Unity.

**Conflictos en archivos `.meta`**
→ Nunca borres archivos `.meta`. Si hay un conflicto en uno, avisa al equipo antes de resolverlo.

**Error al hacer `git push` (acceso denegado)**
→ Asegúrate de tener acceso al repositorio. Pídele al owner que te añada como colaborador.
