using UnityEngine;
using UnityEngine.TextCore.Text;

public class Shoot : Ability
{
    private int max_dmg = 5;
    private int min_dmg = 2;

    public Shoot(Character character, int start_level)
    {
        AP_Cost = 3;
        owner = character;
        level = start_level;
        is_opposite_cast = true;
        range = 6;
        icon = ImageLookUp("Items/Weapons/01_Bow");
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
            owner.SetActing(owner.SetAnimation("shoot"));
            act_on.DealDamage(Random.Range(min_dmg, max_dmg));

            owner.SpawnProjectile(owner.transform.position + new Vector3(0, 0.5f, 0), act_on.transform.position + new Vector3(0, 0.5f, 0), "Arrow", 1);

            return true;
        }
        else
        {
            return false;
        }
    }
}
