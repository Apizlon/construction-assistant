using System.Text.Json;
using ProjectCalculationService.Application.Contracts.Optimizations;

namespace ProjectCalculationService.Application.Services.Optimizations;

internal static class GridPathfinding
{
    internal sealed record VariantResult(
        bool IsFound,
        string? Message,
        IReadOnlyList<GridPointDto> Path,
        double LengthUnits,
        int Turns,
        double WeightedCost);

    public static bool IsBlocked(OptimizationTemplateResponse template, int x, int y)
    {
        if (x < 0 || y < 0 || x >= template.Width || y >= template.Height) return true;
        foreach (var w in template.Walls)
        {
            if (InRect(x, y, w)) return true;
        }

        foreach (var z in template.ForbiddenZones)
        {
            if (InRect(x, y, z)) return true;
        }

        return false;
    }

    public static VariantResult FindWallFriendly(OptimizationTemplateResponse template, GridPointDto start, GridPointDto end)
    {
        if (IsBlocked(template, start.X, start.Y))
            return new VariantResult(false, "Стартовая точка находится в стене/запрещённой зоне.", Array.Empty<GridPointDto>(), 0, 0, 0);
        if (IsBlocked(template, end.X, end.Y))
            return new VariantResult(false, "Конечная точка находится в стене/запрещённой зоне.", Array.Empty<GridPointDto>(), 0, 0, 0);

        // Dijkstra on (x,y,dir) with wall-preference + turn penalty.
        // This is mathematically optimal for the defined cost function.
        // dir: 0..3 = (E,S,W,N), 4 = none (start)
        var width = template.Width;
        var height = template.Height;
        const int dirNone = 4;
        const double stepCost = 1.0;
        const double turnPenalty = 0.35;
        const double awayFromWallPenalty = 0.25;

        var dist = new double[width, height, 5];
        var prev = new (int px, int py, int pdir)[width, height, 5];
        var hasPrev = new bool[width, height, 5];

        for (var x = 0; x < width; x++)
        for (var y = 0; y < height; y++)
        for (var d = 0; d < 5; d++)
            dist[x, y, d] = double.PositiveInfinity;

        var pq = new PriorityQueue<(int x, int y, int dir), double>();
        dist[start.X, start.Y, dirNone] = 0;
        pq.Enqueue((start.X, start.Y, dirNone), 0);

        var dirs = new (int dx, int dy, int dir)[]
        {
            (1, 0, 0),
            (0, 1, 1),
            (-1, 0, 2),
            (0, -1, 3)
        };

        var visited = new bool[width, height, 5];
        while (pq.TryDequeue(out var node, out var nodeDist))
        {
            if (visited[node.x, node.y, node.dir]) continue;
            visited[node.x, node.y, node.dir] = true;
            if (nodeDist > dist[node.x, node.y, node.dir]) continue;

            foreach (var (dx, dy, ndir) in dirs)
            {
                var nx = node.x + dx;
                var ny = node.y + dy;
                if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                if (IsBlocked(template, nx, ny)) continue;

                var cost = stepCost;

                // prefer "along walls/partitions": penalize cells not adjacent to walls.
                if (!IsAdjacentToWall(template, nx, ny))
                {
                    cost += awayFromWallPenalty;
                }

                // penalize turns for easier maintenance.
                if (node.dir != dirNone && node.dir != ndir)
                {
                    cost += turnPenalty;
                }

                var nd = nodeDist + cost;
                if (nd < dist[nx, ny, ndir])
                {
                    dist[nx, ny, ndir] = nd;
                    prev[nx, ny, ndir] = (node.x, node.y, node.dir);
                    hasPrev[nx, ny, ndir] = true;
                    pq.Enqueue((nx, ny, ndir), nd);
                }
            }
        }

        // choose best dir at end.
        var bestDir = -1;
        var best = double.PositiveInfinity;
        for (var d = 0; d < 5; d++)
        {
            if (dist[end.X, end.Y, d] < best)
            {
                best = dist[end.X, end.Y, d];
                bestDir = d;
            }
        }

        if (double.IsInfinity(best) || bestDir == -1)
        {
            return new VariantResult(false, "Путь не найден (все варианты перекрыты стенами/зонами).", Array.Empty<GridPointDto>(), 0, 0, 0);
        }

        var path = ReconstructPathWithDir(start, end, bestDir, prev, hasPrev);
        var (len, turns) = ComputeMetrics(path);
        return new VariantResult(true, null, path, len, turns, best);
    }

    public static VariantResult FindDirect(OptimizationTemplateResponse template, GridPointDto start, GridPointDto end)
    {
        if (IsBlocked(template, start.X, start.Y))
            return new VariantResult(false, "Стартовая точка находится в стене/запрещённой зоне.", Array.Empty<GridPointDto>(), 0, 0, 0);
        if (IsBlocked(template, end.X, end.Y))
            return new VariantResult(false, "Конечная точка находится в стене/запрещённой зоне.", Array.Empty<GridPointDto>(), 0, 0, 0);

        // A* on (x,y) with 8-neighborhood to get an optimal shortest path under the chosen metric.
        var width = template.Width;
        var height = template.Height;

        var dist = new double[width, height];
        var prev = new (int px, int py)[width, height];
        var hasPrev = new bool[width, height];
        var visited = new bool[width, height];

        for (var x = 0; x < width; x++)
        for (var y = 0; y < height; y++)
            dist[x, y] = double.PositiveInfinity;

        var pq = new PriorityQueue<(int x, int y), double>();
        dist[start.X, start.Y] = 0;
        pq.Enqueue((start.X, start.Y), HeuristicOctile(start.X, start.Y, end.X, end.Y));

        var neighbors = new (int dx, int dy, double cost)[]
        {
            (1, 0, 1.0),
            (0, 1, 1.0),
            (-1, 0, 1.0),
            (0, -1, 1.0),
            (1, 1, Math.Sqrt(2)),
            (-1, 1, Math.Sqrt(2)),
            (-1, -1, Math.Sqrt(2)),
            (1, -1, Math.Sqrt(2))
        };

        while (pq.TryDequeue(out var node, out _))
        {
            if (visited[node.x, node.y]) continue;
            visited[node.x, node.y] = true;

            if (node.x == end.X && node.y == end.Y)
            {
                break;
            }

            var baseDist = dist[node.x, node.y];
            foreach (var (dx, dy, step) in neighbors)
            {
                var nx = node.x + dx;
                var ny = node.y + dy;
                if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                if (IsBlocked(template, nx, ny)) continue;

                // prevent "corner cutting": if moving diagonally, both adjacent orth cells must be free.
                if (dx != 0 && dy != 0)
                {
                    if (IsBlocked(template, node.x + dx, node.y) || IsBlocked(template, node.x, node.y + dy)) continue;
                }

                var nd = baseDist + step;
                if (nd < dist[nx, ny])
                {
                    dist[nx, ny] = nd;
                    prev[nx, ny] = (node.x, node.y);
                    hasPrev[nx, ny] = true;
                    var priority = nd + HeuristicOctile(nx, ny, end.X, end.Y);
                    pq.Enqueue((nx, ny), priority);
                }
            }
        }

        if (double.IsInfinity(dist[end.X, end.Y]))
        {
            return new VariantResult(false, "Путь не найден (все варианты перекрыты стенами/зонами).", Array.Empty<GridPointDto>(), 0, 0, 0);
        }

        var path = ReconstructPath(start, end, prev, hasPrev);
        var (len, turns) = ComputeMetrics(path);
        return new VariantResult(true, null, path, len, turns, dist[end.X, end.Y]);
    }

    public static string ToResultJson(ProjectOptimizationPreviewResponse preview)
    {
        return JsonSerializer.Serialize(preview, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    private static bool InRect(int x, int y, RectDto r)
    {
        return x >= r.X && y >= r.Y && x < r.X + r.W && y < r.Y + r.H;
    }

    private static bool IsAdjacentToWall(OptimizationTemplateResponse template, int x, int y)
    {
        // Adjacent to a wall/partition (not forbidden zone).
        static bool IsWall(OptimizationTemplateResponse t, int cx, int cy)
        {
            if (cx < 0 || cy < 0 || cx >= t.Width || cy >= t.Height) return true;
            foreach (var w in t.Walls)
            {
                if (InRect(cx, cy, w)) return true;
            }
            return false;
        }

        return IsWall(template, x + 1, y) || IsWall(template, x - 1, y) || IsWall(template, x, y + 1) || IsWall(template, x, y - 1);
    }

    private static double HeuristicOctile(int x, int y, int tx, int ty)
    {
        var dx = Math.Abs(tx - x);
        var dy = Math.Abs(ty - y);
        var f = Math.Sqrt(2) - 1;
        return (dx < dy) ? f * dx + dy : f * dy + dx;
    }

    private static IReadOnlyList<GridPointDto> ReconstructPath(GridPointDto start, GridPointDto end, (int px, int py)[,] prev, bool[,] hasPrev)
    {
        var stack = new Stack<GridPointDto>();
        var x = end.X;
        var y = end.Y;
        stack.Push(new GridPointDto(x, y));
        while (!(x == start.X && y == start.Y))
        {
            if (!hasPrev[x, y]) break;
            var p = prev[x, y];
            x = p.px;
            y = p.py;
            stack.Push(new GridPointDto(x, y));
        }

        return stack.ToList();
    }

    private static IReadOnlyList<GridPointDto> ReconstructPathWithDir(
        GridPointDto start,
        GridPointDto end,
        int endDir,
        (int px, int py, int pdir)[,,] prev,
        bool[,,] hasPrev)
    {
        var stack = new Stack<GridPointDto>();
        var x = end.X;
        var y = end.Y;
        var d = endDir;
        stack.Push(new GridPointDto(x, y));
        while (!(x == start.X && y == start.Y))
        {
            if (!hasPrev[x, y, d]) break;
            var p = prev[x, y, d];
            x = p.px;
            y = p.py;
            d = p.pdir;
            stack.Push(new GridPointDto(x, y));
        }

        return stack.ToList();
    }

    private static (double lengthUnits, int turns) ComputeMetrics(IReadOnlyList<GridPointDto> path)
    {
        if (path.Count <= 1) return (0, 0);

        double length = 0;
        var turns = 0;
        var prevDx = 0;
        var prevDy = 0;

        for (var i = 1; i < path.Count; i++)
        {
            var dx = path[i].X - path[i - 1].X;
            var dy = path[i].Y - path[i - 1].Y;
            length += (dx != 0 && dy != 0) ? Math.Sqrt(2) : 1.0;

            if (i >= 2 && (dx != prevDx || dy != prevDy))
            {
                turns++;
            }

            prevDx = dx;
            prevDy = dy;
        }

        return (length, turns);
    }
}

