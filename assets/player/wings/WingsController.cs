using Godot;
using System;

/// <summary>
/// "Мозг" Крыльев (Wings Brain).
/// "Живет" (Lives) в сцене 'Wings.tscn'.
/// "Отслеживает" (Tracks) Игрока, "читает" (reads) 'WingsData',
/// "управляет" (manages) "силой" (Stamina) и "крутит" (sets) "ручки" (params)
/// "своего" (its own) 'AnimationTree' (Wings_Brain).
/// </summary>
public partial class WingsController : Node2D
{
    // --- "РУЧКА" (Handle) (Настраивается в Инспекторе Godot) ---
    [Export] private AnimationTree _wingsBrain; // ("Мозг" Анимаций *Крыльев*)
    
    // --- "АДРЕСА" ("ПУТИ") К "РУЧКАМ" (в 'Wings_Brain') ---
    private const string FSM_PATH = "parameters/playback"; // ("Главный Мозг" Крыльев)
    
    // (ЗАМЕТКА: Если "Мозг" Крыльев (Wings_Brain) "станет" (becomes) "умнее" (smarter)
    // (например, с 'Blend3' (-1/0/+1) для "направления" (direction) полета),
    // "адреса" (paths) "будут добавлены" (will be added) "здесь" (here))
    // private const string FLY_DIRECTION_BLEND = "parameters/Flying/Blend3/blend_amount";
    
    // --- ССЫЛКИ (Получаются в коде) ---
    private Player _player; // (Ссылка на "Главный Мозг" (Player.cs))
    private WingsData _currentWingsData; // (Ссылка на "Базу Данных" (Default_Wings.tres))

    // --- "СИЛА" (Stamina) (Твоя "Высота Полета") ---
    private float _currentFlightStamina; // (Текущее "значение" (value))
    
    // ("Публичный" (public) "вопрос" (question), на который "отвечает" (answers) 'Player.cs')
    public bool HasStamina => _currentFlightStamina > 0; // (true, если "сила" > 0)

    public override void _Ready()
    {
        // ("Проверяем" (Check), "подключили" (linked) ли мы "Мозг" Крыльев в Инспекторе)
        if (_wingsBrain == null)
        {
            GD.PrintErr("WingsController: 'Wings_Brain' (AnimationTree) не назначен! ВЫКЛЮЧЮ скрипт.");
            // ("Выключаем" (Disable) '_PhysicsProcess', чтобы "избежать" (avoid) "краша" (crash))
            SetPhysicsProcess(false);
            return;
        }
    }

    /// <summary>
    /// "Главный Мозг" (Player.cs) "вызывает" (calls) этот метод "один раз" (once) в '_Ready()'.
    /// </summary>
    public void Initialize(Player playerNode, WingsData data)
    {
        _player = playerNode; // ("Запоминаем" (Save) "ссылку" (link) на Игрока)
        _currentWingsData = data; // ("Запоминаем" (Save) "ссылку" (link) на "Базу Данных" (Data))
        
        // (Устанавливаем "полную" (full) "силу" (stamina) при "старте" (start))
        if (_currentWingsData != null)
        {
            _currentFlightStamina = _currentWingsData.MaxFlightStamina;
        }
    }

    /// <summary>
    /// (Вызывается "каждый" (every) "физический" (physics) "кадр" (frame))
    /// </summary>
    public override void _PhysicsProcess(double delta)
    {
        // --- "ЗАЩИТА" (Guard Clause) ---
        // (Если Игрок "мертв" (dead) или "уничтожен" (disposed) - "стоп")
        if (_player == null || !IsInstanceValid(_player) || _currentWingsData == null)
        {
            return; 
        }

        // --- 1. "ОТСЛЕЖИВАЕМ" (Track) "СОСТОЯНИЕ" (State) "ИГРОКА" (Player) ---
        bool isOnFloor = _player.IsOnFloor();
        bool isJumping = Input.IsActionPressed("jump"); // (Игрок "жмёт вверх")
        
        // (Мы "спрашиваем" (ask) 'Player.cs', "считает ли" (does he think) он "себя" (himself) "летящим" (flying))
        bool playerIsFlying = _player.IsFlying; 

        // --- 2. ЛОГИКА "СИЛЫ" (Stamina) (Твоя "Высота Полета") ---
        if (isOnFloor)
        {
            // "после приземления она обновляется"
            // ("Восстанавливаем" (Recharge) "силу" (stamina) "со скоростью" (at rate) 'StaminaRechargeRate')
            _currentFlightStamina = Mathf.MoveToward(_currentFlightStamina, _currentWingsData.MaxFlightStamina, _currentWingsData.StaminaRechargeRate * (float)delta);
        }
        // (Если мы "жмем вверх" И "сила" (stamina) "есть" (available) И "Игрок" (Player) "летит" (is flying))
        else if (isJumping && HasStamina && !_player.Attack.IsCurrentlyAttacking() && playerIsFlying)
        {
            // "мы летим вверх крылья высота уменьшается"
            _currentFlightStamina -= (float)delta; // (Тратим 1.0 "силы" (stamina) в "секунду" (second))
        }
        
        // --- 3. ЛОГИКА АНИМАЦИИ КРЫЛЬЕВ (Управляем "Мозгом" Крыльев) ---
        var fsm = _wingsBrain.Get(FSM_PATH).As<AnimationNodeStateMachinePlayback>();

        if (isOnFloor)
        {
            // (Мы на "земле" (ground) -> "Сложить" (Close) крылья)
            fsm.Travel("Closed");
        }
        else if (_player.Attack.IsCurrentlyAttacking())
        {
            // (Атака "перебивает" (interrupts) "полёт" (flight) -> "Падение" (Falling))
            fsm.Travel("Falling"); 
        }
        // (Если "жмем вверх" И "сила" (stamina) "есть" (available) И "Игрок" (Player) "летит" (is flying))
        else if (isJumping && HasStamina && playerIsFlying)
        {
            // (Твоя "анимация полёта")
            fsm.Travel("Flying");
        }
        // (Если "жмем вверх" И "силы" (stamina) "НЕТ" (no) И "Игрок" (Player) "все еще" (still) "пытается" (trying))
        else if (isJumping && !HasStamina && playerIsFlying)
        {
            // (Твое "медленное падение", "сила" (stamina) "кончилась" (ran out))
            fsm.Travel("Hovering");
        }
        else
        {
            // (Мы "просто" (just) "падаем" (falling))
            fsm.Travel("Falling");
        }
    }
    
    // --- 4. "ОТДАЕМ" (Provide) "ФИЗИКУ" (Physics) "Мозгу" (Brain) "ИГРОКА" (Player) ---
    
    // ("Главный Мозг" (Player.cs) "спрашивает" (asks) "эти" (these) "цифры" (numbers)
    // из "Базы Данных" (Data) для "расчета" (calculating) 'velocity')
    public float GetVerticalSpeed() => _currentWingsData.VerticalFlySpeed;
    public float GetMaxHorizontalSpeed() => _currentWingsData.MaxHorizontalFlySpeed;
    public float GetFlightAcceleration() => _currentWingsData.FlightAcceleration;
    public float GetSlowFallSpeed() => _currentWingsData.SlowFallSpeed;
}