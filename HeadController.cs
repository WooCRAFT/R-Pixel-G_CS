using Godot;
using System;

/// <summary>
/// "Мозг" (Brain) "Головы" (Head).
/// "Живет" (Lives) на 'RigidBody2D' в 'Head.tscn'.
/// "Умеет" (Knows how) "Замораживать" (Freeze) "себя" (itself), "Падать" (FallOff),
/// "Отталкиваться" (be hit) от "ударов" (attacks) и "Исчезать" (Disappear) со "временем" (time).
/// </summary>
public partial class HeadController : RigidBody2D
{
    private Timer _lifetimeTimer; // (Наш "Таймер" на 60 секунд (Твой План))
    private Area2D _hitbox;       // (Область для "обнаружения" (detecting) "ударов" (hits))

    // --- НАСТРОЙКИ ---
    [Export] private float _hitImpulseStrength = 200.0f; // (Насколько "сильно" (hard) "оттолкнуть" (push) "голову" (head) при "ударе" (hit))

    public override void _Ready()
    {
        // (1. "Находим" (Find) "дочерние" (child) узлы)
        _lifetimeTimer = GetNode<Timer>("LifetimeTimer");
        _hitbox = GetNode<Area2D>("Hitbox");
        
        // (2. "Выключаем" (Disable) "физику" (physics) и "коллизии" (collisions) "при старте" (on start).
        // (Голова "крепится" (attached) к Игроку "до" (until) "смерти"))
        this.Freeze = true; 
        this.CollisionLayer = 0; // (Слой для "физических" (physics) "столкновений" (collisions))
        this.CollisionMask = 0;
        _hitbox.CollisionLayer = 0; // (Слой для "обнаружения" (detecting) "ударов" (hits))
        _hitbox.CollisionMask = 0;
        
        // (3. "Подписываемся" (Subscribe) на "сигнал" (signal) от 'Hitbox')
        _hitbox.BodyEntered += OnHitboxBodyEntered;
        _lifetimeTimer.Timeout += OnLifetimeTimerTimeout;
    }

    /// <summary>
    /// "Главный Мозг" (Player.cs) "вызывает" (calls) "эту" (this) "функцию" (function)
    /// "в момент" (at the moment) "смерти" (death).
    /// </summary>
    public void FallOff(Vector2 initialImpulse)
    {
        // (1. "Включаем" (Enable) "физику" (physics)!)
        this.Freeze = false;
        
        // (2. "Включаем" (Enable) "коллизию" (collision) "RigidBody2D")
        this.CollisionLayer = 1; // (Или "твой" (your) "слой" (layer) "Игрока")
        this.CollisionMask = 1;  // (Или "твой" (your) "слой" (layer) "Мира" (World))
        
        // (3. "Включаем" (Enable) "коллизию" (collision) "Hitbox" для "ударов" (hits))
        //_hitbox.CollisionLayer = 2; // (Новый "слой" (layer) для "объектов", которые "могут" (can) "ударить" (hit) "голову")
        //_hitbox.CollisionMask = 2;  // (Например: Монстры, Игрок)

        // (4. "Отталкиваем" (Apply) "голову" (head) "от" (from) "тела" (body))
        this.ApplyImpulse(initialImpulse);
        
        // (5. "Запускаем" (Start) "Таймер" (Timer) "на 1 минуту" (for 1 minute) (Твой План))
        _lifetimeTimer.WaitTime = 60.0; // (60 секунд)
        _lifetimeTimer.Start();
    }

    /// <summary>
    /// "Вызывается" (Called) "автоматически" (automatically), когда "что-то" (something) "заходит" (enters) в 'Hitbox' головы.
    /// </summary>
    private void OnHitboxBodyEntered(Node2D body)
    {
        // (Мы "хотим" (want) "реагировать" (react) "только" (only) на "врагов" (enemies) или "игрока" (player))
        // (Твоя "логика" (logic) "проверки" (checking) "монстров" (monsters) / "игрока" (player) "здесь")
        if (body is Slime || body is Player) // (Пример: "Если" (If) это "Слайм" (Slime) или "Игрок" (Player))
        {
            // (1. "Направление" (Direction) "удара" (hit) - "от" (from) "того" (who) "ударил" (hit))
            Vector2 hitDirection = (this.GlobalPosition - body.GlobalPosition).Normalized();
            
            // (2. "Отталкиваем" (Apply) "голову" (head))
            ApplyImpulse(hitDirection * _hitImpulseStrength);
            
            // (Можно "добавить" (add) "звук" (sound) "удара" (hit) по "голове" (head) "здесь")
            // GD.Print("Голова получила удар!");
        }
    }

    /// <summary>
    /// "Вызывается" (Called) "автоматически" (automatically) "через" (after) 60 "секунд" (seconds).
    /// </summary>
    private void OnLifetimeTimerTimeout()
    {
        // "Голова" (Head) "исчезает" (disappears)
        QueueFree();
    }
}