using Godot;

/// <summary>
/// "Мозг Мира". "Наследуется" (Inherits) от 'TileMap',
/// потому что он "сам" (itself) является "главным" (main) 'TileMap'.
/// </summary>
public partial class WorldTileManager : TileMap 
{
    // --- "СЛОИ" (Layers) (Настраиваются в Инспекторе Godot) ---
    [Export] private TileMapLayer mainBlockLayer; // (Сюда "перетаскиваем" (drag) 'Main_Block_Layer')
    [Export] private TileMapLayer grassLayer;     // (Сюда "перетаскиваем" (drag) 'Grass_Layer')

    // --- "ЭТИКЕТКА" (Custom Data) ---
    // (Это "имя" (name) "галочки" (boolean), которую мы "ставим" (set)
    // в "Атласе" (Tileset) для тайлов "Земли" (Earth))
    private const string DATA_IS_EARTH = "IsEarth";
    
    // ID "пустого" (empty) тайла (воздуха)
    private const int TILE_ID_AIR_SOURCE = -1; 
    
    // --- НАСТРОЙКИ "АТЛАСА" (Tileset Atlas) ---
    // (ID "Атласа" (Source ID) для "Земли")
    private const int TILE_SOURCE_ID_EARTH = 0; // (Вероятно 0)
    // (ID "Атласа" (Source ID) для "Травы")
    private const int TILE_SOURCE_ID_GRASS = 1; // (Вероятно 1)
    // --- КОНЕЦ НАСТРОЕК ---

    // --- (Массив "смещений" (offsets) для "проверки" (checking) "соседей" (neighbors)) ---
    /// <summary>
    /// Массив "смещений" (offsets) для "проверки" (checking) ВСЕХ 8 "соседей".
    /// </summary>
    private readonly Vector2I[] EIGHT_NEIGHBORS = new Vector2I[]
    {
        new Vector2I(0, -1), // Вверх
        new Vector2I(0, 1),  // Вниз
        new Vector2I(-1, 0), // Влево
        new Vector2I(1, 0),  // Вправо
        new Vector2I(-1, -1), // Вверх-Влево (Диагональ)
        new Vector2I(1, -1),  // Вверх-Вправо (Диагональ)
        new Vector2I(-1, 1),  // Вниз-Влево (Диагональ)
        new Vector2I(1, 1)    // Вниз-Вправо (Диагональ)
    };

    public override void _Ready()
    {
        // ("Проверяем" (Check) "ссылки" (links) из Инспектора)
        if (mainBlockLayer == null) { GD.PrintErr("WorldManager: 'mainBlockLayer' не назначен!"); return; }
        if (grassLayer == null) { GD.PrintErr("WorldManager: 'grassLayer' не назначен!"); return; }
        
        // ("Откладываем" (Defer) "сканирование" (scan) на 1 кадр (frame),
        // чтобы "гарантировать" (guarantee), что "мир" (world) "полностью" (fully) "загружен" (loaded))
        CallDeferred(nameof(GrowGrassOnLoad)); 
    }

    /// <summary>
    /// "Сканирует" (Scans) "всю" (entire) "карту" (map) и "растит" (grows) "траву" (grass).
    /// </summary>
    private void GrowGrassOnLoad()
    {
        GD.Print("WorldManager: Запускаю симуляцию роста травы...");
        
        // (Получаем "список" (List) "всех" (all) "ячеек" (cells),
        // которые "не-пустые" (not-empty) на "слое" (layer) "Земли")
        var usedEarthCells = mainBlockLayer.GetUsedCells();

        if (usedEarthCells.Count == 0)
        {
            GD.PrintErr("WorldManager: ОШИБКА! Не найдено тайлов на 'mainBlockLayer'.");
            return;
        }

        // ("Проходим" (Loop) по "каждой" (each) "ячейке" (cell))
        foreach (Vector2I cellPos in usedEarthCells)
        {
            // (1. "Спрашиваем" (Ask) "данные" (data) "ячейки" (cell))
            TileData earthData = mainBlockLayer.GetCellTileData(cellPos);
            
            // (Если "ячейка" (cell) - "не Земля" (not 'IsEarth'), "пропускаем" (skip) ее)
            if (earthData == null || !earthData.HasCustomData(DATA_IS_EARTH) || !(bool)earthData.GetCustomData(DATA_IS_EARTH))
            {
                continue; // (Это "Камень" (Stone) или "Дерево" (Wood))
            }

            // (2. "Проверяем" (Check), "касается" (touches) ли "ячейка" (cell) "воздуха" (air))
            if (IsSurfaceBlock(cellPos))
            {
                // (3. ЭТО ПОВЕРХНОСТЬ! "Растим" (Grow) "траву" (grass))
                
                // ("Узнаем" (Get) "координаты" (coords) "контура" (autotile) "Земли".
                // Например, 'Земля' (Earth) (1, 2) (левый-верхний угол))
                Vector2I earthContourCoords = mainBlockLayer.GetCellAtlasCoords(cellPos);
                
                // ("Говорим" (Tell) "Слою Травы" (Grass Layer):
                // "Нарисуй" (Draw) в "этой же" (same) "ячейке" (cellPos)
                // "тайл" (tile) из "Атласа Травы" (Grass Atlas) (ID 1)
                // с "теми же" (same) "контурами" (coords) (1, 2))
                grassLayer.SetCell(cellPos, TILE_SOURCE_ID_GRASS, earthContourCoords);
            }
            else
            {
                // (4. Это "закопанная" (buried) "земля" (earth). "Трава" (Grass) здесь "не растет" (cannot grow))
                // ("Стираем" (Erase) "траву" (grass) на "этой" (this) "ячейке" (cell))
                grassLayer.SetCell(cellPos, TILE_ID_AIR_SOURCE);
            }
        }
        
        GD.Print($"WorldManager: Рост травы завершен (проверено {usedEarthCells.Count} ячеек).");
    }
    
    /// <summary>
    /// "Вспомогательный" (Helper) метод.
    /// "Проверяет" (Checks), "является ли" (is) "ячейка" (cell) "поверхностью" (surface).
    /// </summary>
    private bool IsSurfaceBlock(Vector2I cellPos)
    {
        // ("Проходим" (Loop) по "всем" (all) 8 "соседям" (neighbors))
        foreach (Vector2I offset in EIGHT_NEIGHBORS)
        {
            Vector2I neighborPos = cellPos + offset;
            
            // ("Спрашиваем" (Ask) "ID Атласа" (Source ID) "соседа" (neighbor)
            // на "главном" (main) "слое" (layer) "Земли")
            int neighborSourceId = mainBlockLayer.GetCellSourceId(neighborPos);

            // (Если "сосед" (neighbor) - "Воздух" (Air) (ID -1)...)
            if (neighborSourceId == TILE_ID_AIR_SOURCE)
            {
                return true; // (...то 'cellPos' - это 100% "поверхность" (surface))
            }
        }
        
        // (Мы "проверили" (checked) "всех" (all) 8 "соседей",
        // и "никто" (none) из них "не" (not) "Воздух" (Air))
        return false; // (Этот "тайл" (tile) "похоронен" (buried))
    }
    
    /// <summary>
    /// (Этот метод "будет вызван" (will be called) "Киркой" (Pickaxe),
    /// когда "Игрок" (Player) "сломает" (breaks) "блок" (block))
    /// </summary>
    public void NotifyTileChanged(Vector2I cellPos)
    {
        // (TODO: "Пере-запустить" (Re-run) 'IsSurfaceBlock()'
        // "только" (only) для 9 "ячеек" (cells) "вокруг" (around) 'cellPos')
    }
}