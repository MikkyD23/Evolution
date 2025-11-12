using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using Unity.VisualScripting;
using UnityEngine;

public class NodeManaging
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
            previousLayer = new();
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

    List<List<Node>> allNodeLayers(List<Node> outputLayer)
    {
        List<List<Node>> networkAcc = new();

        List<Node> currentLayer = new();
        currentLayer.AddRange(outputLayer);
        while(currentLayer.Count > 0)
        {
            networkAcc.Insert(0, currentLayer);
            List<Node> processNextLayer = currentLayer[0].previousLayerNodes();

            currentLayer = new();

            currentLayer.AddRange(processNextLayer);
        }

        return networkAcc;
    }

    public void mutateWholeNetwork(List<Node> outputLayer, float magnitude)
    {
        foreach (List<Node> layer in allNodeLayers(outputLayer))
        {
            foreach (Node n in layer)
            {
                n.adjustInputWeights(magnitude);
            }            
        }
    }

    public void deepCloneNetwork(List<Node> originalOutputLayer, Fighter applyTo)
    {
        string networkString = serializeNetwork(originalOutputLayer);
        applySerializedNetwork(networkString, applyTo);
    }

    [Serializable]
    public class serializableNetwork
    {
        public List<List<serializableNode>> nodeLayers;
    }
    [Serializable]
    public class serializableNode
    {
        public List<float> previousLayerWeights;
        public string forInputType;
        public string forOutputType;
    }

    public string serializeNetwork(List<Node> outputLayer)
    {
        serializableNetwork accSerial = new serializableNetwork();
        accSerial.nodeLayers = serializeDeepLayers(outputLayer);

        // https://stackoverflow.com/questions/8334527/save-listt-to-xml-file
        StringWriter stringWriter = new StringWriter(new StringBuilder());
        new XmlSerializer(typeof(serializableNetwork)).Serialize(stringWriter, accSerial);
        return stringWriter.ToString(); ;
    }

    List<serializableNode> serializeLayer(List<Node> layer)
    {
        List<serializableNode> acc = new();
        foreach (Node n in layer)
        {
            serializableNode addToAcc = new();
            addToAcc.previousLayerWeights = new();
            foreach (float weight in n.previousLayerWeights())
            {
                addToAcc.previousLayerWeights.Add(weight);
            }
            if(n is InputNode)
            {
                addToAcc.forInputType = (n as InputNode).inputType();
            }
            acc.Add(addToAcc);
        }
        return acc;
    }

    List<List<serializableNode>> serializeDeepLayers(List<Node> outputLayer)
    {
        List<List<serializableNode>> accLayers = new();

        foreach (List<Node> layer in allNodeLayers(outputLayer))
        {
            accLayers.Add(serializeLayer(layer));
        }

        return accLayers;
    }

    public void applySerializedNetwork(string nodeNetworkString, Fighter applyTo)
    {
        StringReader stringReader = new StringReader(nodeNetworkString);
        XmlSerializer serializer = new XmlSerializer(typeof(serializableNetwork));
        serializableNetwork deserializedNetwork = (serializableNetwork)serializer.Deserialize(stringReader);

        List<Node> currentLayer = new();
        // first input layers which are slightly different
        foreach (Fighter.inputType ipt in Enum.GetValues(typeof(Fighter.inputType)))
        {
            InputNode newNode = new InputNode();
            newNode.setupInputNode(applyTo, ipt);
            currentLayer.Add(newNode);
            // do some sort of correlation check to see if input nodes have changed
            // and give warning TODO
        }
        deserializedNetwork.nodeLayers.RemoveAt(0);

        foreach (List<serializableNode> layer in deserializedNetwork.nodeLayers)
        {
            List<Node> thisLayer = new();
            foreach (serializableNode n in layer)
            {
                Node newNode = new Node(currentLayer, n.previousLayerWeights);
                thisLayer.Add(newNode);
            }
            currentLayer = new();
            currentLayer.AddRange(thisLayer);
        }

        // add output layer reference to fighter
        applyTo.loadExistingNetwork(currentLayer);
    }

    public void recalculateNetworkOutputs(List<Node> outputLayer)
    {
        List<List<Node>> layers = allNodeLayers(outputLayer);

        foreach (List<Node> layer in layers)
        {
            foreach (Node n in layer)
            {
                n.recalculateOutputting();
            }
        }
    }
}
