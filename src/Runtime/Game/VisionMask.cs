using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace BlindSpire.Game;

/// <summary>
/// In combat the player only sees their own character and the area around it
/// (spec 0.01 4.1): a fullscreen fog with a soft circular hole around the player.
/// </summary>
internal sealed partial class VisionMask : CanvasLayer
{
    private const string ShaderCode = """
        shader_type canvas_item;
        uniform vec2 hole_center = vec2(0.5, 0.5);
        uniform float hole_radius = 0.30;
        uniform float softness = 0.12;
        uniform float aspect = 1.777;
        void fragment() {
            vec2 p = vec2((UV.x - hole_center.x) * aspect, UV.y - hole_center.y);
            float d = length(p);
            float a = smoothstep(hole_radius, hole_radius + softness, d);
            COLOR = vec4(0.0, 0.0, 0.0, a);
        }
        """;

    private ColorRect _rect = null!;
    private ShaderMaterial _material = null!;
    private NCreature? _playerNode;
    private Vector2 _lastCenter = new(-1f, -1f);
    private float _lastAspect = -1f;

    public override void _Ready()
    {
        Layer = 90;
        _material = new ShaderMaterial { Shader = new Shader { Code = ShaderCode } };
        _rect = new ColorRect
        {
            Name = "BlindSpireVisionMask",
            Color = Colors.White,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Material = _material,
        };
        _rect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(_rect);
        _rect.Visible = false;
    }

    public override void _Process(double delta)
    {
        try
        {
            ProcessCore(delta);
        }
        catch (Exception ex)
        {
            PatchGuard.Run("VisionMask.Process", () => throw ex);
        }
    }

    private void ProcessCore(double delta)
    {
        if (ModRuntime.Disabled || NCombatRoom.Instance is not { } room || !IsInstanceValid(room))
        {
            _rect.Visible = false;
            return;
        }

        if (_playerNode == null || !IsInstanceValid(_playerNode))
        {
            _playerNode = FindLocalPlayerNode(room);
        }
        var player = _playerNode;
        if (player == null)
        {
            _rect.Visible = false;
            return;
        }

        var viewportSize = GetViewport().GetVisibleRect().Size;
        if (viewportSize.X <= 0f || viewportSize.Y <= 0f)
        {
            return;
        }

        var center = player.GetGlobalRect().GetCenter() / viewportSize;
        var aspect = viewportSize.X / viewportSize.Y;
        if (!center.IsEqualApprox(_lastCenter) || !Mathf.IsEqualApprox(aspect, _lastAspect))
        {
            _material.SetShaderParameter("hole_center", center);
            _material.SetShaderParameter("aspect", aspect);
            _lastCenter = center;
            _lastAspect = aspect;
        }
        _rect.Visible = true;
    }

    private static NCreature? FindLocalPlayerNode(NCombatRoom room)
    {
        foreach (var node in room.CreatureNodes)
        {
            if (node.Entity is { IsPlayer: true } && LocalContext.IsMe(node.Entity))
            {
                return node;
            }
        }
        return null;
    }
}
