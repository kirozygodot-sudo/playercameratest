using System.Collections.Generic;
using System.Numerics;
using Virabis.Movement.Core.Modifiers;

namespace Virabis.Movement.Core;

/// <summary>
/// "Sudo" Yetkili Movement Decorator.
/// Mevcut MovementSystem'i sarmalayarak admin yetkilerini (ölümsüzlük, sınırsız zıplama vb.) ekler.
/// </summary>
public sealed class SudoMovementDecorator : IMovementSystem
{
    private readonly IMovementSystem _inner;
    
    // Sudo Yetkileri
    public bool GodMode { get; set; } = false;
    public bool InfiniteJumps { get; set; } = false;
    public bool NoClip { get; set; } = false;
    public float SpeedMultiplier { get; set; } = 1.0f;

    public SudoMovementDecorator(IMovementSystem inner)
    {
        _inner = inner;
    }

    public MovementState CurrentState => _inner.CurrentState;
    public int JumpsRemaining => InfiniteJumps ? 999 : _inner.JumpsRemaining;

    public MovementResult Update(MovementContext ctx)
    {
        // NoClip durumu: Yer çekimi ve çarpışma yok sayılır (Bridge tarafında yönetilir)
        var modifiedCtx = ctx with {
            IsFlying = NoClip || ctx.IsFlying,
            IsSprinting = ctx.IsSprinting,
            JumpRequested = ctx.JumpRequested || (InfiniteJumps && ctx.JumpRequested)
        };

        var result = _inner.Update(modifiedCtx);

        // Hız çarpanı uygula
        if (SpeedMultiplier != 1.0f)
        {
            result = result with { NewVelocity = result.NewVelocity * SpeedMultiplier };
        }

        // GodMode: Gravity her zaman devre dışı kalabilir (NoClip ise)
        if (NoClip)
        {
            result = result with { DisableGravity = true };
        }

        return result;
    }

    public bool AddModifier(IMovementModifier modifier) => _inner.AddModifier(modifier);
    public void RemoveModifier(IMovementModifier modifier) => _inner.RemoveModifier(modifier);
    public void ClearModifiers() => _inner.ClearModifiers();
    public bool HasModifier<T>() where T : IMovementModifier => _inner.HasModifier<T>();
    public IReadOnlyList<string> GetModifierLabels() 
    {
        var labels = new List<string>(_inner.GetModifierLabels());
        if (GodMode) labels.Add("SUDO: GodMode");
        if (NoClip) labels.Add("SUDO: NoClip");
        if (InfiniteJumps) labels.Add("SUDO: InfiniteJumps");
        return labels;
    }
    public IReadOnlyList<IMovementModifier> GetModifiers() => _inner.GetModifiers();
    public void SetConfig(MovementConfig cfg) => _inner.SetConfig(cfg);
    public MovementConfig GetConfig() => _inner.GetConfig();
}
