using UnityEngine;
using UnityEngine.TextCore.Text;

public class Silence : Ability
{
    public Silence(Character character, int start_level)
    {
        AP_Cost = 3;
        owner = character;
        level = start_level;
        is_opposite_cast = true;
        range = 6;
        icon = ImageLookUp("Skills/15_curse");
    }

    public override bool Action(RaycastHit act_on)
    {
        return Action(act_on.collider.gameObject.GetComponent<Character>());
    }

    public override string GetDesc()
    {
        return "Costs " + AP_Cost + "AP to silence an enemy within " + range + (range > 1 ? " squares" : " square" + " for 1 turn");
    }

    public override bool Action(Character act_on)
    {
        if (InRange(act_on.transform.position, range) && owner.TryUseAP(AP_Cost))
        {
            owner.SetActing(owner.SetAnimation("cast"));
            AddEffect(new Silenced(act_on), act_on);
            return true;
        }
        else
        {
            return false;
        }
    }
}
