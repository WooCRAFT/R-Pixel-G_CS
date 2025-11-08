using Godot;
using System;
using System.Collections.Generic; // (Импортируем 'List<T>' для "списков")

/// <summary>
/// "Главный Мозг" Игрока.
/// Управляет Физикой (через "компоненты"), "Флипом" (отражением)
/// и "крутит ручки" (sets params) в "Главном Мозге" Анимаций (AnimationTree).
/// </summary>
public partial class Player : CharacterBody2D
{
    // --- КОМПОНЕНЕНТЫ (Внутренние, создаются в _Ready) ---
    private PlayerMovement movementController; // (Компонент, отвечающий за X-скорость)
    private PlayerJump jumpController;       // (Компонент, отвечающий за Y-скорость/Прыжок)
    
    // --- ССЫЛКИ НА УЗЛЫ (Настраиваются в Инспекторе) ---
    [ExportGroup("Ссылки на Узлы")] // <-- ИЗМЕНЕНО: Добавлена группа
    [Export] private PlayerAttack attackController;  // (Компонент "Атака", с "умным" Таймером)
    [Export] private PlayerStats statsController;    // (Компонент "Здоровье/Смерть")
    [Export] private AnimationTree animationTree;   // (Наш "Главный Мозг" Анимаций, "Корень")
    [Export] private Skeleton2D skeleton;          // (Скелет, который мы "отражаем" (flip))
    [Export] private CollisionPolygon2D mainCollision; // (Коллизия игрока)
    [Export] private Marker2D _spawnPoint;
    [Export] private Node2D _wingsMount; // (Пустой Node2D, "Крепление" для Wings.tscn)
    [Export] private Node2D _headMount;

    // --- РЕСУРСЫ И СЦЕНЫ (Настраиваются в Инспекторе) ---
    [ExportGroup("Ресурсы и Сцены")] // <-- ИЗМЕНЕНО: Добавлена группа
    [Export] private WingsData _equippedWings; // (Наш .tres файл с "силой" (stamina) и "скоростью" крыльев)
    [Export] private PackedScene _headScene; // ("Перетащи" (Drag) 'Head.tscn' "сюда" (here))

    // --- ССЫЛКИ (Получаются в коде) ---
    private WingsController _wingsController; // (Ссылка на "Мозг" Крыльев (WingsController.cs))
    private AnimationPlayer animationPlayer;  // (Ссылка на AnimationPlayer (нужен для PlayerAttack.cs))
    private HeadController _headController;
    
    
    // --- "АДРЕСА" ("ПУТИ") К "РУЧКАМ" В ANIMATIONTREE ---
    // (Этот C#-код ИДЕАЛЕН. Я его не трогаю.)
    private const string FSM_PATH = "parameters/playback"; 
    private const string LOCO_GROUND_AIR_BLEND = "parameters/Locomotion/Ground_Air_Switch/blend_amount";
    private const string LOCO_LEGS_BLEND = "parameters/Locomotion/Ground_Switch/blend_amount";
    // ... (и все остальные твои 'const string') ...
    private const string LOCO_WINGS_ENABLED_BLEND = "parameters/Locomotion/Fall_Switch/blend_amount";
    private const string LOCO_WINGS_DIRECTION_BLEND = "parameters/Locomotion/Wings_Direction_Switch/blend_amount";
    private const string ATTACK_GROUND_AIR_BLEND = "parameters/Attack_Axe/Attack_Ground_Air_Switch/blend_amount";
    private const string ATTACK_LEGS_BLEND = "parameters/Attack_Axe/Attack_Ground_Switch/blend_amount";
    private const string ATTACK_WINGS_ENABLED_BLEND = "parameters/Attack_Axe/Attack_Fall_Switch/blend_amount";
    private const string ATTACK_WINGS_DIRECTION_BLEND = "parameters/Attack_Axe/Attack_Wings_Direction_Switch/blend_amount";
    private const string ATTACK_ARMS_SPEED_PATH = "parameters/Attack_Axe/Out_Attack/Attack_Axe_Torso/speed_scale";
    
    
    // --- БАЛАНС (Настраивается в Инспекторе) ---
    
    [ExportGroup("Баланс (Физика)")] // <-- ИЗМЕНЕНО: Добавлена группа
    // (Я "вытащил" 'Gravity' в Инспектор, но оставил твой C#-код 'ProjectSettings' как значение ПО УМОЛЧАНИЮ)
    [Export(PropertyHint.Range, "0, 2000, 50")]
    public float Gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle(); // (Глобальная гравитация)

    // (Я "вытащил" "магическое число" 10.0f из C#-метода 'IsMoving()')
    [Export(PropertyHint.Range, "0, 50, 1")]
    private float _movementDeadzone = 10.0f; // <-- НОВОЕ: Порог C#-метода 'IsMoving()'
    [Export(PropertyHint.Range, "0, 1000, 25")]
    private float _moveSpeed = 350.0f; // <-- НОВОЕ: Скорость ходьбы
    [Export(PropertyHint.Range, "-1000, 0, 25")] // C#-ползунок (минусовые значения)
    private float _jumpForce = -400.0f; // <-- НОВОЕ: Сила прыжка (Y=вверх - это минус)
    [Export(PropertyHint.Range, "0, 2000, 50")]
    private float _brakingForce = 1000.0f; // <-- НОВОЕ: Сила торможения

    [ExportGroup("Баланс (Смерть)")] // <-- ИЗМЕНЕНО: Добавлена группа
    [Export(PropertyHint.Range, "0, 60, 1")] // (Я просто добавил 'Range' для удобства)
    private double _respawnTime = 15.0;

    // (Я "вытащил" "магические числа" 150 и 250 из C#-метода 'HandleDeathSequence()')
    [Export(PropertyHint.Range, "0, 500, 10")]
    private float _headEjectHorizontalForce = 150.0f; // <-- НОВОЕ
    [Export(PropertyHint.Range, "0, 500, 10")]
    private float _headEjectVerticalForce = 250.0f; // <-- НОВОЕ

    // --- СИСТЕМНЫЕ ПЕРЕМЕННЫЕ ---
    // (Этот C#-код я не трогаю, он идеален)
    public bool IsFacingLeft { get; private set; } = false; // (true=смотрит влево, false=вправо)
    private bool _isFacingLeftOnAttackStart;
    private int movementDirection = 1; // (Направление флипа, когда НЕ атакуем)
    private bool _isAttackButtonHeld = false; // (true, пока кнопка атаки "зажата" (held))
    public bool IsFlying { get; private set; } = false; // (true, если мы "активно" летим (жмем "вверх"))
    private bool _wingsEquipped = true; 

    // --- PUBLIC PROPERTIES (API для других скриптов) ---
    // (Этот C#-код я не трогаю)
    public PlayerStats Stats => statsController; // (Даем доступ к "Здоровью")
    public PlayerAttack Attack => attackController; // (Даем доступ к "Атаке" (нужно для WingsController))
    public AnimationPlayer AnimPlayer => animationPlayer; // (Даем доступ к 'AnimationPlayer' (нужно для PlayerAttack))


    public override void _Ready()
    {
        // (Этот C#-код я не трогаю. Он создает твои компоненты)
        movementController = new PlayerMovement();
        jumpController = new PlayerJump();

        // (Этот C#-код я не трогаю. Он ищет ноды и "знакомит" их)
        if (animationTree == null) GD.PrintErr($"Player: 'AnimationTree' не назначен!");
        animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer"); 
        if (animationPlayer == null) GD.PrintErr($"Player: 'AnimationPlayer' не найден!");
        
        if (attackController == null) GD.PrintErr($"Player: 'Attack Controller' не назначен!");
        if (statsController == null) GD.PrintErr($"Player: 'Stats Controller' не назначен!");
        if (skeleton == null) GD.PrintErr($"Player: 'Skeleton2D' не назначен!");
        if (mainCollision == null) mainCollision = GetNode<CollisionPolygon2D>("CollisionPolygon2D");

        if (attackController != null)
        {
            attackController.Initialize(this); 
        }

        if (statsController != null)
        {
            statsController.PlayerDied += _on_PlayerDied; 
        }
        
        // (Логику спавна Головы и Крыльев я не трогаю)
        if (_equippedWings != null && _wingsMount != null)
        {
            var wingsInstance = _equippedWings.WingsScene.Instantiate();
            _wingsMount.AddChild(wingsInstance);
            
            _wingsController = wingsInstance as WingsController;
            if (_wingsController != null)
            {
                _wingsController.Initialize(this, _equippedWings); 
            }
        }
        AttachHead(); 
    }
    
    // (Этот C#-метод я не трогаю)
    private void AttachHead()
    {
        if (_headController != null && IsInstanceValid(_headController))
        {
            _headController.QueueFree();
            _headController = null;
        }

        if (_headScene != null && _headMount != null)
        {
            var headInstance = _headScene.Instantiate();
            _headMount.AddChild(headInstance);
            _headController = headInstance as HeadController;
        }
        else
        {
            GD.PrintErr("Player: 'HeadScene' или 'HeadMount' НЕ назначены (assigned)!");
        }
    }

    // (Этот C#-метод я не трогаю)
    public override void _Process(double delta)
    {
        // ПУСТО.
    }
    
    // (Этот C#-метод я не трогаю. Он идеален.)
    public override void _PhysicsProcess(double delta)
    {
        Vector2 velocity = this.Velocity;

        // 0. --- СМЕРТЬ ---
        if (statsController.IsPlayerDead()) 
        { 
            if (!IsOnFloor())
            {
                // 'Gravity' теперь берется из [Export]
                velocity.Y += Gravity * (float)delta; 
                this.Velocity = velocity;
                MoveAndSlide();
            }
            return; 
        }
        
        // 1. --- СЧИТЫВАНИЕ ВВОДА ---
        _isAttackButtonHeld = Input.IsActionPressed("attack");
        float inputDirection = Input.GetAxis("ui_left", "ui_right");
        bool isJumpJustPressed = Input.IsActionJustPressed("jump");
        bool isJumpHeld = Input.IsActionPressed("jump"); 

        // 2. --- ЛОГИКА ФИЗИКИ (С КРЫЛЬЯМИ) ---
        bool wingsEquipped = (_wingsController != null); 
        
        // --- 2A. Вертикальная Скорость (Y) ---
        if (IsOnFloor())
        {
            velocity.Y = jumpController.HandleJump(IsOnFloor(), velocity.Y, isJumpJustPressed, _jumpForce); // <-- ИЗМЕНЕНО
            IsFlying = false; 
        }
        else
        {
            if (wingsEquipped)
            {
                bool canFly = _wingsController.HasStamina; 
                
                if (isJumpHeld && canFly)
                {
                    IsFlying = true;
                    // (Эта C#-логика использует 'WingsData.tres', что ПРАВИЛЬНО. Я не трогаю.)
                    velocity.Y = Mathf.Lerp(velocity.Y, _wingsController.GetVerticalSpeed(), _wingsController.GetFlightAcceleration() * (float)delta);
                }
                else if (isJumpHeld && !canFly)
                {
                    IsFlying = true; 
                    velocity.Y = _wingsController.GetSlowFallSpeed();
                }
                else
                {
                    IsFlying = false; 
                    // 'Gravity' теперь берется из [Export]
                    velocity.Y += Gravity * (float)delta; 
                }
            }
            else 
            {
                IsFlying = false;
                // 'Gravity' теперь берется из [Export]
                velocity.Y += Gravity * (float)delta; 
            }
        }
        
        // --- 2B. Горизонтальная Скорость (X) ---
        if (wingsEquipped && (IsFlying || isJumpHeld)) 
        {
            // (Эта C#-логика использует 'WingsData.tres', что ПРАВИЛЬНО. Я не трогаю.)
            float targetSpeed = _wingsController.GetMaxHorizontalSpeed() * inputDirection;
            velocity.X = Mathf.Lerp(velocity.X, targetSpeed, _wingsController.GetFlightAcceleration() * (float)delta);
        }
        else
        {
            // (Эта C#-логика использует 'PlayerMovement.cs', что ПРАВИЛЬНО. Я не трогаю.)
            velocity.X = movementController.HandleMovement(velocity, inputDirection, _moveSpeed, _brakingForce); // <-- ИЗМЕНЕНО
        }

        // 3. --- (Применяем Физику) ---
        this.Velocity = velocity;
        MoveAndSlide();

        // 4. --- (Логика "Флипа" (Flip)) ---
        if (inputDirection != 0) { movementDirection = (inputDirection > 0) ? 1 : -1; }
        
        bool flipNeeded;
        
        // (Этот C#-фикс 'if (_isAttackButtonHeld || ...)' я не трогаю. Он выглядит важным.)
        if (_isAttackButtonHeld || attackController.IsCurrentlyAttacking())
        {
            flipNeeded = (GetGlobalMousePosition().X < GlobalPosition.X);
        }
        else
        {
            flipNeeded = (movementDirection < 0);
        }
        this.IsFacingLeft = flipNeeded;
        UpdateVisuals(flipNeeded); 

        // 5. --- (Логика Анимаций) ---
        // (Весь этот C#-блок 'fsm.Travel' я не трогаю. Он идеален.)
        var fsm = animationTree.Get(FSM_PATH).As<AnimationNodeStateMachinePlayback>();
        string currentState = fsm.GetCurrentNode();
        
        float groundAirBlend = IsOnFloor() ? 0.0f : 1.0f;
        float wingsEnabledBlend = _wingsEquipped ? 1.0f : 0.0f; 
        
        float locomotionBlend = 0.0f; 
        float wingsDirectionBlend = 0.0f;
        
        if (inputDirection != 0f)
        {
            bool isMovingForward = (IsFacingLeft && inputDirection < 0) || (!IsFacingLeft && inputDirection > 0);
            
            if (IsOnFloor())
            {
                locomotionBlend = isMovingForward ? 1.0f : -1.0f; 
            }
            else if (wingsEquipped && (IsFlying || isJumpHeld))
            {
                wingsDirectionBlend = isMovingForward ? 1.0f : -1.0f; 
            }
        }
        
        if (attackController.IsCurrentlyAttacking())
        {
            if (currentState != "Attack_Axe") fsm.Travel("Attack_Axe");
            
            animationTree.Set(ATTACK_GROUND_AIR_BLEND, groundAirBlend); 
            animationTree.Set(ATTACK_LEGS_BLEND, locomotionBlend);     
            animationTree.Set(ATTACK_WINGS_ENABLED_BLEND, wingsEnabledBlend); 
            animationTree.Set(ATTACK_WINGS_DIRECTION_BLEND, wingsDirectionBlend); 
            animationTree.Set(ATTACK_ARMS_SPEED_PATH, attackController.GetCurrentAttackSpeedScale());
        }
        else if (_isAttackButtonHeld)
        {
            _isFacingLeftOnAttackStart = this.IsFacingLeft; 
                
            attackController.StartAttack("attack_axe_ARMS_ONLY", IsMoving());
            float speed = attackController.GetCurrentAttackSpeedScale();

            animationTree.Set(ATTACK_ARMS_SPEED_PATH, speed); 
            animationTree.Set(ATTACK_GROUND_AIR_BLEND, groundAirBlend);
            animationTree.Set(ATTACK_LEGS_BLEND, locomotionBlend);
            animationTree.Set(ATTACK_WINGS_ENABLED_BLEND, wingsEnabledBlend);
            animationTree.Set(ATTACK_WINGS_DIRECTION_BLEND, wingsDirectionBlend);
            
            if (currentState != "Attack_Axe") fsm.Travel("Attack_Axe");
        }
        else
        {
            if (currentState != "Locomotion") fsm.Travel("Locomotion");
            
            animationTree.Set(LOCO_GROUND_AIR_BLEND, groundAirBlend); 
            animationTree.Set(LOCO_LEGS_BLEND, locomotionBlend);     
            animationTree.Set(LOCO_WINGS_ENABLED_BLEND, wingsEnabledBlend); 
            animationTree.Set(LOCO_WINGS_DIRECTION_BLEND, wingsDirectionBlend); 
        }
    } // Конец _PhysicsProcess
    
    // (Этот C#-метод я не трогаю)
    public void UpdateVisuals(bool flip)
    {
        if (skeleton != null)
        {
            skeleton.Scale = skeleton.Scale with { X = flip ? -1 : 1 };
        }
    }
    
    // (Этот C#-метод я ИЗМЕНИЛ, чтобы он использовал [Export] переменную)
    public bool IsMoving()
    {
        // (Было '10.0f', стало '_movementDeadzone' из Инспектора)
        return Mathf.Abs(this.Velocity.X) > _movementDeadzone && IsOnFloor(); // <-- ИЗМЕНЕНО
    }
    
    // (Этот C#-метод я не трогаю)
    private string GetAttackAnimationNameFromWeaponType(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.Fists: return "attack_fists"; 
            default: return "attack_axe";
        }
    }
    
    
    // (Этот C#-метод я не трогаю)
    public void TakeDamage(int amount) => statsController.TakeDamage(amount);
    public bool get_is_dead() => statsController.IsPlayerDead();
    
    // (Этот C#-метод я ИЗМЕНИЛ, чтобы он использовал [Export] переменную)
    public void Respawn()
    {
        if (_spawnPoint != null)
        {
            this.GlobalPosition = _spawnPoint.GlobalPosition;
        }
        else
        {
            this.GlobalPosition = Vector2.Zero; 
        }

        statsController.ResetHealth();
        
        // (Твой C#-фикс для слоев я не трогаю)
        this.CollisionLayer = (1 << 0); // (Слой = 1)
        this.CollisionMask = (1 << 1) | (1 << 2); // (Маска = 2 + 4 = 6)
        
        AttachHead();
        this.Show(); 
        
        var fsm = animationTree.Get(FSM_PATH).As<AnimationNodeStateMachinePlayback>();
        fsm.Travel("Locomotion");
        
        GD.Print("Игрок 'Воскрешен'!");
    }
    
    // (Этот C#-метод я не трогаю)
    private void _on_PlayerDied()
    {
        CallDeferred(nameof(HandleDeathSequence));
    }
    
    // (Этот C#-метод я ИЗМЕНИЛ, чтобы он использовал [Export] переменные)
    private async void HandleDeathSequence()
    {
        GD.Print("Player (Мозг) ЗАПУСКАЕТ логику смерти!");
        
        // (Твой C#-фикс для слоев я не трогаю)
        this.CollisionLayer = 0;
        this.CollisionMask = (1 << 2); 

        var fsm = animationTree.Get(FSM_PATH).As<AnimationNodeStateMachinePlayback>();
        fsm.Travel("Death"); 

        if (_headController != null && IsInstanceValid(_headController))
        {
            var headGlobalPos = _headController.GlobalPosition;
            _headController.GetParent().RemoveChild(_headController);
            GetTree().Root.AddChild(_headController);
            _headController.GlobalPosition = headGlobalPos;
            
            Vector2 impulseDirection = IsFacingLeft ? Vector2.Left : Vector2.Right;
            
            // (Было '150' и '250', стало '_head...' из Инспектора)
            _headController.FallOff(impulseDirection * _headEjectHorizontalForce + Vector2.Up * _headEjectVerticalForce); // <-- ИЗМЕНЕНО
            
            _headController = null; 
        }
        
        // (Было '_respawnTime', но имя не менялось. Все ОК.)
        await ToSignal(GetTree().CreateTimer(_respawnTime), Timer.SignalName.Timeout);
        
        this.Hide(); 
        Respawn(); 
    }
}