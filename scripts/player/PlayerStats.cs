using Godot;

/// <summary>
/// "Компонент" (Component) (Узел).
/// Отвечает ТОЛЬКО за "здоровье" (health) и "состояние" (state) "жив/мертв" (alive/dead).
/// </summary>
public partial class PlayerStats : Node
{
    // --- "СИГНАЛЫ" (Signals) ---
    // (Этот C#-код идеален, я его не трогаю)
    
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
    
    [ExportGroup("Баланс (Здоровье)")] // <-- ИЗМЕНЕНО: Добавлена группа
    [Export]
    public int MaxHealth { get; private set; } = 100; // (Максимальное "здоровье")
    
    
    // --- "Внутренние" (Internal) Переменные ---
    // (Этот C#-код идеален, я его не трогаю)
    public int CurrentHealth { get; private set; } // (Текущее "здоровье")
    public bool IsDead { get; private set; } = false; // (Флаг "смерти", 'true' = мертв)

    
    // --- C#-МЕТОДЫ (Я их не трогаю, они работают) ---
    
    public override void _Ready()
    {
        CurrentHealth = MaxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (IsDead) return;
        
        CurrentHealth -= amount;
        GD.Print($"Игрок получил {amount} урона. Осталось: {CurrentHealth}");

        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);

        if (CurrentHealth <= 0 && !IsDead)
        {
            IsDead = true;
            GD.Print("Игрок побеждён!");
            EmitSignal(SignalName.PlayerDied);
        }
    }
    
    public void ResetHealth()
    {
        CurrentHealth = MaxHealth;
        IsDead = false; 
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
    }
    
    public bool IsPlayerDead() => IsDead;
}