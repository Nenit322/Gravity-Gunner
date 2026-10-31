using UnityEngine;

public class FirstPersonLook : MonoBehaviour
{

    [SerializeField] GameSession session;

    [SerializeField] Transform player;

    [SerializeField] float sensitivity = 2f;

    [SerializeField] float pitchLimit = 85f;

    float pitch;

    void Update()
    {

        if (!session.CanPlay) return;

        // Mouse axes already represent displacement per frame, so no deltaTime here.
        player.Rotate(Vector3.up * Input.GetAxisRaw("Mouse X") * sensitivity);

        pitch = Mathf.Clamp(pitch - Input.GetAxisRaw("Mouse Y") * sensitivity, -pitchLimit, pitchLimit);

        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

    }

}

