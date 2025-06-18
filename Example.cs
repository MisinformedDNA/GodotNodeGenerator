using System;
using Godot;
using GodotNodeGenerator;

namespace GodotExample
{
    // This attribute tells the generator to generate node accessor properties
    // for the Player class based on the specified scene file
    [NodeGenerator("res://scenes/Player.tscn")]
    public partial class Player : CharacterBody2D
    {
        // The source generator will generate properties for all nodes in the scene
        // For example:
        //
        // private Sprite2D? _Sprite;
        // public Sprite2D Sprite => _Sprite ??= GetNode<Sprite2D>("Sprite");
        //
        // private Camera2D? _Camera;
        // public Camera2D Camera => _Camera ??= GetNode<Camera2D>("Camera");
        
        public override void _Ready()
        {
            // With the generated code, you can access nodes directly as properties
            // instead of using GetNode<T>() every time
            Sprite.Texture = ResourceLoader.Load<Texture2D>("res://assets/player.png");
            Camera.Current = true;
            
            // This is much cleaner and safer than:
            // GetNode<Sprite2D>("Sprite").Texture = ...
            // GetNode<Camera2D>("Camera").Current = true;
        }
    }
}
