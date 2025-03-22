using UnityEditor;
using UnityEngine;

public class Bleed : Effect
{
    int damage = 1;
    public Bleed(Character character) : base(character)
    {
        healable = true;
        turns_left = 3;
        end_of_turn = false;
        sprite = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/Basic_RPG_Icons/Skills/16_life_drain.png", typeof(Sprite));
    }
    public override string GetDesc()
    {
        return "Take " + damage + " damage for " + turns_left + " turns";
    }

    public override void OnTurnStartAction()
    {
        owner.DealDamage(damage, true);
    }
}
