using Godot;

// (Это "Специалист" (Specialist) по "Анимации". Он НЕ 'CharacterBody2D')
public partial class SlimeAnimation : Node
{
    // --- "РУЧКА" (Handle) (Настраивается в Инспекторе Godot) ---
    /// <summary>
    /// Сюда (в Инспектор) нужно "перетащить" (drag) узел 'Sprite2D'.
    /// </summary>
    [Export] private Sprite2D sprite2D;

    public override void _Ready()
    {
        // ("Проверяем" (Check), "подключили" (linked) ли мы 'Sprite2D' в Инспекторе)
        if (sprite2D == null)
        {
            GD.PrintErr("SlimeAnimation: 'Sprite2D' не назначен в инспекторе!");
        }
    }

    // --- ПУБЛИЧНЫЙ МЕТОД (API) ---

    /// <summary>
    /// "Мозг" (Slime.cs) "вызывает" (calls) этот метод "каждый кадр" (every frame).
    /// </summary>
    /// <param name="directionX">Горизонтальное направление (-1.0f или +1.0f)</param>
    public void UpdateFlip(float directionX)
    {
        // (Мы "проверяем" (check) '0.1f', чтобы "избежать" (avoid) "дёрганья" (twitching),
        // если 'directionX' "случайно" (accidentally) '0.01f')
        
        if (directionX > 0.1f) // (Если "смотрим" (facing) "вправо")
        {
            sprite2D.FlipH = false; // (НЕ "отражаем" (flip) спрайт)
        }
        else if (directionX < -0.1f) // (Если "смотрим" (facing) "влево")
        {
            sprite2D.FlipH = true; // ("ОТРАЖАЕМ" (flip) спрайт)
        }
        
        // (Если 'directionX' == 0, мы "ничего не делаем" (do nothing),
        // "сохраняя" (keeping) "старый" (old) "флип")
    }

    /// <summary>
    /// "Публичный" (Public) метод.
    /// ("Мозг" (Slime.cs) "вызывает" (calls) его, когда "срабатывает" (fires) сигнал 'SlimeDied')
    /// </summary>
    public void Hide()
    {
        sprite2D.Hide(); // ("Прячем" (Hide) 'Sprite2D')
    }
    
    // (Здесь "будет" (will live) 'PlayDeath()' или 'PlayJump()',
    // если мы "добавим" (add) 'AnimationPlayer' Слайму)
}