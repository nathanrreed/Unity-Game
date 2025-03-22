using UnityEngine;

public class Hunker : Ability
{

    public Hunker(Character character, int start_level)
    {
        AP_Cost = 1;
        owner = character;
        level = start_level;
        is_self_cast = true;
        icon = ImageLookUp("Items/Weapons/05_Shield");
    }

    public override string GetDesc()
    {
        return "Costs " + AP_Cost + "AP to apply Hunker to the player" + "\n" + new Hunker_Effect(owner).GetDesc();
    }
    public override bool Action(RaycastHit act_on)
    {
        if (owner.TryUseAP(AP_Cost))
        {
            AddEffect(new Hunker_Effect(owner), owner);
            if (owner.GetAP() > 0)
                owner.SetNextTurn();

            return true;
        }
        else
        {
            return false;
        }
    }
}
