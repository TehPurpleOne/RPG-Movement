using Godot;
using System;

[Tool]
public class Camera2D : Godot.Camera2D
{
    [Export] private NodePath focusOn;
    private PC pcToFollow;

    public override void _PhysicsProcess(float delta) {
        if(focusOn != null && pcToFollow == null) {
            pcToFollow = (PC)GetNode(focusOn);
        }

        if(pcToFollow != null) {
            GlobalPosition = pcToFollow.GlobalPosition;
        }
    }
}
