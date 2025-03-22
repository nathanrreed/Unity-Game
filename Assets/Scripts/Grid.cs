using OpenCover.Framework.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using TMPro.EditorUtilities;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using static UnityEditor.PlayerSettings;
using Random = UnityEngine.Random;

public class Grid : MonoBehaviour
{
    private Vector3 gridPos;

    private float mapBorderXL = 0;
    private float mapBorderXR = 15;
    private float mapBorderZT = 25;
    private float mapBorderZB = 0;

    private CharacterController character;
    private Character character_status;

    private int turn_num = 1;
    private int round_num = 0;
    private bool player_turn = true;
    private bool game_over = true;
    private int num_players;
    private int num_enemies;

    private Dictionary<Vector3, bool> obstacles = new Dictionary<Vector3, bool>();
    Tuple<int, String>[] enemy_types = { Tuple.Create(1, "Grunt"), Tuple.Create(2, "Shooting Grunt"), Tuple.Create(3, "Elite"), Tuple.Create(2, "Healer") };

    public GameObject target_obj;
    public GameObject panel;
    private GameObject desc_panel;
    public LineRenderer cursor;
    public TextMeshProUGUI turnGUI;
    private TextMeshProUGUI cursor_text;

    private Transform camera_pivot;
    private bool moving_camera = false;
    private bool scrolling_camera = false;
    private bool rotating_camera = false;
    private Vector3 camera_end_pos;
    private Quaternion camera_end_rotation;
    private float camera_rotation_covered;
    private float camera_move_dist;
    private float camera_dist_covered;
    private float camera_move_speed = 2;
    private float camera_scroll_speed = 80;

    private float max_camera_y = 3;
    private float min_camera_y = -3;
    public Vector3[] corners;

    void Start()
    {
        SpawnPlayers();

        StartCoroutine("WaitForCharacter");
        
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        character = player.GetComponent<CharacterController>();
        character_status = player.GetComponent<Character>();
        character_status.SetControlled(true);
        cursor_text = cursor.GetComponentInChildren<TextMeshProUGUI>();
        camera_pivot = GameObject.Find("Camera Pivot").transform;
        desc_panel = GameObject.Find("DescPanel");
        desc_panel.SetActive(false);

        target_obj = GameObject.Find("TargetObj");

        //Setup game border
        corners = new Vector3[] { new Vector3(mapBorderXL - 0.5f, 0, mapBorderZB - 0.5f), new Vector3(mapBorderXR + 0.5f, 0, mapBorderZB - 0.5f), new Vector3(mapBorderXR + 0.5f, 0, mapBorderZT + 0.5f), new Vector3(mapBorderXL - 0.5f, 0, mapBorderZT + 0.5f) };
        GameObject.Find("Border").GetComponent<LineRenderer>().SetPositions(corners);
    }

    // Update is called once per frame
    void Update()
    {
        if (moving_camera)
        {
            camera_dist_covered += Time.deltaTime * (scrolling_camera ? camera_scroll_speed : camera_move_speed);
            camera_pivot.position = Vector3.Lerp(camera_pivot.position, camera_end_pos, camera_dist_covered / camera_move_dist);

            if (camera_pivot.position == camera_end_pos)
            {
                camera_pivot.position = camera_end_pos;
                moving_camera = false;
                scrolling_camera = false;
            }
            else
            {
                return;
            }
        }

        if (rotating_camera)
        {
            camera_rotation_covered += Time.deltaTime * camera_move_speed;
            camera_pivot.transform.rotation = Quaternion.Slerp(camera_pivot.rotation, camera_end_rotation, camera_rotation_covered);

            if (camera_pivot.transform.rotation.eulerAngles == camera_end_rotation.eulerAngles)
            {
                camera_pivot.transform.rotation = camera_end_rotation;
                rotating_camera = false;
            }
            else
            {
                return;
            }
        }

        if (character_status.CanControl())
        {
            if (Input.mousePosition.x > 0 && Input.mousePosition.x < Screen.width && Input.mousePosition.y > 0 && Input.mousePosition.y < Screen.height)
            {
                Vector3 pos = GetMousePos();
                if (InGrid(pos))
                {
                    gridPos = pos;
                }

                cursor.transform.position = gridPos;

                if (player_turn && !character_status.IsMoving() && !character_status.GetChoosing())
                {
                    float ap_to_use = Mathf.Ceil(GetPoints(character.transform.position).Count / character_status.GetDistPerAP());
                    if (ap_to_use > 0)
                    {
                        cursor_text.text = "" + ap_to_use + " AP";
                    }
                    else
                    {
                        cursor_text.text = "- AP";
                    }
                }
                else
                {
                    cursor_text.text = "";
                }
            }
        }
        else
        {
            cursor.transform.position = new Vector3(0, -1, 0);
        }

        if (Input.mousePosition.x <= 0 || Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
        {
            MoveCamera(-90);
        }
        else if (Input.mousePosition.x >= Screen.width - 1 || Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
        {
            MoveCamera(90);
        }
        else if (Input.mousePosition.y >= Screen.height - 1 || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) {
            MoveCamera(0);
        }
        else if (Input.mousePosition.y <= 0 || Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
        {
            MoveCamera(180);
        }

        if (player_turn) {
            if (Input.GetMouseButtonDown(0) && !character_status.GetChoosing())
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit[] hits = Physics.RaycastAll(ray);
                foreach (var hit in hits)
                {
                    if (hit.collider.gameObject.tag == "Player")
                    {
                        SwitchToCharacter(hit.collider.gameObject);
                        return;
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.Tab) && !character_status.GetMoving())
            {
                ((Player)character_status).DeSelectButton();
                SwitchCharacter(true);
            } else if (Input.GetKeyDown(KeyCode.Backspace) && !character_status.IsMoving())
            {
                ((Player)character_status).DeSelectButton();
                ChangeTurn();
            }/*else if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (Time.timeScale == 1)
                {
                    Time.timeScale = 0;
                }
                else
                {
                    Time.timeScale = 1;
                }
                
            }*/
        }
       
        if (Input.GetKeyDown(KeyCode.Q))
        {
            rotating_camera = true;
            camera_rotation_covered = 0;
            camera_end_rotation = Quaternion.Euler(0, camera_pivot.rotation.eulerAngles.y + 45, 0);
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            rotating_camera = true;
            camera_rotation_covered = 0;
            camera_end_rotation = Quaternion.Euler(0, camera_pivot.rotation.eulerAngles.y - 45, 0);
        }

        if (Input.mouseScrollDelta.y != 0)
        {
            Vector3 new_pos = camera_pivot.transform.position + new Vector3(0, Input.mouseScrollDelta.y, 0);
            if (new_pos.y >= min_camera_y && new_pos.y <= max_camera_y)
            {
                scrolling_camera = true;
                MoveCameraTo(new_pos);
            }
        }

        if (Input.GetKeyDown(KeyCode.Tilde) || Input.GetKeyDown(KeyCode.BackQuote) )
        {
            round_num = 15;
            NewRound();
        }

        CharacterDied();
    }

    public void MoveCamera(float angle)
    {
        angle += camera_pivot.rotation.eulerAngles.y;
        float panDist = (10 * Time.deltaTime);
        float x_part = Mathf.Round(Mathf.Sin(Mathf.Deg2Rad * angle));
        float z_part = Mathf.Round(Mathf.Cos(Mathf.Deg2Rad * angle));

        if (InGrid(camera_pivot.position.x + panDist * x_part, camera_pivot.position.z + panDist * z_part))
            camera_pivot.position = new Vector3(camera_pivot.position.x + panDist * x_part, camera_pivot.position.y, camera_pivot.position.z + panDist * z_part);
    }

    public IEnumerator Wait(string routine, float time, Character character_change_from = null)
    {
        yield return new WaitForSecondsRealtime(time);

        if (routine == "CharacterTurnIsOver")
        {
            CharacterTurnIsOver(character_change_from);
        }
        else if (routine == "FadeDownPanel")
        {
            FadeDownPanel();
        }

        yield return null;
    }
    public bool InGrid(Vector3 point)
    {
        return InGrid(point.x, point.z);
    }
    public bool InGrid(float x_point, float z_point)
    {
        return x_point >= mapBorderXL && x_point <= mapBorderXR && z_point <= mapBorderZT && z_point >= mapBorderZB;
    }

    public Vector3 GetMousePos()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray);
        foreach (var hit in hits)
        {
            if (hit.collider.gameObject.name == "Plane")
            {
                float x_point = Mathf.Floor(hit.point.x + 0.5f);
                float z_point = Mathf.Floor(hit.point.z + 0.5f);
                return new Vector3(x_point, 0, z_point);
            }
        }

        return new Vector3(-1, -1, -1);
    }


    public GameObject GetClick(string tag)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray);
        foreach (var hit in hits)
        {
            if (hit.collider.gameObject.tag == tag)
            {
                return hit.collider.gameObject;
            }
        }

        return null;
    }

    public RaycastHit[] GetClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        return Physics.RaycastAll(ray);
    }

    public Vector3 GetGridPos()
    {
        return new Vector3(gridPos.x - 0.5f, 0, gridPos.z - 0.5f);
    }

    public bool CheckSquare(Vector3 pos)
    {
        if (!InGrid(pos))
        {
            return false;
        }

        if (obstacles.ContainsKey(pos))
        {
            return false;
        }
                    
        foreach (var i in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (Mathf.Ceil(i.transform.position.x) == Mathf.Ceil(pos.x) && Mathf.Ceil(i.transform.position.z) == Mathf.Ceil(pos.z))
            {
                Character character = i.GetComponent<Character>();
                if (character.IsAlive())
                    return false;
            }

        }
        foreach (var i in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            if (Mathf.Ceil(i.transform.position.x) == Mathf.Ceil(pos.x) && Mathf.Ceil(i.transform.position.z) == Mathf.Ceil(pos.z))
            {
                if (i.GetComponent<Character>().IsAlive())
                    return false;
            }
        }
        return true;
    }

    public void CharacterDied()
    {
        int num_chars = GetCharactersAlive();
        if (num_chars <= 0 && !game_over)
        {
            if (!player_turn)
            {
                panel.GetComponentInChildren<TextMeshProUGUI>().text = "Game Over";
                panel.SetActive(true);
                FadeUpPanel();
                character_status.SetControlled(false);
                game_over = true;
            }
            else
            {
                NewRound();
            }
        }
    }

    public int GetCharactersWithActions()
    {
        int count = 0;
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag(player_turn ? "Player" : "Enemy"))
        {
            Character character = obj.GetComponent<Character>();
            if (character.IsAlive() && !character.GetTurnOver())
            {
                count++;
            }
        }
        return count;
    }

    public int GetCharactersAlive()
    {
        int count = 0;
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag(player_turn ? "Enemy" : "Player"))
        {
            Character character = obj.GetComponent<Character>();
            if (character.IsAlive())
            {
                count++;
            }
        }
        return count;
    }

    public void ChangeTurn()
    {
        string tag;
        if (player_turn) // Now enemy turn
        {
            player_turn = false;
            foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
            {
                player.GetComponent<Character>().TurnIsOver();
            }
            tag = "Enemy";
        }
        else
        {
            player_turn = true;
            foreach (GameObject player in GameObject.FindGameObjectsWithTag("Enemy"))
            {
                player.GetComponent<Character>().TurnIsOver();
            }
            tag = "Player";
            turn_num++;
        }

        PrintPanel((player_turn ? "Player" : "Enemy") + "Turn", 1);
        GameObject alive = null;
        foreach (GameObject player in GameObject.FindGameObjectsWithTag(tag))
        {
            player.GetComponent<Character>().NextTurn();
            if (player.GetComponent<Character>().IsAlive())
            {
                alive = player;
            }
        }
        SwitchToCharacter(alive);
    }

    public void CharacterTurnIsOver(Character character_change_from = null)
    {
        int chars_with_actions = GetCharactersWithActions();
        if (chars_with_actions == 0)
        {
            ChangeTurn();
        }
        else
        {
            if (character_change_from == null || character_change_from == character_status)
            {
                SwitchCharacter(false);
            }
        }
    }

    void OnGUI()
    {
        int num_chars = GetCharactersAlive();
        turnGUI.text = character_status.GetStatus(GetCharactersWithActions(), num_chars);
    }

    public void FocusCamera()
    {
        camera_pivot.position = FocusCameraPos();
    }

    public Vector3 FocusCameraPos()
    {
        return character.transform.position;
    }

    public void MoveCameraTo(Vector3 pos)
    {
        moving_camera = true;
        camera_end_pos = pos;
        camera_move_dist = Vector3.Distance(transform.position, camera_end_pos);
        camera_dist_covered = 0;
    }

    public void SwitchToCharacter(GameObject player)
    {
        if (player != null)
            SwitchToCharacter(player.GetComponent<CharacterController>(), player.GetComponent<Character>());
    }
    public void SwitchToCharacter(CharacterController temp_cont, Character temp_status)
    {
        if (temp_status.CanControl())
        {
            character_status.SetControlled(false);
            character = temp_cont;
            character_status = temp_status;

            MoveCameraTo(FocusCameraPos());

            character_status.SetControlled(true);
            character_status.SetAbilityIcons();
        }
    }

    public void SwitchCharacter(bool tab)
    {
        if (tab && GetCharactersWithActions() == 1)
        {
            return;
        }

        bool last = false;
        CharacterController temp_cont = null;
        Character temp_status = null;
        foreach (GameObject player in GameObject.FindGameObjectsWithTag(player_turn ? "Player" : "Enemy"))
        {
            if (player.name == character.name)  //current was found
            {
                last = true;
                continue;
            }
            if (player.GetComponent<Character>().CanControl())
            {
                if (last) // A nonused was found after current
                {
                    SwitchToCharacter(player);
                    return;
                }
                else if (temp_status == null) // First non used in list
                {
                    temp_cont = player.GetComponent<CharacterController>();
                    temp_status = player.GetComponent<Character>();
                }
            }
        }
        SwitchToCharacter(temp_cont, temp_status);
    }

    public Stack<Vector3> GetPoints(Vector3 start, Vector3 end)
    {
        if (CheckSquare(end))
        {
            Stack<Vector3> rtn = A_Star(start, end);
            if (rtn.Count > 0 && rtn.Peek() == start)
            {
                rtn.Pop();
            }
            return rtn;
        }
        else
            return new Stack<Vector3>();
    }
    public Stack<Vector3> GetPoints(Vector3 start)
    {
        return GetPoints(start, gridPos);
    }

    /* Adapted from the Pseudocode at https://en.wikipedia.org/wiki/A*_search_algorithm */
    private Stack<Vector3> A_Star(Vector3 start, Vector3 goal)
    {
        // The set of discovered nodes

        start = new Vector3(Mathf.Ceil(start.x), 0, Mathf.Ceil(start.z));

        HashSet <Vector3> openSet = new HashSet<Vector3>();
        openSet.Add(start);

        // For node n, cameFrom[n] is the node immediately preceding it on the cheapest path from start to n currently known.
        Dictionary<Vector3, Vector3> cameFrom = new Dictionary<Vector3, Vector3>();

        // For node n, gScore[n] is the cost of the cheapest path from start to n currently known.
        Dictionary<Vector3, float> gScore = new Dictionary<Vector3, float>();
        gScore[start] = 0;

        // For node n, fScore[n] := gScore[n] + h(n). fScore[n] represents our current best guess as to
        // how cheap a path could be from start to finish if it goes through n.
        Dictionary<Vector3, float> fScore = new Dictionary<Vector3, float>();
        fScore[start] = character_status.GetDistanceChessboard(start, goal);

        Vector3 current = start;
        while (openSet.Count != 0)
        {
            // Find node with lowest fScore
            float min = float.MaxValue;
            foreach (Vector3 v in openSet)
            {
                if (fScore.ContainsKey(v) && fScore[v] < min) // If the fscore of v is not found then fScore[v] = infinity
                {
                    min = fScore[v];
                    current = v;
                }
            }

            // Found path
            if (current == goal || cameFrom.Count > 1000)
                return reconstruct_path(cameFrom, current);

            openSet.Remove(current);
            List<Vector3> neighbours = new List<Vector3>();

            // Check each cardinal direction for space
            Vector3[] directions = { new Vector3(1, 0, 0), new Vector3(-1, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 0, -1),
                                     new Vector3(1, 0, 1), new Vector3(-1, 0, 1), new Vector3(1, 0, -1), new Vector3(-1, 0, -1)};
            foreach (Vector3 direction in directions)
            {
                Vector3 temp = current + direction;
                if (CheckSquare(temp))
                {
                    neighbours.Add(temp);
                }
            }

            foreach (Vector3 neighbour in neighbours)
            {
                // tentative_gScore is the distance from start to the neighbour through current
                float tentative_gScore = gScore[current] + character_status.GetDistanceChessboard(neighbour, current);
                if (!gScore.ContainsKey(neighbour) || tentative_gScore < gScore[neighbour])
                {
                    // This path to neighbour is better than any previous one. Record it!
                    cameFrom[neighbour] = current;
                    gScore[neighbour] = tentative_gScore;
                    fScore[neighbour] = tentative_gScore + character_status.GetDistanceChessboard(neighbour, goal);
                    if (!openSet.Contains(neighbour))
                        openSet.Add(neighbour);
                }
            }
        }

        // No Path found
        return new Stack<Vector3>();
    }

    private Stack<Vector3> reconstruct_path(Dictionary<Vector3, Vector3> cameFrom, Vector3 current)
    {
        Stack<Vector3> total_path = new Stack<Vector3>();
        total_path.Push(current);

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            total_path.Push(current);
        }
        return total_path;
    }

    public GameObject PrefabLookUp(string name)
    {
        return (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Prefabs/" + name + ".prefab", typeof(GameObject));
    }

    public IEnumerator WaitForCharacter()
    {
        foreach (var player in GameObject.FindGameObjectsWithTag("Player"))
        {
            var created_status = player.GetComponent<Character>();
            yield return new WaitUntil(() => created_status.health_bar != null && created_status.abilities != null && created_status.animations != null);
        }

        NewRound();
    }

    private void SpawnPlayers()
    {
        num_players = 3;
        for (var i = 0; i < num_players; i++)
        {
            var created = Instantiate(PrefabLookUp("Player"), new Vector3(3 * i - 3 + 8, 0, 0), Quaternion.identity);
            created.name = "Player" + i;
            Character player = created.GetComponent<Character>();

            switch (i)
            {
                case(0): player.abilities = new Ability[] { new Hit(player, 1), new Shoot(player, 1), null, new Hunker(player, 1) }; break;
                case(1): player.abilities = new Ability[] { new Hit(player, 1), new Fireball(player, 1, 1), null, new Hunker(player, 1) }; break;
                case(2): player.abilities = new Ability[] { new Hit(player, 1), new Silence(player, 1), new Heal(player, 1), new Hunker(player, 1) }; break;
            }
        }
    }

    private String random_enemy(ref int enemy_allotment)
    {
        int choosen_type;
        while (true)
        {
            choosen_type = Mathf.FloorToInt(Random.Range(0, enemy_types.Length));
            if (enemy_types[choosen_type].Item1 <= enemy_allotment)
            {
                enemy_allotment -= enemy_types[choosen_type].Item1;
                return enemy_types[choosen_type].Item2;
            }
        }        
    }

    public Vector3[] GenerateRangeOutline(Vector3 pos, float size, bool add_half = false)
    {
        List<Vector3> range = new List<Vector3>();
        float end_pos, start_pos = -(size / 2);
        if (size % 2 == 0)
        {
            // EVEN
            end_pos = (size / 2);
        }
        else
        {
            end_pos = (size / 2);
        }

        Vector3 bottom_left = pos + new Vector3(start_pos, 0, start_pos) + (add_half ? new Vector3(-0.5f, 0, -0.5f) : new Vector3(0, 0, 0));
        Vector3 top_left = pos + new Vector3(start_pos, 0, end_pos) + (add_half ? new Vector3(-0.5f, 0, 0.5f) : new Vector3(0, 0, 0));
        Vector3 bottom_right = pos + new Vector3(end_pos, 0, start_pos) + (add_half ? new Vector3(0.5f, 0, -0.5f) : new Vector3(0, 0, 0));
        Vector3 top_right = pos + new Vector3(end_pos, 0, end_pos) + (add_half ? new Vector3(0.5f, 0, 0.5f) : new Vector3(0, 0, 0));
        float borderXL = mapBorderXL - 0.5f;
        float borderXR = mapBorderXR + 0.5f;
        float borderZT = mapBorderZT + 0.5f;
        float borderZB = mapBorderZB - 0.5f;
        if (top_left.x < borderXL)
        {
            top_left.x = borderXL;
        }
        if (top_left.z > borderZT)
        {
            top_left.z = borderZT;
        }

        if (top_right.x > borderXR)
        {
            top_right.x = borderXR;
        }
        if (top_right.z > borderZT)
        {
            top_right.z = borderZT;
        }

        if (bottom_right.x > borderXR)
        {
            bottom_right.x = borderXR;
        }
        if (bottom_right.z < borderZB)
        {
            bottom_right.z = borderZB;
        }

        if (bottom_left.x < borderXL)
        {
            bottom_left.x = borderXL;
        }
        if (bottom_left.z < borderZB)
        {
            bottom_left.z = borderZB;
        }


        range.Add(top_left);
        range.Add(top_right);
        range.Add(bottom_right);
        range.Add(bottom_left);
        
        return range.ToArray();
    }

    private void SpawnEnemies()
    {
        int enemy_allotment = Mathf.RoundToInt(2.5f * Mathf.Log(round_num) + 2.0f);
        int i = 0;
        while(enemy_allotment > 0)
        {
            var pos = new Vector3(Random.Range(0, 15), 0, Random.Range(2, 24));
            while (!CheckSquare(pos))
            {
                pos = new Vector3(Random.Range(0, 15), 0, Random.Range(2, 24));
            }

            var created = Instantiate(PrefabLookUp(random_enemy(ref enemy_allotment)), pos, Quaternion.identity);
            created.name = "Enemy" + i++;
            created.transform.LookAt(GetNearest("Enemy", created.transform.position).transform.position);
        }
    }

    private void NewRound()
    {
        // Cleanup text
        foreach (var obj in GameObject.FindGameObjectsWithTag("Billboard"))
        {
            Destroy(obj);
        }
            game_over = true;
        round_num += 1;
        Character temp_char;
        int i = 0;
        foreach (var player in GameObject.FindGameObjectsWithTag("Player"))
        {
            temp_char = player.GetComponent<Character>();
            if (!temp_char.IsAlive())
            {
                temp_char.SetHP(temp_char.GetMaxHP() / 2);
                temp_char.SetAnimation("idle");
                temp_char.health_bar.SetVisible(true);
            }
            else
            {
                temp_char.SetHP(temp_char.GetMaxHP());
            }

            temp_char.SetAP(temp_char.GetMaxAP());

            player.transform.position = new Vector3(3 * i - 3 + 8, 0, 0);
            i++;
        }
        Despawn();
        CreateMap();
        SpawnEnemies();

        PrintPanel("Round " + round_num);
        SwitchToCharacter(character, character_status);
        game_over = false;
    }

    public IEnumerator FadeUp(CanvasGroup canvas_group)
    {
        panel.SetActive(true);
        canvas_group.alpha = 0;
        while (canvas_group.alpha < 1)
        {
            canvas_group.alpha += Time.deltaTime * 4;
            yield return null;
        }
        yield return null;
    }

    public IEnumerator FadeDown(CanvasGroup canvas_group)
    {
        canvas_group.alpha = 1;
        while (canvas_group.alpha > 0)
        {
            canvas_group.alpha -= Time.deltaTime * 2;
            yield return null;
        }

        panel.SetActive(false);
        yield return null;
    }

    public void FadeDownPanel()
    {
        CanvasGroup canvas_group = panel.GetComponent<CanvasGroup>();
        StartCoroutine(FadeDown(canvas_group));
    }

    public void PrintPanel(string str)
    {
        PrintPanel(str, 1);
    }


    public void FadeUpPanel()
    {
        CanvasGroup canvas_group = panel.GetComponent<CanvasGroup>();
        StartCoroutine(FadeUp(canvas_group));
    }

    public void PrintPanel(string str, float time)
    {
        panel.GetComponentInChildren<TextMeshProUGUI>().text = str;
        FadeUpPanel();
        StartCoroutine(Wait("FadeDownPanel", time));
    }

    private void Despawn()
    {
        obstacles = new Dictionary<Vector3, bool>();
        foreach (var i in GameObject.FindGameObjectsWithTag("Obstacles"))
        {
            Destroy(i);
        }
        foreach (var i in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            Destroy(i);
        }
    }
    private void CreateMap()
    {
        int rows_since = 5;
        for (var row = 2; row < 25; row++)
        {
            //bool add_to_row = false;
            int chance = Random.Range(0, 100);
            if ((rows_since == 1 && chance > 25) || (rows_since == 2 && chance > 65) ||  (rows_since > 2))
            {
                rows_since = 0;
                bool last_was = false;
                int number_in_row = 0;
                for(var i = 0; i < 16; i++)
                {
                    chance = Random.Range(0, 100);
                    if (number_in_row < 13 && ((chance > 34 && last_was) || (chance > 66 && !last_was)) ) {
                        if (last_was)
                        {
                            Instantiate(PrefabLookUp("Wall"), new Vector3(i, 0, row), Quaternion.identity);
                        }
                        else
                        {
                            Instantiate(PrefabLookUp("Barrel"), new Vector3(i, -0.1f, row), Quaternion.identity);
                        }
                        
                        obstacles.Add(new Vector3(i, 0, row), true);
                        last_was = true;
                        number_in_row++;
                    }
                    else
                    {
                        last_was = false;
                    }
                }
            }
            else
            {
                rows_since++;
            }
        }
    }

    public GameObject GetNearest(string tag, Vector3 pos)
    {
        GameObject nearest = null;
        int nearest_distance = int.MaxValue, temp;
        Character temp_char;
        foreach (var i in GameObject.FindGameObjectsWithTag( tag == "Enemy" ?  "Player" : "Enemy" ))
        {
            temp_char = i.GetComponent<Character>();
            temp = temp_char.GetDistanceChessboard(pos);
            if (temp < nearest_distance && temp_char.IsAlive())
            {
                nearest = i;
                nearest_distance = temp;
            }
        }

        return nearest;
    }

    public int GetTurnNum()
    {
        return turn_num;
    }

    public IEnumerator WaitInstantiate(GameObject obj, Vector3 pos, Quaternion q, float time)
    {
        yield return new WaitForSecondsRealtime(time);
        Instantiate(obj, pos, q);
    }

    public bool InstantiateItem(GameObject obj, Vector3 pos, Quaternion q, float time)
    {
        StartCoroutine(WaitInstantiate(obj, pos, q, time));
        return true;
    }

    public void ShowDescPanel(int id)
    {
        if (character_status.abilities[id] != null)
        {
            desc_panel.SetActive(true);
            desc_panel.GetComponentInChildren<TextMeshProUGUI>().text = character_status.abilities[id].GetDesc();
        }
    }
    public void HideDescPanel()
    {
        desc_panel.SetActive(false);
    }
}