using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonMovement : MonoBehaviour
{

    [SerializeField] GameSession session;

    [SerializeField] float speed = 5f;

    [SerializeField] float gravity = -20f;

    CharacterController controller;

    float verticalSpeed;

    void Awake()
    {

        controller = GetComponent<CharacterController>();

    }

    void Update()
    {

        if (!session.CanPlay)
        {

            return;

        }

        Vector2 input = Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);

        if (controller.isGrounded && verticalSpeed < 0f)
        {

            verticalSpeed = -2f;

        }

        verticalSpeed += gravity * Time.deltaTime;

        Vector3 velocity = (transform.right * input.x + transform.forward * input.y) * speed;

        velocity.y = verticalSpeed;

        controller.Move(velocity * Time.deltaTime);

    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {

        Grenade grenade = hit.collider.GetComponentInParent<Grenade>();

        if (grenade != null)
        {

            grenade.HitPlayer(GetComponent<Health>());

        }

    }

}

