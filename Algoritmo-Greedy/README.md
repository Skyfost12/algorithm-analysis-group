# Sistema de Asignación de Turnos y Salas — Algoritmo Greedy (.NET 10)

**Examen 1 — Análisis de Algoritmos**
Opción 1 (Desarrollo) — versión ASP.NET Core / .NET 10

## El problema

Una universidad recibe muchas más solicitudes de clases/actividades de las
que un salón puede atender en un día, y varias de esas solicitudes se
cruzan en el tiempo. Se necesita un sistema que, dado un conjunto de
actividades (cada una con nombre, salón y horario de inicio/fin), determine
**el mayor número posible de actividades que pueden dictarse en cada salón
sin que se crucen los horarios**.

Este es el problema clásico de **Activity Selection** (Selección de
actividades), y se resuelve de forma óptima con una estrategia **Greedy**.

## El algoritmo — Activity Selection (Greedy)

**Idea central:** en cada paso, de todas las actividades que todavía
podrían agendarse, se elige siempre la que **termina más temprano**. Esa
decisión nunca se deshace (no hay backtracking ni reconsideración).

**Pasos:**

1. Agrupar las actividades por salón (salones distintos no compiten entre sí).
2. Dentro de cada salón, ordenar las actividades por hora de **fin** ascendente.
3. Recorrer la lista ordenada:

   * Si la actividad actual **inicia después o justo cuando termina** la
última aceptada → se acepta.
   * Si se cruza con la última aceptada → se rechaza.
4. El resultado es el subconjunto máximo de actividades sin solape para ese salón.

**¿Por qué greedy y no fuerza bruta?**
Probar todas las combinaciones posibles de actividades sería exponencial
(2ⁿ). El algoritmo greedy resuelve el problema en **O(n log n)** (el costo
del ordenamiento), y está demostrado matemáticamente que la estrategia
"elegir la que termina antes" produce el óptimo global, no solo uno local.

La implementación está en
[`Algorithms/GreedyScheduler.cs`](Algorithms/GreedyScheduler.cs), con
comentarios explicando cada decisión.

## Cómo se aplicó al problema

* El usuario agrega actividades desde el formulario del frontend (nombre,
salón, hora de inicio y fin).
* Al presionar **"Calcular asignación óptima"**, el frontend envía la lista
completa al backend (`POST /api/schedule`).
* La Minimal API ejecuta `GreedyScheduler.SelectActivities()` (el algoritmo
greedy) y devuelve dos listas: `selected` (asignadas) y `rejected` (con
el motivo del rechazo).
* El frontend dibuja un **tablero por salón** con los bloques asignados, y
además muestra la lista de actividades rechazadas.
* Cada corrida queda registrada en `data/historial.json` (vía
`HistoryService`), consultable en `GET /api/history`, para llevar
estadísticas de uso.



## Equipo y división de trabajo

Integrante

* Alejandro Atehortúa Pineda
* Efren Felipe Cuadrado Barboza
* Denisse Scarleth Gallardo Benjumea

## Link video
[Video demostrativo](https://drive.google.com/file/d/1XX7zDROQvvKlu0gyeCXy3QNt75U2YbCQ/view)