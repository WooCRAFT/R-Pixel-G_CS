using Godot;
using System;

/// <summary>
/// "Компонент" (Component) Физики.
/// Отвечает ТОЛЬКО за горизонтальное движение (ходьба, бег, торможение).
/// (Это НЕ УЗЕЛ. "Главный Мозг" (Player.cs) "создает" (creates) его с 'new PlayerMovement()').
/// </summary>
[GlobalClass] 
// (Ты "наследовал" (inherited) 'Resource', чтобы "видеть" '[Export]' в Инспекторе 'Player.cs')
// (ВНИМАНИЕ: Так как 'Player.cs' "создает" (creates) его через 'new()',
// Инспектор Godot НЕ "увидит" (see) этот [Export].
// 'Speed' (Скорость) ВСЕГДА будет '150.0f'.
// Если ты хочешь "менять" (change) 'Speed' в Инспекторе, 'Player.cs' должен 'Export' ЕГО,
// а НЕ 'PlayerMovement'.)
public partial class PlayerMovement : Resource 
{
    // "Ручка" (Handle) (Видна в Инспекторе Godot, ЕСЛИ 'PlayerMovement' - это .tres файл)
    [Export] public float Speed { get; private set; } = 150.0f; // (Базовая скорость)

    /// <summary>
    /// "Считает" (Calculates) 'X' скорость.
    /// (Вызывается "Главным Мозгом" (Player.cs) каждый кадр _PhysicsProcess)
    /// </summary>
    public float HandleMovement(Vector2 currentVelocity, float inputDirection)
    {
        float newVelocityX = currentVelocity.X;
        
        // (Если "жмем" (press) "влево" (-1) или "вправо" (1))
        if (inputDirection != 0) 
        {
            // (Устанавливаем "полную" (full) скорость)
            newVelocityX = inputDirection * Speed;
        }
        else // (Если "кнопки отпущены" (released))
        {
            // ("Плавно" (smoothly) "тормозим" (brake) до 0)
            newVelocityX = Mathf.MoveToward(currentVelocity.X, 0, Speed); 
        }
        
        // (Возвращаем "новую" (new) 'X' обратно "Мозгу")
        return newVelocityX;
    }

    /// <summary>
    /// "Запоминает" (Remembers) направление для "отражения" (flip).
    /// (Вызывается "Главным Мозгом" (Player.cs) каждый кадр _PhysicsProcess)
    /// </summary>
    public int GetMovementDirection(float inputDirection, int currentDirection)
    {
        if (inputDirection != 0)
        {
            // (Ты (правильно!) "исправил" (fixed) 'Math.Sign' на 'Mathf.Sign')
            // (Это "превращает" (converts) -1.0f в -1, 1.0f в 1)
            return (int)Mathf.Sign(inputDirection); 
        }
        
        // (Если мы "не жмем" (not pressing) - "помним" (remember) "старое" (old) направление)
        return currentDirection; 
    }
}