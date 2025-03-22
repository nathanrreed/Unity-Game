using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class Silenced : Effect
{

    public Silenced(Character character) : base(character)
    {
        end_of_turn = true;
        turns_left = 1;
        sprite = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/Basic_RPG_Icons/Skills/15_curse.png", typeof(Sprite));
    }

    public override string GetDesc()
    {
        return "Prevent character from acting for " + turns_left + " turns";
    }
}
