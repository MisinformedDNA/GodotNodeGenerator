using Godot;
using GodotNodeGenerator;

[NodeGenerator("../Main.tscn")]
public partial class MainScriptPath : Node2D
{

    public override void _Ready()
    {
        var generated = new MainScriptPath();
        // Player node doesn't conflict with the class name, so no suffix
        GD.Print($"Player node type: {generated.Player.GetType().Name}");
    }
}

[NodeGenerator("Main.tscn")]
public partial class MainScriptName : Node2D
{

    public override void _Ready()
    {
        var generated = new MainScriptName();
        // Player node doesn't conflict with the class name, so no suffix
        GD.Print($"Player node type: {generated.Player.GetType().Name}");
    }
}

[NodeGenerator]
public partial class Main : Node2D
{

    public override void _Ready()
    {
        var generated = new Main();
        // Player node doesn't conflict with Main class name, but Main node does, so it gets MainNode
        GD.Print($"Player node type: {generated.Player.GetType().Name}");
    }
}

[NodeGenerator("NotFound.tscn")]
public partial class NotFound : Node2D
{

    public override void _Ready()
    {
        var generated = new NotFound();
    }
}
