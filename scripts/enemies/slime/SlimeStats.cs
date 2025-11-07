using Godot;

// (Это "Специалист" (Specialist) по "Здоровью". Он "наследуется" (inherits) от Node)
public partial class SlimeStats : Node
{
    // --- "СИГНАЛЫ" (Signals) ---
    /// <summary>
    /// "Событие" (Event), "срабатывает" (fires) ОДИН РАЗ, когда "здоровье" (health) <= 0.
    /// ("Мозг" (Slime.cs) "подписывается" (subscribes) на этот "сигнал")
    /// </summary>
    [Signal]
    public delegate void SlimeDiedEventHandler();

    // --- "РУЧКИ" (Handles) (Настраиваются в Инспекторе Godot) ---
    [Export]
    public int MaxHealth { get; private set; } = 100; // (Максимальное "здоровье")

    // --- "Внутренние" (Internal) Переменные ---
    public int CurrentHealth { get; private set; } // (Текущее "здоровье")
    public bool IsDead { get; private set; } = false; // (Флаг "смерти", 'true' = мертв)
    
    // --- НОВОЕ: Ссылка на UI ---
    /// <summary>
    /// Сюда (в Инспектор) мы "перетащим" (drag) узел 'TextureProgressBar' (Полоска Здоровья).
    /// </summary>
    [Export] private TextureProgressBar healthBar;

    
    public override void _Ready()
    {
        // (Устанавливаем "полное" (full) "здоровье" при "старте")
        CurrentHealth = MaxHealth;

        // --- НОВОЕ: Настраиваем "Полоску Здоровья" (Health Bar) ---
        if (healthBar == null)
        {
            GD.PrintErr("SlimeStats: Узел HealthBar не назначен в инспекторе!");
        }
        else
        {
            // ("Говорим" (Tell) полоске, какое у нее "максимальное" (max) значение)
            healthBar.MaxValue = MaxHealth;
            // ("Говорим" (Tell) полоске, какое у нее "текущее" (current) значение)
            healthBar.Value = CurrentHealth;
            // ("Прячем" (Hide) полоску, пока Слайм "здоров" (healthy))
            healthBar.Hide(); 
        }
    }

    // --- ПУБЛИЧНЫЕ МЕТОДЫ (API) ---

    /// <summary>
    /// "Публичный" (Public) метод.
    /// (Вызывается "извне" (externally) "хитбоксом" (hitbox) Игрока (Player))
    /// </summary>
    public void TakeDamage(int amount)
    {
        // (Если мы "уже" (already) мертвы - "игнорируем" (ignore) урон)
        if (IsDead)
        {
            return;
        }

        // (Отнимаем "здоровье")
        CurrentHealth -= amount;
        GD.Print($"Слизень получил {amount} урона. Осталось: {CurrentHealth}");
        
        // --- НОВОЕ: "Обновляем" (Update) и "Показываем" (Show) "Полоску Здоровья" ---
        if (healthBar != null)
        {
            // ("Показываем" (Show) полоску, т.к. Слайма "ударили" (hit))
            healthBar.Show();
            // ("Обновляем" (Update) "значение" (value) полоски)
            healthBar.Value = CurrentHealth;
        }

        // ("Проверяем" (Check) "смерть" (death))
        if (CurrentHealth <= 0 && !IsDead)
        {
            // (1. "Взводим" (Set) флаг 'IsDead')
            IsDead = true;
            GD.Print("Слизень побеждён!");

            // (2. "ВЫПУСКАЕМ СИГНАЛ" (EMIT SIGNAL))
            // ("Сообщаем" (Tell) "Мозгу" (Slime.cs), что мы "умерли" (died))
            EmitSignal(SignalName.SlimeDied);
        }
    }

    /// <summary>
    /// "Вспомогательный" (Helper) метод.
    /// ("Мозг" (Slime.cs) и "Хитбокс" (Hitbox) "спрашивают" (ask) его)
    /// </summary>
    public bool IsSlimeDead()
    {
        return IsDead;
    }
}