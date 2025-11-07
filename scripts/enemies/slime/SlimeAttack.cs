using Godot;

// (Это "Специалист" (Specialist) по "Атаке". Он "наследуется" (inherits) от Node)
public partial class SlimeAttack : Node
{
    // --- "РУЧКИ" (Handles) (Настраиваются в Инспекторе Godot) ---
    [Export] public int AttackDamage = 10; // (Урон, который "наносит" (deals) Слайм)
    
    // --- "Внутренний" (Internal) Флаг ---
    // ('private' - "Мозг" (Brain) "не знает" (doesn't know) об этом флаге,
    // "только" (only) "Таймер" (Timer) "управляет" (controls) им)
    private bool canAttack = true; 

    // --- ССЫЛКИ НА УЗЛЫ ---
    /// <summary>
    /// Сюда (в Инспектор) нужно "перетащить" (drag) узел 'AttackCooldownTimer'.
    /// </summary>
    [Export] private Timer attackCooldownTimer;

    public override void _Ready()
    {
        // ("Проверяем" (Check), "подключили" (linked) ли мы 'Timer' в Инспекторе)
        if (attackCooldownTimer == null)
        {
            GD.PrintErr("SlimeAttack: 'AttackCooldownTimer' не назначен в инспекторе!");
        }
        
        // (Мы "могли" (could) "подключить" (connect) "сигнал" (signal) 'timeout'
        // здесь (в коде), но ты (правильно!) "сделал" (did) это в Редакторе Godot)
    }

    // --- ПУБЛИЧНЫЙ МЕТОД (API) ---

    /// <summary>
    /// "Мозг" (Slime.cs) "вызывает" (calls) этот метод, когда "касается" (collides) Игрока.
    /// Этот "специалист" (specialist) "сам решает" (decides), "может ли" (can) он атаковать.
    /// </summary>
    /// <param name="victimBody">"Тело" (Node), в которое мы "врезались" (collided)</param>
    public void TryToAttack(Node victimBody)
    {
        // (1. "Проверяем" (Check) "перезарядку" (cooldown). Если 'false' - "стоп")
        if (!canAttack)
        {
            return;
        }

        // (2. "Проверяем" (Check), "является ли" (is) "жертва" (victim) Игроком ('Player'))
        if (victimBody is Player victim)
        {
            // --- (Это "исправление" (fix) "краша" (crash), который у нас "был" (was)) ---
            // (Мы "вежливо" (politely) "спрашиваем" (ask) Игрока, "мертв ли он" (is he dead))
            if (victim.get_is_dead())
            {
                return; // ("Не бьём" (Don't hit) "мёртвых" (dead))
            }
            
            // (3. Если "Игрок" (Player) "жив" (alive) - "наносим урон" (deal damage))
            victim.TakeDamage(AttackDamage);
            
            // (4. "Уходим на перезарядку" (Go on cooldown))
            canAttack = false; // ("Выключаем" (Disable) "атаку")
            attackCooldownTimer.Start(); // ("Запускаем" (Start) "таймер")
        }
    }


    // --- ОБРАБОТЧИК СИГНАЛА (Signal Handler) ---
    // (Этот метод "переехал" (moved) из 'Slime.cs')
    // (Godot "вызывает" (calls) его "автоматически", когда 'attackCooldownTimer' "закончится" (finishes))
    private void _on_attack_cooldown_timer_timeout()
    {
        canAttack = true; // ("Разрешаем" (Allow) "атаку" (attack) "снова" (again))
    }
}