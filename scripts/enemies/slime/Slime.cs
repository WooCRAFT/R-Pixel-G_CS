using Godot;
using System;

public partial class Slime : CharacterBody2D
{
    // --- Настройки Движения ---
    public const float JumpVelocity = -450.0f; 
    public const float JumpSpeed = 100.0f;
    public const float Friction = 50.0f;

    // --- Системные переменные ---
    public float Gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
    private Player player = null; // (Именно 'Player', а не 'CharacterBody2D')

    // --- Ссылки на дочерние узлы ---
    private Timer jumpTimer; 
    
    // (ССЫЛКА НА "ХИТБОКС" "КАСАНИЯ")
    private Area2D touchHitbox; 
    
    // (Параметры "запроса" "к" "физике")
    private PhysicsShapeQueryParameters2D queryParams; 

    // --- ССЫЛКИ НА КОМПОНЕНТЫ ("Специалисты") ---
    private SlimeStats slimeStats;
    private SlimeAttack slimeAttack;
    private SlimeAnimation slimeAnimation;

    
    public override void _Ready()
    {
       // "Находим" "специалистов"
       slimeStats = GetNode<SlimeStats>("SlimeStats");
       slimeAttack = GetNode<SlimeAttack>("SlimeAttack");
       slimeAnimation = GetNode<SlimeAnimation>("SlimeAnimation");
       jumpTimer = GetNode<Timer>("JumpTimer");

       // "Подписываемся" "на" "сигнал" "смерти"
       slimeStats.SlimeDied += _on_SlimeDied;

       // --- "ИСПРАВЛЕНИЕ" (FIX) "ХИТБОКСА" (HITBOX) "И" (AND) "ПОИСКА" (SEARCH) "ИГРОКА" (PLAYER) ---
       
       // "СНАЧАЛА" (FIRST) "ищем" (find) "Игрока" (Player)
       // (Мы "ищем" (find) "тип" (type) 'Player', "а" (and) "не" (not) 'CharacterBody2D')
       try
       {
          player = GetTree().GetFirstNodeInGroup("player") as Player;
       }
       catch (Exception e)
       {
          GD.PrintErr($"Не удалось найти игрока: {e.Message}");
       }
       
       // "ПОТОМ" (THEN) "настраиваем" (setup) "хитбокс" (hitbox)
       touchHitbox = GetNode<Area2D>("Touch_Hitbox");
       if (touchHitbox == null)
       {
           GD.PrintErr("Slime: 'Touch_Hitbox' (Area2D) не найден!");
       }
       
       // "Настраиваем" "наш" "физический" "запрос" (query)
       queryParams = new PhysicsShapeQueryParameters2D();
       queryParams.Shape = touchHitbox.GetNode<CollisionShape2D>("CollisionShape2D").Shape;
       
       // "Ищем" (Look for) "ТОЛЬКО" (ONLY) "Слой 1" (Layer 1) (Игрок)
       queryParams.CollisionMask = (1 << 0); 
       
       // "Игнорируем" (Ignore) "самих" (ourselves) "себя" (us)
       queryParams.Exclude = new Godot.Collections.Array<Rid> { this.GetRid() };
       
       // --- (Конец "Исправления") ---
    }
    
    // --- МЕТОДЫ-ПЕРЕАДРЕСАТОРЫ (Public API) ---
    
    public void TakeDamage(int amount)
    {
        slimeStats.TakeDamage(amount);
    }

    public bool get_is_dead()
    {
       return slimeStats.IsSlimeDead();
    }


    public override void _PhysicsProcess(double delta)
    {
       // --- "ИСПРАВЛЕНИЕ" (FIX): "Ищем" (Find) "Игрока" (Player) "постоянно" (constantly), "если" (if) "потеряли" (lost) ---
       if (player == null || !IsInstanceValid(player))
       {
           // "Пытаемся" (Try) "найти" (find) "его" (him) "снова" (again)
           player = GetTree().GetFirstNodeInGroup("player") as Player;
           if (player == null)
           {
               // "Если" (If) "Игрока" (Player) "нет" (is not) "в" (in) "сцене" (scene) - "стоп" (stop)
               return;
           }
       }

       // --- "ЗАЩИТА" (Guard Clause) ---
       if (slimeStats.IsSlimeDead()) 
       {
          if (!IsOnFloor())
          {
             Velocity = Velocity with { Y = Velocity.Y + Gravity * (float)delta };
             MoveAndSlide();
          }
          return; 
       }

       // --- "Гравитация", "Движение", "Поворот спрайта" ---
       if (!IsOnFloor())
          Velocity = Velocity with { Y = Velocity.Y + Gravity * (float)delta };
       
       Vector2 directionToPlayer = Vector2.Zero; 

       if (!IsOnFloor())
       {
          directionToPlayer = (player.GlobalPosition - GlobalPosition);
          Velocity = Velocity with { X = Mathf.Sign(directionToPlayer.X) * JumpSpeed };
       }
       else
       {
          Velocity = Velocity with { X = Mathf.MoveToward(Velocity.X, 0, Friction * (float)delta) };
       }

       if (Mathf.Abs(GlobalPosition.DistanceTo(player.GlobalPosition)) > 1)
       {
          if (IsOnFloor())
          {
             directionToPlayer = (player.GlobalPosition - GlobalPosition);
          }
          slimeAnimation.UpdateFlip(directionToPlayer.X);
       }

       MoveAndSlide();

       // --- "ИСПРАВЛЕННАЯ" (FIXED) "АТАКА" (ATTACK) "КАСАНИЕМ" (ON TOUCH) ---
       var spaceState = GetWorld2D().DirectSpaceState;
       if (touchHitbox != null && spaceState != null)
       {
           queryParams.Transform = touchHitbox.GlobalTransform;
           
           var overlappingResult = spaceState.IntersectShape(queryParams);

           if (overlappingResult.Count > 0)
           {
               foreach (var bodyDict in overlappingResult)
               {
                   var collider = (Node)bodyDict["collider"];
                   
                   // "Проверяем" "ВЛАДЕЛЬЦА" (OWNER) "тела" (body)
                   if (collider.Owner is Player victim)
                   {
                       slimeAttack.TryToAttack(victim);
                   }
               }
           }
       }
    }


    // --- (Сигнал от 'jumpTimer') ---
    private void _on_jump_timer_timeout()
    {
       if (slimeStats.IsSlimeDead() || player == null || !IsInstanceValid(player)) return;

       if (IsOnFloor())
       {
          float directionX = (player.GlobalPosition - GlobalPosition).X;
          Velocity = new Vector2(Mathf.Sign(directionX) * JumpSpeed, JumpVelocity);
       }
    }

    // --- (Сигнал от 'SlimeDied') ---
    private async void _on_SlimeDied()
    {
        GetNode<CollisionPolygon2D>("CollisionPolygon2D").SetDeferred("disabled", true);
        jumpTimer.Stop();
        
        // slimeAnimation.PlayDeath();
        
        slimeAnimation.Hide();

        await ToSignal(GetTree().CreateTimer(1.0), Timer.SignalName.Timeout);
        
        QueueFree();
    }
}