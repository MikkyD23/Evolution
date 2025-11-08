using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node
{
    class InputWeight
    {
        public Node node;
        public float weight;

        public InputWeight(Node useNode, float useWeight)
        {
            node = useNode;
            weight = useWeight;
        }
    }

    List<InputWeight> inputWeights = new();


    // creating nodes from file/cloned from serialized
    public Node(List<Node> previousLayerNodes, List<float> loadWeights)
    {
        for (int i = 0; i < previousLayerNodes.Count; i++)
        {
            inputWeights.Add(new InputWeight(previousLayerNodes[i], loadWeights[i]));
        }
    }

    // creating brand new nodes
    public Node(List<Node> previousLayerNodes)
    {
        float startingWeight = 1f / (float)previousLayerNodes.Count;
        foreach (Node n in previousLayerNodes)
        {
            inputWeights.Add(new InputWeight(n, startingWeight));
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
            percentActivated += item.node.isOutputting() ? Mathf.Max(item.weight, 0f) : 0f;
            if(percentActivated >= 1f)
            {
                return true;
            }
        }
        return false;
    }

    public void adjustInputWeights(float magnitude)
    {
        // setting slightly negative minimum so it doesn't constantly get randomised to a few percent
        const float MINIMUM_WEIGHT = -0.1f;
        foreach (InputWeight inputWeight in inputWeights)
        {
            inputWeight.weight += Random.Range(-magnitude, magnitude);
            inputWeight.weight = Mathf.Max(inputWeight.weight, MINIMUM_WEIGHT);
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
            nodeAcc.Add(item.node);
        }

        return nodeAcc;
    }

    public virtual List<float> previousLayerWeights() {
        List<float> accList = new();
        foreach (var item in inputWeights)
        {
            accList.Add(item.weight);
        }
        return accList;
    }
}
