"""
Genera Data/grafo_barrio.json a partir de datos REALES de calles de
El Poblado, Medellín, usando OpenStreetMap + OSMnx.

Por qué la API oficial de OSM y no Overpass API directamente:
los mirrors públicos de Overpass API suelen estar sobrecargados o
caídos (es un problema conocido y documentado en la comunidad OSM).
La API oficial (/api/0.6/map) es más estable para áreas pequeñas
(< 0.25 grados cuadrados) y da exactamente los mismos datos fuente.

Requisitos:
    pip install osmnx networkx

Uso:
    python scripts/generar_grafo.py
"""

import json
import urllib.request
import networkx as nx
import osmnx as ox

# ---- Configuración del área a descargar ----
CENTRO = (6.2087, -75.5673)  # El Poblado, Medellín
RADIO_METROS = 900
BBOX = (-75.57544, 6.20061, -75.55916, 6.21679)  # calculado a partir del centro+radio

# Tipos de vía que se excluyen por no ser aptas para un vehículo
TIPOS_EXCLUIDOS = {
    "footway", "steps", "pedestrian", "path", "cycleway", "bridleway",
    "corridor", "elevator", "escalator", "proposed", "construction",
    "abandoned", "platform", "raceway", "service", "track",
}


def descargar_xml_osm(bbox, destino="medellin.osm"):
    """Descarga los datos crudos de OSM para el bbox usando la API oficial."""
    minlon, minlat, maxlon, maxlat = bbox
    url = (
        f"https://www.openstreetmap.org/api/0.6/map"
        f"?bbox={minlon},{minlat},{maxlon},{maxlat}"
    )
    print(f"Descargando datos de OSM: {url}")
    urllib.request.urlretrieve(url, destino)
    return destino


def construir_grafo_filtrado(ruta_xml):
    """Parsea el XML con OSMnx y filtra solo vías aptas para vehículos."""
    G = ox.graph_from_xml(ruta_xml, simplify=True, retain_all=False)
    print(f"Grafo crudo: {G.number_of_nodes()} nodos, {G.number_of_edges()} aristas")

    G_simple = nx.Graph()
    for u, v, data in G.edges(data=True):
        highway = data.get("highway", "")
        if isinstance(highway, list):
            highway = highway[0] if highway else ""
        if highway in TIPOS_EXCLUIDOS or not highway:
            continue

        peso = float(data.get("length", 1.0))
        nombre = data.get("name", "")
        if isinstance(nombre, list):
            nombre = nombre[0] if nombre else ""

        if G_simple.has_edge(u, v):
            if peso < G_simple[u][v]["peso"]:
                G_simple[u][v]["peso"] = peso
        else:
            G_simple.add_edge(u, v, peso=peso, nombre=nombre or "")

    # Solo el componente conectado más grande: garantiza que Dijkstra/A*
    # siempre encuentren una ruta entre dos nodos cualesquiera.
    componente_mayor = max(nx.connected_components(G_simple), key=len)
    G_final = G_simple.subgraph(componente_mayor).copy()

    print(
        f"Grafo final (filtrado y conectado): "
        f"{G_final.number_of_nodes()} nodos, {G_final.number_of_edges()} aristas"
    )
    return G, G_final


def exportar_json(G, G_final, ruta_salida):
    """Convierte el grafo de NetworkX al formato JSON que espera GrafoService.cs."""
    nodos_json = []
    mapa_id = {}

    for i, osmid in enumerate(G_final.nodes()):
        nuevo_id = f"N{i}"
        mapa_id[osmid] = nuevo_id

        nombres_incidentes = sorted({
            G_final[osmid][vecino].get("nombre", "")
            for vecino in G_final.neighbors(osmid)
            if G_final[osmid][vecino].get("nombre", "")
        })[:2]
        nombre_nodo = " x ".join(nombres_incidentes) or f"Interseccion {i}"

        datos_nodo = G.nodes[osmid]
        nodos_json.append({
            "id": nuevo_id,
            "nombre": nombre_nodo,
            "lat": round(datos_nodo["y"], 6),
            "lng": round(datos_nodo["x"], 6),
        })

    aristas_json = [
        {
            "origen": mapa_id[u],
            "destino": mapa_id[v],
            "peso": round(data["peso"], 1),
            "dirigida": False,
        }
        for u, v, data in G_final.edges(data=True)
    ]

    with open(ruta_salida, "w", encoding="utf-8") as f:
        json.dump({"nodos": nodos_json, "aristas": aristas_json}, f, ensure_ascii=False, indent=2)

    print(f"Guardado en {ruta_salida}: {len(nodos_json)} nodos, {len(aristas_json)} aristas")


if __name__ == "__main__":
    ruta_xml = descargar_xml_osm(BBOX)
    G, G_final = construir_grafo_filtrado(ruta_xml)
    exportar_json(G, G_final, "../Data/grafo_barrio.json")
