using Godot;

// (Это "Мозг" (Brain) твоего "Интерфейса" (UI).
// Он "наследуется" (inherits) от 'Control', "базового" (base) узла для UI)
public partial class HUD : Control
{
    // --- "Внутренняя" (Internal) Ссылка ---
    // (Мы "найдем" (find) ее в _Ready())
    private TextureProgressBar playerHealthBar;

    public override void _Ready()
    {
        // 1. "Находим" (Find) узел "Полоски Здоровья" (Health Bar)
        playerHealthBar = GetNode<TextureProgressBar>("PlayerHealthBar");

        // 2. "Отложенный" (Deferred) "Поиск" (Search) Игрока
        // (Это "умный" (smart) ход. "Интерфейс" (UI) и "Игрок" (Player)
        // "загружаются" (load) "одновременно" (at the same time).
        // 'CallDeferred' "ждет" (waits) 1 кадр (frame),
        // чтобы "гарантировать" (guarantee), что Игрок "уже" (already) "существует" (exists))
        Callable.From(InitializePlayer).CallDeferred();
    }

    /// <summary>
    /// (Этот метод "вызывается" (called) "с задержкой" (deferred) (1 кадр) после _Ready())
    /// </summary>
    private void InitializePlayer()
    {
        // 3. "Ищем" (Find) узел Игрока (Player) в "группе" (group) "player"
        var playerNode = GetTree().GetFirstNodeInGroup("player");
        if (playerNode == null)
        {
            GD.PrintErr("HUD: Не удалось найти узел игрока! (Он в группе 'player'?)");
            return;
        }

        // 4. "Берем" (Get) "компонент" (component) 'PlayerStats' у Игрока
        var playerStats = playerNode.GetNode<PlayerStats>("PlayerStats");
        if (playerStats == null)
        {
            GD.PrintErr("HUD: Не удалось найти PlayerStats у игрока!");
            return;
        }

        // 5. "ПОДПИСЫВАЕМСЯ" (SUBSCRIBE) на "Сигнал" (Signal)
        // (Мы "говорим" (tell) 'playerStats':
        // "Эй, когда ты "выпустишь" (emit) 'HealthChanged',
        // "вызови" (call) мой (HUD's) 'OnPlayerHealthChanged'")
        playerStats.HealthChanged += OnPlayerHealthChanged;

        // 6. "Синхронизируем" (Sync) "начальное" (initial) "здоровье"
        // (Это "гарантирует" (guarantees), что полоска "полная" (full)
        // при "старте" (start), "даже если" (even if) мы "пропустили" (missed)
        // "самый первый" (very first) сигнал)
        OnPlayerHealthChanged(playerStats.CurrentHealth, playerStats.MaxHealth);
    }

    /// <summary>
    /// Этот метод "вызывается" (called) АВТОМАТИЧЕСКИ
    /// "сигналом" (signal) 'HealthChanged' от 'PlayerStats'.
    /// </summary>
    private void OnPlayerHealthChanged(int current, int max)
    {
        // ("Обновляем" (Update) "значения" (values) "Полоски Здоровья" (Health Bar))
        playerHealthBar.MaxValue = max;
        playerHealthBar.Value = current;
    }
}