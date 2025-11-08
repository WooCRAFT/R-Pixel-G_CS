using Godot;
using System;

/// <summary>
/// "Компонент" (Component) Физики.
/// Отвечает ТОЛЬКО за горизонтальное движение (ходьба, бег, торможение).
/// (Это НЕ УЗЕЛ. "Главный Мозг" (Player.cs) "создает" (creates) его с 'new PlayerMovement()').
/// </summary>
[GlobalClass] 
// (Мы оставляем 'Resource', чтобы Godot "видел" [GlobalClass])
public partial class PlayerMovement : Resource 
{
    // --- [Export] Speed УДАЛЕН ---
    // (Этот C#-класс больше не "владеет" переменными баланса.
    // 'Player.cs' (Мозг) теперь передает их сюда.)

    /// <summary>
    /// "Считает" (Calculates) 'X' скорость.
    /// (Вызывается "Главным Мозгом" (Player.cs) каждый кадр _PhysicsProcess)
    /// </summary>
    // --- C#-СИГНАТУРА ИЗМЕНЕНА ---
    // (Теперь 'Player.cs' должен "передать" (pass) нам 'moveSpeed' и 'brakingForce')
    public float HandleMovement(Vector2 currentVelocity, float inputDirection, float moveSpeed, float brakingForce)
    {
        float newVelocityX = currentVelocity.X;
        
        if (inputDirection != 0) 
        {
            // (Используем 'moveSpeed', который нам "прислал" Player.cs)
            newVelocityX = inputDirection * moveSpeed; // <-- ИЗМЕНЕНО
        }
        else 
        {
            // (Используем 'brakingForce', как у Слайма, чтобы не скользить)
            newVelocityX = Mathf.MoveToward(currentVelocity.X, 0, brakingForce); // <-- ИЗМЕНЕНО
        }
        
        return newVelocityX;
    }

    /// <summary>
    /// "Запоминает" (Remembers) направление для "отражения" (flip).
    /// (Этот C#-метод ИДЕАЛЕН, мы его не трогаем)
    /// </summary>
    public int GetMovementDirection(float inputDirection, int currentDirection)
    {
        if (inputDirection != 0)
        {
            return (int)Mathf.Sign(inputDirection); 
        }
        
        return currentDirection; 
    }
}