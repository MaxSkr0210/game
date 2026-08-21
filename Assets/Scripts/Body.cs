using UnityEngine;

public static class Body
{
    public static void Setup(Rigidbody2D rb, float damping, float mass)
    {
        rb.gravityScale = 0f;
        rb.mass = mass;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
#if UNITY_6000_0_OR_NEWER
        rb.linearDamping = damping;
        rb.angularDamping = 0.05f;
#else
        rb.drag = damping;
        rb.angularDrag = 0.05f;
#endif
    }

    public static void SetVelocity(Rigidbody2D rb, Vector2 velocity)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = velocity;
#else
        rb.velocity = velocity;
#endif
    }
}
