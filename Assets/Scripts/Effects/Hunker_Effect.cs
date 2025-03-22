using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class Hunker_Effect : Effect {

    int damage_reduction = 1;

    public Hunker_Effect(Character character) : base(character)
    {
        end_of_turn = false;
        turns_left = 1;
        sprite = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/Basic_RPG_Icons/Items/Weapons/05_Shield.png", typeof(Sprite));
    }

    public override string GetDesc()
    {
        return "Reduces damage taken from enemies by " + damage_reduction + " for " + turns_left + " turns";
    }

    public override int OnDamaged(int dmg)
    {
        if (owner.GetTurnOver())
            return dmg - damage_reduction;
        else
        {
            return dmg;
        }
    }
}
