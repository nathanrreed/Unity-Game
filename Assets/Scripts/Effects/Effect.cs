using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Effect
{
    public Character owner;
    public Sprite sprite = null;
    public int turns_left;
    public bool healable = false;
    public bool end_of_turn = false;

    public Effect(Character character)
    {
        owner = character;
    }

    public abstract string GetDesc();

    // By default effects will not do anything
    public virtual int OnDamaged(int dmg)
    {
        return dmg; // Does not change the damage taken by default
    }

    public bool OnTurnEnd()
    {
        if (!end_of_turn)
            return true;
        turns_left -= 1;
        if (turns_left <= 0)
        {
            owner.effects.Remove(this);
            return false;
        }

        OnTurnEndAction();
        return true;
    }

    public bool OnTurnStart()
    {
        if (end_of_turn)
            return true;
        turns_left -= 1;
        if (turns_left <= 0)
        {
            owner.effects.Remove(this);
            return false;
        }

        OnTurnStartAction();
        return true;
    }

    public virtual void OnTurnEndAction()
    {

    }
    public virtual void OnTurnStartAction()
    {

    }
}
