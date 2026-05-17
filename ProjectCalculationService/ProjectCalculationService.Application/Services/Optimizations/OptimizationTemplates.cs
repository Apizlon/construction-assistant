using ProjectCalculationService.Application.Contracts.Optimizations;

namespace ProjectCalculationService.Application.Services.Optimizations;

internal static class OptimizationTemplates
{
    public static IReadOnlyList<OptimizationTemplateResponse> All { get; } = Build();

    public static OptimizationTemplateResponse? Get(string templateId)
    {
        return All.FirstOrDefault(t => string.Equals(t.Id, templateId, StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<OptimizationTemplateResponse> Build()
    {
        // Grid is a simplified architectural plan:
        // - Walls/partitions as 1-cell thick segments (rectangles)
        // - Door openings are simply gaps in wall segments
        // - Forbidden zones are red areas (shafts, structural cores, glazing areas etc.)
        // This makes plans feel like real квартирные/домовые схемы and avoids disconnected rooms.

        var list = new List<OptimizationTemplateResponse>();

        list.Add(Apartment1());
        list.Add(Apartment2());
        list.Add(Apartment3());

        list.Add(House1());
        list.Add(House2());
        list.Add(House3());

        list.Add(Office1());
        list.Add(Warehouse1());

        return list;
    }

    // Helpers: 1-cell walls with optional door gaps (inclusive ranges in grid coordinates)
    private static IEnumerable<RectDto> HWall(int x1, int x2, int y, params (int gapStart, int gapEnd)[] gaps)
    {
        for (var x = x1; x <= x2; x++)
        {
            if (gaps.Any(g => x >= g.gapStart && x <= g.gapEnd)) continue;
            yield return new RectDto(x, y, 1, 1);
        }
    }

    private static IEnumerable<RectDto> VWall(int x, int y1, int y2, params (int gapStart, int gapEnd)[] gaps)
    {
        for (var y = y1; y <= y2; y++)
        {
            if (gaps.Any(g => y >= g.gapStart && y <= g.gapEnd)) continue;
            yield return new RectDto(x, y, 1, 1);
        }
    }

    private static List<RectDto> OuterWalls(int width, int height, (int xStart, int xEnd)? bottomDoor = null, (int yStart, int yEnd)? leftDoor = null)
    {
        // Perimeter with optional entrance opening(s).
        var walls = new List<RectDto>();

        // top
        walls.AddRange(HWall(0, width - 1, 0));
        // bottom
        if (bottomDoor.HasValue)
            walls.AddRange(HWall(0, width - 1, height - 1, (bottomDoor.Value.xStart, bottomDoor.Value.xEnd)));
        else
            walls.AddRange(HWall(0, width - 1, height - 1));
        // left
        if (leftDoor.HasValue)
            walls.AddRange(VWall(0, 0, height - 1, (leftDoor.Value.yStart, leftDoor.Value.yEnd)));
        else
            walls.AddRange(VWall(0, 0, height - 1));
        // right
        walls.AddRange(VWall(width - 1, 0, height - 1));

        return walls;
    }

    // --- Templates ---
    private static OptimizationTemplateResponse Apartment1()
    {
        // Similar spirit to the reference: living room center, kitchen zone, bathroom, small room.
        // Entrance at bottom, corridor connects all rooms via doors.
        const int w = 36;
        const int h = 24;
        var walls = new List<RectDto>();
        walls.AddRange(OuterWalls(w, h, bottomDoor: (16, 18)));

        // Corridor vertical spine
        // left block (bath+closet) and right rooms connected via doors.
        // Main vertical wall splitting left rooms/corridor from right side.
        walls.AddRange(VWall(12, 1, 22, (10, 11))); // door to living
        // horizontal wall to create top-right room
        walls.AddRange(HWall(12, 34, 7, (22, 23))); // doorway from corridor to room
        // horizontal wall to create bottom-right small room
        walls.AddRange(HWall(12, 34, 16, (28, 29))); // doorway

        // left block: bathroom (top-left) + storage (bottom-left)
        walls.AddRange(HWall(1, 11, 9, (6, 7))); // bathroom door to corridor
        walls.AddRange(VWall(6, 1, 9)); // split bathroom from hall niche
        walls.AddRange(HWall(1, 11, 18, (3, 3))); // storage door

        // small partition near entrance (hall)
        walls.AddRange(VWall(20, 17, 22, (19, 20)));

        var forbidden = new List<RectDto>
        {
            // shaft near bathroom
            new RectDto(2, 2, 3, 3),
            // structural column in living area
            new RectDto(18, 11, 2, 2),
            // glazing strip at far right (no штробление)
            new RectDto(34, 1, 1, 22)
        };

        return new OptimizationTemplateResponse
        {
            Id = "apt_v1",
            Title = "Квартира A — 2 комнаты + санузел",
            Category = "Apartment",
            Width = w,
            Height = h,
            Walls = walls,
            ForbiddenZones = forbidden
        };
    }

    private static OptimizationTemplateResponse Apartment2()
    {
        // Two bedrooms + living/kitchen, corridor with doors.
        const int w = 40;
        const int h = 26;
        var walls = new List<RectDto>();
        walls.AddRange(OuterWalls(w, h, bottomDoor: (6, 8)));

        // Layout idea:
        // Left block: 2 bedrooms stacked.
        // Middle: corridor/hall connecting entrance to right living/kitchen + doors to bedrooms/bathroom.
        // Right: living + kitchen zone.

        // Vertical separation between left bedrooms and corridor (x=20), with doors.
        // Use 2-cell wide door openings so it's easier to click points and avoids accidental "one-cell choke".
        walls.AddRange(VWall(20, 1, 24, (4, 5), (13, 14), (21, 22))); // doors: bedroom1, bedroom2, hall

        // Bedroom split horizontal wall in left block (y=12), keep a small opening near outer wall as an extra pass-through.
        // This prevents creating large dead pockets if a user chooses points around the split.
        walls.AddRange(HWall(1, 20, 12, (2, 2)));

        // Bathroom near entrance inside corridor area (a small box), with door.
        walls.AddRange(HWall(21, 29, 16, (24, 24))); // bathroom door
        walls.AddRange(HWall(21, 29, 22));
        walls.AddRange(VWall(21, 16, 22));
        walls.AddRange(VWall(29, 16, 22));

        // Separate kitchen nook from living on the far right, with wide opening.
        walls.AddRange(VWall(30, 1, 24, (8, 10))); // opening between living and kitchen
        walls.AddRange(HWall(30, 38, 12, (33, 34))); // partial partition

        // bathroom near entrance
        walls.AddRange(HWall(1, 9, 12, (3, 3))); // door to corridor
        walls.AddRange(VWall(9, 12, 16));
        walls.AddRange(HWall(1, 9, 16));

        var forbidden = new List<RectDto>
        {
            new RectDto(2, 13, 2, 2), // plumbing shaft
            new RectDto(33, 2, 2, 2), // column
            new RectDto(38, 1, 1, 24) // glazing strip
        };

        return new OptimizationTemplateResponse
        {
            Id = "apt_v2",
            Title = "Квартира B — 3 комнаты и коридор",
            Category = "Apartment",
            Width = w,
            Height = h,
            Walls = walls,
            ForbiddenZones = forbidden
        };
    }

    private static OptimizationTemplateResponse Apartment3()
    {
        // Compact apartment with central hallway and 3 rooms.
        const int w = 44;
        const int h = 28;
        var walls = new List<RectDto>();
        walls.AddRange(OuterWalls(w, h, bottomDoor: (20, 22)));

        // central hallway rectangle-ish
        walls.AddRange(VWall(16, 1, 26, (21, 22))); // door to living
        walls.AddRange(VWall(28, 1, 26, (12, 13))); // door to top room
        walls.AddRange(HWall(16, 28, 12, (21, 22))); // opening in hall
        walls.AddRange(HWall(16, 28, 20, (23, 24))); // opening

        // top-left room divider
        walls.AddRange(HWall(1, 16, 10, (6, 7)));
        // top-right room divider
        walls.AddRange(HWall(28, 42, 10, (34, 35)));
        // bottom-right room divider
        walls.AddRange(HWall(28, 42, 22, (33, 33)));

        // bathroom block in bottom-left corner
        walls.AddRange(HWall(1, 12, 22, (5, 5)));
        walls.AddRange(VWall(12, 22, 26));
        walls.AddRange(HWall(1, 12, 26));

        var forbidden = new List<RectDto>
        {
            new RectDto(2, 23, 2, 2),
            new RectDto(21, 15, 2, 2),
            new RectDto(42, 1, 1, 26)
        };

        return new OptimizationTemplateResponse
        {
            Id = "apt_v3",
            Title = "Квартира C — 4 зоны (коридор + комнаты)",
            Category = "Apartment",
            Width = w,
            Height = h,
            Walls = walls,
            ForbiddenZones = forbidden
        };
    }

    private static OptimizationTemplateResponse House1()
    {
        // House: kitchen/living + 2 rooms + boiler room.
        const int w = 46;
        const int h = 30;
        var walls = new List<RectDto>();
        walls.AddRange(OuterWalls(w, h, bottomDoor: (7, 9)));

        // hallway corridor
        walls.AddRange(VWall(14, 1, 28, (6, 7), (18, 19))); // doorways
        walls.AddRange(HWall(14, 44, 14, (20, 22))); // wide opening to living

        // two rooms on left
        walls.AddRange(HWall(1, 14, 10, (6, 6)));
        walls.AddRange(HWall(1, 14, 20, (8, 8)));

        // boiler/tech room on right-bottom
        walls.AddRange(VWall(34, 14, 28, (24, 25)));
        walls.AddRange(HWall(34, 44, 22, (38, 38)));

        var forbidden = new List<RectDto>
        {
            new RectDto(2, 2, 3, 3), // structural core
            new RectDto(36, 24, 3, 3) // equipment zone
        };

        return new OptimizationTemplateResponse
        {
            Id = "house_v1",
            Title = "Дом A — гостиная + 2 комнаты + котельная",
            Category = "House",
            Width = w,
            Height = h,
            Walls = walls,
            ForbiddenZones = forbidden
        };
    }

    private static OptimizationTemplateResponse House2()
    {
        // House with central living and side rooms.
        const int w = 52;
        const int h = 32;
        var walls = new List<RectDto>();
        walls.AddRange(OuterWalls(w, h, bottomDoor: (25, 27)));

        // central corridor band
        walls.AddRange(HWall(1, 50, 12, (26, 27)));
        walls.AddRange(HWall(1, 50, 20, (26, 27)));

        // vertical splits to make rooms
        walls.AddRange(VWall(16, 1, 12, (6, 7)));
        walls.AddRange(VWall(34, 1, 12, (6, 7)));
        walls.AddRange(VWall(16, 20, 30, (24, 25)));
        walls.AddRange(VWall(34, 20, 30, (24, 25)));

        // small bathroom in corridor area
        walls.AddRange(HWall(2, 10, 20, (5, 5)));
        walls.AddRange(VWall(10, 20, 26));
        walls.AddRange(HWall(2, 10, 26));

        var forbidden = new List<RectDto>
        {
            new RectDto(3, 21, 2, 2),
            new RectDto(48, 1, 2, 6) // glazing/structural zone
        };

        return new OptimizationTemplateResponse
        {
            Id = "house_v2",
            Title = "Дом B — 4 комнаты вокруг гостиной",
            Category = "House",
            Width = w,
            Height = h,
            Walls = walls,
            ForbiddenZones = forbidden
        };
    }

    private static OptimizationTemplateResponse House3()
    {
        // House with long corridor and 3 rooms.
        const int w = 54;
        const int h = 30;
        var walls = new List<RectDto>();
        walls.AddRange(OuterWalls(w, h, bottomDoor: (4, 6)));

        // corridor line
        walls.AddRange(VWall(18, 1, 28, (6, 7), (14, 15), (22, 23)));
        walls.AddRange(VWall(36, 1, 28, (10, 11), (18, 19), (26, 27)));

        // room separators
        walls.AddRange(HWall(18, 53, 10, (26, 27), (42, 43)));
        walls.AddRange(HWall(18, 53, 20, (28, 29), (44, 45)));

        var forbidden = new List<RectDto>
        {
            new RectDto(2, 2, 4, 4),
            new RectDto(40, 14, 2, 2)
        };

        return new OptimizationTemplateResponse
        {
            Id = "house_v3",
            Title = "Дом C — длинный коридор + 3 комнаты",
            Category = "House",
            Width = w,
            Height = h,
            Walls = walls,
            ForbiddenZones = forbidden
        };
    }

    private static OptimizationTemplateResponse Office1()
    {
        const int w = 56;
        const int h = 28;
        var walls = new List<RectDto>();
        walls.AddRange(OuterWalls(w, h, leftDoor: (12, 14)));

        // meeting rooms block on right with doors to open space
        walls.AddRange(VWall(40, 1, 26, (6, 7), (18, 19)));
        walls.AddRange(VWall(54, 1, 26));
        walls.AddRange(HWall(40, 54, 9, (46, 47)));
        walls.AddRange(HWall(40, 54, 17, (48, 49)));

        // small corridor to meeting rooms
        walls.AddRange(HWall(40, 54, 5, (42, 43)));

        var forbidden = new List<RectDto>
        {
            // server room / equipment
            new RectDto(42, 2, 6, 3),
            // elevator/shaft
            new RectDto(2, 2, 3, 6)
        };

        return new OptimizationTemplateResponse
        {
            Id = "office_v1",
            Title = "Офис — open space + переговорные (с дверями)",
            Category = "Office",
            Width = w,
            Height = h,
            Walls = walls,
            ForbiddenZones = forbidden
        };
    }

    private static OptimizationTemplateResponse Warehouse1()
    {
        const int w = 64;
        const int h = 30;
        var walls = new List<RectDto>();
        walls.AddRange(OuterWalls(w, h, bottomDoor: (30, 34)));

        // shelves with cross aisles
        walls.AddRange(VWall(14, 2, 27, (14, 16)));
        walls.AddRange(VWall(26, 2, 27, (10, 12), (22, 24)));
        walls.AddRange(VWall(38, 2, 27, (14, 16)));
        walls.AddRange(VWall(50, 2, 27, (10, 12), (22, 24)));

        // loading bay partition near bottom with wide gate
        walls.AddRange(HWall(1, 62, 24, (28, 36)));

        var forbidden = new List<RectDto>
        {
            new RectDto(2, 2, 10, 6), // loading equipment area (no routing)
            new RectDto(54, 20, 8, 8) // hazardous zone
        };

        return new OptimizationTemplateResponse
        {
            Id = "warehouse_v1",
            Title = "Склад — стеллажи + погрузочная зона (проходы)",
            Category = "Warehouse",
            Width = w,
            Height = h,
            Walls = walls,
            ForbiddenZones = forbidden
        };
    }
}
