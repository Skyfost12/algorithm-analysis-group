[Kth Largest Element in an Array](https://leetcode.com/problems/kth-largest-element-in-an-array/description/)

Autor: Denisse Gallardo (denissegallardo09)  
Familia: Heap
Idea: Utilizamos un Min-Heap de tamaño máximo k para mantener los k elementos más grandes encontrados hasta el momento. Cuando el heap alcanza ese tamaño, si encontramos un número mayor que su raíz, eliminamos el menor y agregamos el nuevo valor.
Complejidad:
  n= Cantidad de elementos del arreglo
  k= Posición del elemento mayor que queremos encontrar
  Tiempo: O(n log k)
  Espacio: O(k)

![Accepted — Kth Largest](evidencias/kth-largest-accepted.png)


[Largest Number](https://leetcode.com/problems/largest-number/description/)

Autor: Denisse Gallardo (denissegallardo09)  
Familia: Sorting (Ordenamiento)
Idea: Convertimos todos los números a cadenas y los ordenamos utilizando un comparador basado en la concatenación: para dos valores a y b, colocamos primero a si a + b es mayor que b + a (concatenados, no sumados).
Complejidad:
  n= cantidad de elementos del arreglo
  Tiempo: O(n log n)
  Espacio: O(n)

![Accepted — Largest number](evidencias/largest-number-accepted.png)

[Longest Increasing Subsequence](https://leetcode.com/problems/longest-increasing-subsequence/description/)

Autor: Alejandro Atehortua (Skyfost12)  
Familia: Dynamic Programming
Idea: Creamos un arreglo dp donde cada posición representa la longitud de la subsecuencia creciente más larga que termina exactamente en ese índice. Para cada elemento, revisamos los elementos anteriores. Si encontramos uno menor, podemos extender la subsecuencia que termina en él.
Complejidad:
  n= cantidad de elementos del arreglo
  Tiempo: O(n²)
  Espacio: O(n)

![Accepted — Longest Increasing Subsequence](evidencias/Longes_Increasing_Subsequence.png)


[Gas station](https://leetcode.com/problems/gas-station/description/)

Autor: Alejandro Atehortua (Skyfost12)  
Familia: Greedy
Idea: Recorrimos las estaciones una sola vez acumulando la diferencia entre el gas disponible y el costo del viaje. Si el tanque acumulado queda negativo, se descarta todo el tramo recorrido como posible punto de partida y se reinicia el acumulado desde la siguiente estación. Si el balance total es negativo no existe solución, sino, el valor de start es la respuesta.
Complejidad:
  n= cantidad de elementos del arreglo
  Tiempo: O(n)
  Espacio: O(1)

![Accepted — Gas station](evidencias/Gas_Station.png)


[Word search](https://leetcode.com/problems/word-search/description/)

Autor: Alejandro Atehortua (Skyfost12)  
Familia: Backtracking
Idea: Recorremos la grilla buscando una celda que coincida con la primera letra de la palabra e iniciamos una búsqueda DFS. Comprobamos si la letra de la celda coincide con la posición actual de la palabra; si coincide marcamos la celda como utilizada y buscamos la siguiente letra en sus cuatro direcciones posibles, si ninguna dirección permite completar la palabra deshacemos la marca y probamos otra alternativa.
Complejidad:
  m= Cantidad de filas
  n= Cantidad de columnas
  L= Longitud de la palabra
  Tiempo: O(m n · 4^L)
  Espacio: O(L)

![Accepted — Word searchd](evidencias/Word_Search.png)



[Edit Distance](https://leetcode.com/problems/edit-distance/description/)

Autor: Efrén Cuadrado (efrencuadrado289348)
Familia: Dynamic Programming
Idea: Creamos una matriz dp donde dp[i][j] representa el número mínimo de operaciones para convertir los primeros i caracteres de word1 en los primeros j caracteres de word2. Los casos base son convertir desde o hacia una cadena vacía (i o j operaciones). Si los caracteres actuales coinciden, heredamos dp[i-1][j-1]; si no, tomamos 1 más el mínimo entre eliminar, insertar o reemplazar.
Complejidad:
  n= longitud de word1
  m= longitud de word2
  Tiempo: O(n · m)
  Espacio: O(n · m)

![Accepted — Edit Distance](evidencias/edit-distance-accepted.png)


[Network Delay Time](https://leetcode.com/problems/network-delay-time/description/)

Autor: Efrén Cuadrado (efrencuadrado289348)
Familia: Grafos (Dijkstra)
Idea: Utilizamos Dijkstra para encontrar el camino de menor tiempo acumulado desde el nodo inicial k hasta todos los demás nodos. En cada iteración seleccionamos mediante un Min-Heap el nodo que tenga la menor distancia tentativa y actualizamos las distancias de sus vecinos si encontramos un camino más corto.
Complejidad:
  n= cantidad de vértices o nodos.
  m= cantidad de aristas o conexiones (times.length).
  Tiempo: O((n + m) log n)
  Espacio: O(n + m)

![Accepted — Network Delay Time](evidencias/network-delay-time-accepted.png)


[Min Cost to Connect All Points](https://leetcode.com/problems/min-cost-to-connect-all-points/description/)

Autor: Efrén Cuadrado (efrencuadrado289348)
Familia: Grafos (Árbol de expansión mínima - Prim)
Idea: Comenzamos desde un punto cualquiera y, con el algoritmo de Prim, en cada paso agregamos al árbol el punto no visitado más cercano según la distancia Manhattan. Mantenemos para cada punto la distancia mínima hacia el árbol actual y la actualizamos cada vez que se agrega un nuevo punto, hasta conectar todos.
Complejidad:
  n= cantidad de puntos
  Tiempo: O(n²)
  Espacio: O(n)

![Accepted — Min Cost to Connect All Points](evidencias/min-cost-connect-points-accepted.png)