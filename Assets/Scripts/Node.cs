using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node
{
    float STARTING_WEIGHT = 0f; // I guess this is in the middle because it can go negative?

    Dictionary<Node, float> inputWeights = new();


    // creating nodes from file/cloned from serialized
    public Node(List<Node> previousLayerNodes, List<float> loadWeights)
    {
        for (int i = 0; i < previousLayerNodes.Count; i++)
        {
            inputWeights.Add(previousLayerNodes[i], loadWeights[i]);
        }
    }

    // creating brand new nodes
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


    public virtual bool isOutputting()
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

    public void adjustInputWeights(float magnitude)
    {
        foreach (Node item in previousLayerNodes())
        {
            inputWeights[item] += Random.Range(-magnitude, magnitude);
        }
    }

    /// <summary>
    /// Gives references to previous layer so things can recursively see the whole network
    /// </summary>
    /// <returns></returns>
    public List<Node> previousLayerNodes()
    {
        List<Node> nodeAcc = new();
        foreach (var item in inputWeights)
        {
            nodeAcc.Add(item.Key);
        }

        return nodeAcc;
    }

    public virtual List<float> previousLayerWeights() {
        List<float> accList = new();
        foreach (var item in inputWeights)
        {
            accList.Add(item.Value);
        }
        return accList;
    }
}
