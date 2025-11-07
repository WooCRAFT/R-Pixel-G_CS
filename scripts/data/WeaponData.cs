using Godot;
using System;

// --- "ПЕРЕЧИСЛЕНИЕ" (Enum) ТИПОВ ОРУЖИЯ ---
// (Это "глобальный" (global) "список" (list) "всех" (all) "типов" (types) оружия,
// который "виден" (visible) "всем" (all) скриптам)
public enum WeaponType
{
    Fists,   // Кулаки (По умолчанию)
    Sword,   // Меч
    Axe,     // Топор
    Pickaxe, // Кирка
    Bow,     // Лук
}

/// <summary>
/// "База Данных" (Resource) для Оружия.
/// (Это "шаблон" (template) для 'Axe.tres', 'Pickaxe.tres' и т.д.)
/// </summary>
[GlobalClass] // (Позволяет "Создать" (Create) -> "Resource" -> "WeaponData" в Godot)
public partial class WeaponData : Resource
{
    // --- "РУЧКИ" (Handles) (Настраиваются в Инспекторе Godot) ---
    
    [Export] public int Damage { get; set; } = 10; // (Урон)
    
    // (Это "ключевая" (key) "ручка". "Мозг" Атаки (PlayerAttack.cs)
    // "читает" (reads) ее, чтобы "рассчитать" (calculate) 'SpeedScale')
    [Export] public double AttackDuration { get; set; } = 0.5; // (Желаемая "длительность" (duration) атаки)
    
    /// <summary>
    /// (Этот "путь" (path) "больше не используется" (no longer used) "Главным Мозгом" (Player.cs),
    /// так как "Мозг" (Brain) "использует" (uses) 'WeaponType' для "выбора" (select) "этажа" (state)
    /// в 'AnimationTree'. Но "полезно" (useful) "оставить" (keep) для "отладки" (debug))
    /// </summary>
    [Export] public string AttackAnimationName { get; set; } = "attack_fists"; 

    // --- (НОВОЕ "КЛЮЧЕВОЕ" (KEY) ПОЛЕ) ---
    /// <summary>
    /// "Тип" (Type) этого "оружия" (weapon).
    /// ("Главный Мозг" (Player.cs) "будет" (will) "читать" (read) это,
    /// чтобы "выбрать" (Travel) "правильный" (correct) "этаж" (state) в 'AnimationTree'
    /// (например, 'Attack_Axe' или 'Attack_Pickaxe'))
    /// </summary>
    [Export] public WeaponType Type { get; set; } = WeaponType.Fists; // (По умолчанию - Кулаки)

    /// <summary>
    /// "Сцена" (Scene) (.tscn) "самого" (itself) "оружия",
    /// которую 'PlayerAttack.cs' "заспавнит" (will spawn) и "прикрепит" (attach).
    /// </summary>
    [Export] public PackedScene WeaponScene { get; set; }
}