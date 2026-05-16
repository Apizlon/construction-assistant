using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using ProjectCalculationService.Application.Contracts.Reports;

namespace ProjectCalculationService.Application.Services.Reports;

public static class GanttHtmlExporter
{
    public static byte[] BuildHtml(ProjectGanttResponse gantt)
    {
        var rows = BuildWbsRows(gantt.Tasks);

        var payload = new
        {
            projectId = gantt.ProjectId,
            generatedAt = gantt.GeneratedAt,
            totalDays = Math.Max(0, gantt.TotalDays),
            totalMonths = Math.Max(0, gantt.TotalMonths),
            rows
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        var html = $@"<!doctype html>
<html lang=""ru"">
<head>
  <meta charset=""utf-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
  <title>Диаграмма Ганта</title>
  <style>
    :root {{
      --bg: #0b1220;
      --card: rgba(2,6,23,.72);
      --border: #1f2937;
      --muted: #94a3b8;
      --text: #e2e8f0;
      --text2: #cbd5e1;
      --accent: #6366f1;
      --rowHover: rgba(51,65,85,.35);
    }}
    * {{ box-sizing: border-box; }}
    body {{
      margin: 0;
      font-family: ui-sans-serif, system-ui, -apple-system, Segoe UI, Roboto, Arial, sans-serif;
      background: radial-gradient(1000px 600px at 20% 0%, rgba(99,102,241,.14), transparent 60%), var(--bg);
      color: var(--text);
    }}
    .wrap {{ max-width: 1280px; margin: 0 auto; padding: 20px; }}
    .header {{ display:flex; justify-content:space-between; gap:16px; margin-bottom:14px; }}
    h1 {{ font-size: 20px; margin: 0; letter-spacing: .2px; }}
    .sub {{ color: var(--muted); font-size: 12px; margin-top: 6px; }}
    .card {{
      border: 1px solid var(--border);
      background: var(--card);
      border-radius: 16px;
      padding: 14px;
      box-shadow: 0 14px 40px rgba(0,0,0,.28);
    }}
    .toolbar {{ display:flex; flex-wrap:wrap; gap:10px; align-items:center; justify-content:space-between; margin: 10px 0 12px; }}
    .btn {{ background: rgba(15,23,42,.75); border: 1px solid var(--border); color: var(--text2); border-radius: 12px; padding: 8px 12px; cursor: pointer; font-size: 13px; }}
    .btn:hover {{ border-color: rgba(148,163,184,.55); }}
    .range {{ display:flex; align-items:center; gap:10px; color: var(--muted); font-size: 12px; }}
    input[type=range] {{ width: 220px; }}

    .grid {{ display:grid; grid-template-columns: 460px 1fr; border: 1px solid var(--border); border-radius: 14px; overflow:hidden; }}
    .left {{ background: rgba(2,6,23,.55); border-right: 1px solid var(--border); }}
    .right {{ background: rgba(2,6,23,.35); overflow:auto; }}
    .thead {{
      display:grid; grid-template-columns: 70px 1fr 80px 90px;
      gap:10px; padding:10px 12px; font-size:12px; color: var(--muted);
      border-bottom:1px solid var(--border); position:sticky; top:0; background: rgba(2,6,23,.95); z-index:3;
    }}
    .row {{
      display:grid; grid-template-columns: 70px 1fr 80px 90px;
      gap:10px; padding:9px 12px; border-bottom:1px solid rgba(31,41,55,.7); align-items:center; font-size:13px;
    }}
    .row:hover {{ background: var(--rowHover); }}
    .wbs {{ font-variant-numeric: tabular-nums; color: var(--text2); }}
    .name {{ color: var(--text); overflow:hidden; text-overflow: ellipsis; white-space: nowrap; }}
    .num {{ text-align:right; font-variant-numeric: tabular-nums; color: var(--text2); }}

    .rhead {{ position:sticky; top:0; z-index:2; background: rgba(2,6,23,.95); border-bottom:1px solid var(--border); padding:10px 12px; }}
    .ticks {{ position:relative; height: 18px; }}
    .tick {{ position:absolute; top:0; bottom:0; width:1px; background: rgba(148,163,184,.25); }}
    .tickLabel {{ position:absolute; top:0; transform:translateX(-50%); font-size:11px; color: rgba(148,163,184,.9); padding-top:2px; white-space:nowrap; }}

    .bars {{ position:relative; }}
    .barRow {{ height: 38px; border-bottom:1px solid rgba(31,41,55,.7); position:relative; }}
    .bar {{ position:absolute; top: 9px; height: 20px; border-radius: 10px; background: linear-gradient(90deg, rgba(99,102,241,.95), rgba(99,102,241,.6)); box-shadow: 0 6px 16px rgba(99,102,241,.22); border:1px solid rgba(99,102,241,.45); }}

    .tooltip {{ position:fixed; pointer-events:none; z-index:999; background: rgba(2,6,23,.92); border: 1px solid rgba(148,163,184,.25); border-radius: 12px; padding: 10px 12px; color: var(--text); font-size: 12px; box-shadow: 0 18px 50px rgba(0,0,0,.35); width: 340px; display:none; }}
    .tmuted {{ color: var(--muted); }}
    .kv {{ display:flex; justify-content:space-between; gap:10px; margin-top:6px; }}
  </style>
</head>
<body>
  <div class=""wrap"">
    <div class=""header"">
      <div>
        <h1>Диаграмма Ганта</h1>
        <div class=""sub"">Проект: <span id=""pid""></span> · Сформировано: <span id=""gen""></span></div>
      </div>
      <div class=""sub"">Оценка: <span id=""days""></span> дней · <span id=""months""></span> мес.</div>
    </div>

    <div class=""card"">
      <div class=""toolbar"">
        <div class=""range"">
          <span>Масштаб</span>
          <input id=""zoom"" type=""range"" min=""2"" max=""20"" step=""1"" value=""7"">
          <span><span id=""zv""></span> px/день</span>
        </div>
      </div>

      <div class=""grid"">
        <div class=""left"">
          <div class=""thead"">
            <div>WBS</div>
            <div>Этап</div>
            <div class=""num"">Старт</div>
            <div class=""num"">Длит.</div>
          </div>
          <div id=""wbsBody""></div>
        </div>
        <div class=""right"">
          <div class=""rhead"">
            <div class=""ticks"" id=""ticks""></div>
          </div>
          <div class=""bars"" id=""bars""></div>
        </div>
      </div>
    </div>
  </div>

  <div class=""tooltip"" id=""tip""></div>

  <script>
    const data = {json};
    const pid = document.getElementById('pid');
    const gen = document.getElementById('gen');
    const days = document.getElementById('days');
    const months = document.getElementById('months');
    pid.textContent = data.projectId;
    gen.textContent = new Date(data.generatedAt).toLocaleString();
    days.textContent = data.totalDays;
    months.textContent = data.totalMonths;

    const zoom = document.getElementById('zoom');
    const zv = document.getElementById('zv');
    const wbsBody = document.getElementById('wbsBody');
    const bars = document.getElementById('bars');
    const ticks = document.getElementById('ticks');
    const tip = document.getElementById('tip');

    function escapeHtml(s) {{
      return String(s)
        .replaceAll('&','&amp;')
        .replaceAll('<','&lt;')
        .replaceAll('>','&gt;')
        .replaceAll('""','&quot;')
        .replaceAll(""'"",'&#39;');
    }}
    function majorStep(totalDays) {{
      if (totalDays <= 30) return 5;
      if (totalDays <= 90) return 10;
      if (totalDays <= 180) return 15;
      if (totalDays <= 365) return 30;
      return 60;
    }}

    function render() {{
      const pxPerDay = Number(zoom.value);
      zv.textContent = pxPerDay;
      const totalDays = Math.max(0, Number(data.totalDays || 0));
      const width = Math.max(900, (totalDays + 1) * pxPerDay);

      wbsBody.innerHTML = data.rows.map(r => `
        <div class=""row"">
          <div class=""wbs"">${{escapeHtml(r.wbs)}}</div>
          <div class=""name"" title=""${{escapeHtml(r.title)}}"">${{escapeHtml(r.title)}}</div>
          <div class=""num"">${{r.startDay}}</div>
          <div class=""num"">${{r.durationDays}}</div>
        </div>
      `).join('');

      const step = majorStep(totalDays);
      ticks.style.width = width + 'px';
      ticks.innerHTML = '';
      for (let d = 0; d <= totalDays; d += step) {{
        const x = d * pxPerDay;
        const t = document.createElement('div');
        t.className = 'tick';
        t.style.left = x + 'px';
        ticks.appendChild(t);
        const lab = document.createElement('div');
        lab.className = 'tickLabel';
        lab.style.left = x + 'px';
        lab.textContent = d;
        ticks.appendChild(lab);
      }}

      bars.style.width = width + 'px';
      bars.innerHTML = data.rows.map(() => `<div class=""barRow""></div>`).join('');
      const rowEls = Array.from(bars.children);

      data.rows.forEach((r, idx) => {{
        const rowEl = rowEls[idx];
        const left = r.startDay * pxPerDay;
        const w = Math.max(2, r.durationDays * pxPerDay);
        const b = document.createElement('div');
        b.className = 'bar';
        b.style.left = left + 'px';
        b.style.width = w + 'px';

        b.addEventListener('mousemove', (e) => {{
          tip.style.display = 'block';
          const end = r.startDay + r.durationDays;
          tip.innerHTML = `
            <div style=""font-weight:600;"">${{escapeHtml(r.wbs)}} · ${{escapeHtml(r.title)}}</div>
            <div class=""kv""><span class=""tmuted"">Старт</span><span>${{r.startDay}} день</span></div>
            <div class=""kv""><span class=""tmuted"">Окончание</span><span>${{end}} день</span></div>
            <div class=""kv""><span class=""tmuted"">Длительность</span><span>${{r.durationDays}} дней</span></div>
            <div class=""kv""><span class=""tmuted"">Предшественники</span><span>${{escapeHtml(r.dependsOnWbs || '—')}}</span></div>
          `;
          const pad = 14;
          tip.style.left = Math.min(window.innerWidth - 360, e.clientX + pad) + 'px';
          tip.style.top = Math.min(window.innerHeight - 200, e.clientY + pad) + 'px';
        }});
        b.addEventListener('mouseleave', () => {{ tip.style.display = 'none'; }});
        rowEl.appendChild(b);
      }});
    }}

    zoom.addEventListener('input', render);
    render();
  </script>
</body>
</html>";

        return Encoding.UTF8.GetBytes(html);
    }

    private static IReadOnlyList<object> BuildWbsRows(IReadOnlyList<GanttTaskResponse> tasks)
    {
        var idToWbs = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 0; i < tasks.Count; i++)
        {
            idToWbs[tasks[i].Id] = (i + 1).ToString(CultureInfo.InvariantCulture);
        }

        var rows = new List<object>();
        for (var i = 0; i < tasks.Count; i++)
        {
            var t = tasks[i];
            var duration = Math.Max(0, t.EndDay - t.StartDay);
            var wbs = (i + 1).ToString(CultureInfo.InvariantCulture);
            var depends = string.Join(";", t.DependsOnTaskIds.Select(id => idToWbs.TryGetValue(id, out var dep) ? dep : null).Where(x => !string.IsNullOrWhiteSpace(x))!);

            rows.Add(new { wbs, title = t.Title, startDay = t.StartDay, durationDays = duration, dependsOnWbs = depends });

            var details = BuildDetailsForPhase(t.Title, t.StartDay, duration);
            for (var j = 0; j < details.Count; j++)
            {
                var d = details[j];
                rows.Add(new { wbs = $"{wbs}.{j + 1}", title = d.Title, startDay = d.StartDay, durationDays = d.DurationDays, dependsOnWbs = wbs });
            }
        }

        return rows;
    }

    private sealed record Detail(string Title, int StartDay, int DurationDays);

    private static IReadOnlyList<Detail> BuildDetailsForPhase(string phaseTitle, int startDay, int durationDays)
    {
        if (durationDays <= 0) return Array.Empty<Detail>();

        var titles = phaseTitle switch
        {
            var s when s.Contains("Проектирование", StringComparison.OrdinalIgnoreCase) => new[] { "ТЗ и замеры", "Планировки и ведомости", "Согласования" },
            var s when s.Contains("Демонтаж", StringComparison.OrdinalIgnoreCase) => new[] { "Подготовка", "Демонтаж", "Вывоз и уборка" },
            var s when s.Contains("Инженер", StringComparison.OrdinalIgnoreCase) => new[] { "Электрика", "Сантехника/отопление", "Слаботочка" },
            var s when s.Contains("Чернов", StringComparison.OrdinalIgnoreCase) => new[] { "Стены/потолок", "Пол/стяжка", "Подготовка под чистовую" },
            var s when s.Contains("Чистов", StringComparison.OrdinalIgnoreCase) => new[] { "Покраска/обои", "Плитка/полы", "Двери/плинтусы" },
            var s when s.Contains("Мебл", StringComparison.OrdinalIgnoreCase) => new[] { "Поставка", "Сборка/монтаж", "Финальная приемка" },
            _ => new[] { "Подзадача 1", "Подзадача 2", "Подзадача 3" }
        };

        var d1 = Math.Max(1, (int)Math.Round(durationDays * 0.3, MidpointRounding.AwayFromZero));
        var d2 = Math.Max(1, (int)Math.Round(durationDays * 0.4, MidpointRounding.AwayFromZero));
        var d3 = Math.Max(1, durationDays - d1 - d2);

        var s1 = startDay;
        var s2 = startDay + d1;
        var s3 = startDay + d1 + d2;
        var end = startDay + durationDays;

        return new[]
        {
            new Detail(titles[0], s1, Math.Max(0, Math.Min(end - s1, d1))),
            new Detail(titles[1], s2, Math.Max(0, Math.Min(end - s2, d2))),
            new Detail(titles[2], s3, Math.Max(0, Math.Min(end - s3, d3)))
        };
    }
}
