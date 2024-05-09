using Godot;
using System;

[Tool]
public class PC : Node2D
{
    public AnimatedSprite sprite;
    private enum classes {HERO, SOLDIER, SAGE, JESTER, WIZARD, MERCHANT, PRIEST, FIGHTER};
    private classes currentClass;
    [Export] private classes _currentClass {
        get{return currentClass;}
        set{currentClass = value;
        setClass(value);}
    }

    public override void _Ready() {
        if(sprite == null) {
            sprite = (AnimatedSprite)GetChild(0);
        }
    }

    private void setClass(classes value) {
        for(int i = 0; i < GetChildCount(); i++) {
            AnimatedSprite aspr = (AnimatedSprite)GetChild(i);
            aspr.Hide();
        }

        sprite = (AnimatedSprite)GetChild((int)value);
        sprite.Show();
        currentClass = value;
    }
}
