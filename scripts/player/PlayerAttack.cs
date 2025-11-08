using Godot;
using System;

public partial class PlayerAttack : Node
{
    // --- НАСТРОЙКИ (Баланс) ---
    [ExportGroup("Настройки Оружия (Баланс)")]
    // (Это ГЛАВНАЯ C#-переменная. Все "магические числа" 
    // (Урон, Длительность) "живут" ВНУТРИ этого Ресурса .tres)
    [Export] public WeaponData CurrentWeapon;

    // --- ССЫЛКИ НА УЗЛЫ (Настройка) ---
    [ExportGroup("Ссылки на Узлы (Настройка)")]
    [Export] private NodePath weaponHolderPath;
    
    // --- РУЧКА ДЛЯ АНИМАЦИЙ (Настройка) ---
    [ExportGroup("Ручка для Анимаций (Hitbox)")]
    // (Твой "умный" C#-сеттер. Я его не трогаю, он идеален.)
    [Export]
    public bool HitboxControl
    {
        get => _hitboxEnabled;
        set
        {
            _hitboxEnabled = value; 
            if (_hitboxEnabled)
            {
                EnableWeaponHitbox();
            }
            else
            {
                DisableWeaponHitbox();
            }
        }
    }
    // (Внутренняя C#-переменная для "ручки")
    private bool _hitboxEnabled = false;

    // --- ССЫЛКИ (Внутренние) ---
    private Player player;
    private AnimationPlayer animationPlayer; 
    private Node2D weaponHolder;
    private Node currentWeaponInstance;
    private Area2D currentHitbox;
    private bool isHitboxSignalConnected = false;

    // --- НАШ "УМНЫЙ" ТАЙМЕР ---
    private Timer _attackTimer; 
    private float _currentAttackSpeedScale = 1.0f; 

    // --- ФЛАГИ СОСТОЯНИЯ (Внутренние) ---
    private bool isAttacking = false;
    private bool wasMovingOnAttackStart = false; 
    private string currentAttackAnimName = ""; 
    
    
    // --- C#-МЕТОДЫ (Я их не трогаю, они работают) ---
    
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
        
        _currentAttackSpeedScale = GetAttackSpeedScale(animName);
        
        double attackDuration = GetAttackDuration();
        _attackTimer.WaitTime = attackDuration;
        _attackTimer.Start();
        
        GD.Print($"PlayerAttack: Атака НАЧАТА. Скорость: {_currentAttackSpeedScale}x.");
    }

    private void _OnAttackTimerTimeout()
    {
        GD.Print($"PlayerAttack: Таймер атаки ЗАВЕРШЕН.");
        
        isAttacking = false; 
        currentAttackAnimName = "";
        _currentAttackSpeedScale = 1.0f; 
    }
    
    private void _on_weapon_hitbox_body_entered(Node2D body)
    {
       if (body.IsInGroup("enemies") && body.HasMethod("TakeDamage"))
       {
          if (body.HasMethod("get_is_dead") && (bool)body.Call("get_is_dead") == true) return;
          // (C#-код УЖЕ использует 'CurrentWeapon' для урона. Идеально!)
          if (CurrentWeapon != null) body.Call("TakeDamage", CurrentWeapon.Damage);
       }
    }

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