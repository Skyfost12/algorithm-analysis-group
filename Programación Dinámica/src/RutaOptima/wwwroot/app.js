const ORIGEN_API = "";

const COLOR_ARISTA = "#2A313C";
const COLOR_NODO_BASE = "#4A5361";

let mapa;
let grafoActual = null;

// ---------- Inicialización ----------

document.addEventListener("DOMContentLoaded", async () => {
    inicializarMapa();
    await cargarGrafo();
});

function inicializarMapa() {
    mapa = L.map("mapa", {
        zoomControl: true,
        attributionControl: true,
        preferCanvas: true,
    }).setView([6.213, -75.564], 15);

    L.tileLayer("https://tile.openstreetmap.org/{z}/{x}/{y}.png", {
        attribution: '&copy; OpenStreetMap contributors',
        maxZoom: 19,
    }).addTo(mapa);
}

async function cargarGrafo() {
    const resp = await fetch(`${ORIGEN_API}/api/grafo`);
    grafoActual = await resp.json();

    const bounds = [];

    // Dibujar aristas primero (para que queden debajo de los nodos)
    const nodosPorId = new Map(grafoActual.nodos.map((n) => [n.id, n]));
    const dibujadas = new Set();

    grafoActual.aristas.forEach((a) => {
        const clave = [a.origen, a.destino].sort().join("|");
        if (dibujadas.has(clave)) return;
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
        bounds.push([n.lat, n.lng]);
    });

    mapa.fitBounds(bounds, { padding: [40, 40] });
}
