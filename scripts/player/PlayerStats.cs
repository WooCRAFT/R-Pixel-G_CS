using Godot;

/// <summary>
/// "Компонент" (Component) (Узел).
/// Отвечает ТОЛЬКО за "здоровье" (health) и "состояние" (state) "жив/мертв" (alive/dead).
/// </summary>
public partial class PlayerStats : Node
{
    // --- "СИГНАЛЫ" (Signals) ---
    // (Мы "объявляем" (declare) "события" (events),
    // на которые "подпишутся" (subscribe) другие узлы (например, UI-полоска "здоровья"))
    
    /// <summary>
    /// "Событие" (Event), "срабатывает" (fires) при изменении "здоровья".
    /// </summary>
    [Signal]
    public delegate void HealthChangedEventHandler(int currentHealth, int maxHealth);
    
    /// <summary>
    ///"Событие" (Event), "срабатывает" (fires) ОДИН РАЗ, когда "здоровье" (health) <= 0.
    /// </summary>
    [Signal]
    public delegate void PlayerDiedEventHandler();

    // --- "РУЧКИ" (Handles) (Настраиваются в Инспекторе Godot) ---
    [Export]
    public int MaxHealth { get; private set; } = 100; // (Максимальное "здоровье")
    
    // --- "Внутренние" (Internal) Переменные ---
    public int CurrentHealth { get; private set; } // (Текущее "здоровье")
    public bool IsDead { get; private set; } = false; // (Флаг "смерти", 'true' = мертв)

    /// <summary>
    /// (Вызывается 1 раз при "старте" (start) игры)
    /// </summary>
    public override void _Ready()
    {
        // (Устанавливаем "полное" (full) "здоровье" при "старте")
        CurrentHealth = MaxHealth;
    }

    /// <summary>
    /// "Публичный" (Public) метод.
    /// (Вызывается "извне" (externally), например, 'Slime.cs' или 'Trap.cs')
    /// </summary>
    public void TakeDamage(int amount)
    {
        // (Если мы "уже" (already) мертвы - "игнорируем" (ignore) урон)
        if (IsDead) return;
        
        // (Отнимаем "здоровье")
        CurrentHealth -= amount;
        GD.Print($"Игрок получил {amount} урона. Осталось: {CurrentHealth}");

        // ("Отправляем" (Emit) "сигнал" (signal) 'HealthChanged',
        // чтобы UI-полоска "здоровья" (health bar) "обновилась" (updated))
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);

        // (Проверяем, "умер" (died) ли игрок "в этом кадре" (this frame))
        if (CurrentHealth <= 0 && !IsDead)
        {
            // (1. "Взводим" (Set) флаг 'IsDead', чтобы "остановить" (stop) 'TakeDamage'
            // и "включить" (enable) 'if (statsController.IsPlayerDead())' в 'Player.cs')
            IsDead = true;
            GD.Print("Игрок побеждён!");
            
            // (2. "Отправляем" (Emit) "сигнал" (signal) 'PlayerDied'.
            // 'Player.cs' "подписан" (subscribed) на него и "запустит" (will run) '_on_PlayerDied')
            EmitSignal(SignalName.PlayerDied);
        }
    }
    
    /// <summary>
    /// "Воскрешает" (Resets) "здоровье" (health) "Игрока" (Player).
    /// "Вызывается" (Called) "Главным Мозгом" (Player.cs) "при" (on) "воскрешении" (respawn).
    /// </summary>
    public void ResetHealth()
    {
        CurrentHealth = MaxHealth;
        IsDead = false; 
        
        // (Мы "также" (also) "должны" (must) "отправить" (emit) "сигнал" (signal),
        // "чтобы" (so that) 'HUD.cs' "узнал" (knows) о "восстановлении" (restore))
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
    }
    
    /// <summary>
    /// "Вспомогательный" (Helper) метод.
    /// ("Главный Мозг" (Player.cs) "спрашивает" (asks) его "каждый кадр" (every frame))
    /// </summary>
    public bool IsPlayerDead() => IsDead;
}