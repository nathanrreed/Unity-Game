using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using TMPro;
using TMPro.EditorUtilities;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

public abstract class Character : MonoBehaviour
{
    private int hp;
    private int max_hp;
    private int ap;
    private int max_ap;
    private float dist_per_ap;
    private int move_ap = 0;
    public int acting_slot = -1;

    private bool isAlive = true;
    private bool isTurnOver = false;
    private bool isActing = false;
    private bool isControlled = false;
    private bool isChoosing = false;
    private bool isMoving = false;

    private string animation_to_play = "";
    private string char_tag = "";

    private float walkSpeed = 5.0f;

    public Stack<Vector3> points;
    private Vector3 target;

    public Grid grid;
    
    private GameObject model;
    private LineRenderer path;
    public HealthBar health_bar;
    private new Animation animation;
    private CharacterController controller;
    private Transform effects_UI;
    private TextMeshProUGUI AP_UI;

    public Ability[] abilities;
    public List<Effect> effects;

    public Dictionary<string, string> animations;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animation = GetComponent<Animation>();
        health_bar = GetComponentInChildren<HealthBar>();
        path = GameObject.Find("Path").GetComponent<LineRenderer>();

        Transform character_UI = transform.Find("Character_UI");
        effects_UI = character_UI.Find("Effects");

        if (character_UI.Find("AP") != null)
            AP_UI = character_UI.Find("AP").GetComponent<TextMeshProUGUI>();
        grid = Camera.main.GetComponent<Grid>(); 

        model = this.gameObject;
        char_tag = this.gameObject.tag;
        effects = new List<Effect>();
    }
    public void GenericUpdate()
    {
        if (isActing)
        {
            if (!animation.isPlaying)
            {
                isActing = false;
                SetAnimation("idle");
                if (acting_slot > 0)
                {
                    abilities[acting_slot].OnAnimationEnd();
                    acting_slot = -1;
                }
            }
        }
        else if (isMoving)
        {
            var step = walkSpeed * Time.deltaTime; // calculate distance to move
            transform.position = Vector3.MoveTowards(transform.position, target, step);
            transform.LookAt(target);
            grid.FocusCamera();
            if (Vector3.Distance(transform.position, target) < 0.001f)
            {
                transform.position = target;

                if (points.Count == 0)
                {
                    TryUseAP(move_ap);
                    move_ap = 0;
                    transform.position = target;
                    transform.LookAt(grid.GetNearest(this.tag, transform.position).transform.position);
                    SetAnimation("idle");

                    isMoving = false;
                }
                else
                {
                    target = points.Pop();
                    if (path.positionCount > 0)
                        path.positionCount--;
                }
            }
        }

        if (isAlive || (!isAlive && animation.isPlaying))
            animation.CrossFade(animation_to_play, 0.1f);
    }

    public IEnumerator KeepForSetTime(GameObject obj, float time)
    {
        yield return new WaitForSecondsRealtime(time);
        Destroy(obj);
        yield return null;
    }

    public void SpawnText(string text, Vector3 pos, Color color)
    {
        GameObject text_obj = Instantiate(grid.PrefabLookUp("Text"), pos, Quaternion.identity);
        var a = text_obj.GetComponentInChildren<TextMeshProUGUI>();
        a.text = text;
        a.color = color;
        StartCoroutine(KeepForSetTime(text_obj, 1.5f));
    }

    public int ApplyDamageEffects(int dmg)
    {
        foreach(var effect in effects) // Applies all the damage effects
        {
            dmg = effect.OnDamaged(dmg);
        }
        return dmg;
    }

    public void DealDamage(int dmg, bool ignore_effects = false)
    {
        if(!ignore_effects)
            dmg = ApplyDamageEffects(dmg);
        hp -= dmg;
        SpawnText("-" + dmg, health_bar.transform.position + new Vector3(0.5f, 0.5f, 0), Color.red);
        health_bar.SetHealth(hp);
        if (hp <= 0)
        {
            isAlive = false;
            isTurnOver = true;
            SetAnimation("die");
            health_bar.SetVisible(false);
            grid.CharacterDied();
            SetNextTurn();
        }
    }

    public void HealDamage(int heal)
    {
        foreach (var effect in effects.ToArray()) // Removes negative effects
        {
            if (effect.healable)
            {
                effects.Remove(effect);
            }
        }

        hp += heal;
        if (hp > max_hp)
        {
            hp = max_hp;
        }

        SpawnText("+" + heal, health_bar.transform.position + new Vector3(0.5f, 0.5f, 0), Color.green);
        health_bar.SetHealth(hp);
    }
    public int GetMaxHP()
    {
        return max_hp;
    }
    public int GetHP()
    {
        return hp;
    }
    public bool IsAlive()
    {
        return isAlive;
    }

    public bool IsMoving()
    {
        return isMoving;
    }

    public bool CanControl()
    {
        return (isAlive && !isTurnOver);
    }

    public void SetupHP(int new_hp)
    {
        hp = new_hp;
        max_hp = new_hp;
        health_bar.SetMaxHealth(new_hp);
    }

    public void SetHP(int new_hp)
    {
        if (new_hp > max_hp)
        {
            new_hp = max_hp;
        }else if (new_hp < 0)
        {
            new_hp = 0;
        }
            
        hp = new_hp;
        health_bar.SetHealth(new_hp);

        if (hp > 0)
        {
            isAlive = true;
        }
        else
        {
            isAlive = false;
        }
    }

    public void SetAP(int new_ap)
    {
        if (new_ap > max_ap)
        {
            new_ap = max_ap;
        }
        else if (new_ap < 0)
        {
            new_ap = 0;
        }

        ap = new_ap;

        SetAPText();
    }

    public int GetMaxAP()
    {
        return max_ap;
    }

    public void SetAPText()
    {
        if (AP_UI != null)
            AP_UI.text = ap + "/" + max_ap;
    }
    public void SetupAP(int new_ap, int dist)
    {
        ap = new_ap;
        max_ap = new_ap;
        dist_per_ap = dist;

        SetAPText();

        //action_bar.SetMaxAP(new_ap);
    }

    public void SetNextTurn()
    {
        SetAPText();
        isTurnOver = true;
        isActing = false;
        isMoving = false;

        StartCoroutine(grid.Wait("CharacterTurnIsOver", 0.5f, this));
    }

    public bool TryUseAP(int ap_to_use)
    {
        if (CheckAP(ap_to_use))
        {
            ap -= ap_to_use;
            SetAPText();
            if (ap == 0)
            {
                SetNextTurn();
            }
            return true;
        }
        return false;
    }

    public bool CheckAP(int ap_to_use)
    {
        int new_ap = ap - ap_to_use;
        if (new_ap >= 0)
            return true;
        return false;
    }

    public int GetAP()
    {
        return ap;
    }

    public bool SetAnimation(string name)
    {
        name = animations[name];
        if (animation.GetClip(name) != null && name != animation_to_play)
        {
            animation_to_play = name;
            return true;
        }
        return false;
    }

    public void SetAnimationSpeed(string name, float speed)
    {
        name = animations[name];
        if (animation.GetClip(name) != null)
        {
            animation[name].speed = speed;
        }
    }

    public void SetAnimations(Dictionary<string, string> anims)
    {
        animations = anims;
        SetAnimation("idle");
    }

    public void LocationToMove()
    {
        move_ap = Mathf.CeilToInt((float)points.Count / dist_per_ap);
        if (points.Count > 0 && CheckAP(move_ap))
        {
            isMoving = true;
            target = points.Pop();
            SetAnimation("walk");
            path.positionCount = points.Count + 1;
            List<Vector3> temp = points.ToList();
            temp.Insert(0, transform.position);
            temp.Reverse();
            path.SetPositions(temp.ToArray());
            path.transform.position = transform.position;

            if (this.tag == "Player")
            {
                path.startColor = Color.cyan;
                path.endColor = Color.blue;
            }
            else
            {
                path.startColor = Color.red;
                path.endColor = new Color32(255, 140, 0, 255);
            }
        }
        else
        {
            move_ap = 0;
        }
    }

    public bool CheckMaxDistance(Vector3 v)
    {
        return CheckMaxDistance(v.x, v.z);
    }
    public bool CheckMaxDistance(float x, float z)
    {
        x = Mathf.Abs(transform.position.x - Mathf.Floor(x));
        z = Mathf.Abs(transform.position.z - Mathf.Floor(z));
        int max_distance = GetMaxDistance();
        if (x < max_distance && z < max_distance && x + z <= max_distance)
            return true;
        return false;
    }

    public int GetMaxDistance()
    {
        return Mathf.FloorToInt((float)ap * dist_per_ap);
    }

    public float GetDistPerAP()
    {
        return dist_per_ap;
    }

    /*public int GetDistance(Vector3 v)
    {
        float x = Mathf.Abs(transform.position.x - Mathf.Floor(v.x));
        float z = Mathf.Abs(transform.position.z - Mathf.Floor(v.z));

        return (int)Mathf.Round(x + z);
    }*/

    public int GetDistanceChessboard(Vector3 v)
    {
        return GetDistanceChessboard(transform.position, v);
    }

    public int GetDistanceChessboard(Vector3 a, Vector3 b)
    {
        float x = Mathf.Abs(a.x - Mathf.Floor(b.x));
        float z = Mathf.Abs(a.z - Mathf.Floor(b.z));

        return (int)Mathf.Max(x, z);
    }

    public void SetControlled(bool status)
    {
        isControlled = status;
    }
    public bool GetControlled()
    {
        return isControlled;
    }

    public bool GetChoosing()
    {
        return isChoosing;
    }

    public void SetChoosing(bool choosing)
    {
        isChoosing = choosing;
    }

    public bool GetMoving()
    {
        return isMoving;
    }

    public bool GetActing()
    {
        return isActing;
    }

    public void SetActing(bool acting)
    {
        isActing = acting;
    }

    public void NextTurn()
    {       
        isTurnOver = false;
        ap = max_ap;

        for (var i = 0; i < effects_UI.childCount; i++)
        {
            Destroy(effects_UI.GetChild(i).gameObject);
        }
        foreach (var effect in effects.ToArray())
        {
            if (effect.OnTurnStart())
                AddToEffectsUI(effect);
        }

        SetAPText();
    }

    public void AddToEffectsUI(Effect effect)
    {
        if (effect.sprite != null)
        {
            var new_img = Instantiate(grid.PrefabLookUp("Image"), effects_UI);
            new_img.GetComponent<UnityEngine.UI.Image>().sprite = effect.sprite;
        }
    }

    public bool GetTurnOver()
    {
        return isTurnOver;
    }

    public string GetTag()
    {
        return char_tag;
    }

    public virtual string GetStatus(int players_with_actions, int num_players)
    {
        return "Turn:" + grid.GetTurnNum() + "\nAP: " + ap + "/" + max_ap + "\nHP: " + hp + "/" + max_hp;
    }

    public void SetAbilityIcons()
    {
        GameObject ability_icon;
        Ability ability;
        for (int i = 0; i < 4; i++)
        {
            if (i < abilities.Length)
            {
                ability = abilities[i];
            }
            else
            {
                ability = null;
            }
            
            ability_icon = GameObject.Find("Ability (" + i + ")");
            UnityEngine.UI.Button btn = ability_icon.GetComponentInChildren<UnityEngine.UI.Button>();
            btn.onClick.RemoveAllListeners();

            foreach (var image in ability_icon.GetComponentsInChildren<UnityEngine.UI.Image>())
            {
                if (image.name == "Image")
                {
                    if (ability != null)
                    {
                        image.sprite = ability.icon;
                        image.color = Color.white;
                        break;
                    }
                    else
                    {
                        image.sprite = null;
                        image.color = Color.clear;
                    }
                }
            }
            
            if (ability != null)
            {
                int temp = i;
                btn.onClick.AddListener(() => { ((Player)this).UseAbility(temp); });
                btn.GetComponentInChildren<TextMeshProUGUI>().text = "" + abilities[i].AP_Cost;
            }
            else
            {
                btn.GetComponentInChildren<TextMeshProUGUI>().text = "";
            }
        }
    }

    public IEnumerator LerpTo(GameObject obj, Vector3 pos, Vector3 target, float speed)
    {
        float dist_covered = 0;
        float dist = Vector3.Distance(target, pos);
        while (Vector3.Distance(obj.transform.position, target) > 0.1f)
        {
            dist_covered += Time.deltaTime * speed;
            obj.transform.position = Vector3.Lerp(obj.transform.position, target, dist_covered / dist);
            yield return null;
        }

        Destroy(obj);
        yield return null;
    }

    public void SpawnProjectile(Vector3 pos, Vector3 target, String prefab, float speed)
    {
        var projectile = Instantiate(grid.PrefabLookUp(prefab), pos, Quaternion.identity);
        projectile.transform.LookAt(target);
        StartCoroutine(LerpTo(projectile, pos, target, speed));
    }

    public bool HasEffect(string effect_name)
    {
        foreach (var effect in effects) // Applies all the damage effects
        {
            if (effect.ToString() == effect_name)
            {
                return true;
            }
        }
        return false;
    }

    public void TurnIsOver()
    {
        for (var i = 0; i < effects_UI.childCount; i++)
        {
            Destroy(effects_UI.GetChild(i).gameObject);
        }
        foreach (var effect in effects.ToArray())
        {
            if (effect.OnTurnEnd())
                AddToEffectsUI(effect);
        }
    }
}
