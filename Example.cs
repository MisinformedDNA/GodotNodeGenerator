using System;
using Godot;
using GodotNodeGenerator;
using System.Diagnostics.CodeAnalysis;

namespace GodotExample
{
    // This attribute tells the generator to generate node accessor properties
    // for the Player class based on the specified scene file
    [NodeGenerator("res://scenes/Player.tscn")]
    public partial class Player : CharacterBody2D
    {
        // The source generator will generate properties for all nodes in the scene
        // with improved type safety and error handling:
        //
        // private Sprite2D? _Sprite;
        // public Sprite2D Sprite 
        // {
        //     get
        //     {
        //         if (_Sprite == null)
        //         {
        //             var node = GetNodeOrNull("Sprite");
        //             if (node == null)
        //             {
        //                 throw new NullReferenceException("Node not found: Sprite");
        //             }
        //             
        //             _Sprite = node as Sprite2D;
        //             if (_Sprite == null)
        //             {
        //                 throw new InvalidCastException($"Node at path {node.GetPath()} is of type {node.GetType()}, not Sprite2D");
        //             }
        //         }
        //         
        //         return _Sprite;
        //     }
        // }
        //
        // // Also generates TryGet methods for safer access:
        // public bool TryGetSprite([NotNullWhen(true)] out Sprite2D? node)
        // {
        //     node = null;
        //     if (_Sprite != null)
        //     {
        //         node = _Sprite;
        //         return true;
        //     }
        //     
        //     var tempNode = GetNodeOrNull("Sprite");
        //     if (tempNode is Sprite2D typedNode)
        //     {
        //         _Sprite = typedNode;
        //         node = typedNode;
        //         return true;
        //     }
        //     
        //     return false;
        // }
        
        public override void _Ready()
        {
            // Safe access with the regular property
            try
            {
                Sprite.Texture = ResourceLoader.Load<Texture2D>("res://assets/player.png");
                Camera.Current = true;
            }
            catch (NullReferenceException ex)
            {
                GD.PrintErr($"Missing node: {ex.Message}");
            }
            catch (InvalidCastException ex)
            {
                GD.PrintErr($"Type error: {ex.Message}");
            }
            
            // Safer access with TryGet methods
            if (TryGetSprite(out var sprite))
            {
                sprite.Texture = ResourceLoader.Load<Texture2D>("res://assets/player.png");
            }
            
            if (TryGetCamera(out var camera))
            {
                camera.Current = true;
            }
        }
    }
}
