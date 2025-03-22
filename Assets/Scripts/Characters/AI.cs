using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.TextCore.Text;
using static UnityEngine.GraphicsBuffer;

public abstract class AI : Character
{
    public bool waiting = false;

    public Vector3 ClosedSquareToPlayer(Vector3 player)
    {
        Vector3 best_square = new Vector3(-1, -1, -1);
        int best_distance = int.MaxValue;
        Vector3[] directions = { new Vector3(1, 0, 0), new Vector3(-1, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 0, -1),
                                 new Vector3(1, 0, 1), new Vector3(-1, 0, 1), new Vector3(-1, 0, 1), new Vector3(-1, 0, -1)};
        Vector3 temp;
        foreach(Vector3 direction in directions)
        {
            temp = direction + player;
            if (GetDistanceChessboard(temp) < best_distance && grid.CheckSquare(temp))
            {
                best_square = temp;
                best_distance = GetDistanceChessboard(temp);
            }
        }

        //if (best_square = new Vector3(-1, -1, -1)){
        return best_square;
    }

    public IEnumerator ChooseAction()
    {
        waiting = true;
        yield return new WaitForSecondsRealtime(0.5f);

        GameObject nearest = GetNearest();

        if (HasEffect("Silenced") || !ChooseAbility(nearest)) // If can act, act else move
            MoveTowards(nearest);
        
        transform.LookAt(nearest.transform.position);

        waiting = false;
    }

    public virtual GameObject GetNearest()
    {
        return grid.GetNearest("Enemy", transform.position);
    }

    public virtual void MoveTowards(GameObject nearest)
    {
        Vector3 nearest_square = ClosedSquareToPlayer(nearest.transform.position);
        Stack<Vector3> temp_points = grid.GetPoints(transform.position, nearest_square);

        if (temp_points.Contains(nearest_square))
        {
            temp_points = new Stack<Vector3>(temp_points);
            while (temp_points.Peek() != nearest_square)
            {
                temp_points.Pop();
            }
            temp_points = new Stack<Vector3>(temp_points);
        }

        if (GetMaxDistance() - 1 < temp_points.Count) // Saves 1 AP to Hunker after moving
        {
            temp_points = new Stack<Vector3>(temp_points.Take(GetMaxDistance() - 1).Reverse());
        }
        int ap_to_use = Mathf.CeilToInt(temp_points.Count / GetDistPerAP());

        if (temp_points.Count > 0 && GetDistanceChessboard(nearest.transform.position) > 1 && CheckAP(ap_to_use))
        {
            points = temp_points;
            LocationToMove();
        }
        else // Cant move
        {
            SetNextTurn();
        }
    }

    public abstract bool ChooseAbility(GameObject nearest);
}
