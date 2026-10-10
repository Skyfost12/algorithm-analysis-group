const MAX_ENTREGAS = 17;
const MAX_AJUSTE_METROS = 300;
const COLOR_OPTIMA = "#47E6B1";
const COLOR_VECINO = "#FF8A3D";
const COLOR_CELDA = "#5AA9FF";

const fmtEntero = new Intl.NumberFormat("es-CO", { maximumFractionDigits: 0 });
const fmtDecimal = new Intl.NumberFormat("es-CO", { maximumFractionDigits: 1 });

let deposito = null;
let entregas = [];
let resultadoRep = null; 
let calculandoRep = false;
let versionSolicitud = 0;
let tablaAbierta = false;
let celdaActiva = null;
let temporizadorToast = null;
let paradasConProblema = new Set();

let capaMarcadores;
let capaRutas;
let capaCelda;

// ---------- Inicialización ----------

document.addEventListener("DOMContentLoaded", () => {
    capaRutas = L.layerGroup();
    capaCelda = L.layerGroup();
    capaMarcadores = L.layerGroup();

    mapa.on("click", manejarClicMapa);
    document.getElementById("repCalcular").addEventListener("click", calcularRuta);
    document.getElementById("repLimpiar").addEventListener("click", limpiarParadas);
    document.getElementById("repToggleDp").addEventListener("click", () => alternarTabla(!tablaAbierta));

    const tabla = document.getElementById("dpTabla");
    tabla.addEventListener("mouseover", (e) => {
        const celda = e.target.closest("td.celda");
        if (celda) seleccionarCelda(Number(celda.dataset.mascara), Number(celda.dataset.j));
    });
    tabla.addEventListener("click", (e) => {
        const celda = e.target.closest("td.celda");
        if (celda) seleccionarCelda(Number(celda.dataset.mascara), Number(celda.dataset.j));
    });
    tabla.addEventListener("mouseleave", () => seleccionarCelda(null));

    capaRutas.addTo(mapa);
    capaCelda.addTo(mapa);
    capaMarcadores.addTo(mapa);
    renderizar();
    actualizarPanelRep();
});

// ---------- Paradas ----------

function haversineMetros(lat1, lng1, lat2, lng2) {
    const R = 6371000;
    const rad = Math.PI / 180;
    const dLat = (lat2 - lat1) * rad;
    const dLng = (lng2 - lng1) * rad;
    const h = Math.sin(dLat / 2) ** 2 + Math.cos(lat1 * rad) * Math.cos(lat2 * rad) * Math.sin(dLng / 2) ** 2;
    return 2 * R * Math.atan2(Math.sqrt(h), Math.sqrt(1 - h));
}

function nodoMasCercano(lat, lng) {
    let mejor = null;
    let mejorMetros = Infinity;
    for (const n of grafoActual.nodos) {
        const m = haversineMetros(lat, lng, n.lat, n.lng);
        if (m < mejorMetros) {
            mejorMetros = m;
            mejor = n;
        }
    }
    return mejor ? { nodo: mejor, metros: mejorMetros } : null;
}

function manejarClicMapa(e) {
    if (!grafoActual) return;

    const cercano = nodoMasCercano(e.latlng.lat, e.latlng.lng);
    if (!cercano || cercano.metros > MAX_AJUSTE_METROS) {
        const m = cercano ? Math.round(cercano.metros) : null;
        mostrarToast(
            m === null
                ? "Ese punto queda fuera del mapa de calles"
                : `Ese punto queda a ${fmtEntero.format(m)} m de la calle más cercana (máximo ${MAX_AJUSTE_METROS} m): fuera del mapa`
        );
        return;
    }
    alternarParadaEn(cercano.nodo);
}

/** Agrega una parada en ese nodo de calle o, si ya hay una, la quita. */
function alternarParadaEn(nodo) {
    if (deposito && deposito.id === nodo.id) {
        deposito = null;
    } else if (entregas.some((p) => p.id === nodo.id)) {
        entregas = entregas.filter((p) => p.id !== nodo.id);
    } else if (!deposito) {
        deposito = nodo;
    } else if (entregas.length >= MAX_ENTREGAS) {
        mostrarToast(`Máximo ${MAX_ENTREGAS + 1} paradas (depósito + ${MAX_ENTREGAS} entregas)`);
        return;
    } else {
        entregas.push(nodo);
    }
    paradasCambiaron();
}

function limpiarParadas() {
    deposito = null;
    entregas = [];
    paradasCambiaron();
}

function paradasCambiaron() {
    versionSolicitud++;
    paradasConProblema = new Set();
    resultadoRep = null;
    alternarTabla(false);
    ocultarError();
    renderizar();
    actualizarPanelRep();
}

function todasLasParadas() {
    return deposito ? [deposito, ...entregas] : [];
}

// ---------- Panel ----------

function actualizarPanelRep() {
    document.getElementById("repDeposito").textContent = deposito ? deposito.nombre : "—";
    document.getElementById("repEntregas").textContent = `${entregas.length} / ${MAX_ENTREGAS}`;
    document.getElementById("repCalcular").disabled = calculandoRep || !deposito || entregas.length === 0;
    document.getElementById("repCalcular").textContent = calculandoRep ? "Calculando…" : "Calcular ruta óptima";
    document.getElementById("repResultado").classList.toggle("oculto", resultadoRep === null);

    const instruccion = document.getElementById("instruccion");
    if (!deposito) {
        instruccion.innerHTML = "Haz clic en el mapa para fijar el <strong>depósito</strong>";
    } else if (entregas.length === 0) {
        instruccion.innerHTML = "Ahora agrega <strong>entregas</strong> con clic (clic sobre una entrega la quita)";
    } else if (resultadoRep) {
        instruccion.innerHTML = "Ruta calculada — cambia las paradas para recalcular";
    } else {
        instruccion.innerHTML = "Agrega más entregas o pulsa <strong>Calcular ruta óptima</strong>";
    }
}

function mostrarError(texto) {
    const el = document.getElementById("repError");
    el.textContent = texto;
    el.classList.remove("oculto");
    el.classList.add("nota--error");
}

function ocultarError() {
    document.getElementById("repError").classList.add("oculto");
}

function mostrarToast(texto) {
    const el = document.getElementById("toast");
    el.textContent = texto;
    el.classList.remove("oculto");
    clearTimeout(temporizadorToast);
    temporizadorToast = setTimeout(() => el.classList.add("oculto"), 2600);
}

// ---------- Cálculo ----------

async function calcularRuta() {
    if (calculandoRep || !deposito || entregas.length === 0) return;

    const miVersion = ++versionSolicitud;
    calculandoRep = true;
    ocultarError();
    if (paradasConProblema.size > 0) {
        paradasConProblema = new Set();
        renderizarMarcadores(null);
    }
    actualizarPanelRep();

    try {
        const resp = await fetch(`${ORIGEN_API}/api/repartidor/ruta`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ paradas: todasLasParadas().map((p) => ({ lat: p.lat, lng: p.lng })) }),
        });
        const datos = await resp.json().catch(() => null);
        if (miVersion !== versionSolicitud) return; // el usuario cambió las paradas mientras se calculaba

        if (!resp.ok) {
            // La API explica por qué no hay ruta (422): mensaje claro y las paradas implicadas, que se marcan en rojo.
            if (datos?.codigo) {
                paradasConProblema = new Set(datos.paradas ?? []);
                mostrarError(datos.error);
                renderizarMarcadores(null);
                return;
            }
            throw new Error(datos?.error ?? `El servidor respondió ${resp.status}`);
        }

        resultadoRep = datos;
        mostrarResultado();
        renderizar();
    } catch (error) {
        mostrarError(`No se pudo calcular la ruta: ${error.message}`);
    } finally {
        calculandoRep = false;
        actualizarPanelRep();
    }
}

// ---------- Resultado ----------

const SUPERINDICES = { 0: "⁰", 1: "¹", 2: "²", 3: "³", 4: "⁴", 5: "⁵", 6: "⁶", 7: "⁷", 8: "⁸", 9: "⁹" };

/** Enteros grandes legibles: 5 040 · 3,6 × 10¹⁴ */
function grande(x) {
    if (x < 1e9) return fmtEntero.format(x);
    const exp = Math.floor(Math.log10(x));
    const mantisa = x / 10 ** exp;
    return `${fmtDecimal.format(mantisa)} × 10${String(exp).split("").map((c) => SUPERINDICES[c]).join("")}`;
}

function metros(x) {
    return `${fmtEntero.format(x)} m`;
}

function mostrarResultado() {
    const r = resultadoRep;
    const optima = r.optima.distancia;
    const vecino = r.vecinoCercano.distancia;
    const km = optima >= 1000 ? ` · ${fmtDecimal.format(optima / 1000)} km` : "";

    document.getElementById("repDistancia").textContent = fmtEntero.format(optima);
    document.getElementById("repDistanciaUnidad").textContent = `metros por calle${km} (óptimo)`;
    document.getElementById("repOptimaDist").textContent = fmtEntero.format(optima);
    document.getElementById("repVecinoDist").textContent = fmtEntero.format(vecino);

    const extra = document.getElementById("repExtra");
    if (r.porcentajeExtraVecinoCercano >= 0.05) {
        extra.innerHTML =
            `El vecino más cercano recorrería <strong>${fmtDecimal.format(r.porcentajeExtraVecinoCercano)} % más</strong> ` +
            `(${metros(vecino - optima)} de más).`;
    } else {
        extra.textContent = "En este caso el vecino más cercano también encontró el óptimo.";
    }

    const p = r.problema;
    document.getElementById("repEtiquetaFact").textContent = `Rutas posibles (${p.entregas}!)`;
    document.getElementById("repRutas").textContent = grande(p.rutasPosibles);
    document.getElementById("repEstados").textContent = grande(p.estadosDp);
    document.getElementById("repTransiciones").textContent = grande(p.transicionesDp);
    document.getElementById("repRecursion").textContent = grande(p.llamadasRecursionSinMemo);

    document.getElementById("repTiempoMatriz").textContent = `${fmtDecimal.format(r.tiempoMatrizMs)} ms`;
    document.getElementById("repTiempoHk").textContent = `${fmtDecimal.format(r.tiempoHeldKarpMs)} ms`;

    const etiqueta = etiquetaDeParada;
    document.getElementById("repTramos").innerHTML = r.optima.tramos
        .map((t) => `<li><span>${etiqueta(t.desde)} → ${etiqueta(t.hasta)}</span><span>${metros(t.distancia)}</span></li>`)
        .join("");

    const boton = document.getElementById("repToggleDp");
    boton.classList.toggle("oculto", !r.tablaDp);
    boton.textContent = "Ver tabla DP";
    document.getElementById("repNotaDp").classList.toggle("oculto", Boolean(r.tablaDp));
}

/** Marca de una parada en el mapa y en la lista de tramos: orden de visita (D = depósito). */
function etiquetaDeParada(indice) {
    if (indice === 0) return "D";
    if (!resultadoRep) return String(indice);
    return String(resultadoRep.optima.paradas.indexOf(indice));
}

// ---------- Dibujo en el mapa ----------

function renderizar() {
    capaRutas.clearLayers();
    capaCelda.clearLayers();

    if (resultadoRep) {
        // el vecino más cercano va debajo, punteado; la óptima encima
        dibujarRecorrido(resultadoRep.vecinoCercano, COLOR_VECINO, 9, "6 8");
        dibujarRecorrido(resultadoRep.optima, COLOR_OPTIMA, 4);
    }
    renderizarMarcadores(null);
}

function dibujarRecorrido(recorrido, color, grosor, guiones) {
    recorrido.tramos.forEach((tramo) => {
        L.polyline(tramo.geometria, { color, weight: grosor, opacity: 0.95, dashArray: guiones || null, lineCap: "round", interactive: false })
            .addTo(capaRutas);
    });
}

function renderizarMarcadores(camino) {
    capaMarcadores.clearLayers();

    todasLasParadas().forEach((parada, i) => {
        let etiqueta;
        let clases = "parada__chip";
        if (i === 0) {
            etiqueta = "D";
            clases += " parada__chip--deposito";
        } else if (camino) {
            const pos = camino.indexOf(i);
            etiqueta = pos > 0 ? String(pos) : String(i);
            clases += pos > 0 ? " parada__chip--camino" : " parada__chip--tenue";
        } else if (resultadoRep && !tablaAbierta) {
            etiqueta = etiquetaDeParada(i);
        } else {
            etiqueta = String(i);
            if (!resultadoRep) clases += " parada__chip--pendiente";
        }

        const conProblema = paradasConProblema.has(i);
        if (conProblema) clases += " parada__chip--error";

        const marcador = L.marker([parada.lat, parada.lng], {
            icon: L.divIcon({
                className: "parada",
                html: `<div class="${clases}">${etiqueta}</div>`,
                iconSize: [26, 26],
                iconAnchor: [13, 13],
            }),
            keyboard: false,
        });
        const nombre = i === 0 ? "Depósito" : `Entrega ${i}`;
        const aviso = conProblema ? " — sin ruta posible" : "";
        marcador.bindTooltip(`${nombre} — ${parada.nombre}${aviso} (clic para quitar)`, { direction: "top", offset: [0, -12] });
        marcador.on("click", () => alternarParadaEn(parada));
        marcador.addTo(capaMarcadores);
    });
}

// ---------- Tabla DP ----------

function alternarTabla(abrir) {
    const posible = abrir && resultadoRep && resultadoRep.tablaDp;
    const abierta = Boolean(posible);
    if (abierta === tablaAbierta) return;
    tablaAbierta = abierta;

    document.querySelector(".mapa-envoltorio").classList.toggle("mapa-envoltorio--con-tabla", abierta);
    document.getElementById("dpCajon").classList.toggle("oculto", !abierta);
    document.getElementById("repToggleDp").textContent = abierta ? "Ocultar tabla DP" : "Ver tabla DP";

    if (abierta) construirTabla();
    celdaActiva = null;
    capaCelda.clearLayers();
    renderizarMarcadores(null);
    mapa.invalidateSize({ pan: false });
}

function etiquetaSubconjunto(mascara) {
    const items = [];
    for (let b = 0; mascara >> b > 0; b++) if (mascara & (1 << b)) items.push(b + 1);
    return `{${items.join(",")}}`;
}

function construirTabla() {
    const tabla = resultadoRep.tablaDp;
    const m = tabla.entregas;

    let html = '<table class="dp"><thead><tr><th>Subconjunto</th>';
    for (let j = 1; j <= m; j++) html += `<th>${j}</th>`;
    html += "</tr></thead><tbody>";

    for (const fila of tabla.filas) {
        html += `<tr><th scope="row">${fila.etiqueta}</th>`;
        for (let j = 1; j <= m; j++) {
            const celda = fila.celdas.find((c) => c.j === j);
            if (!celda) {
                html += '<td class="na">·</td>';
            } else {
                const opt = celda.enRutaOptima ? " opt" : "";
                html += `<td class="celda${opt}" data-mascara="${fila.mascara}" data-j="${j}">${fmtEntero.format(celda.valor)}</td>`;
            }
        }
        html += "</tr>";
    }
    html += "</tbody></table>";

    document.getElementById("dpTabla").innerHTML = html;
    document.getElementById("dpExplica").innerHTML = explicacionInicial();
}

function explicacionInicial() {
    return `
    <h3>Tabla de Programación Dinámica</h3>
    <p>Cada fila es un subconjunto de entregas ya visitadas; cada columna, la entrega donde termina el camino.
       Los valores son metros por calle. Pasa el cursor por una celda: el mapa dibuja su camino y aquí se
       muestra cómo se calculó.</p>
    <code class="dp-formula">dp[S][j] = min sobre k ∈ S∖{j} de ( dp[S∖{j}][k] + d(k, j) )</code>
    <p>Las celdas con borde verde forman el camino óptimo. Las entregas se numeran en el mapa
       en el orden en que las colocaste, igual que las columnas.</p>`;
}

function buscarCelda(mascara, j) {
    const fila = resultadoRep.tablaDp.filas.find((f) => f.mascara === mascara);
    return fila ? fila.celdas.find((c) => c.j === j) : null;
}

function seleccionarCelda(mascara, j) {
    if (!tablaAbierta) return;

    if (mascara === null) {
        celdaActiva = null;
    } else {
        if (celdaActiva && celdaActiva.mascara === mascara && celdaActiva.j === j) return;
        celdaActiva = { mascara, j };
    }

    const tabla = document.getElementById("dpTabla");
    tabla.querySelectorAll("td.sel, td.pred").forEach((td) => td.classList.remove("sel", "pred"));
    capaCelda.clearLayers();

    if (!celdaActiva) {
        document.getElementById("dpExplica").innerHTML = explicacionInicial();
        renderizarMarcadores(null);
        return;
    }

    const celda = buscarCelda(mascara, j);
    const resto = mascara ^ (1 << (j - 1));

    tabla.querySelector(`td.celda[data-mascara="${mascara}"][data-j="${j}"]`)?.classList.add("sel");
    if (celda.candidatos) {
        tabla.querySelectorAll(`td.celda[data-mascara="${resto}"]`).forEach((td) => td.classList.add("pred"));
    }

    dibujarCaminoDeCelda(celda.camino);
    renderizarMarcadores(celda.camino);
    document.getElementById("dpExplica").innerHTML = explicarCelda(mascara, resto, celda);
}

function dibujarCaminoDeCelda(camino) {
    const pares = resultadoRep.tablaDp.geometriaPares;
    const puntos = [];
    for (let t = 1; t < camino.length; t++) {
        const tramo = pares[camino[t - 1]][camino[t]];
        puntos.push(...(t === 1 ? tramo : tramo.slice(1)));
    }
    L.polyline(puntos, { color: COLOR_CELDA, weight: 6, opacity: 0.95, lineCap: "round", interactive: false }).addTo(capaCelda);
}

function explicarCelda(mascara, resto, celda) {
    const S = etiquetaSubconjunto(mascara);
    let html = `<h3>Cómo se calcula esta celda</h3>
    <p><b>dp[${S}][${celda.j}]</b> es el menor costo de salir del depósito, visitar ${S} y terminar en la entrega ${celda.j}.</p>`;

    if (!celda.candidatos) {
        html += `<p><b>Caso base:</b> con una sola entrega, el camino es ir directo desde el depósito.</p>
      <code class="dp-formula">dp[${S}][${celda.j}] = d(D, ${celda.j}) = ${metros(celda.valor)}</code>`;
    } else {
        const R = etiquetaSubconjunto(resto);
        html += `<p>La entrega anterior a ${celda.j} pudo ser cualquiera de ${R}. Se prueba cada una y gana la más barata
      (en empate, la k más pequeña):</p>
      <table class="dp-cands"><thead><tr><th>k</th><th>dp[${R}][k]</th><th>d(k, ${celda.j})</th><th>Suma</th></tr></thead><tbody>`;
        for (const c of celda.candidatos) {
            html += `<tr class="${c.k === celda.padre ? "ganador" : ""}"><td>${c.k}</td><td>${fmtEntero.format(c.dpResto)}</td>` +
                `<td>${fmtEntero.format(c.distanciaKJ)}</td><td>${fmtEntero.format(c.suma)}</td></tr>`;
        }
        html += `</tbody></table>
      <p>Resultado: <b>dp = ${metros(celda.valor)}</b>, viniendo de la entrega ${celda.padre}.</p>`;
    }

    const recorrido = celda.camino.map((p) => (p === 0 ? "D" : p)).join(" → ");
    html += `<p>Camino: <b>${recorrido}</b></p>`;
    return html;
}
