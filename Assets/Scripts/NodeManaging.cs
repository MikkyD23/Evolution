using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class NodeManaging : MonoBehaviour
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="intermediaryNodesForLayers"></param>
    /// <returns>The output nodes in no specific order</returns>
    public static List<Node> generateNetwork(List<int> intermediaryNodesForLayers, Fighter forFighter)
    {
        List<InputNode> inputNodes = new();
        Fighter.inputType[] inputTypes = (Fighter.inputType[])Enum.GetValues(typeof(Fighter.inputType));
        // always generate same amount of input and output notes (quantity of enum values)
        foreach (Fighter.inputType input in inputTypes)
        {
            InputNode newNode = new InputNode();
            newNode.setupInputNode(forFighter, input);
            inputNodes.Add(newNode);
        }

        List<Node> previousLayer = new();
        previousLayer.AddRange(inputNodes);
        foreach (int layerSize in intermediaryNodesForLayers)
        {
            List<Node> newLayer = createIntermediaryLayer(previousLayer, layerSize);
            previousLayer.Clear();
            previousLayer.AddRange(newLayer);
        }

        List<Node> outputNodes = new();
        Fighter.outputType[] outputTypes = (Fighter.outputType[])Enum.GetValues(typeof(Fighter.outputType));
        outputNodes.AddRange(createIntermediaryLayer(previousLayer, outputTypes.Length));

        return outputNodes;
    }

    static List<Node> createIntermediaryLayer(List<Node> previousLayer, int layerSize)
    {
        List<Node> layerAcc = new();
        for (int i = 0; i < layerSize; i++)
        {
            layerAcc.Add(new Node(previousLayer));
        }
        return layerAcc;
    }

    public static void mutateWholeNetwork(List<Node> outputLayer, float magnitude)
    {
        foreach (Node n in outputLayer)
        {
            n.adjustInputWeights(magnitude);
        }

        // all nodes are connected to entire previous layer so can use first ref
        List<Node> priorLayer = outputLayer[0].previousLayerNodes();
        if (priorLayer[0] is InputNode)
        {
            return;
        }
        else
        {
            mutateWholeNetwork(priorLayer, magnitude);
        }
    }

    void deepCloneNetwork(List<Node> originalOutputLayer, Fighter applyTo)
    {
        string networkString = serializeNetwork(originalOutputLayer);
        applySerializedNetwork(networkString, applyTo);
    }

    class serializableNetwork
    {
        public List<serializableNode> previousLayerWeights = new();
    }

    class serializableNode
    {
        public List<float> previousLayerWeights = new();

    }

    string serializeNetwork(List<Node> outputLayer)
    {
        List<float> inputWeights = new();

        return "";
    }

    void applySerializedNetwork(string networkString, Fighter applyTo)
    {

    }
}
