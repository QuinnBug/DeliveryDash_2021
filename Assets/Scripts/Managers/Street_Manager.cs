using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Street_Manager : Singleton<Street_Manager>
{
    public bool testGeneration = false;

    public GameObject roadPrefab;
    [Space]
    public LSystem lSys = new LSystem();
    [Space]
    public int count = 5;
    public int length;
    public Vector2 angles;
    public Vector2Int extraLength;
    public float timePerRoad;

    List<Vector3> points = new List<Vector3>();

    #region popcenter system
    //List<Vector3> populationCenters;
    //List<Road> roads = new List<Road>();
    //public Vector3 bottomLeft;
    //public Vector3 topRight;

    //public int popCenterCounts;
    //public int popCenterDistance;
    //public int popCenterMinDistance;

    //void GenerateStreets() 
    //{

    //}

    //void PlacePopulationCenters(int count, float distance, int pointsPerPoint)
    //{
    //    populationCenters = new List<Vector3>();

    //    if (count < 2) count = 2;

    //    Vector3 midPoint = Vector3.Lerp(bottomLeft, topRight, 0.5f);

    //    Queue<Vector3> points = new Queue<Vector3>();

    //    for (int i = 0; i < pointsPerPoint; i++)
    //    {
    //        points.Enqueue(midPoint);
    //    }

    //    for (int i = 0; i < count; i++)
    //    {
    //        Vector3 direction = Random.insideUnitCircle.normalized * distance;
    //        direction.z = direction.y;
    //        direction.y = 0;

    //        Vector3 startPoint = points.Dequeue();
    //        for (int j = 0; j < pointsPerPoint; j++)
    //        {
    //            points.Enqueue(startPoint + direction);
    //        }   

    //        populationCenters.Add(startPoint + direction);
    //    }

    //    CullPopulationCenters();
    //}

    //void CullPopulationCenters() 
    //{
    //    List<int> indexes = new List<int>();

    //    for (int i = 0; i < populationCenters.Count; i++)
    //    {
    //        if (indexes.Contains(i)) continue;

    //        for (int j = 0; j < populationCenters.Count; j++)
    //        {
    //            if (j == i) continue;

    //            if (Vector3.Distance(populationCenters[i], populationCenters[j]) < popCenterMinDistance)
    //            {
    //                if (!indexes.Contains(j)) indexes.Add(j);
    //            }
    //        }
    //    }

    //    indexes.Sort();
    //    indexes.Reverse();
    //    foreach (int index in indexes)
    //    {
    //        populationCenters.RemoveAt(index);
    //    }
    //    Debug.Log("point count = " + populationCenters.Count);
    //}
    #endregion

    public void VisualizeSequence() 
    {
        lSys.GenerateSequence(count);
        StartCoroutine(DelayedInstructionExecution(lSys.finalString));
    }

    public IEnumerator DelayedInstructionExecution(string sequence)
    {
        int roadCount = 0;
        Stack<LAgent> savePoints = new Stack<LAgent>();
        Vector3 currentPos = transform.position;
        Vector3 tempPos = currentPos;
        Vector3 direction = Vector3.forward;

        points.Add(currentPos);

        foreach (char letter in sequence)
        {
            Instructions _instruction = (Instructions)letter;
            switch (_instruction)
            {
                case Instructions.DRAW:
                    CreateRoad(currentPos, tempPos, roadCount);
                    roadCount++;
                    tempPos = currentPos;
                    break;
                case Instructions.MOVE:
                    currentPos += direction * (length + Random.Range(extraLength.x, extraLength.y));
                    break;
                case Instructions.LEFT_TURN:
                    direction = Quaternion.Euler(0, Random.Range(angles.x, angles.y) * -1, 0) * direction;
                    break;
                case Instructions.RIGHT_TURN:
                    direction = Quaternion.Euler(0, Random.Range(angles.x, angles.y), 0) * direction;
                    break;

                case Instructions.SAVE:
                    savePoints.Push(new LAgent(currentPos, tempPos, direction, length));
                    break;

                case Instructions.LOAD:
                    if (savePoints.Count > 0)
                    {
                        LAgent ag = savePoints.Pop();
                        currentPos = ag.position;
                        tempPos = ag.tempPos;
                        direction = ag.direction;
                        length = ag.length;
                    }
                    break;
                default:
                    break;
            }
            yield return new WaitForSeconds(timePerRoad);
        }
    }

    private void CreateRoad(Vector3 start, Vector3 end, int num)
    {
        GameObject roadObj = Instantiate(roadPrefab, start, Quaternion.identity, transform);
        Road road = roadObj.GetComponent<Road>();
        road.Init(start, end);
        road.GenerateMesh();
        roadObj.name = roadObj.name + num.ToString();
    }
}

[System.Serializable]
public class LSystem
{
    public List<LRule> rules;
    public string axiom;

    public string finalString;
    [Space]
    public bool CancelRun;

    public void IterateTree()
    {
        string newLine = finalString;
        foreach (LRule rule in rules)
        {
            newLine = rule.Pass(newLine);
        }

        //Debug.Log(newLine);
        finalString = newLine;
    }

    public void GenerateSequence(int iterations)
    {
        finalString = axiom;
        for (int i = 0; i < iterations; i++)
        {
            IterateTree();
        }
    }

    public IEnumerator RunCoRoutine(int iterations, float _delay)
    {
        finalString = axiom;
        for (int i = 0; i < iterations; i++)
        {
            IterateTree();
            yield return new WaitForSeconds(_delay);

            if (CancelRun)
            {
                yield return null;
            }
        }
    }
}

[System.Serializable]
public struct LRule 
{
    public string input;
    public string output;

    public LRule(string _in, string _out) 
    {
        input = _in;
        output = _out;
    }

    public string Pass(string line) 
    {
        if (line.Contains(input))
        {
            line = line.Replace(input, output);
        }

        return line;

    }
}

public class LAgent
{
    public Vector3 position, tempPos, direction;
    public int length;

    public LAgent(Vector3 pos, Vector3 tPos, Vector3 dir, int len)
    {
        position = pos;
        direction = dir;
        tempPos = tPos;
        length = len;
    }
}

public enum Instructions
{
    DRAW = 'x',
    MOVE = 'f',
    LEFT_TURN = '<',
    RIGHT_TURN = '>',
    SAVE = '[',
    LOAD = ']'
}
