using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Wander : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float directionChangeInterval = 2f;
    private Rigidbody2D rb;
    private Vector2 movement;
    private float timer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        ChangeDirection();
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            ChangeDirection();
            timer = directionChangeInterval;
        }

        // Flip sprite for aesthetics
        if (movement.x > 0)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }
        else if (movement.x < 0)
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
    }

    void FixedUpdate()
    {
        rb.velocity = movement * moveSpeed;
    }

    void ChangeDirection()
    {
        movement = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
    }
}
