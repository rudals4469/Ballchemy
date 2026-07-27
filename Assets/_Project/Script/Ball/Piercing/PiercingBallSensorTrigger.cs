using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class PiercingBallSensorTrigger :
    MonoBehaviour
{
    [SerializeField]
    private PiercingBallSensor owner;

    public void Initialize(
        PiercingBallSensor sensorOwner)
    {
        owner =
            sensorOwner;
    }

    private void Awake()
    {
        FindOwner();
    }

    private void OnValidate()
    {
        FindOwner();
    }

    private void FindOwner()
    {
        if (owner != null)
        {
            return;
        }

        owner =
            GetComponentInParent<
                PiercingBallSensor
            >();
    }

    private void OnTriggerEnter2D(
        Collider2D other)
    {
        if (owner == null)
        {
            FindOwner();
        }

        owner?.HandleSensorEnter(
            other
        );
    }

    private void OnTriggerExit2D(
        Collider2D other)
    {
        if (owner == null)
        {
            FindOwner();
        }

        owner?.HandleSensorExit(
            other
        );
    }
}