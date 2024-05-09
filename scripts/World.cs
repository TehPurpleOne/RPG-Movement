using Godot;
using System;
using System.Collections.Generic;

public class World : Node2D
{
    private ColorRect backColor;
    private TileMap map;
    private Tween mover;
    private CanvasLayer debugLayer;
    private Label debugLabel;

    private enum states {NULL, INIT, IDLE, CHECKTILE, MOVE, EVENTCHECK, BATTLE}; // States for the world map.
    private states state; // Current state.
    private states previousState; // Previous state.

    private List<PC> party = new List<PC>(); // List of party members.
    private List<int> invalidTiles = new List<int>(); // List of tiles that are invalid.

    private Vector2 inputDir = Vector2.Zero; // Direction the player has pressed.
    private int delayMovement = 0; // Delay the movement of the party.
    private int swampFlashDelay = 0;
    private int randomBattle = 0;

    [Export] Vector2 startPos; // Starting position of the party. Set via Godot Editor.
    [Export] float tileMoveTime = 0.5f;

    private bool debugMode = false;

    public override void _Ready() {
        // Get the TileMap and Tween nodes.
        backColor = (ColorRect)GetNode("Background/ColorRect");
        map = (TileMap)GetNode("Background/Map");
        mover = (Tween)GetNode("Tween");
        debugLayer = (CanvasLayer)GetNode("Debug");
        debugLabel = (Label)debugLayer.GetNode("Label");

        // Set unwalkable tiles. Make sure they are listed by their tile ID and NOT their tile name.
        invalidTiles.Add(5);
        invalidTiles.Add(12);
        invalidTiles.Add(13);
        invalidTiles.Add(14);
        invalidTiles.Add(15);
        invalidTiles.Add(16);
        invalidTiles.Add(17);
        invalidTiles.Add(18);
        invalidTiles.Add(19);
        invalidTiles.Add(20);
        invalidTiles.Add(21);
        invalidTiles.Add(22);
        invalidTiles.Add(23);
        invalidTiles.Add(24);
        invalidTiles.Add(25);
        invalidTiles.Add(26);

        SetState(states.INIT);
    }

    public override void _Input(InputEvent @event) {
        // Limit movement to only 4 directions.
        if(Input.IsActionPressed("ui_up")) {
            inputDir = Vector2.Up;
        } else if(Input.IsActionPressed("ui_down")) {
            inputDir = Vector2.Down;
        } else if(Input.IsActionPressed("ui_left")) {
            inputDir = Vector2.Left;
        } else if(Input.IsActionPressed("ui_right")) {
            inputDir = Vector2.Right;
        } else {
            inputDir = Vector2.Zero;
        }

        if(Input.IsActionJustPressed("ui_accept")) debugMode = !debugMode;
    }

    public override void _PhysicsProcess(float delta) {
        if(state != states.NULL) {
            StateLogic(delta);
            states g = GetTransition(delta);
            if(g != states.NULL) {
                SetState(g);
            }
        }
    }

    private void StateLogic(float delta) {
        // Delay the movement of the party.
        if(delayMovement > 0) delayMovement--;
        if(swampFlashDelay > 0) swampFlashDelay--;

        if(swampFlashDelay == 0 && backColor.Color != Color.Color8(0, 0, 0, 255)) backColor.Color = Color.Color8(0, 0, 0, 255);

        if(!debugMode && debugLayer.Visible) debugLayer.Hide();
        if(debugMode && !debugLayer.Visible) debugLayer.Show();

        if(debugMode) { // This window will display various information about the PC sprites, the tiles they overlap, along with how many steps until the next battle.
            string debugInfo = "";

            for(int i = 0; i < party.Count; i++) {
                debugInfo += party[i].Name + "\n";
                debugInfo += "  GPos:" + new Vector2((float)Math.Round(party[i].GlobalPosition.x), (float)Math.Round(party[i].GlobalPosition.y)) + "\n";
                debugInfo += "  TPos:" + map.WorldToMap(party[i].GlobalPosition) + "\n";
                debugInfo += "  TID: " + map.GetCellv(map.WorldToMap(party[i].GlobalPosition)) + "\n";
            }

            debugInfo += "\nNext Battle: " + randomBattle + "\n"; 
            debugInfo += "Move Delay:     " + delayMovement + "\n";
            debugInfo += "Swamp Flash:    " + swampFlashDelay + "\n";

            if(debugLabel.Text != debugInfo) debugLabel.Text = debugInfo;
        }
    }

    private states GetTransition( float delta) {
        states check = states.NULL;

        switch(state) {
            case states.INIT:
                if(party.Count > 0) return states.IDLE;
                break;
            
            case states.IDLE:
                if(inputDir != Vector2.Zero && delayMovement == 0) return states.CHECKTILE;
                break;
            
            case states.BATTLE:
                if(delayMovement == 0) {
                    PlaySound("Victory");
                    UpdateConsole("Thou hast vanquished the enemy!");
                    SetState(states.IDLE);
                }
                break;
        }

        return check;
    }

    private void EnterState(states newState, states oldState) {
        switch(newState) {
            case states.INIT:
                Node pcs = (Node)GetNode("Sprites"); // Get the category of party members.

                for(int i = 0; i < pcs.GetChildCount(); i++) { // Add party members to the party list and set position to the starting vector.
                    PC player = (PC)pcs.GetChild(i);

                    party.Add(player);
                    player.GlobalPosition = map.MapToWorld(startPos) + map.CellSize / 2;
                }

                party.Reverse(); // Reverse the party list. This is to ensure that the lowest party member in the tree is the first in the list.

                randomBattle = SetRNG();
                break;
            
            
            case states.CHECKTILE:
                // Set the 1st party member's direction to the input direction.
                switch(inputDir) {
                    case Vector2 v when inputDir == Vector2.Up:
                        party[0].sprite.Play("n");
                        UpdateConsole("North");
                        break;

                    case Vector2 v when inputDir == Vector2.Down:
                        party[0].sprite.Play("s");
                        UpdateConsole("South");
                        break;
                    
                    case Vector2 v when inputDir == Vector2.Left:
                        party[0].sprite.Play("w");
                        UpdateConsole("West");
                        break;
                    
                    case Vector2 v when inputDir == Vector2.Right:
                        party[0].sprite.Play("e");
                        UpdateConsole("East");
                        break;
                }

                // Check the tile the party is wanting to move to.
                Vector2 tilePos = map.WorldToMap(party[0].GlobalPosition);
                int desiredTile = map.GetCellv(tilePos + inputDir);

                if(invalidTiles.Contains(desiredTile)) {
                    // Can't move to that tile.
                    PlaySound("Bump");
                    UpdateConsole("Thou canst not go that way!");
                    SetState(states.IDLE);
                } else {
                    // Move the first party member.
                    mover.InterpolateProperty(party[0], "global_position", party[0].GlobalPosition, map.MapToWorld(tilePos + inputDir) + map.CellSize / 2, tileMoveTime);
                    
                    // Move the others based on the previous member's last position and turn them into that direction.
                    for(int i = 1; i < party.Count; i++) {
                        Vector2 followerDir = (map.WorldToMap(party[i - 1].GlobalPosition)) - map.WorldToMap(party[i].GlobalPosition);

                        switch(followerDir) {
                            case Vector2 v when followerDir == Vector2.Up:
                                party[i].sprite.Play("n");
                                break;
                            
                            case Vector2 v when followerDir == Vector2.Down:
                                party[i].sprite.Play("s");
                                break;
                            
                            case Vector2 v when followerDir == Vector2.Left:
                                party[i].sprite.Play("w");
                                break;
                            
                            case Vector2 v when followerDir == Vector2.Right:
                                party[i].sprite.Play("e");
                                break;
                        }

                        mover.InterpolateProperty(party[i], "global_position", party[i].GlobalPosition,party[i - 1].GlobalPosition, tileMoveTime);
                    }

                    mover.Start();
                }
                break;

                case states.EVENTCHECK:
                    // Check the current tile for a town, castle, swamp, or dungeon. Then see if a random battle is triggered.

                    for(int i = 0; i < party.Count; i++) {
                        Vector2 tPos = map.WorldToMap(party[i].GlobalPosition);
                        int tID = map.GetCellv(tPos);

                        switch(tID) { // Check the current tile the party leader, or all of the party are on.
                            case 0:
                            case 1:
                            case 6:
                            case 10:
                                if(i == 0) { // Only the party leader needs to be checked. Use this event to transfer the party to a new map.
                                    string tileName = (string)map.TileSet.TileGetName(tID);
                                    UpdateConsole("Thou hast discovered a " + tileName + "!");
                                    PlaySound("Enter");
                                }
                                break;
                            
                            case 4:
                                if(i == 0) { // Party leader had landed on a hill. Delay movement.
                                    delayMovement = 10;
                                }
                                break;
                            
                            case 8: // Uh oh. The party landed on a swamp tile. Deal damage to all members touching the tiles.
                                swampFlashDelay = 10;
                                delayMovement = 10;
                                backColor.Color = Color.Color8(228, 0, 96, 255);
                                UpdateConsole("Ouch!");
                                PlaySound("Swamp");
                                break;
                        }
                    }

                    randomBattle--;

                    switch(randomBattle) {
                        case 0:
                            SetState(states.BATTLE);
                            break;
                        
                        case int v when randomBattle > 0:
                            SetState(states.IDLE);
                            break;
                    }
                    break;
                
            case states.BATTLE:
                PlaySound("Enemy");
                UpdateConsole("An enemy approaches!");
                randomBattle = SetRNG();
                delayMovement = 120;
                break;
            
        }
    }

    private void ExitState(states oldState, states newState) {
        
    }

    private void SetState(states newState) {
        previousState = state;
        state = newState;

        EnterState(newState, previousState);
        ExitState(previousState, newState);
    }

    private void OnTweenDone() {
        SetState(states.EVENTCHECK);
    }

    private void PlaySound(string name) {
        Node sfx = (Node)GetNode("Sounds");

        for(int i = 0; i < sfx.GetChildCount(); i++) {
            if(sfx.GetChild(i).Name == name) {
                AudioStreamPlayer snd = (AudioStreamPlayer)sfx.GetChild(i);

                if(snd.Playing && snd.Name != "Swamp") return;

                snd.Play();
            }
        }
    }

    private void UpdateConsole(string text) {
        Label console = (Label)GetNode("Console/Label");
        console.Text += text + "\n";

        if(console.GetLineCount() > 6) console.LinesSkipped = console.GetLineCount() - 6;
    }

    private int SetRNG() {
        Random RNGesus = new Random();
        int value = RNGesus.Next(5, 64);
        return value;
    }
}
