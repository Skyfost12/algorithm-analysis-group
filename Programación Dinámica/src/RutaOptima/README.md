# RutaOptima API

## 1. El problema

Un repartidor sale del **depósito**, debe pasar por todas las **entregas** exactamente una vez y volver al depósito. Conocemos la distancia `d(a, b)` entre cualquier par de paradas. **¿En qué orden debe visitarlas para recorrer lo menos posible?** Es el *problema del viajante* (TSP) con depósito.

Probar todos los órdenes cuesta `(n-1)!` rutas: con 8 entregas son 40.320 y con 17 son 3,6 × 10¹⁴. Sin una idea mejor, el problema es inviable.

## 2. Por qué Programación Dinámica

- **Subestructura óptima:** el mejor camino a un grupo de entregas contiene el mejor camino a un grupo más pequeño. Si no fuera así, cambiaríamos el tramo anterior por uno mejor y mejoraríamos el total.
- **Subproblemas traslapados:** muchos órdenes comparten el mismo comienzo. El mismo subproblema se necesita muchas veces, así que se calcula una sola vez y se guarda en una tabla.

La parada 0 es el depósito y las entregas son `1..m` (`m = n-1`). Un recorrido es un ciclo, así que fijar el depósito como origen no pierde ninguna solución.

## 3. Estado, recurrencia y casos base

Los subconjuntos `S` de entregas se guardan como **máscaras de bits**: el bit `b` vale 1 si la entrega `b+1` está en el conjunto.

- **Estado:** `dp[S][j]` = menor costo de salir del depósito, visitar exactamente las entregas de `S` y terminar en `j` (`j ∈ S`). Se guarda también `Parent[S][j]` = parada anterior (0 = depósito). Hay `m · 2^(m-1)` estados.
- **Caso base:** `dp[{j}][j] = d(0, j)`: ir directo del depósito a `j`.
- **Recurrencia:** `dp[S][j] = min sobre k ∈ S∖{j} de dp[S∖{j}][k] + d(k, j)`. La entrega anterior a `j` fue alguna `k` del resto del conjunto: se prueba cada una y gana la más barata.
- **Orden de cálculo:** los subconjuntos se recorren en orden numérico; así `S∖{j}`, que es un número menor, ya está calculado.
- **Respuesta:** `min sobre j de dp[todas][j] + d(j, 0)` (se cierra el recorrido volviendo al depósito). El recorrido `[0, ..., 0]` se reconstruye con `Parent`, hacia atrás desde la mejor `j`.
- **Desempates deterministas:** gana la `k` más pequeña y, al cerrar, la `j` más pequeña.

## 4. Implementación

El algoritmo está en [`Algoritmos/HeldKarp.cs`](Algoritmos/HeldKarp.cs) (`HeldKarp.Solve`), sin dependencias del mapa ni de la API: solo recibe una matriz de distancias. Pseudocódigo:

```
m = n - 1;  subconjuntos = 2^m

para j de 1 a m:                                   // casos base
    dp[{j}][j] = d(0, j);  padre[{j}][j] = 0

para S de 1 a 2^m - 1:                             // orden numérico
    si S tiene un solo elemento: continuar
    para cada j en S:
        resto = S sin j
        mejor = ∞
        para cada k en resto:
            c = dp[resto][k] + d(k, j)
            si c < mejor:  mejor = c;  mejorK = k
        dp[S][j] = mejor;  padre[S][j] = mejorK

total = min sobre j de ( dp[todas][j] + d(j, 0) )  // cerrar el recorrido
si total = ∞:  resultado NO FACTIBLE (no hay recorrido)
si no:         recorrido = seguir padre[...] hacia atrás desde la mejor j
```

`HeldKarp.PathFor` reconstruye el camino óptimo de cualquier celda `(S, j)`: es lo que dibuja el mapa al pasar el cursor por la tabla DP. [`Algoritmos/Reference.cs`](Algoritmos/Reference.cs) tiene los algoritmos con los que se compara: la misma recurrencia **sin tabla** (recursión sin memoización), la **fuerza bruta** y el **vecino más cercano** (heurística voraz).

## 5. Ejemplo resuelto a mano (4 paradas)

Ejemplo pequeño para seguir la recurrencia con lápiz. Parada 0 = depósito.

```
        0   1   2   3
  0 [   0  10  15  20 ]
  1 [  10   0  35  25 ]
  2 [  15  35   0  30 ]
  3 [  20  25  30   0 ]
```

- **Casos base:** `dp[{1}][1] = 10`, `dp[{2}][2] = 15`, `dp[{3}][3] = 20`.
- **De 2 entregas:** `dp[{1,2}][1] = 50`, `dp[{1,2}][2] = 45`, `dp[{1,3}][1] = 45`, `dp[{1,3}][3] = 35`,
  `dp[{2,3}][2] = 50`, `dp[{2,3}][3] = 45`. Ejemplo: `dp[{1,2}][1] = dp[{2}][2] + d(2,1) = 15 + 35 = 50`.
- **De 3 entregas:** `dp[{1,2,3}][1] = min(85, 70) = 70` (viene de 3); `dp[{1,2,3}][2] = min(80, 65) = 65`
  (viene de 3); `dp[{1,2,3}][3] = min(75, 75) = 75` (empate: gana `k = 1`).
- **Cerrar el recorrido:** `70 + 10 = 80`, `65 + 15 = 80`, `75 + 20 = 95`. Mínimo **80** (empate entre
  `j = 1` y `j = 2`: gana `j = 1`).
- **Reconstrucción con `Parent`:** `j = 1` viene de 3, `3` viene de 2, `2` viene del depósito →
  **`0 → 2 → 3 → 1 → 0`**, con costo `15 + 30 + 25 + 10 = 80`.

## 6. Complejidad y límites

| | Held-Karp (DP) | Recursión sin memoización | Fuerza bruta |
|---|:---:|:---:|:---:|
| Tiempo | `O(n² · 2ⁿ)` | del orden de `(n-1)!` llamadas (≈ `e · (n-1)!`) | `O(n!)` |
| Espacio | `O(n · 2ⁿ)` | `O(n)` de pila | `O(n)` |
| Estados / transiciones | `(n-1)·2^(n-2)` / `(n-1)(n-2)·2^(n-3)` | — | — |

Held-Karp **sigue siendo exponencial**: el problema del viajante es NP-difícil. Lo que logra es pasar de `n!` a `2ⁿ`. La tabla guarda un `double` y un `sbyte` por celda (9 bytes): unos 19,1 MB con 18 paradas y 85,5 MB con 20.

- El algoritmo admite hasta **20 paradas** (`HeldKarp.MaxStops`); la interfaz acepta hasta **18** (depósito + 17 entregas).
- La **tabla DP** se muestra con hasta 7 entregas; con más sería demasiado grande para mostrarla.
- Funciona también si la matriz de distancias **no es simétrica**.

## 7. El mapa y las distancias

El algoritmo solo ve una matriz `d(i, j)`. Esa matriz la construye un `IDistanceProvider`; el que se usa hoy (`StreetGraphDistanceProvider`) toma las **distancias reales por calle, en metros**, sobre un grafo del barrio:

1. El grafo es **no dirigido y ponderado**: cada nodo es una intersección (con coordenadas lat/lng reales) y cada arista es una calle, con peso igual al **largo de la calle en metros según OpenStreetMap** (atributo `length` que entrega OSMnx en [`scripts/generar_grafo.py`](scripts/generar_grafo.py)): 932 nodos y 1.089 calles. En las 1.089 aristas ese peso es mayor o igual a la distancia Haversine entre sus dos nodos (en 930 es prácticamente igual; en 159 es mayor porque la calle curva).
2. Se carga desde [`Data/grafo_barrio.json`](Data/grafo_barrio.json) al iniciar la API, y el navegador lo pide una vez (`GET /api/grafo`) para dibujar el mapa y ajustar cada clic al nodo de calle más cercano.
3. Cada parada se ajusta al nodo más cercano (máx. 300 m) y se corre un Dijkstra desde cada una (`n` corridas, no `n²`). Si no hay camino entre dos paradas, la casilla queda en `+∞`.

El código soporta calles de un solo sentido (matriz asimétrica), pero **los datos de este mapa tienen todas las calles de doble sentido**, así que aquí la matriz sale simétrica. Es distancia en metros, no tiempo. Para usar OSRM, Google, etc. basta otra implementación de la interfaz.

## 8. Resultados obtenidos
Con paradas al azar sobre **el grafo real de Medellín** y distancias por calle. Los tres algoritmos dieron siempre el mismo costo óptimo. Los tiempos dependen de la máquina; lo importante es la tendencia. *Nota:* estas cifras salen de un programa de medición temporal que no está en el repositorio.

| Paradas | Rutas posibles (n−1)! | Fuerza bruta (ms) | Recursión sin memo (llamadas) | Recursión sin memo (ms) | Held-Karp (estados) | Held-Karp (ms) |
|:---:|---:|---:|---:|---:|---:|---:|
| 7 | 720 | 0,08 | 1.956 | 0,08 | 192 | 0,02 |
| 9 | 40.320 | 2,1 | 109.600 | 0,64 | 1.024 | 0,02 |
| 11 | 3.628.800 | 85,9 | 9.864.100 | 69,9 | 5.120 | 0,22 |
| 12 | 39.916.800 | 884,7 | 108.505.111 | 808,5 | 11.264 | 0,51 |

Con 12 paradas, Held-Karp es unas 1.700 veces más rápido que la fuerza bruta. La recursión sin
memoización hace **más llamadas que rutas existen** (≈ 2,718 veces más, el número `e`): sin guardar los
subproblemas, la misma recurrencia no ahorra nada.

| Paradas | Rutas posibles (n−1)! | Estados DP | Transiciones | Memoria de la tabla (MB) | Tiempo (ms) |
|:---:|---:|---:|---:|---:|---:|
| 14 | 6,23 × 10⁹ | 53.248 | 319.488 | 0,9 | 2,6 |
| 16 | 1,31 × 10¹² | 245.760 | 1.720.320 | 4,2 | 14,2 |
| 18 | 3,56 × 10¹⁴ | 1.114.112 | 8.912.896 | 19,1 | 62,4 |
| 20 | 1,22 × 10¹⁷ | 4.980.736 | 44.826.624 | 85,5 | 297,2 |

Cada parada nueva duplica aproximadamente el tiempo y la memoria. **Calidad:** sobre 500 mapas
aleatorios con 8 entregas, el **vecino más cercano** fue peor que el óptimo en 461 (92,2 %), en promedio
9,9 % más largo y hasta 40,4 % en el peor mapa. Ejemplo reproducible con 5 entregas (nodos `N510`,
`N63`, `N720`, `N378`, `N25`, `N503`): óptimo 5.729 m contra 6.815 m del vecino más cercano (19 % más).

## 9. Casos borde

| Situación | Qué pasa |
|---|---|
| Solo el depósito | Recorrido `[0]` con costo 0. |
| Una entrega | Recorrido `[0, 1, 0]` con costo `d(0,1) + d(1,0)`. |
| Matriz no cuadrada, vacía o con más de 20 paradas | `HeldKarp.Solve` lanza `ArgumentException`. |
| Sin paradas, más de 18 paradas o coordenadas inválidas | La API responde **400** con un mensaje (`Máximo 18 paradas…`). |
| Una parada a **más de 300 m** de cualquier calle | **422**, `codigo: "parada_fuera_del_mapa"`, mensaje con el nombre de la parada y la distancia (`La entrega 1 está a 1113 m de la calle más cercana (máximo 300 m): queda fuera del mapa.`) y `paradas` con los índices (0 = depósito). Si son varias, las lista todas. En el navegador, un clic así se ignora con un aviso que dice a cuántos metros está la calle más cercana. |
| Distancia `+∞` en alguna casilla (`d(a, b)` = "no se puede ir de a a b"), pero existe un recorrido que la evita | Se resuelve normalmente por otro camino. Si la matriz tiene `+∞` no se muestra la tabla DP. |
| Una entrega **inalcanzable** por las calles desde el depósito, o **desde la que no se puede volver** (parada aislada, calle de un solo sentido) | Si todas las opciones de cierre son `+∞`, `HeldKarp.Solve` **no reconstruye nada**: devuelve `IsFeasible = false`, `TotalCost = +∞` y `Tour` vacío. La API responde **422** con `codigo: "parada_inalcanzable"` y las paradas implicadas, p. ej. `La entrega 2 no es alcanzable por las calles desde el depósito.` o `Desde la entrega 1 no se puede volver al depósito por las calles (¿calle de un solo sentido?).` |
| Todas las paradas se alcanzan, pero ningún recorrido las une | **422**, `codigo: "sin_recorrido"`, sin culpar a una parada. |

## 10. Limitaciones

- Es exacto pero **exponencial**: el problema del viajante es NP-difícil. Pasadas unas 20 paradas la
  memoria es el límite.
- Distancias en metros por calle, no tiempo: sin tráfico; solo un tramo de El Poblado; las calles se
  modelan de doble sentido; cada parada se mueve al nodo de calle más cercano.
- Un solo repartidor, sin capacidad ni ventanas de tiempo.

## Link video
[Video demostrativo]()