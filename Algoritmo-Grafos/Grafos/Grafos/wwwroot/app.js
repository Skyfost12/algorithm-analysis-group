// ============================================================
// RutaOptima — frontend
// ============================================================

const ORIGEN_API = ""; //  la página se sirve desde el propio ASP.NET

const COLOR_ASTAR = "#47E6B1";
const COLOR_DIJKSTRA = "#FF8A3D";
const COLOR_ARISTA = "#2A313C";
const COLOR_NODO_BASE = "#4A5361";
const COLOR_EXPLORADO = "#E6EDF3";

let mapa;
let grafoActual = null;
let marcadoresNodo = new Map(); // id -> L.CircleMarker
let capasDinamicas = []; // todo lo que se dibuja y se debe limpiar entre corridas

let nodoOrigen = null;
let nodoDestino = null;
let algoritmoSeleccionado = "astar";

// ---------- Inicialización ----------

document.addEventListener("DOMContentLoaded", async () => {
  inicializarMapa();
  await cargarGrafo();
  vincularControles();
});

function inicializarMapa() {
  mapa = L.map("mapa", {
    zoomControl: true,
    attributionControl: true,
    preferCanvas: true, // clave para rendir bien con cientos de nodos/aristas
  }).setView([6.213, -75.564], 15);

  // Tiles estándar de OpenStreetMap
  L.tileLayer("https://tile.openstreetmap.org/{z}/{x}/{y}.png", {
    attribution: '&copy; OpenStreetMap contributors',
    maxZoom: 19,
  }).addTo(mapa);
}

async function cargarGrafo() {
  const resp = await fetch(`${ORIGEN_API}/api/ruta/grafo`);
  grafoActual = await resp.json();

  const bounds = [];

  // Dibujar aristas primero (para que queden debajo de los nodos)
  const nodosPorId = new Map(grafoActual.nodos.map((n) => [n.id, n]));
  const dibujadas = new Set();

  grafoActual.aristas.forEach((a) => {
    const clave = [a.origen, a.destino].sort().join("|");
    if (dibujadas.has(clave)) return; // evita dibujar doble las no dirigidas
    dibujadas.add(clave);

    const o = nodosPorId.get(a.origen);
    const d = nodosPorId.get(a.destino);
    if (!o || !d) return;

    L.polyline(
      [
        [o.lat, o.lng],
        [d.lat, d.lng],
      ],
      { color: COLOR_ARISTA, weight: 1.5, opacity: 0.9 }
    ).addTo(mapa);
  });

  // Dibujar nodos
  grafoActual.nodos.forEach((n) => {
    const marcador = L.circleMarker([n.lat, n.lng], {
      radius: 3.5,
      color: COLOR_NODO_BASE,
      fillColor: COLOR_NODO_BASE,
      fillOpacity: 1,
      weight: 1,
    }).addTo(mapa);

    marcador.bindTooltip(n.nombre, { direction: "top", offset: [0, -6] });
    marcador.on("click", () => manejarClicNodo(n.id));

    marcadoresNodo.set(n.id, marcador);
    bounds.push([n.lat, n.lng]);
  });

  mapa.fitBounds(bounds, { padding: [40, 40] });
}

// ---------- Selección de nodos ----------

function manejarClicNodo(id) {
  if (nodoOrigen === null) {
    nodoOrigen = id;
  } else if (nodoDestino === null && id !== nodoOrigen) {
    nodoDestino = id;
  } else {
    // reinicia la selección empezando de nuevo en el nodo clicado
    limpiarSeleccion();
    nodoOrigen = id;
  }
  actualizarEstiloSeleccion();
  actualizarPanelSeleccion();
}

function actualizarEstiloSeleccion() {
  marcadoresNodo.forEach((marcador, id) => {
    if (id === nodoOrigen) {
      marcador.setStyle({ color: COLOR_ASTAR, fillColor: COLOR_ASTAR, radius: 7, weight: 2 });
    } else if (id === nodoDestino) {
      marcador.setStyle({ color: COLOR_DIJKSTRA, fillColor: COLOR_DIJKSTRA, radius: 7, weight: 2 });
    } else {
      marcador.setStyle({ color: COLOR_NODO_BASE, fillColor: COLOR_NODO_BASE, radius: 5, weight: 1 });
    }
  });
}

function actualizarPanelSeleccion() {
  const elOrigen = document.getElementById("valOrigen");
  const elDestino = document.getElementById("valDestino");
  const instruccion = document.getElementById("instruccion");
  const btnCalcular = document.getElementById("btnCalcular");
  const btnComparar = document.getElementById("btnComparar");

  elOrigen.textContent = nodoOrigen ?? "—";
  elDestino.textContent = nodoDestino ?? "—";

  if (nodoOrigen === null) {
    instruccion.innerHTML = "Haz clic en un nodo del mapa para fijar el <strong>origen</strong>";
  } else if (nodoDestino === null) {
    instruccion.innerHTML = "Ahora haz clic en el nodo <strong>destino</strong>";
  } else {
    instruccion.innerHTML = "Listo — elige un algoritmo y calcula la ruta";
  }

  const listo = nodoOrigen !== null && nodoDestino !== null;
  btnCalcular.disabled = !listo;
  btnComparar.disabled = !listo;
}

function limpiarSeleccion() {
  nodoOrigen = null;
  nodoDestino = null;
  limpiarCapasDinamicas();
  actualizarEstiloSeleccion();
  actualizarPanelSeleccion();
  document.getElementById("statsUnico").classList.remove("oculto");
  document.getElementById("statsComparativa").classList.add("oculto");
  ["statDistancia", "statNodos", "statTiempo"].forEach((id) => (document.getElementById(id).textContent = "0"));
}

function limpiarCapasDinamicas() {
  capasDinamicas.forEach((capa) => mapa.removeLayer(capa));
  capasDinamicas = [];
}

// ---------- Controles ----------

function vincularControles() {
  document.getElementById("btnLimpiar").addEventListener("click", limpiarSeleccion);

  document.querySelectorAll(".segmentado__opcion").forEach((boton) => {
    boton.addEventListener("click", () => {
      document.querySelectorAll(".segmentado__opcion").forEach((b) => b.classList.remove("es-activa"));
      boton.classList.add("es-activa");
      algoritmoSeleccionado = boton.dataset.algo;
    });
  });

  document.getElementById("btnCalcular").addEventListener("click", calcularRutaUnica);
  document.getElementById("btnComparar").addEventListener("click", compararAlgoritmos);
}

// ---------- Cálculo de ruta única ----------

async function calcularRutaUnica() {
  if (nodoOrigen === null || nodoDestino === null) return;

  limpiarCapasDinamicas();
  bloquearBotones(true);

  const resp = await fetch(
    `${ORIGEN_API}/api/ruta?origen=${nodoOrigen}&destino=${nodoDestino}&algoritmo=${algoritmoSeleccionado}`
  );
  const resultado = await resp.json();

  if (!resultado.encontrada) {
    alert("No existe una ruta entre esos dos nodos.");
    bloquearBotones(false);
    return;
  }

  document.getElementById("statsUnico").classList.remove("oculto");
  document.getElementById("statsComparativa").classList.add("oculto");

  const color = algoritmoSeleccionado === "astar" ? COLOR_ASTAR : COLOR_DIJKSTRA;

  await animarExploracion(resultado.ordenExploracion, color);
  dibujarRuta(resultado.ruta, color, 5);

  document.getElementById("statDistancia").textContent = Math.round(resultado.distanciaTotal);
  document.getElementById("statNodos").textContent = resultado.nodosExplorados;
  document.getElementById("statTiempo").textContent = resultado.tiempoCalculoMs;

  bloquearBotones(false);
}

// ---------- Dijkstra vs A* ----------

async function compararAlgoritmos() {
  if (nodoOrigen === null || nodoDestino === null) return;

  limpiarCapasDinamicas();
  bloquearBotones(true);

  const resp = await fetch(`${ORIGEN_API}/api/ruta/comparar?origen=${nodoOrigen}&destino=${nodoDestino}`);
  const { dijkstra, astar } = await resp.json();

  document.getElementById("statsUnico").classList.add("oculto");
  document.getElementById("statsComparativa").classList.remove("oculto");

  // Anima ambos entrelazados: un paso de A*, un paso de Dijkstra, etc.
  await animarExploracionParalela(astar.ordenExploracion, dijkstra.ordenExploracion);

  dibujarRuta(astar.ruta, COLOR_ASTAR, 5);
  dibujarRuta(dijkstra.ruta, COLOR_DIJKSTRA, 3, "6 6"); // Dijkstra punteada para distinguir si se solapan

  document.getElementById("cmpDistanciaAstar").textContent = Math.round(astar.distanciaTotal);
  document.getElementById("cmpDistanciaDijkstra").textContent = Math.round(dijkstra.distanciaTotal);
  document.getElementById("cmpNodosAstar").textContent = astar.nodosExplorados;
  document.getElementById("cmpNodosDijkstra").textContent = dijkstra.nodosExplorados;
  document.getElementById("cmpTiempoAstar").textContent = astar.tiempoCalculoMs;
  document.getElementById("cmpTiempoDijkstra").textContent = dijkstra.tiempoCalculoMs;

  bloquearBotones(false);
}

// ---------- Animación ----------

function animarExploracion(ordenIds, color) {
  return new Promise((resolve) => {
    const retardoPorPaso = Math.max(3, Math.min(50, 2500 / ordenIds.length));

    ordenIds.forEach((id, indice) => {
      setTimeout(() => {
        const marcador = marcadoresNodo.get(id);
        if (marcador && id !== nodoOrigen && id !== nodoDestino) {
          marcador.setStyle({ color, fillColor: COLOR_EXPLORADO, radius: 6, weight: 2, fillOpacity: 0.85 });
        }
        if (indice === ordenIds.length - 1) resolve();
      }, indice * retardoPorPaso);
    });

    if (ordenIds.length === 0) resolve();
  });
}

function animarExploracionParalela(ordenAstar, ordenDijkstra) {
  return new Promise((resolve) => {
    const totalPasos = Math.max(ordenAstar.length, ordenDijkstra.length);
    const retardoPorPaso = Math.max(3, Math.min(50, 2800 / totalPasos));

    for (let i = 0; i < totalPasos; i++) {
      setTimeout(() => {
        if (i < ordenAstar.length) {
          const id = ordenAstar[i];
          const m = marcadoresNodo.get(id);
          if (m && id !== nodoOrigen && id !== nodoDestino) {
            m.setStyle({ color: COLOR_ASTAR, fillColor: COLOR_ASTAR, radius: 6, weight: 2, fillOpacity: 0.55 });
          }
        }
        if (i < ordenDijkstra.length) {
          const id = ordenDijkstra[i];
          const m = marcadoresNodo.get(id);
          // si ya lo pintó A*, deja ver ambos con un borde ámbar
          if (m && id !== nodoOrigen && id !== nodoDestino) {
            m.setStyle({ color: COLOR_DIJKSTRA, weight: 2 });
          }
        }
        if (i === totalPasos - 1) resolve();
      }, i * retardoPorPaso);
    }

    if (totalPasos === 0) resolve();
  });
}

function dibujarRuta(nodosRuta, color, grosor, patronGuiones) {
  const latlngs = nodosRuta.map((n) => [n.lat, n.lng]);

  const linea = L.polyline(latlngs, {
    color,
    weight: grosor,
    opacity: 0.95,
    dashArray: patronGuiones || null,
    lineCap: "round",
  }).addTo(mapa);

  capasDinamicas.push(linea);

  // resalta origen/destino por encima de todo
  [nodosRuta[0], nodosRuta[nodosRuta.length - 1]].forEach((n) => {
    const marcador = marcadoresNodo.get(n.id);
    if (marcador) marcador.bringToFront();
  });
}

function bloquearBotones(bloquear) {
  document.getElementById("btnCalcular").disabled = bloquear || nodoOrigen === null || nodoDestino === null;
  document.getElementById("btnComparar").disabled = bloquear || nodoOrigen === null || nodoDestino === null;
}
