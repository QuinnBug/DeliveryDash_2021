using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Road_Manager : Singleton<Road_Manager>
{
    public bool testConnections = false;

    public GameObject roadPrefab;
    [Space]
    public LSystem lSys = new LSystem();
    [Space]
    public int count = 5;
    public int length;
    public float[] angles;
    //public float[] lengths;
    public float timePerRoad;


    List<Vector3> points = new List<Vector3>();
    internal List<Road> roads = new List<Road>();

    bool initDone = false;
    bool testRunning = false;

    public void Start()
    {
        Event_Manager.Instance._OnTerrainGenerated.AddListener(VisualizeSequence);
    }

    public void Update()
    {
        if (testConnections && initDone && !testRunning)
        {
            StartCoroutine(ConnectionTest());
        }
    }

    private IEnumerator ConnectionTest()
    {
        float delay = 1;
        testRunning = true;
        Road currentRoad = roads[0];
        RoadConnection connection = currentRoad.startConnectedRoads.Count > 0 ? RoadConnection.START : RoadConnection.END;
        Road nextRoad = currentRoad.GetRandomConnected(connection);

        Vector3 rayStart, rayEnd;

        while(testConnections)
        {
            Debug.Log(currentRoad.gameObject.name + " -> " + nextRoad.gameObject.name + " @ " + connection.ToString());
            rayStart = currentRoad.startPoint;
            rayEnd = currentRoad.endPoint;

            Debug.DrawLine(rayStart, rayEnd, Color.cyan, delay + 600);

            if (nextRoad.startConnectedRoads.Count > 0 && nextRoad.startConnectedRoads.Contains(currentRoad)) 
            {
                connection = RoadConnection.END;
            }
            else if(nextRoad.endConnectedRoads.Count > 0 && nextRoad.endConnectedRoads.Contains(currentRoad))
            {
                connection = RoadConnection.START;
            }
            else
            {
                Debug.Log("There's an error here");
            }

            switch (connection)
            {
                case RoadConnection.START:
                    if (nextRoad.startConnectedRoads.Count == 0)
                    {
                        connection = RoadConnection.END;
                    }
                    break;
                case RoadConnection.END:
                    if (nextRoad.endConnectedRoads.Count == 0)
                    {
                        connection = RoadConnection.START;
                    }
                    break;
            }

            currentRoad = nextRoad;
            nextRoad = currentRoad.GetRandomConnected(connection);

            yield return new WaitForSeconds(delay);
        }
        testRunning = false;
    }

    public void VisualizeSequence() 
    {
        lSys.GenerateSequence(count);
        StartCoroutine(CreateRoadsCoroutine(lSys.finalString));
        initDone = true;
    }

    public IEnumerator CreateRoadsCoroutine(string sequence)
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
                    currentPos += direction * length;
                    CreateRoad(currentPos, tempPos, roadCount);
                    roadCount++;
                    tempPos = currentPos;
                    break;
                case Instructions.LEFT_TURN:
                    direction = Quaternion.Euler(0, angles[Random.Range(0, angles.Length)] * -1, 0) * direction;
                    break;
                case Instructions.RIGHT_TURN:
                    direction = Quaternion.Euler(0, angles[Random.Range(0, angles.Length)], 0) * direction;
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
        SetUpRoadConnections();

        Debug.Log("Roads Done");
        Event_Manager.Instance._OnRoadsGenerated.Invoke();
    }

    private void CreateRoad(Vector3 start, Vector3 end, int num)
    {
        GameObject roadObj = Instantiate(roadPrefab, start, Quaternion.identity, transform);
        Road road = roadObj.GetComponent<Road>();
        roadObj.name = roadObj.name + num.ToString();
        if (road.Init(start, end)) roads.Add(road);
    }

    public void DestroyRoad(Road road) 
    {
        roads.Remove(road);
        Destroy(road.gameObject, 0.01f);
    }

    
    void SetUpRoadConnections()
    {
        foreach (Road road in Road_Manager.Instance.roads)
        {
            road.SetupConnections();
        }
    }

}

[System.Serializable]
public class LSystem
{
    public List<LRule> rules;
    public string axiom;

    public string finalString;
    [Space]
    public bool randomRuleOutput;

    HashSet<char> inputList = new HashSet<char>();

    public void IterateTree()
    {
        string newLine = "";
        foreach (char character in finalString)
        {
            if (!inputList.Contains(character))
            {
                newLine += character;
            }
            else
            {
                foreach (LRule rule in rules)
                {
                    if (rule.input == character)
                    {
                        newLine += rule.FetchOutput(randomRuleOutput);
                    }
                }
            }
        }

        //string newLine = finalString;
        //foreach (LRule rule in rules)
        //{
        //    newLine = rule.Pass(newLine, randomRuleOutput);
        //}


        //Debug.Log(newLine);
        finalString = newLine;
    }

    public void GenerateSequence(int iterations)
    {
        foreach(LRule rule in rules) 
        {
            inputList.Add(rule.input);
        }

        finalString = axiom;

        for (int i = 0; i < iterations; i++)
        {
            IterateTree();
        }
    }
}

[System.Serializable]
public struct LRule 
{
    public char input;
    public string[] outputs;

    public LRule(char _in, string _out) 
    {
        input = _in;
        outputs = new string[]{_out};
    }

    public string Pass(string line, bool randomOutput) 
    {
        if (line.Contains(input.ToString()))
        {
            int index = randomOutput ? Random.Range(0, outputs.Length) : 0;
            line = line.Replace(input.ToString(), outputs[index]);
        }

        return line;

    }

    public string FetchOutput(bool randomOutput) 
    {
        int index = randomOutput ? Random.Range(0, outputs.Length) : 0;
        return outputs[index];
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
    DRAW = 'f',
    LEFT_TURN = '<',
    RIGHT_TURN = '>',
    SAVE = '[',
    LOAD = ']'
}
