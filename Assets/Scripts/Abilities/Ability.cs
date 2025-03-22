using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public abstract class Ability
{
    public Image img;
    public Character owner;

    public int AP_Cost;
    public int percent_chance = 100;
    public int level;
    public int range;
    public Sprite icon;

    public int aoe = 0;

    public bool is_self_cast = false;
    public bool is_same_cast = false;
    public bool is_opposite_cast = false;
    public bool global_cast = false;

    public abstract string GetDesc();

    public abstract bool Action(RaycastHit act_on);
    public virtual bool Action(Character act_on)
    {
        return false;
    }

    public virtual void OnAnimationEnd()
    {

    }

    public virtual int IsValidTarget(RaycastHit[] hits)
    {
        
        string opposite_tag = GetOppositeTag(owner.tag);
        int i = 0;
        foreach (var hit in hits)
        {
            GameObject target = hit.collider.gameObject;
            if (target.tag.Equals(owner.tag) && is_same_cast && target.GetComponent<Character>().IsAlive())
            {
                return i;
            }
            else if (target.tag.Equals(opposite_tag) && is_opposite_cast && target.GetComponent<Character>().IsAlive())
            {
                return i;
            }
            i++;
        }
        return -1;
    }

    public virtual bool InRange(Vector3 target, int range)
    {
        if (owner.GetDistanceChessboard(target) <= range)
        {
            return true;
        }
        return false;
    }

    public virtual bool InRange(Vector3 target)
    {
        return InRange(target, range);
    }

    public virtual string GetOppositeTag(string tag)
    {
        if (tag.Equals("Enemy"))
        {
            return "Player";
        }
        else if (tag.Equals("Player"))
        {
            return "Enemy";
        }
        return tag;
    }

    public bool IsSelfCast()
    {
        return is_self_cast;
    }

    public Sprite ImageLookUp(string name)
    {
        return (Sprite)AssetDatabase.LoadAssetAtPath("Assets/Basic_RPG_Icons/" + name + ".png", typeof(Sprite));
    }

    public Vector3[] GenerateRange(Vector3 pos, int size)
    {
        List<Vector3> range = new List<Vector3>();
        int end_pos, start_pos = -(size / 2);
        if (size % 2 == 0)
        {
            // EVEN
            end_pos = (size / 2);
        }
        else
        {
            end_pos = (size / 2) + 1;
        }

        for(int x = start_pos; x < end_pos; x++)
        {
            for (int z = start_pos; z < end_pos; z++)
            {
                range.Add(pos + new Vector3(x, 0, z));
            }
        }

        return range.ToArray();
    }

    public List<Character> GetCharactersAtPos(Vector3 pos)
    {
        return GetCharactersInRange(new Vector3[] { pos });
    }

    public List<Character> GetCharactersInRange(Vector3 [] pos )
    {
        List<Character> characters = new List<Character>();
        Character to_add;
        foreach (var i in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (pos.Contains(i.transform.position))
            {
                to_add = i.GetComponent<Character>();
                if (to_add.IsAlive())
                    characters.Add(i.GetComponent<Character>());
            }

        }
        foreach (var i in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            if (pos.Contains(i.transform.position))
            {
                to_add = i.GetComponent<Character>();
                if (to_add.IsAlive())
                    characters.Add(i.GetComponent<Character>());
            }
        }

        return characters;
    }

    public void AddEffect(Effect effect, Character act_on)
    {
        act_on.effects.Add(effect);
        act_on.AddToEffectsUI(effect);
    }
}
