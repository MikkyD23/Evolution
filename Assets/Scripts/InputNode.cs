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



    protected override bool isOutputting()
    {
        return host.checkInput(forInputType);
    }

}
