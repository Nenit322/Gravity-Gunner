using UnityEngine;

public class ExplosionFlash : MonoBehaviour
{

    float remaining = 0.25f;

    public static void Show(Vector3 position, float radius)
    {

        var flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);

        flash.name = "ExplosionFlash"; flash.transform.position = position;

        var shape = flash.GetComponent<Collider>();

        shape.enabled = false;

        Destroy(shape);

        flash.transform.localScale = Vector3.one * 0.7f;

        flash.GetComponent<Renderer>().sharedMaterial = Resources.Load<Material>("ExplosionFlash");

        flash.AddComponent<ExplosionFlash>();

    }

    void Update()
    {

        remaining -= Time.deltaTime;

        transform.localScale += Vector3.one * (6f * Time.deltaTime);

        if (remaining <= 0f) Destroy(gameObject);

    }

}

