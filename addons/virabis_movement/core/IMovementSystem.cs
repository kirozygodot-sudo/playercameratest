using System.Collections.Generic;
using System.Numerics;
using Virabis.Movement.Core.Modifiers;

namespace Virabis.Movement.Core;

/// <summary>
/// Decorator Pattern için MovementSystem interface'i.
/// </summary>
public interface IMovementSystem
{
    MovementState CurrentState { get; }
    int JumpsRemaining { get; }
    
    MovementResult Update(MovementContext ctx);
    bool AddModifier(IMovementModifier modifier);
    void RemoveModifier(IMovementModifier modifier);
    void ClearModifiers();
    bool HasModifier<T>() where T : IMovementModifier;
    IReadOnlyList<string> GetModifierLabels();
    IReadOnlyList<IMovementModifier> GetModifiers();
    void SetConfig(MovementConfig cfg);
    MovementConfig GetConfig();
}
