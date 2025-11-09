using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputNode : Node
{
    Fighter host;
    Fighter.inputType forInputType;

    public void setupInputNode(Fighter useHost, Fighter.inputType useInputType)
    {
        host = useHost;
        forInputType = useInputType;
    }

    public InputNode() : base()
    {

    }

    public override void recalculateOutputting()
    {
        currentlyOutputting = host.checkInput(forInputType);
    }

    public override List<float> previousLayerWeights()
    {
        return new();
    }

    public string inputType()
    {
        return forInputType.ToString();
    }
}
