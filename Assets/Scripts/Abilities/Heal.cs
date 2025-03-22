using UnityEngine;
using UnityEngine.TextCore.Text;

public class Heal : Ability
{
    private int max_heal = 5;
    private int min_heal = 5;

    public Heal(Character character, int start_level)
    {
        AP_Cost = 3;
        owner = character;
        level = start_level;
        is_same_cast = true;
        range = 3;
        icon = ImageLookUp("Skills/13_Healing_spell_2");
    }

    public override bool Action(RaycastHit act_on)
    {
        return Action(act_on.collider.gameObject.GetComponent<Character>());
    }

    public override string GetDesc()
    {
        return "Costs " + AP_Cost + "AP to heal " + min_heal + " HP to a player within " + range + (range > 1 ? " squares" : " square");
    }

    public override bool Action(Character act_on)
    {
        if (InRange(act_on.transform.position, range) && owner.TryUseAP(AP_Cost))
        {
            owner.SetActing(true);
            owner.SetAnimation("cast");
            act_on.HealDamage(Random.Range(min_heal, max_heal));
            return true;
        }
        else
        {
            return false;
        }
    }
}
