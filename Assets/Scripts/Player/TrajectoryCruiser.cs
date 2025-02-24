using UnityEngine;

public class TrajectoryCruiser : MonoBehaviour
{
    [SerializeField]
    GameObject _trajectory;

    [SerializeField]
    float _speed = 1f;

    [SerializeField]
    float _rotationSpeed = 1f;

    int _currentWaypointIndex = 0;

    void Update()
    {
        Transform nextWaypoint;

        if (_currentWaypointIndex < _trajectory.transform.childCount - 1)
            nextWaypoint = _trajectory.transform.GetChild(_currentWaypointIndex + 1);
        else
            nextWaypoint = _trajectory.transform.GetChild(0);

        var targetRotation = Quaternion.LookRotation(nextWaypoint.position - transform.position);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);

        transform.position = transform.position + _speed * Time.deltaTime * transform.forward;

        if (Vector3.Distance(transform.position, nextWaypoint.position) < 1f)
            _currentWaypointIndex = (_currentWaypointIndex + 1) % _trajectory.transform.childCount;
    }
}
