// Estado en memoria de las actividades pendientes (aún no calculadas)
let pending = [];
let idCounter = 1;

const form = document.getElementById("activity-form");
const pendingList = document.getElementById("pending-list");
const pendingCount = document.getElementById("pending-count");
const runBtn = document.getElementById("run-btn");
const demoBtn = document.getElementById("demo-btn");

function timeToMinutes(hhmm) {
  const [h, m] = hhmm.split(":").map(Number);
  return h * 60 + m;
}

function minutesToTime(mins) {
  const h = String(Math.floor(mins / 60)).padStart(2, "0");
  const m = String(mins % 60).padStart(2, "0");
  return `${h}:${m}`;
}

function renderPending() {
  pendingList.innerHTML = "";
  pending.forEach((act) => {
    const li = document.createElement("li");
    li.innerHTML = `
      <span>${act.name} — ${act.room} (${minutesToTime(act.start)}–${minutesToTime(act.end)})</span>
      <button class="remove" data-id="${act.id}" type="button" aria-label="Eliminar">quitar</button>
    `;
    pendingList.appendChild(li);
  });
  pendingCount.textContent = pending.length;
  runBtn.disabled = pending.length === 0;
}

form.addEventListener("submit", (e) => {
  e.preventDefault();
  const name = document.getElementById("name").value.trim();
  const room = document.getElementById("room").value.trim();
  const start = timeToMinutes(document.getElementById("start").value);
  const end = timeToMinutes(document.getElementById("end").value);

  if (end <= start) {
    alert("La hora de fin debe ser posterior a la hora de inicio.");
    return;
  }

  pending.push({ id: `a${idCounter++}`, name, room, start, end });
  renderPending();
  form.reset();
  document.getElementById("name").focus();
});

// Delegación de eventos: los botones "quitar" se generan dinámicamente
pendingList.addEventListener("click", (e) => {
  if (e.target.matches(".remove")) {
    const id = e.target.dataset.id;
    pending = pending.filter((a) => a.id !== id);
    renderPending();
  }
});

// Carga rápida de actividades de ejemplo para probar el algoritmo
demoBtn.addEventListener("click", () => {
  const demo = [
    { name: "Cálculo II", room: "Salón 301", start: "07:00", end: "09:00" },
    { name: "Álgebra Lineal", room: "Salón 301", start: "08:30", end: "10:00" },
    { name: "Física I", room: "Salón 301", start: "09:00", end: "11:00" },
    { name: "Programación", room: "Salón 301", start: "11:00", end: "12:30" },
    { name: "Bases de Datos", room: "Salón 105", start: "07:00", end: "08:30" },
    { name: "Redes", room: "Salón 105", start: "08:00", end: "10:00" },
    { name: "Ingeniería de Software", room: "Salón 105", start: "10:00", end: "12:00" },
  ];
  pending = demo.map((a) => ({
    id: `a${idCounter++}`,
    name: a.name,
    room: a.room,
    start: timeToMinutes(a.start),
    end: timeToMinutes(a.end),
  }));
  renderPending();
});

runBtn.addEventListener("click", async () => {
  runBtn.disabled = true;
  runBtn.textContent = "Calculando…";

  try {
    const res = await fetch("/api/schedule", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ activities: pending }),
    });

    if (!res.ok) {
      const err = await res.json();
      throw new Error(err.error || "Error del servidor");
    }

    const result = await res.json();
    renderBoard(result.selected);
    renderResults(result.selected, result.rejected);
    await loadStats();
  } catch (err) {
    alert("No se pudo calcular la asignación: " + err.message);
  } finally {
    runBtn.disabled = false;
    runBtn.textContent = "Calcular asignación óptima";
  }
});

// Arma el tablero agrupando las actividades asignadas por salón
function renderBoard(selected) {
  const grid = document.getElementById("board-grid");
  grid.innerHTML = "";

  if (selected.length === 0) {
    grid.innerHTML = `<p class="empty-state">No hay actividades asignadas.</p>`;
    return;
  }

  const byRoom = {};
  selected.forEach((a) => {
    if (!byRoom[a.room]) byRoom[a.room] = [];
    byRoom[a.room].push(a);
  });

  Object.keys(byRoom)
    .sort()
    .forEach((room) => {
      const col = document.createElement("div");
      col.className = "room-column";
      col.innerHTML = `<div class="room-column__head">${room}</div>`;

      byRoom[room]
        .sort((a, b) => a.start - b.start)
        .forEach((act) => {
          const block = document.createElement("div");
          block.className = "room-block";
          block.innerHTML = `
            <div class="room-block__time">${minutesToTime(act.start)}–${minutesToTime(act.end)}</div>
            <div class="room-block__name">${act.name}</div>
          `;
          col.appendChild(block);
        });

      grid.appendChild(col);
    });
}

function renderResults(selected, rejected) {
  const selList = document.getElementById("selected-list");
  const rejList = document.getElementById("rejected-list");

  document.getElementById("selected-count").textContent = selected.length;
  document.getElementById("rejected-count").textContent = rejected.length;

  selList.innerHTML = selected
    .map(
      (a) => `<li>${a.name}<span class="meta">${a.room} · ${minutesToTime(a.start)}–${minutesToTime(a.end)}</span></li>`
    )
    .join("") || `<li style="border-left-color: var(--line); background: transparent;">—</li>`;

  rejList.innerHTML = rejected
    .map(
        (a) => `<li>${a.name}<span class="meta">${a.room}· ${minutesToTime(a.start)}–${minutesToTime(a.end)} · ${a.reason}</span></li>`
    )
    .join("") || `<li style="border-left-color: var(--line); background: transparent;">—</li>`;
}

async function loadStats() {
  const res = await fetch("/api/history");
  const history = await res.json();
  if (history.length === 0) return;

  const last = history[history.length - 1];
  const bar = document.getElementById("stats-bar");
  const text = document.getElementById("stats-text");
  text.textContent = `Última corrida: ${last.totalAsignadas}/${last.totalSolicitadas} actividades asignadas · ${history.length} corrida(s) en el historial`;
  bar.hidden = false;
}

loadStats();