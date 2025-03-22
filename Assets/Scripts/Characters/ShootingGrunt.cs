using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ShootingGrunt : AI
{
    void Start()
    {
        SetupHP(4);
        SetupAP(6, 1);

        Dictionary<string, string> animations = new Dictionary<string, string>();
        animations.Add("idle", "idle");
        animations.Add("attack", "attack");
        animations.Add("cast", "dance");
        animations.Add("walk", "run");
        animations.Add("die", "die");
        animations.Add("shoot", "idle");

        SetAnimations(animations);

        abilities = new Ability[] { new Hit(this, 1), new Shoot(this, 1), null, new Hunker(this, 1) };
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

    public override bool ChooseAbility(GameObject nearest)
    {
        if (CheckAP(abilities[0].AP_Cost) && abilities[0].InRange(nearest.transform.position)) // Hit
        {
            SetAnimation("attack");
            abilities[0].Action(nearest.GetComponent<Character>());
            SetActing(true);
        }
        else if (CheckAP(abilities[1].AP_Cost) && abilities[1].InRange(nearest.transform.position)) // Shoot
        {
            SetAnimation("shoot");
            abilities[1].Action(nearest.GetComponent<Character>());
            SetActing(true);
        }
        else if (CheckAP(abilities[3].AP_Cost) && abilities[3].AP_Cost == GetAP()) // Hunker
        {
            abilities[3].Action(new RaycastHit());
            SetActing(true);
        }
        else
        {
            return false;
        }
        return true;
    }
}
