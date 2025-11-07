using Godot;
using System;

public partial class PlayerAttack : Node
{
    [Export] public WeaponData CurrentWeapon;
    [Export] private NodePath weaponHolderPath;

    // --- ССЫЛКИ ---
    private Player player;
    private AnimationPlayer animationPlayer; 
    private Node2D weaponHolder;
    private Node currentWeaponInstance;
    private Area2D currentHitbox;
    private bool isHitboxSignalConnected = false;

    // --- НАШ "УМНЫЙ" ТАЙМЕР ---
    private Timer _attackTimer; 
    
    private float _currentAttackSpeedScale = 1.0f; // ("Запоминаем" (Remember) "скорость")

    // --- ФЛАГИ СОСТОЯНИЯ ---
    private bool isAttacking = false;
    private bool wasMovingOnAttackStart = false; 
    private string currentAttackAnimName = ""; 
    
    // --- "ИСПРАВЛЕНИЕ" (FIX) (ТВОЯ "ИДЕЯ") ---
    
    // (1. Мы "прячем" (hide) "настоящую" (real) "переменную" (variable))
    private bool _hitboxEnabled = false;

    // (2. Мы "ПОКАЗЫВАЕМ" (SHOW) "Анимации" (Animation)
    // "фальшивую" (fake) "ручку" (handle))
    [Export]
    public bool HitboxControl
    {
        // (Когда "Анимация" (Animation) "читает" (reads) "ручку")
        get => _hitboxEnabled;
        
        // (Когда "АНИМАЦИЯ" (ANIMATION) "ПИШЕТ" (writes) "в" (in) "ручку" (e.g., 'true'))
        set
        {
            _hitboxEnabled = value; // ("Обновляем" (Update) "переменную")
            
            // (3. "СЕТТЕР" (SETTER) "ВЫЗЫВАЕТ" (CALLS) "МЕТОД" (METHOD)!)
            if (_hitboxEnabled)
            {
                EnableWeaponHitbox(); // (Если 'true' -> "Включить")
            }
            else
            {
                DisableWeaponHitbox(); // (Если 'false' -> "Выключить")
            }
        }
    }
    // --- (Конец "Исправления") ---


    public void Initialize(Player playerNode)
    {
        this.player = playerNode;
        this.animationPlayer = player.AnimPlayer; 
    }

    public override void _Ready()
    {
        if (weaponHolderPath != null && !weaponHolderPath.IsEmpty) { weaponHolder = GetNode<Node2D>(weaponHolderPath); }
        if (weaponHolder == null) { GD.PrintErr($"PlayerAttack: 'WeaponHolder' не найден!"); }
        
        _attackTimer = new Timer();
        _attackTimer.Name = "AttackTimer";
        _attackTimer.OneShot = true;
        _attackTimer.Timeout += _OnAttackTimerTimeout; 
        AddChild(_attackTimer);
        
        EquipWeapon(CurrentWeapon);
    }
    
    public bool IsCurrentlyAttacking() => isAttacking;
    public bool WasMovingOnAttackStart() => wasMovingOnAttackStart;
    public double GetAttackDuration()
    {
        if (CurrentWeapon == null) return 1.0;
        return CurrentWeapon.AttackDuration;
    }
    
    public float GetCurrentAttackSpeedScale()
    {
        return _currentAttackSpeedScale;
    }

    public float GetAttackSpeedScale(string animName)
    {
        if (CurrentWeapon == null) return 1.0f;
        Animation anim = animationPlayer.GetAnimation(animName);
        if (anim == null)
        {
            GD.PrintErr($"PlayerAttack: Не могу найти анимацию '{animName}' для расчета скорости!");
            return 1.0f;
        }
        double animLength = anim.Length;
        double desiredDuration = CurrentWeapon.AttackDuration; 
        if (desiredDuration <= 0) return 1.0f;
        float speedScale = (float)(animLength / desiredDuration);
        return speedScale;
    }
    
    public void EquipWeapon(WeaponData weapon) 
    {
        CurrentWeapon = weapon;
        if (CurrentWeapon == null) { GD.PrintErr("EquipWeapon: Попытка экипировать null!"); return; }
        if (currentWeaponInstance != null)
        {
            if (currentHitbox != null && isHitboxSignalConnected)
            {
                currentHitbox.BodyEntered -= _on_weapon_hitbox_body_entered;
                isHitboxSignalConnected = false;
            }
            currentWeaponInstance.QueueFree();
        }
        if (CurrentWeapon.WeaponScene == null) { GD.PrintErr($"EquipWeapon: У '{CurrentWeapon.ResourceName}' не назначена WeaponScene!"); return; }
        currentWeaponInstance = CurrentWeapon.WeaponScene.Instantiate();
        if (weaponHolder == null) { GD.PrintErr("EquipWeapon: Не могу добавить оружие, 'weaponHolder' не найден!"); currentWeaponInstance.QueueFree(); return; }
        weaponHolder.AddChild(currentWeaponInstance);
        currentHitbox = currentWeaponInstance.GetNode<Area2D>("Hitbox");
        if (currentHitbox == null) { GD.PrintErr($"ОШИБКА: 'Hitbox' НЕ НАЙДЕН в '{currentWeaponInstance.Name}'!"); return; }
        currentHitbox.BodyEntered += _on_weapon_hitbox_body_entered;
        isHitboxSignalConnected = true;
    }

    public void StartAttack(string animName, bool isMoving)
    {
        if (isAttacking) return; 

        isAttacking = true;
        wasMovingOnAttackStart = isMoving; 
        currentAttackAnimName = animName; 
        
        // --- "ИСПРАВЛЕНИЕ" (FIX) "ЗДЕСЬ" (HERE) ---
        // (1. "Рассчитываем" (Calculate) "И" (AND) "Запоминаем" (SAVE) "скорость")
        _currentAttackSpeedScale = GetAttackSpeedScale(animName);
        
        // (Мы "УДАЛИЛИ" (REMOVED) 'EnableWeaponHitbox' "отсюда" (from here))
        
        // (2. "ЗАПУСКАЕМ" (START) "ТАЙМЕР")
        double attackDuration = GetAttackDuration();
        _attackTimer.WaitTime = attackDuration;
        _attackTimer.Start();
        
        GD.Print($"PlayerAttack: Атака НАЧАТА. Скорость: {_currentAttackSpeedScale}x.");
    }

    /// <summary>
    /// Вызывается АВТОМАТИЧЕСКИ, когда "умный" таймер атаки завершается.
    /// </summary>
    private void _OnAttackTimerTimeout()
    {
        GD.Print($"PlayerAttack: Таймер атаки ЗАВЕРШЕН.");
        
        isAttacking = false; 
        currentAttackAnimName = "";
        
        // --- "ИСПРАВЛЕНИЕ" (FIX) "ЗДЕСЬ" (HERE) ---
        _currentAttackSpeedScale = 1.0f; // ("Сбрасываем" (Reset) "скорость" (speed))
        
        // (Мы "УДАЛИЛИ" (REMOVED) 'DisableWeaponHitbox' "отсюда" (from here))
    }
    
    private void _on_weapon_hitbox_body_entered(Node2D body)
    {
       if (body.IsInGroup("enemies") && body.HasMethod("TakeDamage"))
       {
          if (body.HasMethod("get_is_dead") && (bool)body.Call("get_is_dead") == true) return;
          if (CurrentWeapon != null) body.Call("TakeDamage", CurrentWeapon.Damage);
       }
    }

    /// <summary>
    /// (Это "старые" (old) "методы" (methods). "Теперь" (Now) "они" (they)
    /// "управляются" (are controlled) "ручкой" (handle) 'HitboxControl')
    /// </summary>
    public void EnableWeaponHitbox()
    {
        if (currentHitbox != null)
        {
            currentHitbox.SetDeferred("monitoring", true);
        }
    }

    public void DisableWeaponHitbox()
    {
        if (currentHitbox != null)
        {
            currentHitbox.SetDeferred("monitoring", false);
        }
    }
}