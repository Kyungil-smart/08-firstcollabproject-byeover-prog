using UnityEngine;
using MyGame2.Stage;

public  class InteractionTag : IComponentData
{
    private bool _canInteractPlayerA;
    private bool _canInteractPlayerB;

    public bool A { get { return _canInteractPlayerA; } }
    public bool B { get { return _canInteractPlayerB; } }

    public InteractionTag(bool canInteractA, bool canInteractB)
    {
        _canInteractPlayerA = canInteractA;
        _canInteractPlayerB = canInteractB;
    }
}
