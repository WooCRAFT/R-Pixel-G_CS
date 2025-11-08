using Godot;
using System.Linq; 

public partial class Slime : CharacterBody2D
{
    // --- 1. Настраиваемые Переменные (в Инспекторе) ---
    [ExportGroup("Прыжки")] 
    [Export(PropertyHint.Range, "100,1000,10")]
    public float BigJumpDistance { get; private set; } = 300.0f; 
    [Export]
    public float BigJumpHeight { get; private set; } = 500.0f;
    [Export]
    public float BigJumpSpeed { get; private set; } = 250.0f; // Это "начальная" скорость прыжка
    [Export]
    public float BigJumpCooldown { get; private set; } = 3.0f; 
    [Export]
    public float SmallJumpHeight { get; private set; } = 250.0f;
    [Export]
    public float SmallJumpSpeed { get; private set; } = 150.0f;
    [Export]
    public float SmallJumpCooldown { get; private set; } = 2.0f; 
    
    // --- НОВАЯ ПЕРЕМЕННАЯ ТОРМОЖЕНИЯ ---
    [Export(PropertyHint.Range, "100,2000,50")] 
    public float BrakingForce { get; private set; } = 700.0f; 
    
    // --- НОВАЯ ПЕРЕМЕННАЯ КОНТРОЛЯ В ВОЗДУХЕ ---
    [Export(PropertyHint.Range, "0, 1000, 25")]
    public float AirControlSpeed { get; private set; } = 200.0f; // <-- НОВОЕ: Сила "подруливания" в полете


    [ExportGroup("Атака")] 
    // ... (старые переменные) ...
    [Export]
    public float AttackDamage { get; private set; } = 5.0f; 
    [Export]
    public float AttackCooldown { get; private set; } = 0.5f;

    [ExportGroup("Здоровье")] 
    // ... (старые переменные) ...
    [Export]
    public float MaxHealth { get; private set; } = 30.0f; 
    private float currentHealth; 

    [ExportGroup("Имена Анимаций")] 
    // ... (старые переменные) ...
    [Export]
    public string AnimJump { get; private set; } = "jump"; 
    [Export]
    public string AnimIdle { get; private set; } = "idle"; 
    [Export]
    public string AnimDead { get; private set; } = "dead"; 

    // --- 2. Ссылки на Ноды (для перетаскивания) ---
    [ExportGroup("Ссылки на Ноды")]
    // ... (старые переменные) ...
    [Export]
    private NodePath _touchHitboxPolygonPath; 
    [Export]
    private NodePath _jumpTimerPath; 
    [Export] 
    private NodePath _attackCooldownTimerPath; 
    [Export] 
    private NodePath _animationPlayerPath; 
    [Export] 
    private NodePath _statsNodePath; 
    [Export]
    private NodePath _spritePath; 

    // --- 3. Приватные Переменные ---
    // (Без изменений)
    private Node2D playerNode; 
    private Timer jumpTimer;
    private Timer attackCooldownTimer; 
    private CollisionPolygon2D touchHitboxNode;
    private ConvexPolygonShape2D touchHitboxShapeResource;
    private AnimationPlayer animationPlayer; 
    private Node statsNode; 
    private Sprite2D sprite; 
    private bool isDead = false; 
    private float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

    
    // --- 4. Метод _Ready() ---
    // (Без изменений)
    public override void _Ready()
    {
        // ... (весь старый код _Ready() остается тут) ...
        jumpTimer = GetNode<Timer>(_jumpTimerPath);
        touchHitboxNode = GetNode<CollisionPolygon2D>(_touchHitboxPolygonPath);
        attackCooldownTimer = GetNode<Timer>(_attackCooldownTimerPath); 
        animationPlayer = GetNode<AnimationPlayer>(_animationPlayerPath); 
        statsNode = GetNode<Node>(_statsNodePath); 
        sprite = GetNode<Sprite2D>(_spritePath); 

        if (jumpTimer == null || touchHitboxNode == null || attackCooldownTimer == null || 
            animationPlayer == null || statsNode == null || sprite == null) 
        {
            GD.PrintErr($"ОШИБКА в Slime.cs: Одна из нод (Timer, Hitbox, Animation, Stats, Sprite) не установлена в Инспекторе!");
            return;
        }

        attackCooldownTimer.WaitTime = AttackCooldown; 
        attackCooldownTimer.OneShot = true; 
        jumpTimer.OneShot = true; 
        jumpTimer.Start(SmallJumpCooldown); 

        currentHealth = MaxHealth; 

        touchHitboxShapeResource = new ConvexPolygonShape2D();
        touchHitboxShapeResource.Points = touchHitboxNode.Polygon;
        
        animationPlayer?.Play(AnimIdle); 
        animationPlayer.AnimationFinished += OnAnimationFinished; 
    }

    // --- 5. Метод _PhysicsProcess() (ГЛАВНЫЕ ИЗМЕНЕНИЯ) ---
    public override void _PhysicsProcess(double delta)
    {
        if (isDead) return; 

        // --- ИЗМЕНЕНИЕ: Ищем игрока КАЖДЫЙ кадр ---
        if (playerNode == null)
        {
            playerNode = GetTree().GetNodesInGroup("player").FirstOrDefault() as Node2D;
        }
        // Если игрока нет, просто стоим и ждем
        if (playerNode == null) return; 
        
        // Рассчитываем направление к игроку КАЖДЫЙ кадр
        Vector2 direction = (playerNode.GlobalPosition - this.GlobalPosition).Normalized();
        // --- КОНЕЦ ИЗМЕНЕНИЯ ---


        // 1. Применяем гравитацию
        if (!IsOnFloor())
        {
            Velocity = new Vector2(Velocity.X, Velocity.Y + gravity * (float)delta);
        }

        // 2. Логика Прыжка / Ожидания / Полета
        
        // A. Если таймер прыжка ГОТОВ
        if (jumpTimer.IsStopped())
        {
            if (IsOnFloor()) 
            {
                // Передаем направление в C#-метод прыжка
                PerformJumpLogic(direction); // <-- ИЗМЕНЕНО
            }
        }
        // B. Если таймер на КД (мы ждем или летим)
        else
        {
            // B1. Мы на КД и НА ЗЕМЛЕ -> Тормозим
            if (IsOnFloor()) 
            {
                Velocity = new Vector2(
                    Mathf.MoveToward(Velocity.X, 0, BrakingForce * (float)delta), 
                    Velocity.Y 
                );
                
                StringName currentAnim = animationPlayer.CurrentAnimation;
                if (currentAnim != new StringName(AnimIdle) && currentAnim != new StringName(AnimJump))
                {
                     animationPlayer.Play(AnimIdle); 
                }
            }
            // B2. Мы на КД и В ВОЗДУХЕ -> КОНТРОЛЬ ПОЛЕТА!
            else
            {
                // --- НОВЫЙ C#-КОД: КОНТРОЛЬ В ВОЗДУХЕ ---
                // Мы плавно "подруливаем" нашу X-скорость к
                // 'direction.X * AirControlSpeed'
                Velocity = new Vector2(
                    Mathf.MoveToward(
                        Velocity.X, // Текущая X-скорость
                        direction.X * AirControlSpeed, // Целевая X-скорость (в сторону игрока)
                        // Скорость, с которой мы "подруливаем"
                        // (Используем половину силы торможения, чтобы было плавно)
                        (BrakingForce / 2) * (float)delta 
                    ),
                    Velocity.Y // Y-скорость (гравитацию) не трогаем
                );
                // --- КОНЕЦ НОВОГО КОДА ---
            }
        }
        
        // 3. Поворачиваем спрайт (теперь это здесь, чтобы работать всегда)
        // C#-логика: 'if' (если) X < 0... 'else if' (иначе если) X > 0...
        if (direction.X < -0.1f) // Добавляем "мертвую зону" 0.1
        {
            sprite.FlipH = true; // Смотрим влево
        }
        else if (direction.X > 0.1f) 
        {
            sprite.FlipH = false; // Смотрим вправо
        }
        
        // 4. Применяем движение
        MoveAndSlide();

        // 5. Проверяем атаку
        PerformAttackCheck();
    }

    // --- 6. "Умный" Прыжок (ИЗМЕНЕН) ---
    // C#-синтаксис: 'private void PerformJumpLogic(Vector2 direction)'
    // Теперь этот C#-метод "принимает" направление, которое ему передали
    private void PerformJumpLogic(Vector2 direction) // <-- ИЗМЕНЕНО
    {
        // (Блок поиска игрока и 'direction' УДАЛЕН отсюда. Он переехал в _PhysicsProcess)
        // (Блок поворота спрайта УДАЛЕН отсюда. Он переехал в _PhysicsProcess)

        float distance = this.GlobalPosition.DistanceTo(playerNode.GlobalPosition);
        
        Vector2 newVelocity = Vector2.Zero; 

        if (distance > BigJumpDistance)
        {
            // --- Логика БОЛЬШОГО прыжка ---
            newVelocity.X = direction.X * BigJumpSpeed;
            newVelocity.Y = -BigJumpHeight;
            jumpTimer.WaitTime = BigJumpCooldown; 
        }
        else
        {
            // --- Логика МАЛЕНЬКОГО прыжка ---
            newVelocity.X = direction.X * SmallJumpSpeed;
            newVelocity.Y = -SmallJumpHeight;
            jumpTimer.WaitTime = SmallJumpCooldown; 
        }
        
        Velocity = newVelocity; 
        jumpTimer.Start(); 
        
        animationPlayer?.Play(AnimJump); 
    }

    
    // --- 7. Метод Атаки ---
    // (Без изменений)
    private void PerformAttackCheck()
    {
        if (isDead || !attackCooldownTimer.IsStopped()) return;
        var spaceState = GetWorld2D().DirectSpaceState;
        // ... (остальной код метода не изменился) ...
        var queryParameters = new PhysicsShapeQueryParameters2D();
        queryParameters.Shape = touchHitboxShapeResource;
        queryParameters.Transform = touchHitboxNode.GlobalTransform;
        queryParameters.CollisionMask = 1; 
        var results = spaceState.IntersectShape(queryParameters);
        if (results.Count > 0)
        {
            foreach (var result in results)
            {
                var hitObject = result["collider"].As<Node>();
                if (hitObject != null && hitObject.IsInGroup("player"))
                {
                    GD.Print($"Слайм атакует {hitObject.GetClass()} на {AttackDamage} урона!");
                    hitObject.Call("TakeDamage", AttackDamage);
                    attackCooldownTimer.Start(); 
                    break; 
                }
            } 
        }
    }
    
    // --- 8. УРОН И СМЕРТЬ ---
    // (Без изменений)
    public void TakeDamage(float amount)
    {
        if (isDead) return;
        currentHealth -= amount;
        GD.Print($"Слайму нанесли {amount} урона. Осталось: {currentHealth}");
        if (currentHealth <= 0)
        {
            PerformDeath();
        }
    }
    private void PerformDeath()
    {
        isDead = true; 
        GD.Print("Слайм умер!");
        SetPhysicsProcess(false); 
        var area = touchHitboxNode.GetParent<Area2D>();
        if (area != null)
        {
            area.Monitoring = false; 
        }
        animationPlayer?.Play(AnimDead); 
    }
    private void OnAnimationFinished(StringName animName)
    {
        if (animName == new StringName(AnimDead))
        {
            QueueFree();
        }
    }
}