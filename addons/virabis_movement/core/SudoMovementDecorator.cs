using System.Collections.Generic;
using System.Numerics;
using Virabis.Movement.Core.Modifiers;

namespace Virabis.Movement.Core;

/// <summary>
/// "Sudo" Yetkili Movement Decorator.
/// "Ghost Logic" (MovementGates) entegrasyonu ile modernize edildi.
/// </summary>
public sealed class SudoMovementDecorator : IMovementSystem
{
    private readonly IMovementSystem _inner;
    
    // Eski mülkler geriye dönük uyumluluk için duruyor, ancak artık Gate'leri de kontrol ediyor.
    public bool GodMode { get; set; } = false;
    public bool InfiniteJumps { get; set; } = false;
    public bool NoClip { get; set; } = false;
    public float SpeedMultiplier { get; set; } = 1.0f;

    public SudoMovementDecorator(IMovementSystem inner)
    {
        _inner = inner;
        
        // Varsayılan Gate tanımlamaları
        MovementGates.SetGate(MovementGates.GateType.CanFly, () => NoClip || GodMode);
        MovementGates.SetGate(MovementGates.GateType.CanInfiniteJump, () => InfiniteJumps || GodMode);
        MovementGates.SetGate(MovementGates.GateType.IsInvulnerable, () => GodMode);
    }

    public MovementState CurrentState => _inner.CurrentState;
    public int JumpsRemaining => MovementGates.Check(MovementGates.GateType.CanInfiniteJump) ? 999 : _inner.JumpsRemaining;

    public MovementResult Update(MovementContext ctx)
    {
        bool canFly = MovementGates.Check(MovementGates.GateType.CanFly);
        
        var modifiedCtx = ctx with {
            IsFlying = canFly || ctx.IsFlying,
            JumpRequested = ctx.JumpRequested
        };

        var result = _inner.Update(modifiedCtx);

        if (SpeedMultiplier != 1.0f)
        {
            result = result with { NewVelocity = result.NewVelocity * SpeedMultiplier };
        }

        if (canFly)
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
        if (MovementGates.Check(MovementGates.GateType.IsInvulnerable)) labels.Add("GATE: Invulnerable");
        if (MovementGates.Check(MovementGates.GateType.CanFly)) labels.Add("GATE: Flight");
        if (MovementGates.Check(MovementGates.GateType.CanInfiniteJump)) labels.Add("GATE: InfJump");
        return labels;
    }
    
    public IReadOnlyList<IMovementModifier> GetModifiers() => _inner.GetModifiers();
    public void SetConfig(MovementConfig cfg) => _inner.SetConfig(cfg);
    public MovementConfig GetConfig() => _inner.GetConfig();
}
