using Godot;
using System;

// (Это "частичный" (partial) класс, но он НЕ наследуется (inherit) от Node)
// (Это "чистый" C#-класс. "Главный Мозг" (Player.cs) "создает" (creates) его в _Ready())
public partial class PlayerJump
{
    // --- "РУЧКА" (Handle) (Настраивается в Инспекторе Godot) ---
    // ('[Export]' "показывает" (shows) эту "ручку" в Инспекторе 'Player.cs',
    // хотя этот скрипт НЕ "прикреплен" (attached) к узлу)
    [Export] public float JumpVelocity { get; set; } = -400.0f; // (Сила прыжка, Y вверх = минус)

    /// <summary>
    /// Управляет ТОЛЬКО логикой прыжка.
    /// (Вызывается "Главным Мозгом" (Player.cs) каждый кадр _PhysicsProcess)
    /// </summary>
    // (Принимает 3 "аргумента" (arguments) от "Главного Мозга")
    public float HandleJump(bool isOnFloor, float velocityY, bool isJumpJustPressed)
    {
        // (Мы прыгаем, ТОЛЬКО если "стоим на полу" (isOnFloor)
        // И "кнопка прыжка" (jump button) "только что нажата" (JustPressed))
        if (isOnFloor && isJumpJustPressed)
        {
            // (Мы "перебиваем" (override) 'Y' "силой прыжка" (JumpVelocity))
            velocityY = JumpVelocity;
        }
        
        // (Возвращаем "новую" (new) (или "старую" (old)) 'Y' обратно "Мозгу")
        return velocityY;
    }
    
    // (Здесь "жили" бы (would live) "двойные прыжки" (double jumps)
    // или "прыжки от стен" (wall jumps), если бы мы их "добавили" (added))
}