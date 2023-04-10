using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Uses an L system to generate a sequence of roads, and then creates a mesh for each of them
/// </summary>

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
