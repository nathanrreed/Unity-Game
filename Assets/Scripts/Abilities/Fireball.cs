using Unity.VisualScripting.FullSerializer;
using UnityEditor;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public class Fireball : Ability
{
    private int max_dmg = 5;
    private int min_dmg = 4;
    private int slot;
    private Vector3 pos;

    public Fireball(Character character, int start_level, int set_slot)
    {
        AP_Cost = 5;
        owner = character;
        level = start_level;
        global_cast = true;
        icon = ImageLookUp("Skills/01_Fireball");
        slot = set_slot;
        aoe = 3;
    }

    public override string GetDesc()
    {
        return "Costs " + AP_Cost + "AP to deal " + min_dmg + "-" + max_dmg + " damage to all characters in a " + aoe + "x" + aoe + " area";
    }

    public override bool Action(RaycastHit act_on)
    {
        float x_point = Mathf.Floor(act_on.point.x + 0.5f);
        float z_point = Mathf.Floor(act_on.point.z + 0.5f);

        pos = new Vector3(x_point, 0, z_point);
        if (owner.CheckAP(AP_Cost))
        {
            owner.SetActing(true);
            owner.SetAnimationSpeed("cast", 1.5f);
            owner.SetAnimation("cast");

            owner.acting_slot = slot;

            return true;
        }
        else
        {
            return false;
        }
    }

    public override void OnAnimationEnd()
    {
        Grid grid = Camera.main.GetComponent<Grid>();
        grid.InstantiateItem((GameObject)AssetDatabase.LoadAssetAtPath("Assets/PyroParticles/Prefab/Prefab/Firebolt.prefab", typeof(GameObject)), pos, Quaternion.identity, 0);
        owner.TryUseAP(AP_Cost);
        foreach (var i in GetCharactersInRange(GenerateRange(pos, aoe)))
        {
            i.DealDamage(Random.Range(min_dmg, max_dmg));
        }
    }

    public override int IsValidTarget(RaycastHit[] hits)
    {
        string opposite_tag = GetOppositeTag(owner.tag);
        int i = 0;
        foreach (var hit in hits)
        {
            if (hit.collider.gameObject.name == "Plane")
            {
                return i;
            }
            i++;
        }
        return -1;
    }
}
