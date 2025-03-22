using UnityEngine;
using UnityEngine.TextCore.Text;

public class Cut : Ability
{
    private int max_dmg = 4;
    private int min_dmg = 2;

    public Cut(Character character, int start_level)
    {
        AP_Cost = 3;
        owner = character;
        level = start_level;
        is_opposite_cast = true;
        range = 1;
        icon = ImageLookUp("Skills/11_Melee_Cone");
    }

    public override bool Action(RaycastHit act_on)
    {
        return Action(act_on.collider.gameObject.GetComponent<Character>());
    }
    public override string GetDesc()
    {
        return "Costs " + AP_Cost + "AP to deal " + min_dmg + "-" + max_dmg + " damage to an enemy within " + range + (range > 1 ? " squares" : " square");
    }

    public override bool Action(Character act_on)
    {
        if (InRange(act_on.transform.position, range) && owner.TryUseAP(AP_Cost))
        {
            owner.SetActing(true);
            owner.SetAnimation("attack");
            AddEffect(new Bleed(act_on), act_on);
            act_on.DealDamage(Random.Range(min_dmg, max_dmg));
            return true;
        }
        else
        {
            return false;
        }
    }
}
