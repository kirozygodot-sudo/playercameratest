using System.Numerics;
using Virabis.Movement.Core;
using Virabis.Movement.Core.Modifiers;
using Xunit;

namespace Virabis.Movement.Tests;

public class MovementSystemTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static MovementSystem Make(MovementConfig? cfg = null) => new(cfg);

    private static MovementContext Ctx(
        Vector3? dir       = null,
        bool onFloor       = true,
        bool sprinting     = false,
        bool flying        = false,
        bool jumpReq       = false,
        int  jumpsLeft     = 1,
        float dt           = 0.016f,
        Vector3? velocity  = null) => new()
    {
        InputDirection  = dir      ?? Vector3.Zero,
        IsOnFloor       = onFloor,
        IsSprinting     = sprinting,
        IsFlying        = flying,
        JumpRequested   = jumpReq,
        JumpsRemaining  = jumpsLeft,
        DeltaTime       = dt,
        CurrentVelocity = velocity ?? Vector3.Zero
    };

    // ── State ─────────────────────────────────────────────────────────────────

    [Fact] public void Idle_No_Input()
        => Assert.Equal(MovementState.Idle, Make().Update(Ctx()).State);

    [Fact] public void Moving_Has_Input()
        => Assert.Equal(MovementState.Moving, Make().Update(Ctx(dir: Vector3.UnitX)).State);

    [Fact] public void Sprinting()
        => Assert.Equal(MovementState.Sprinting,
            Make().Update(Ctx(dir: Vector3.UnitX, sprinting: true)).State);

    [Fact] public void Airborne()
        => Assert.Equal(MovementState.Airborne,
            Make().Update(Ctx(dir: Vector3.UnitX, onFloor: false)).State);

    [Fact] public void Flying_State()
        => Assert.Equal(MovementState.Flying,
            Make().Update(Ctx(dir: Vector3.UnitX, flying: true)).State);

    [Fact] public void Flying_DisablesGravity()
        => Assert.True(Make().Update(Ctx(flying: true)).DisableGravity);

    // ── Jump ─────────────────────────────────────────────────────────────────

    [Fact] public void Jump_Consumed_When_Has_Jumps()
    {
        var sys    = Make();
        var result = sys.Update(Ctx(dir: Vector3.UnitX, jumpReq: true, jumpsLeft: 1));
        Assert.True(result.JumpConsumed);
        Assert.Equal(0, sys.JumpsRemaining);
    }

    [Fact] public void Jump_Blocked_When_No_Jumps()
    {
        var sys    = Make();
        var result = sys.Update(Ctx(jumpReq: true, jumpsLeft: 0));
        Assert.False(result.JumpConsumed);
    }

    [Fact] public void DoubleJump_Config_Allows_Two()
    {
        var sys = Make(MovementConfig.DoubleJump);
        sys.Update(Ctx(dir: Vector3.UnitX, jumpReq: true, jumpsLeft: 2, onFloor: false));
        Assert.Equal(1, sys.JumpsRemaining);
        sys.Update(Ctx(dir: Vector3.UnitX, jumpReq: true, jumpsLeft: 1, onFloor: false));
        Assert.Equal(0, sys.JumpsRemaining);
    }

    [Fact] public void Jump_Resets_On_Floor()
    {
        var sys = Make(MovementConfig.DoubleJump);
        sys.Update(Ctx(jumpReq: true, jumpsLeft: 2, onFloor: false));
        sys.Update(Ctx(jumpReq: true, jumpsLeft: 1, onFloor: false));
        Assert.Equal(0, sys.JumpsRemaining);
        sys.Update(Ctx(onFloor: true));
        Assert.Equal(2, sys.JumpsRemaining);
    }

    // ── Velocity ─────────────────────────────────────────────────────────────

    [Fact] public void Friction_Decays_Velocity()
    {
        var result = Make().Update(Ctx(velocity: new Vector3(6f, 0f, 0f)));
        Assert.True(result.NewVelocity.Length() < 6f);
    }

    [Fact] public void Sprint_Capped_At_MaxSpeed()
    {
        var result = Make().Update(Ctx(dir: Vector3.UnitX, sprinting: true,
            dt: 1.0f, velocity: Vector3.UnitX * 100f));
        Assert.True(result.NewVelocity.Length() <= MovementConfig.Default.MaxSpeedCap + 0.01f);
    }

    [Fact] public void Velocity_Y_Always_Zero()
    {
        var result = Make().Update(Ctx(dir: new Vector3(1f, 5f, 1f)));
        Assert.Equal(0f, result.NewVelocity.Y, precision: 4);
    }

    // ── Modifiers ────────────────────────────────────────────────────────────

    [Fact] public void Stun_Zeroes_Velocity()
    {
        var sys = Make();
        sys.AddModifier(new StunModifier(1f));
        var result = sys.Update(Ctx(dir: Vector3.UnitX, velocity: new Vector3(6f, 0f, 0f)));
        Assert.Equal(Vector3.Zero, result.NewVelocity);
    }

    [Fact] public void Slow_Reduces_Velocity()
    {
        var sys = Make();
        sys.AddModifier(new SlowModifier(0.5f, 1f));
        var result = sys.Update(Ctx(dir: Vector3.UnitX, velocity: new Vector3(6f, 0f, 0f)));
        Assert.True(result.NewVelocity.Length() < 4f);
    }

    [Fact] public void MaxModifiers_Guard()
    {
        var cfg = MovementConfig.Default with { MaxModifiers = 2 };
        var sys = Make(cfg);
        Assert.True(sys.AddModifier(new StunModifier(1f)));
        Assert.True(sys.AddModifier(new StunModifier(1f)));
        Assert.False(sys.AddModifier(new StunModifier(1f)));  // Reddedilmeli
    }

    [Fact] public void Expired_Modifier_Auto_Removed()
    {
        var sys = Make();
        sys.AddModifier(new StunModifier(0.001f));
        sys.Update(Ctx(dt: 1.0f));  // expire
        Assert.False(sys.HasModifier<StunModifier>());
    }

    // ── Air Control ───────────────────────────────────────────────────────────

    [Fact] public void Air_Control_Produces_Velocity()
        => Assert.True(Make().Update(Ctx(dir: Vector3.UnitX, onFloor: false)).NewVelocity.Length() > 0f);

    [Fact] public void Ground_Accel_Greater_Than_Air()
    {
        var ground = Make().Update(Ctx(dir: Vector3.UnitX, onFloor: true)).NewVelocity.Length();
        var air    = Make().Update(Ctx(dir: Vector3.UnitX, onFloor: false)).NewVelocity.Length();
        Assert.True(ground > air);
    }
}
