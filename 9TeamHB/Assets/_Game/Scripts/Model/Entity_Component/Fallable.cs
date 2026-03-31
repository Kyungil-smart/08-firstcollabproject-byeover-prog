using System.Collections;
using MyGame2.Stage;
using UnityEngine;

public class Fallable : IComponentData
{
    private float _fallDuration;
    private float _fallDistance;
    private EntityState _owner;

    public Fallable(float duration, float distance, EntityState owner)
    {
        _fallDuration = duration;
        _fallDistance = distance;
        _owner = owner;
    }
    public void StartFallAnimation(StageState state)
    {
        var request = new ViewRequest
        {
            Id = _owner.Id,
            Callback = (v) => v.StartCoroutine(FallAnimation(v))
        };

        state.Events.RaiseViewRequest(request);
    }

    IEnumerator FallAnimation(GridEntityView view)
    {
        Transform firstChild = null;
            
        if (view.transform.childCount > 0)
        {
            firstChild = view.transform.GetChild(0);
        }
        if(!firstChild) yield break;
            
        yield return new WaitForSeconds(0.3f);
        
        SpriteRenderer renderer = firstChild.GetComponent<SpriteRenderer>();
        renderer.sortingOrder -= 1;
        
        Vector3 targetPosition = firstChild.position + Vector3.down * _fallDistance;
        Vector3 startPosition = firstChild.position;
        float elapsed = 0f;

        while (elapsed < _fallDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _fallDuration;
            firstChild.position = 
                Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }
        firstChild.position = targetPosition;
    }
}
