using Godot;
using System;

// (Это "частичный" (partial) класс, но он НЕ наследуется (inherit) от Node)
// (Это "чистый" C#-класс. "Главный Мозг" (Player.cs) "создает" (creates) его в _Ready())
public partial class PlayerJump
{
    /// <summary>
    /// Управляет ТОЛЬКО логикой прыжка.
    /// (Вызывается "Главным Мозгом" (Player.cs) каждый кадр _PhysicsProcess)
    /// </summary>
    // --- C#-СИГНАТУРА ИЗМЕНЕНА ---
    // (Теперь 'Player.cs' должен "передать" (pass) нам 'jumpVelocity')
    public float HandleJump(bool isOnFloor, float velocityY, bool isJumpJustPressed, float jumpVelocity)
    {
        if (isOnFloor && isJumpJustPressed)
        {
            // (Используем 'jumpVelocity', который нам "прислал" Player.cs)
            velocityY = jumpVelocity; // <-- ИЗМЕНЕНО
        }
        
        return velocityY;
    }
    
    // (Здесь "жили" бы (would live) "двойные прыжки" (double jumps)
    // или "прыжки от стен" (wall jumps), если бы мы их "добавили" (added))
}