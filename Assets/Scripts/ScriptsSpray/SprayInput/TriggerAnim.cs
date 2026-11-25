using UnityEngine;

[RequireComponent(typeof(SprayInput))]
public class TriggerAnim : MonoBehaviour
{
    [SerializeField] private Transform trigger;
    [SerializeField] private Vector3 pressedOffset;
    [SerializeField] private Vector3 pressedEulerAngles;
    [SerializeField, Min(0.01f)] private float travelTime = 0.07f;

    private SprayInput input;
    private Vector3 restPosition;
    private Quaternion restRotation;
    private float travel;

    private void Awake()
    {
        input = GetComponent<SprayInput>();
        restPosition = trigger.localPosition;
        restRotation = trigger.localRotation;
    }

    private void LateUpdate()
    {
        travel = Mathf.MoveTowards(travel, input.TriggerPressure, Time.deltaTime / travelTime);
        trigger.localPosition = restPosition + pressedOffset * travel;
        trigger.localRotation = restRotation *
            Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(pressedEulerAngles), travel);
    }
}
