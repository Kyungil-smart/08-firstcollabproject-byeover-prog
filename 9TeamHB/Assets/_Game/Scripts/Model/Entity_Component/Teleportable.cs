using MyGame2.Stage;
using UnityEngine;
using System.Collections;


public class Teleportable : IComponentData
{
    private EntityState _owner;

    public bool IsTeleporting;

    public Teleportable(EntityState owner)
    {
        _owner = owner;
    }

    
    // 연출 필요 시
    public void StartTeleportAnimation(StageState state, Vector3 destination)
    {
        var request = new ViewRequest
        {
            Id = _owner.Id,
            Callback = (v) => v.StartCoroutine(TeleportAnimation(v, destination))
        };
        
        state.Events.RaiseViewRequest(request);
    }

    IEnumerator TeleportAnimation(GridEntityView view, Vector3 destination)
    {
        view.transform.position = destination;
        yield break;
    }
}
