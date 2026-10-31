using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BoxMassLabel : MonoBehaviour
{

    Rigidbody body;

    void Awake() { body = GetComponent<Rigidbody>(); }
    void OnGUI()
    {

        if (Camera.main == null) return;

        Vector3 point = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 0.7f);

        if (point.z > 0f) GUI.Label(new Rect(point.x - 35, Screen.height - point.y, 90, 24), $"{body.mass:0} kg");

    }

}

