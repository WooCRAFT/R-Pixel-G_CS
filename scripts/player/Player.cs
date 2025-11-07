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
    
    // --- КОМПОНЕНТЫ (Узлы, "перетаскиваются" в Инспектор) ---
    [Export] private PlayerAttack attackController;  // (Компонент "Атака", с "умным" Таймером)
    [Export] private PlayerStats statsController;    // (Компонент "Здоровье/Смерть")
    [Export] private AnimationTree animationTree;   // (Наш "Главный Мозг" Анимаций, "Корень")
    [Export] private Skeleton2D skeleton;          // (Скелет, который мы "отражаем" (flip))
    [Export] private CollisionPolygon2D mainCollision; // (Коллизия игрока)
    [Export] private double _respawnTime = 15.0;
    [Export] private Marker2D _spawnPoint;
    
    // --- "КРЕПЛЕНИЯ" (Узлы, "перетаскиваются" в Инспектор) ---
    [Export] private Node2D _wingsMount; // (Пустой Node2D, "Крепление" для Wings.tscn)
    
    // --- "БАЗЫ ДАННЫХ" (Ресурсы, "перетаскиваются" в Инспектор) ---
    [Export] private WingsData _equippedWings; // (Наш .tres файл с "силой" (stamina) и "скоростью" крыльев)
    [Export] private PackedScene _headScene; // ("Перетащи" (Drag) 'Head.tscn' "сюда" (here))
    [Export] private Node2D _headMount;

    // --- ССЫЛКИ (Получаются в коде) ---
    private WingsController _wingsController; // (Ссылка на "Мозг" Крыльев (WingsController.cs))
    private AnimationPlayer animationPlayer;  // (Ссылка на AnimationPlayer (нужен для PlayerAttack.cs))
    private HeadController _headController;
    
    
    // --- "АДРЕСА" ("ПУТИ") К "РУЧКАМ" В ANIMATIONTREE ---
    
    // --- "АДРЕСА" ("ПУТИ") К "РУЧКАМ" В ANIMATIONTREE ---
    
    // "Главный Мозг" (Корень)
    private const string FSM_PATH = "parameters/playback"; 
    
    // --- Ручки 'Locomotion' (Скриншот {438CF...}) ---
    // "Ground_Air_Switch" (0=Земля, 1=Воздух)
    private const string LOCO_GROUND_AIR_BLEND = "parameters/Locomotion/Ground_Air_Switch/blend_amount";
    // "Ground_Switch" (Ноги на земле: -1/0/+1)
    private const string LOCO_LEGS_BLEND = "parameters/Locomotion/Ground_Switch/blend_amount";
    // "Fall_Switch" (0=Прыжок, 1=Крылья)
    private const string LOCO_WINGS_ENABLED_BLEND = "parameters/Locomotion/Fall_Switch/blend_amount";
    // "Wings_Direction_Switch" (Крылья: -1/0/+1)
    private const string LOCO_WINGS_DIRECTION_BLEND = "parameters/Locomotion/Wings_Direction_Switch/blend_amount";
    
    
    // --- Ручки 'Attack_Axe' (Скриншот {5291C...}) ---
    // "Attack_Ground_Air_Switch" (0=Земля, 1=Воздух)
    private const string ATTACK_GROUND_AIR_BLEND = "parameters/Attack_Axe/Attack_Ground_Air_Switch/blend_amount";
    // "Attack_Ground_Switch" (Ноги на земле: -1/0/+1)
    private const string ATTACK_LEGS_BLEND = "parameters/Attack_Axe/Attack_Ground_Switch/blend_amount";
    // "Attack_Fall_Switch" (0=Прыжок, 1=Крылья)
    private const string ATTACK_WINGS_ENABLED_BLEND = "parameters/Attack_Axe/Attack_Fall_Switch/blend_amount";
    // "Attack_Wings_Direction_Switch" (Крылья: -1/0/+1)
    private const string ATTACK_WINGS_DIRECTION_BLEND = "parameters/Attack_Axe/Attack_Wings_Direction_Switch/blend_amount";
    
    // "Out_Attack" (Ручка СКОРОСТИ АТАКИ)
    private const string ATTACK_ARMS_SPEED_PATH = "parameters/Attack_Axe/Out_Attack/Attack_Axe_Torso/speed_scale";
    
    
    // --- СИСТЕМНЫЕ ПЕРЕМЕННЫЕ ---
    public float Gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle(); // (Глобальная гравитация)
    public bool IsFacingLeft { get; private set; } = false; // (true=смотрит влево, false=вправо)
    private bool _isFacingLeftOnAttackStart;
    private int movementDirection = 1; // (Направление флипа, когда НЕ атакуем)
    
    private bool _isAttackButtonHeld = false; // (true, пока кнопка атаки "зажата" (held))
    public bool IsFlying { get; private set; } = false; // (true, если мы "активно" летим (жмем "вверх"))
    
    // (Это "тестовая" переменная. Позже мы "передадим" (pass) ее из "инвентаря" (inventory))
    private bool _wingsEquipped = true; 

    // --- PUBLIC PROPERTIES (API для других скриптов) ---
    public PlayerStats Stats => statsController; // (Даем доступ к "Здоровью")
    public PlayerAttack Attack => attackController; // (Даем доступ к "Атаке" (нужно для WingsController))
    public AnimationPlayer AnimPlayer => animationPlayer; // (Даем доступ к 'AnimationPlayer' (нужно для PlayerAttack))


    public override void _Ready()
    {
        // --- (Создаем "внутренние" компоненты) ---
        movementController = new PlayerMovement();
        jumpController = new PlayerJump();

        // --- (Проверяем "внешние" (Export) компоненты) ---
        if (animationTree == null) GD.PrintErr($"Player: 'AnimationTree' не назначен!");
        // (Находим 'AnimationPlayer' (он "родственник" (sibling)))
        animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer"); 
        if (animationPlayer == null) GD.PrintErr($"Player: 'AnimationPlayer' не найден!");
        
        if (attackController == null) GD.PrintErr($"Player: 'Attack Controller' не назначен!");
        if (statsController == null) GD.PrintErr($"Player: 'Stats Controller' не назначен!");
        if (skeleton == null) GD.PrintErr($"Player: 'Skeleton2D' не назначен!");
        if (mainCollision == null) mainCollision = GetNode<CollisionPolygon2D>("CollisionPolygon2D");

        // --- "Знакомим" (Initialize) Компоненты ---
        if (attackController != null)
        {
            attackController.Initialize(this); // (Передаем 'this' (Игрока) в "Мозг" Атаки)
        }

        if (statsController != null)
        {
            statsController.PlayerDied += _on_PlayerDied; // (Подписываемся на "смерть")
        }
        
        // --- "СПАВНИМ" (SPAWN) "ГОЛОВУ" (HEAD) ---
        if (_headScene != null && _headMount != null)
        {
            // ("Создаем" (Instantiate) 'Head.tscn')
            var headInstance = _headScene.Instantiate();
            
            // ("Прикрепляем" (Attach) "голову" (head) к "Креплению" (Mount Point))
            _headMount.AddChild(headInstance);
            
            // ("Запоминаем" (Save) "ссылку" (reference) на "Мозг" Головы)
            _headController = headInstance as HeadController; 
        }
        else
        {
            GD.PrintErr("Player: 'HeadScene' или 'HeadMount' НЕ Назначены (assigned)!");
        }
        
        // --- ИНИЦИАЛИЗАЦИЯ КРЫЛЬЕВ (Твой План) ---
        // (Мы "спавним" (spawn) крылья, ТОЛЬКО если они "экипированы" (equipped))
        if (_equippedWings != null && _wingsMount != null)
        {
            // (1. "Спавним" сцену (которую мы "указали" в WingsData.tres))
            var wingsInstance = _equippedWings.WingsScene.Instantiate();
            // (2. "Прикрепляем" (attach) ее к "Креплению")
            _wingsMount.AddChild(wingsInstance);
            
            // (3. "Знакомим" "Мозг" Крыльев с Игроком и "Базой Данных")
            _wingsController = wingsInstance as WingsController;
            if (_wingsController != null)
            {
                // (Передаем 'this' (Игрока) и 'WingsData.tres' в "Мозг" Крыльев)
                _wingsController.Initialize(this, _equippedWings); 
            }
        }
        // --- ИНИЦИАЛИЗАЦИЯ ГОЛОВЫ (ИСПРАВЛЕНО) ---
        AttachHead(); // (Просто "вызываем" (call) "наш" (our) "новый" (new) "метод" (method))
    }
    
    /// <summary>
    /// "Спавнит" (Spawns) "НОВУЮ" (NEW) "голову" (head) и "прикрепляет" (attaches) ее.
    /// </summary>
    private void AttachHead()
    {
        // (1. "Проверяем" (Check), "есть ли" (exists) "старая" (old) "голова" (head))
        if (_headController != null && IsInstanceValid(_headController))
        {
            // ("Старая" (Old) "голова" (head) "еще" (still) "катается" (rolling),
            // "уничтожаем" (destroy) ее "немедленно" (immediately))
            _headController.QueueFree();
            _headController = null;
        }

        // (2. "Спавним" (Spawn) "НОВУЮ" (NEW) "голову" (head))
        if (_headScene != null && _headMount != null)
        {
            var headInstance = _headScene.Instantiate();
            _headMount.AddChild(headInstance);
            _headController = headInstance as HeadController;
            
            // (Мы "не" (don't) "вызываем" (call) 'Initialize()' (знакомство),
            // "потому что" (because) 'HeadController.cs' "не" (doesn't) "нуждается" (need)
            // в "ссылке" (reference) на 'Player')
        }
        else
        {
            GD.PrintErr("Player: 'HeadScene' или 'HeadMount' НЕ назначены (assigned)!");
        }
    }

    /// <summary>
    /// БОЛЬШЕ НЕ ИСПОЛЬЗУЕТСЯ. Вся логика перенесена в _PhysicsProcess.
    /// </summary>
    public override void _Process(double delta)
    {
        // ПУСТО.
    }
    
    /// <summary>
    /// Физический кадр. Обрабатывает ВВОД, ЛОГИКУ, ФИЗИКУ и АНИМАЦИИ.
    /// </summary>
    public override void _PhysicsProcess(double delta)
    {
        Vector2 velocity = this.Velocity;

        // 0. --- СМЕРТЬ ---
        if (statsController.IsPlayerDead()) 
        { 
            if (!IsOnFloor())
            {
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
            velocity.Y = jumpController.HandleJump(IsOnFloor(), velocity.Y, isJumpJustPressed);
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
                    velocity.Y += Gravity * (float)delta;
                }
            }
            else 
            {
                IsFlying = false;
                velocity.Y += Gravity * (float)delta;
            }
        }
        
        // --- 2B. Горизонтальная Скорость (X) ---
        if (wingsEquipped && (IsFlying || isJumpHeld)) 
        {
            float targetSpeed = _wingsController.GetMaxHorizontalSpeed() * inputDirection;
            velocity.X = Mathf.Lerp(velocity.X, targetSpeed, _wingsController.GetFlightAcceleration() * (float)delta);
        }
        else
        {
            velocity.X = movementController.HandleMovement(velocity, inputDirection);
        }

        // 3. --- (Применяем Физику) ---
        this.Velocity = velocity;
        MoveAndSlide();

        if (inputDirection != 0) { movementDirection = (inputDirection > 0) ? 1 : -1; }
        
        bool flipNeeded;
        
        // --- "ИСПРАВЛЕНИЕ" (FIX) "ЗДЕСЬ" (HERE) ---
        // (Мы "проверяем" (check) "ЗАЖАТА" (HELD) "ли" (if) "кнопка" (button),
        // "А" (AND) "НЕ" (NOT) "активна" (active) "ли" (if) "уже" (already) "атака" (attack))
        if (_isAttackButtonHeld || attackController.IsCurrentlyAttacking())
        {
            // "Мы" (We) "хотим" (want) "атаковать" (attack) "ИЛИ" (OR) "уже" (already) "атакуем" (attacking)
            // -> "Смотрим" (Look) "на" (at) "мышь" (mouse)
            flipNeeded = (GetGlobalMousePosition().X < GlobalPosition.X);
        }
        else
        {
            // "Мы" (We) "НЕ" (NOT) "атакуем" (attacking) -> "Смотрим" (Look) "куда" (where) "бежим" (running)
            flipNeeded = (movementDirection < 0);
        }
        // --- (Конец "Исправления") ---

        this.IsFacingLeft = flipNeeded;
        UpdateVisuals(flipNeeded); 

        // 5. --- (Логика Анимаций - "ИСПРАВЛЕНО" (FIXED)) ---
        
        var fsm = animationTree.Get(FSM_PATH).As<AnimationNodeStateMachinePlayback>();
        string currentState = fsm.GetCurrentNode();
        
        // --- 5.1 "Ручки" (Что мы хотим) ---
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
        
        // --- (ЛОГИКА 'Jump_Start' / 'Landing' УДАЛЕНА) ---

        // --- 5.2 Управляем "Мозгом" (FSM) ---
        
        // (Мы УЖЕ атакуем?)
        if (attackController.IsCurrentlyAttacking())
        {
            if (currentState != "Attack_Axe") fsm.Travel("Attack_Axe");
            
            // "Крутим" "все" "ручки" "Атаки" (включая "Скорость")
            animationTree.Set(ATTACK_GROUND_AIR_BLEND, groundAirBlend); 
            animationTree.Set(ATTACK_LEGS_BLEND, locomotionBlend);     
            animationTree.Set(ATTACK_WINGS_ENABLED_BLEND, wingsEnabledBlend); 
            animationTree.Set(ATTACK_WINGS_DIRECTION_BLEND, wingsDirectionBlend); 
            animationTree.Set(ATTACK_ARMS_SPEED_PATH, attackController.GetCurrentAttackSpeedScale());
        }
        // (Мы ХОТИМ начать атаку?)
        else if (_isAttackButtonHeld)
        {
            // ("Запоминаем" "направление" "ПЕРЕД" "атакой")
            _isFacingLeftOnAttackStart = this.IsFacingLeft; 
                
            // ("СНАЧАЛА" "рассчитываем" "скорость")
            attackController.StartAttack("attack_axe_ARMS_ONLY", IsMoving());
            float speed = attackController.GetCurrentAttackSpeedScale();

            // ("ПОТОМ" "крутим" "ручки" "И" "включаем" "состояние")
            animationTree.Set(ATTACK_ARMS_SPEED_PATH, speed); 
            animationTree.Set(ATTACK_GROUND_AIR_BLEND, groundAirBlend);
            animationTree.Set(ATTACK_LEGS_BLEND, locomotionBlend);
            animationTree.Set(ATTACK_WINGS_ENABLED_BLEND, wingsEnabledBlend);
            animationTree.Set(ATTACK_WINGS_DIRECTION_BLEND, wingsDirectionBlend);
            
            if (currentState != "Attack_Axe") fsm.Travel("Attack_Axe");
        }
        // (Мы НЕ атакуем и НЕ хотим)
        else
        {
            if (currentState != "Locomotion") fsm.Travel("Locomotion");
            
            animationTree.Set(LOCO_GROUND_AIR_BLEND, groundAirBlend); 
            animationTree.Set(LOCO_LEGS_BLEND, locomotionBlend);     
            animationTree.Set(LOCO_WINGS_ENABLED_BLEND, wingsEnabledBlend); 
            animationTree.Set(LOCO_WINGS_DIRECTION_BLEND, wingsDirectionBlend); 
        }
    } // Конец _PhysicsProcess
    
    /// <summary>
    /// "Отражает" (flips) скелет.
    /// </summary>
    public void UpdateVisuals(bool flip)
    {
        if (skeleton != null)
        {
            skeleton.Scale = skeleton.Scale with { X = flip ? -1 : 1 };
        }
    }
    
    /// <summary>
    /// "Вспомогательный" (Helper) метод.
    /// </summary>
    public bool IsMoving()
    {
        // (Мы "движемся", если скорость X > 10 И мы на земле)
        return Mathf.Abs(this.Velocity.X) > 10.0f && IsOnFloor();
    }
    
    /// <summary>
    /// (Этот метод "зарезервирован" (reserved) для будущих типов оружия)
    /// </summary>
    private string GetAttackAnimationNameFromWeaponType(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.Fists: return "attack_fists"; 
            default: return "attack_axe";
        }
    }
    
    
    // --- (Методы "Здоровья" (Health) и "Смерти" (Death)) ---
    public void TakeDamage(int amount) => statsController.TakeDamage(amount);
    public bool get_is_dead() => statsController.IsPlayerDead();
    
    /// <summary>
    /// "Воскрешает" (Respawns) "Игрока" (Player) "в" (at) "Точке Спавна" (Spawn Point).
    /// </summary>
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
        
        // --- "ИСПРАВЛЕНИЕ" (FIX) (Твои "Слои") ---
        // "Мы" (We) "меняем" (change) "слои" (layers) "НА" (ON) "САМОМ" (THE) "ИГРОКЕ" (PLAYER)
        
        // "Мы" (We) "снова" (again) "Персонаж" (Player) (Слой 1)
        this.CollisionLayer = (1 << 0); // (Слой = 1)
        
        // "Мы" (We) "видим" (see) "Монстров" (Monsters) (Слой 2) "И" (AND) "Мир" (World) (Слой 3)
        this.CollisionMask = (1 << 1) | (1 << 2); // (Маска = 2 + 4 = 6)
        // --- (Конец "Исправления") ---
        
        AttachHead();
        this.Show(); 
        
        var fsm = animationTree.Get(FSM_PATH).As<AnimationNodeStateMachinePlayback>();
        fsm.Travel("Locomotion");
        
        GD.Print("Игрок 'Воскрешен'!");
    }
    
    /// <summary>
    /// (Это "БЕЗОПАСНЫЙ" "перехватчик" "сигнала")
    /// (Вызывается "сигналом" 'PlayerDied' от 'PlayerStats')
    /// </summary>
    private void _on_PlayerDied()
    {
        // ("Откладываем" "логику" "смерти",
        // "чтобы" "не" "сломать" "физику")
        CallDeferred(nameof(HandleDeathSequence));
    }
    
    /// <summary>
    /// (Это "настоящая" "логика" "смерти")
    /// </summary>
    private async void HandleDeathSequence()
    {
        GD.Print("Player (Мозг) ЗАПУСКАЕТ логику смерти!");
        
        // --- "ИСПРАВЛЕНИЕ" (FIX) (Твои "Слои") ---
        // "Мы" (We) "меняем" (change) "слои" (layers) "НА" (ON) "САМОМ" (THE) "ИГРОКЕ" (PLAYER) (CharacterBody2D),
        // "а" (and) "НЕ" (NOT) "на" (on) 'mainCollision'.
        
        // "Мы" (We) "больше не" (no longer) "Персонаж" (Player) (Слой 1)
        // "или" (or) "Монстр" (Monster) (Слой 2).
        this.CollisionLayer = 0;
        
        // "Мы" (We) "видим" (see) "ТОЛЬКО" (ONLY) "Мир" (World) (Слой 3),
        // "чтобы" (to) "не" (not) "проваливаться" (fall through).
        this.CollisionMask = (1 << 2); // (Маска = 4, "видит" Слой 3)
        // --- (Конец "Исправления") ---

        var fsm = animationTree.Get(FSM_PATH).As<AnimationNodeStateMachinePlayback>();
        fsm.Travel("Death"); 

        if (_headController != null && IsInstanceValid(_headController))
        {
            var headGlobalPos = _headController.GlobalPosition;
            _headController.GetParent().RemoveChild(_headController);
            GetTree().Root.AddChild(_headController);
            _headController.GlobalPosition = headGlobalPos;
            
            Vector2 impulseDirection = IsFacingLeft ? Vector2.Left : Vector2.Right;
            
            _headController.FallOff(impulseDirection * 150 + Vector2.Up * 250); 
            
            _headController = null; 
        }
        

        // (Твой "План": "Тело" "стоит" "все" "время")
        await ToSignal(GetTree().CreateTimer(_respawnTime), Timer.SignalName.Timeout);
        
        this.Hide(); 
        Respawn(); 
    }
    
    
}