using Godot;
using System.Collections.Generic; // Для Списка (List)

/// <summary>
/// Управляет всеми "idle" анимациями.
/// Получает "команду" от Player.cs, когда можно начинать работать.
/// </summary>
public partial class IdleAnimationHandler : Node
{
    // --- ССЫЛКИ ---
    private AnimationPlayer _animationPlayer;
    private Timer _fidgetTimer;

    // --- НАСТРОЙКИ ---
    [Export] private float _minIdleWaitTime = 5.0f; // Мин. время до "особой" анимации
    [Export] private float _maxIdleWaitTime = 10.0f; // Макс. время

    // Наш список "особых" анимаций
    private List<string> _specialIdleAnims = new List<string>
    {
        "idle_fidget",
        "idle_scratch",
        "idle_scuff"
    };

    // --- СОСТОЯНИЕ ---
    private RandomNumberGenerator _rng = new RandomNumberGenerator();
    private bool _isHandlingIdle = false; // Флаг, что мы "главные" за анимацию

    public override void _Ready()
    {
        // Получаем AnimationPlayer от родителя (Player)
        _animationPlayer = GetParent().GetNode<AnimationPlayer>("AnimationPlayer");
        if (_animationPlayer == null)
        {
            GD.PrintErr("IdleAnimationHandler: Не могу найти 'AnimationPlayer' у родителя!");
            return;
        }

        // Создаем наш внутренний таймер
        _fidgetTimer = new Timer();
        _fidgetTimer.Name = "FidgetTimer";
        _fidgetTimer.OneShot = true;
        AddChild(_fidgetTimer);

        // Подключаем сигналы
        _fidgetTimer.Timeout += _OnFidgetTimerTimeout;
        _animationPlayer.AnimationFinished += _OnAnimationPlayerAnimationFinished;
    }

    /// <summary>
    /// Player.cs вызывает это, когда персонаж стоит на месте.
    /// </summary>
    public void StartIdle()
    {
        if (_isHandlingIdle) return; // Мы уже работаем

        _isHandlingIdle = true;
        
        // Запускаем основное дыхание
        _animationPlayer.Play("idle_main"); 
        
        // Запускаем таймер до следующей "особой" анимации
        StartFidgetTimer();
    }

    /// <summary>
    /// Player.cs вызывает это, когда персонаж начинает бежать, прыгать или атаковать.
    /// </summary>
    public void StopIdle()
    {
        if (!_isHandlingIdle) return; // Мы и так не работаем
        
        _isHandlingIdle = false;
        _fidgetTimer.Stop();
    }

    // Запускает таймер на случайное время
    private void StartFidgetTimer()
    {
        _fidgetTimer.WaitTime = _rng.RandfRange(_minIdleWaitTime, _maxIdleWaitTime);
        _fidgetTimer.Start();
    }

    // Таймер сработал - время для "особой" анимации
    private void _OnFidgetTimerTimeout()
    {
        // Если Player.cs все еще разрешает нам (т.е. мы не начали бежать)
        if (_isHandlingIdle)
        {
            // Выбираем случайную анимацию
            string nextAnim = _specialIdleAnims[_rng.RandiRange(0, _specialIdleAnims.Count - 1)];
            _animationPlayer.Play(nextAnim);
        }
    }

    // Анимация закончилась
    private void _OnAnimationPlayerAnimationFinished(StringName animName)
    {
        string animNameStr = animName.ToString();

        // Если это была одна из наших "особых" анимаций И нам все еще можно
        if (_isHandlingIdle && _specialIdleAnims.Contains(animNameStr))
        {
            // Возвращаемся к дыханию
            _animationPlayer.Play("idle_main");
            
            // И снова запускаем таймер
            StartFidgetTimer();
        }
    }
}