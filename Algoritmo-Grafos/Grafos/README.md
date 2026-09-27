# RutaOptima API

API en **C# / ASP.NET Core** que calcula la ruta más corta entre dos puntos de un
barrio (por ejemplo, entre un restaurante y la ubicación de un cliente) usando
algoritmos clásicos de grafos: **Dijkstra** y **A\***.

## Problema planteado

Simular el sistema de asignación de rutas de una app de domicilios (tipo
Uber Eats / Rappi): dado un mapa de calles de un barrio (representado como
grafo), calcular la ruta más corta entre el punto de recogida (restaurante)
y el punto de entrega (cliente), para minimizar el tiempo/distancia de cada
domicilio.

## Algoritmo de grafos utilizado

### Dijkstra
Encuentra el camino más corto desde un nodo origen a todos los demás nodos
del grafo, garantizando la ruta óptima en grafos con pesos no negativos.
Funciona expandiendo siempre el nodo no visitado con menor distancia
acumulada conocida, usando una cola de prioridad (min-heap).

### A* (A-estrella)
Extiende Dijkstra agregando una **heurística**: en vez de expandir el nodo
con menor distancia acumulada `g(n)`, expande el nodo con menor
`f(n) = g(n) + h(n)`, donde `h(n)` es una estimación del costo restante
hasta el destino. Usamos la **distancia Haversine** (línea recta entre
coordenadas geográficas) como heurística, que nunca sobreestima la distancia
real por calle, por lo que A* también garantiza la ruta óptima, pero
explorando muchos menos nodos que Dijkstra.

En las pruebas con el grafo real (932 nodos), para una ruta larga (~3.1 km):

| Algoritmo | Nodos explorados | Distancia obtenida |
|---|---|---|
| Dijkstra | 924 (casi todo el grafo) | 3120.1 m |
| A*       | 554 | 3120.1 m |

Misma ruta óptima, pero A* exploró ~40% menos nodos gracias a la heurística.
Visualmente, en el mapa se nota clarísimo: A* "apunta" hacia el destino en
forma de cono, mientras Dijkstra se expande casi circularmente en todas
direcciones — buen momento para el video de sustentación.

## Cómo se aplicó al problema

1. El barrio se modela como un **grafo no dirigido y ponderado**: cada nodo
   es una intersección/punto de interés (con coordenadas lat/lng reales) y
   cada arista es una calle, con peso igual a la distancia en metros entre
   sus dos nodos (calculada con la fórmula de Haversine).
2. El grafo se carga desde [`Data/grafo_barrio.json`](Data/grafo_barrio.json)
   al iniciar la API.
3. El cliente (frontend) pide una ruta indicando el nodo de origen, el nodo
   de destino y el algoritmo a usar.
4. La API corre el algoritmo correspondiente y devuelve: la ruta completa
   (lista ordenada de nodos), la distancia total, y el **orden de
   exploración** de nodos (para poder animar la búsqueda en el mapa).

## Video de sustentación

`[https://drive.google.com/file/d/1wT7ctgXbycpvjwTb3KyZp9WTqtgG52WV/view?usp=sharing]`

