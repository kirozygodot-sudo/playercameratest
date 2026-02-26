using Godot;

namespace Virabis.Movement.Core;

/// <summary>
/// Tüm movement parametreleri. 
/// GÜNCELLEME: C# tarafında Mathf.Clamp ile veri validasyonu eklendi.
/// </summary>
[GlobalClass]
public partial record MovementConfig : Resource
{
    // ── Hızlar ──────────────────────────────────────────────────────────────
    private float _walkSpeed = 6.0f;
    [Export] public float WalkSpeed 
    { 
        get => _walkSpeed; 
        init => _walkSpeed = Mathf.Max(0.1f, value); 
    }

    private float _sprintSpeed = 9.5f;
    [Export] public float SprintSpeed 
    { 
        get => _sprintSpeed; 
        init => _sprintSpeed = Mathf.Max(0.1f, value); 
    }

    private float _flyingSpeed = 8.0f;
    [Export] public float FlyingSpeed 
    { 
        get => _flyingSpeed; 
        init => _flyingSpeed = Mathf.Max(0.1f, value); 
    }

    // ── Zemin ───────────────────────────────────────────────────────────────
    private float _groundAcceleration = 20.0f;
    [Export] public float GroundAcceleration 
    { 
        get => _groundAcceleration; 
        init => _groundAcceleration = Mathf.Max(0.0f, value); 
    }

    private float _groundFriction = 18.0f;
    [Export] public float GroundFriction 
    { 
        get => _groundFriction; 
        init => _groundFriction = Mathf.Max(0.0f, value); 
    }

    // ── Hava ────────────────────────────────────────────────────────────────
    private float _airAcceleration = 14.0f;
    [Export] public float AirAcceleration 
    { 
        get => _airAcceleration; 
        init => _airAcceleration = Mathf.Max(0.0f, value); 
    }

    private float _airFriction = 2.0f;
    [Export] public float AirFriction 
    { 
        get => _airFriction; 
        init => _airFriction = Mathf.Max(0.0f, value); 
    }

    private float _airControlMultiplier = 0.85f;
    [Export] public float AirControlMultiplier 
    { 
        get => _airControlMultiplier; 
        init => _airControlMultiplier = Mathf.Clamp(value, 0.0f, 2.0f); 
    }

    // ── Yön / Turn ──────────────────────────────────────────────────────────
    private float _turnResponsiveness = 1.2f;
    [Export] public float TurnResponsiveness 
    { 
        get => _turnResponsiveness; 
        init => _turnResponsiveness = Mathf.Max(0.1f, value); 
    }

    private float _sprintTurnMultiplier = 0.6f;
    [Export] public float SprintTurnMultiplier 
    { 
        get => _sprintTurnMultiplier; 
        init => _sprintTurnMultiplier = Mathf.Clamp(value, 0.1f, 1.0f); 
    }

    // ── Zıplama ───────────────────────────────────────────────────────────
    private int _maxJumps = 1;
    [Export] public int MaxJumps 
    { 
        get => _maxJumps; 
        init => _maxJumps = Mathf.Max(0, value); 
    }

    // ── Güvenlik ──────────────────────────────────────────────────────────
    private float _maxSpeedCap = 20.0f;
    [Export] public float MaxSpeedCap 
    { 
        get => _maxSpeedCap; 
        init => _maxSpeedCap = Mathf.Max(1.0f, value); 
    }

    private int _maxModifiers = 8;
    [Export] public int MaxModifiers 
    { 
        get => _maxModifiers; 
        init => _maxModifiers = Mathf.Clamp(value, 1, 32); 
    }
}
