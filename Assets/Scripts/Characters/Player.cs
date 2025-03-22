using System;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEditor.MPE;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEditor.PlayerSettings;

public class Player : Character
{
    private int ability = -1;
    private ColorBlock default_btn;
    private LineRenderer area;

    // Start is called before the first frame update
    void Start()
    {
        SetupHP(10);
        SetupAP(5, 1);

        default_btn = GameObject.Find("Ability (0)").GetComponentInChildren<UnityEngine.UI.Button>().colors;

        Dictionary<string, string> animations = new Dictionary<string, string>();
        animations.Add("idle", "idle");
        animations.Add("attack", "attack");
        animations.Add("cast", "victory");
        animations.Add("walk", "walk");
        animations.Add("die", "die");
        animations.Add("shoot", "idle");

        SetAnimations(animations);

        area = GameObject.Find("Area").GetComponent<LineRenderer>();
    }

    public void UseAbility(int ability_num)
    {
        if (ability_num == ability)
        {
            DeSelectButton(ability);
            return;
        }

        if (GetChoosing())
        {
            DeSelectButton(ability);
        }

        try
        {
            if (abilities[ability_num].AP_Cost > GetAP())
                return;
        }
        catch (NullReferenceException)
        {
            DeSelectButton(ability);
            return;
        }

        ability = ability_num;

        if (ability >= 0 && abilities.Length >= 1)
        {
            if (abilities[ability].IsSelfCast())
            {
                ability = -1;
                abilities[ability_num].Action(new RaycastHit());
            }
            else
            {
                SetChoosing(true);
                UnityEngine.UI.Button btn = GameObject.Find("Ability (" + ability_num + ")").GetComponentInChildren<UnityEngine.UI.Button>();
                ColorBlock colors = btn.colors;
                colors.normalColor = colors.pressedColor;
                colors.selectedColor = colors.pressedColor;
                colors.highlightedColor = colors.pressedColor;
                btn.colors = colors;
            }
        }
    }

    public void DeSelectButton()
    {
        DeSelectButton(ability);
    }
    public void DeSelectButton(int btn_num)
    {
        GameObject ability = GameObject.Find("Ability (" + btn_num + ")");
        area.positionCount = 0;
        SetChoosing(false);

        if (ability != null)
            ability.GetComponentInChildren<UnityEngine.UI.Button>().colors = default_btn;
    }

    public void SetOutline(Vector3[] squares, Color colour)
    {
        area.startColor = colour;
        area.endColor = colour;
        area.positionCount = squares.Length;
        area.SetPositions(squares);
    }

    // Update is called once per frame
    void Update()
    {
        GenericUpdate();

        if (!GetControlled() || GetTurnOver() || GetActing())
            return;

        if (!GetChoosing())
        {
            ability = -1;
        }

        if (Input.GetMouseButton(1) && !GetMoving())
        {
            area.positionCount = 0;
            DeSelectButton(ability);
            
            points = grid.GetPoints(transform.position);
            LocationToMove();

        } else if (!GetActing()) {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                UseAbility(0);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                UseAbility(1);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                UseAbility(2);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                UseAbility(3);
            }
            else if (GetChoosing() && abilities[ability].global_cast)
            {
                Vector3 pos = grid.GetMousePos();
                if (grid.InGrid(pos))
                {
                    Vector3[] squares = grid.GenerateRangeOutline(pos, abilities[ability].aoe);

                    SetOutline(squares, new Color(204f / 255f, 109f / 255f, 9f / 255f));
                }                    
            }
            else if (GetChoosing() && !abilities[ability].is_self_cast)
            {
                Vector3[] squares = grid.GenerateRangeOutline(transform.position, abilities[ability].range * 2, true);

                SetOutline(squares, Color.red);

            }
            else if (GetChoosing() && Input.anyKeyDown && !(Input.GetMouseButtonDown(0)))
            {
                DeSelectButton(ability);
                SetChoosing(false);
                return;
            }
            else if(!IsMoving())
            {
                Vector3[] squares = grid.GenerateRangeOutline(transform.position, GetMaxDistance() * 2, true);

                SetOutline(squares, Color.cyan);
            }
        }
        
        if (Input.GetMouseButton(0) && GetChoosing() && ability >= 0)
        {
            RaycastHit[] hits = grid.GetClick();
            if (EventSystem.current.IsPointerOverGameObject())
                return;
            int target = abilities[ability].IsValidTarget(hits);

            if (target != -1)
            {
                transform.LookAt(hits[target].transform);
                abilities[ability].Action(hits[target]);
                DeSelectButton(ability);
                SetChoosing(false);
            }
            else
            {
                DeSelectButton(ability);
                SetChoosing(false);
            }
        }
    }
}
