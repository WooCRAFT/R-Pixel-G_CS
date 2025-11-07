using Godot;
using System;

/// <summary>
/// "База Данных" (Resource) для Крыльев.
/// (Это "шаблон" (template) для 'Default_Wings.tres', 'Angel_Wings.tres' и т.д.)
/// </summary>
[GlobalClass] // (Позволяет "Создать" (Create) -> "Resource" -> "WingsData" в Godot)
public partial class WingsData : Resource
{
    // --- "РУЧКИ" (Handles) (Настраиваются в Инспекторе Godot) ---
    
    /// <summary>
    /// "Сцена" (Scene) (.tscn) "самих" (themselves) "крыльев",
    /// которую 'Player.cs' "заспавнит" (will spawn) и "прикрепит" (attach).
    /// </summary>
    [Export] public PackedScene WingsScene { get; set; } 
    
    // --- "РУЧКИ" ФИЗИКИ (Physics Handles) ---
    
    /// <summary>
    /// (Твоя "высота полета". "Сила" (Stamina) в "секундах" (seconds))
    /// </summary>
    [Export(PropertyHint.Range, "1.0, 5.0, 0.1")] // (в секундах)
    public float MaxFlightStamina { get; set; } = 2.0f; // (По умолчанию: 2 секунды)

    /// <summary>
    /// (Скорость "восстановления" (recharge) "силы" (stamina) на "земле" (ground))
    /// </summary>
    [Export(PropertyHint.Range, "0.1, 2.0, 0.1")]
    public float StaminaRechargeRate { get; set; } = 1.0f; // (По умолчанию: 1.0 "силы" в сек)

    /// <summary>
    /// (Твоя "скорость вертикально". (Y вверх = минус).
    /// Должна быть "сильнее" (stronger) 'JumpVelocity' (прыжка))
    /// </summary>
    [Export(PropertyHint.Range, "-100, -800, 1")]
    public float VerticalFlySpeed { get; set; } = -300.0f; 

    /// <summary>
    /// (Твоя "скорость горизонтально".
    /// Должна быть "быстрее" (faster), чем 'PlayerMovement.Speed')
    /// </summary>
    [Export(PropertyHint.Range, "100, 1000, 1")]
    public float MaxHorizontalFlySpeed { get; set; } = 400.0f; 

    /// <summary>
    /// (Твое "плавное" (smooth) "ускорение" (acceleration) в "воздухе" (air))
    /// </summary>
    [Export(PropertyHint.Range, "1, 20, 0.5")]
    public float FlightAcceleration { get; set; } = 8.0f; 

    /// <summary>
    /// (Твое "медленное падение", когда "сила" (stamina) "кончилась" (ran out),
    /// но "кнопка" (button) "зажата" (held))
    /// </summary>
    [Export(PropertyHint.Range, "10, 100, 1")]
    public float SlowFallSpeed { get; set; } = 50.0f; 
}