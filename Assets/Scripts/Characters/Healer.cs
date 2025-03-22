using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.PlayerSettings;
using static UnityEngine.UI.GridLayoutGroup;

public class Healer : AI
{
    void Start()
    {
        SetupHP(12);
        SetupAP(4, 1);

        Dictionary<string, string> animations = new Dictionary<string, string>();
        animations.Add("idle", "idle");
        animations.Add("attack", "attack");
        animations.Add("cast", "dance");
        animations.Add("walk", "run");
        animations.Add("die", "die");
        animations.Add("shoot", "idle");

        SetAnimations(animations);

        abilities = new Ability[] { new Heal(this, 1), null, null, null };
    }

    void Update()
    {
        GenericUpdate();

        if (!GetControlled())
            return;

        if (!GetActing() && !GetMoving() && !waiting && !GetTurnOver())
        {
            StartCoroutine("ChooseAction");
        }
    }

    public override GameObject GetNearest()
    {
        float lowest_hp = 1;
        GameObject lowest_hp_enemy = null;
        Character temp_char;
        foreach (var i in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            temp_char = i.GetComponent<Character>();
            if (temp_char.IsAlive() && temp_char.GetHP() / temp_char.GetMaxHP() < lowest_hp)
            {
                lowest_hp = temp_char.GetHP() / temp_char.GetMaxHP();
                lowest_hp_enemy = i;
            }
        }

        if (lowest_hp_enemy != null)
        {
            return lowest_hp_enemy;
        }
        else // Get the corner of the map farthest from any player
        {
            Vector3 pos = new Vector3(0,0,0);
            int max_dist = int.MinValue;
            int temp_dist = 0;
            foreach (var corner in grid.corners)
            {
                temp_dist = 0;
                foreach (var i in GameObject.FindGameObjectsWithTag("Player"))
                { 
                    if (i.GetComponent<Character>().IsAlive())
                    {
                        temp_dist += GetDistanceChessboard(corner, i.transform.position);
                    }
                }

                if (max_dist < temp_dist)
                {
                    max_dist = temp_dist;
                    pos = corner;
                }
            }
            grid.target_obj.transform.position = new Vector3(Mathf.Floor(pos.x), 0, Mathf.Floor(pos.z));
            return grid.target_obj;
        }
    }

    public override bool ChooseAbility(GameObject nearest)
    {
        if (nearest.GetComponent<Character>() != null && CheckAP(abilities[0].AP_Cost) && abilities[0].InRange(nearest.transform.position)) // Heal
        {
            SetAnimation("cast");
            abilities[0].Action(nearest.GetComponent<Character>());
            SetActing(true);
        }
        /*else if (CheckAP(abilities[3].AP_Cost) && abilities[3].AP_Cost == GetAP()) // Hunker
        {
            abilities[3].Action(new RaycastHit());
            SetActing(true);
        }*/
        else
        {
            return false;
        }
        return true;
    }
}
