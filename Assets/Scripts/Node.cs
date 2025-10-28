using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node
{
    float STARTING_WEIGHT = 0f; // I guess this is in the middle because it can go negative?

    Dictionary<Node, float> inputWeights = new();


    public Node(List<Node> previousLayerNodes)
    {
        foreach (Node n in previousLayerNodes)
        {
            inputWeights.Add(n, STARTING_WEIGHT);
        }
    }

    public Node()
    {

    }


    protected virtual bool isOutputting()
    {
        float percentActivated = 0f;
        foreach (var item in inputWeights)
        {
            percentActivated += item.Key.isOutputting() ? item.Value : 0f;
            if(percentActivated >= 1f)
            {
                return true;
            }
        }
        return false;
    }

    void adjustInputWeights(float magnitude)
    {
        foreach (var item in inputWeights)
        {
            inputWeights[item.Key] += Random.Range(-magnitude, magnitude);
        }

    }
}
