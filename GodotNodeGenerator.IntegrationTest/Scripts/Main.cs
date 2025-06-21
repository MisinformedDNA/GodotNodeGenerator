using Godot;
using GodotNodeGenerator;

public partial class Main : Node2D
{
    [NodeGenerator("../Main.tscn")]
    public partial class MainGenerated : Node2D { }

    public override void _Ready()
    {
        var generated = new MainGenerated();
        GD.Print($"Player node type: {generated.Player.GetType().Name}");
    }
}
