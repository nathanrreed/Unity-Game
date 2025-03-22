using UnityEngine;
using UnityEngine.TextCore.Text;

public class Hit : Ability
{
    private int max_dmg = 5;
    private int min_dmg = 3;

    public Hit(Character character, int start_level)
    {
        AP_Cost = 2;
        owner = character;
        level = start_level;
        is_opposite_cast = true;
        range = 1;
        icon = ImageLookUp("Skills/09_Melee_slash");
    }

    public override bool Action(RaycastHit act_on)
    {
        return Action(act_on.collider.gameObject.GetComponent<Character>());
    }

    public override string GetDesc()
    {
        return "Costs " + AP_Cost + "AP to deal " + min_dmg + "-" + max_dmg + " damage to an enemy within " + range + (range > 1 ? " squares": " square");
    }

    public override bool Action(Character act_on)
    {
        if(InRange(act_on.transform.position, range) && owner.TryUseAP(AP_Cost))
        {
            owner.SetActing(owner.SetAnimation("attack"));  
            act_on.DealDamage(Random.Range(min_dmg, max_dmg));
            return true;
        }
        else
        {
            return false;
        }
    }
}
