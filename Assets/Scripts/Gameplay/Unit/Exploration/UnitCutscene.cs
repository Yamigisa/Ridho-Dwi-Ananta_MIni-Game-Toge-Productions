using System.Collections;
using System.Globalization;
using UnityEngine;

[RequireComponent(typeof(UnitMovement))]
public class UnitCutscene : MonoBehaviour
{
    private UnitMovement unitMovement;
    private Coroutine cutsceneMovementRoutine;

    private void Awake()
    {
        unitMovement = GetComponent<UnitMovement>();
    }

    public void Move(string offset)
    {
        if (TryParseOffset(offset, out Vector2 parsedOffset))
            unitMovement.Move(parsedOffset.x, parsedOffset.y);
    }

    public void MoveAndWait(string offset)
    {
        BeginCutsceneMovement(offset, UnitMovement.MovementOrder.Direct);
    }

    public void MoveXThenY(string offset)
    {
        if (TryParseOffset(offset, out Vector2 parsedOffset))
            unitMovement.MoveXThenY(parsedOffset.x, parsedOffset.y);
    }

    public void MoveXThenYAndWait(string offset)
    {
        BeginCutsceneMovement(offset, UnitMovement.MovementOrder.XThenY);
    }

    public void MoveYThenX(string offset)
    {
        if (TryParseOffset(offset, out Vector2 parsedOffset))
            unitMovement.MoveYThenX(parsedOffset.x, parsedOffset.y);
    }

    public void MoveYThenXAndWait(string offset)
    {
        BeginCutsceneMovement(offset, UnitMovement.MovementOrder.YThenX);
    }

    private void BeginCutsceneMovement(string offset, UnitMovement.MovementOrder order)
    {
        if (cutsceneMovementRoutine != null)
            return;

        if (!TryParseOffset(offset, out Vector2 parsedOffset))
            return;

        cutsceneMovementRoutine =
            StartCoroutine(MoveAndResumeTimeline(parsedOffset, order));
    }

    private IEnumerator MoveAndResumeTimeline(
        Vector2 offset,
        UnitMovement.MovementOrder order)
    {
        switch (order)
        {
            case UnitMovement.MovementOrder.XThenY:
                unitMovement.MoveXThenY(offset.x, offset.y);
                break;

            case UnitMovement.MovementOrder.YThenX:
                unitMovement.MoveYThenX(offset.x, offset.y);
                break;

            default:
                unitMovement.Move(offset.x, offset.y);
                break;
        }

        bool pausedTimeline =
            NewTimelineManager.Instance != null &&
            NewTimelineManager.Instance.PauseTimeline();

        yield return new WaitUntil(() => !unitMovement.IsMovingToDestination);

        if (pausedTimeline && NewTimelineManager.Instance != null)
            NewTimelineManager.Instance.ResumeTimeline();

        cutsceneMovementRoutine = null;
    }

    private bool TryParseOffset(string offset, out Vector2 parsedOffset)
    {
        parsedOffset = Vector2.zero;

        if (string.IsNullOrWhiteSpace(offset))
        {
            Debug.LogWarning("Movement offset cannot be empty.");
            return false;
        }

        string[] values = offset.Split(',');

        if (values.Length != 2 ||
            !float.TryParse(
                values[0].Trim(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float x) ||
            !float.TryParse(
                values[1].Trim(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float y))
        {
            Debug.LogWarning(
                $"Invalid movement offset '{offset}'. Use the format x,y, for example 5,2."
            );
            return false;
        }

        parsedOffset = new Vector2(x, y);
        return true;
    }
}
