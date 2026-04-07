using MyGame2.Stage;
using System.Collections;
using UnityEngine;

public class Fallable : IComponentData
{
    private float _fallDuration;
    private float _fallDistance;
    private EntityState _owner;

    // 틈새에 끼인 상자의 sortingOrder
    private const int FallenSortingOrder = -3;

    private GridEntityView _runner;
    private Coroutine _coroutine;

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
            Callback = (v) =>
            {
                _runner = v;
                _coroutine = v.StartCoroutine(FallAnimation(v));
            }
        };

        state.Events.RaiseViewRequest(request);
    }

    public void StopFallAnimation()
    {
        _runner.StopCoroutine(_coroutine);
    }

    IEnumerator FallAnimation(GridEntityView view)
    {
        view.MarkAsFalling();

        Transform firstChild = null;

        if (view.transform.childCount > 0)
        {
            firstChild = view.transform.GetChild(0);
        }
        if (!firstChild) yield break;

        yield return new WaitForSeconds(0.3f);

        Vector3 targetPosition = firstChild.position + Vector3.down * _fallDistance;
        Vector3 startPosition = firstChild.position;
        float elapsed = 0f;


        SpriteRenderer renderer = firstChild.GetComponent<SpriteRenderer>();
        // 틈새에 끼인 상자는 -3으로 설정 (바닥 타일보다 아래, 틈새 타일보다 위)
        renderer.sortingOrder = FallenSortingOrder;

        while (elapsed < _fallDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _fallDuration;
            firstChild.position =
                Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }
        firstChild.position = targetPosition;

        // Undo 시 복원할 수 있도록 플래그 설정
        view.MarkAsFallen();
    }
}