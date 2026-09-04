using TurnosSalas.Models;

namespace TurnosSalas.Algorithms;

public static class GreedyScheduler
{
    public static ScheduleResult SelectActivities(List<ActivityRequest> activities)
    {
        var selected = new List<ScheduledActivity>();
        var rejected = new List<RejectedActivity>();

        if (activities is null || activities.Count == 0)
        {
            return new ScheduleResult(selected, rejected);
        }

        // Se procesa salón por salón: actividades de salones distintos no compiten
        var byRoom = activities.GroupBy(a => string.IsNullOrWhiteSpace(a.Room) ? "General" : a.Room);

        foreach (var group in byRoom)
        {
            var room = group.Key;

            // Ordenar por fin ascendente es lo que hace funcionar el greedy
            var sorted = group.OrderBy(a => a.End).ToList();

            int lastEnd = int.MinValue;

            foreach (var act in sorted)
            {
                if (act.Start >= lastEnd)
                {
                    selected.Add(new ScheduledActivity(act.Id, act.Name, room, act.Start, act.End));
                    lastEnd = act.End;
                }
                else
                {
                    // Se solapa con la última aceptada, queda afuera
                    rejected.Add(new RejectedActivity(
                        act.Id, act.Name, room, act.Start, act.End,
                        "Se solapa con otra actividad ya asignada"));
                }
            }
        }

        // Salida ordenada por salón y luego por hora de inicio
        selected = selected
            .OrderBy(a => a.Room, StringComparer.Ordinal)
            .ThenBy(a => a.Start)
            .ToList();

        return new ScheduleResult(selected, rejected);
    }
}