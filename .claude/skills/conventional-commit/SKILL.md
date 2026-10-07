---
name: conventional-commit
description: "Redacta y crea commits con formato Conventional Commits (tipo(scope): descripción en imperativo) a partir de los cambios reales del repo. Se usa cuando el usuario pide hacer, crear o redactar un commit, o commitear cambios."
---

# Conventional Commit

Nunca elijas el tipo ni la descripción de memoria: primero mirá qué cambió de
verdad, después redactá el mensaje. El mensaje describe el diff, no la intención
de la conversación.

## El formato (estructura obligatoria)

```
tipo(scope): descripción en imperativo

<cuerpo opcional: qué cambió y por qué>
```

Ejemplos:

```
feat(auth): agregar validación de email en el registro
fix(tickets): rechazar tickets con asunto vacío
docs(prd): aclarar criterio de control de acceso
```

## Paso 1 — Mirar qué se va a commitear

Antes de elegir tipo y descripción, corré:

```bash
git status --short       # qué archivos cambiaron y cuáles están en stage
git diff --staged        # el contenido exacto que entra en el commit
git diff                 # cambios que todavía NO están en stage
git log --oneline -5     # cómo vienen nombrándose los commits del repo
```

- Si no hay nada en stage, agregá solo los archivos que el usuario pidió
  commitear (`git add <archivo>`), nunca `git add .` a ciegas.
- Si hay cambios mezclados (por ejemplo, un fix y una feature), decíselo al
  usuario y proponé separarlos en commits distintos.

## Paso 2 — Elegir el tipo

| Tipo       | Cuándo                                                        |
|------------|---------------------------------------------------------------|
| `feat`     | Funcionalidad nueva para el usuario.                          |
| `fix`      | Corrige un bug.                                               |
| `docs`     | Solo documentación (PRD, README, AGENTS.md, comentarios).     |
| `refactor` | Cambia código sin cambiar comportamiento.                     |
| `test`     | Agrega o corrige tests.                                       |
| `style`    | Formato, espacios, punto y coma; sin cambio de lógica.        |
| `perf`     | Mejora de rendimiento.                                        |
| `build`    | Dependencias, empaquetado, configuración de build.            |
| `ci`       | Pipelines de integración continua.                            |
| `chore`    | Mantenimiento que no encaja en los anteriores.                |

Si el diff encaja en más de un tipo, es señal de que conviene partir el commit.

## Paso 3 — Elegir el scope

- El área o módulo afectado, en minúscula y en una palabra: `auth`, `tickets`,
  `prd`, `skills`, `agents`, `backend`, `frontend`.
- Reusá los scopes que ya aparecen en `git log` antes de inventar uno nuevo.
- Si el cambio toca todo el repo y no hay un área clara, omití el scope:
  `chore: actualizar dependencias`.

## Paso 4 — Redactar la descripción

- En imperativo con verbo en infinitivo: "agregar", "corregir", "eliminar",
  "aclarar". Nunca "agregado", "agrega" ni "se agregó".
- En minúscula, sin punto final, y la primera línea completa en 72 caracteres
  o menos.
- Dice QUÉ cambia, concreto: "rechazar tickets con asunto vacío", no
  "arreglar bug" ni "cambios varios".
- El cuerpo es opcional: usalo cuando el porqué no se deduce del diff, separado
  de la primera línea por una línea en blanco.

## Paso 5 — Confirmar y commitear

Mostrale al usuario el mensaje y los archivos que entran, y commiteá. Después
corré `git log --oneline -1` para confirmar que quedó como se esperaba.

## Reglas duras (siempre)

- Nada de "update", "fix stuff", "wip" ni descripciones genéricas: si no podés
  describir el cambio en una línea concreta, el commit es demasiado grande.
- Nunca commitees archivos que el usuario no pidió incluir.
- Nunca uses `--no-verify` ni saltees hooks; si un hook falla, arreglá la causa.
- Nunca hagas push salvo que el usuario lo pida explícitamente.
- Si el entorno pide una línea de atribución (por ejemplo `Co-Authored-By`), va
  al final del cuerpo, separada por una línea en blanco.
- Ante la duda sobre el tipo o el scope, preguntá; nunca inventes.
