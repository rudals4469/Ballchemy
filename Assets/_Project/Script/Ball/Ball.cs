using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public sealed class Ball : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0.1f)]
    private float moveSpeed = 12f;

    [Header("Combat")]
    [SerializeField, Min(1)]
    private int damage = 1;

    private Rigidbody2D rigidbody2D;
    private bool isMoving;

    public event Action<Ball> Returned;

    public bool IsMoving => isMoving;

    private void Awake()
    {
        rigidbody2D = GetComponent<Rigidbody2D>();

        StopMovement();
    }

    private void FixedUpdate()
    {
        if (!isMoving)
        {
            return;
        }

        // 충돌 후에도 공 속력이 일정하게 유지되도록 보정한다.
        Vector2 currentVelocity = rigidbody2D.linearVelocity;

        if (currentVelocity.sqrMagnitude > 0.01f)
        {
            rigidbody2D.linearVelocity =
                currentVelocity.normalized * moveSpeed;
        }
    }

    public void Launch(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        isMoving = true;
        rigidbody2D.simulated = true;
        rigidbody2D.linearVelocity = direction.normalized * moveSpeed;
    }

    public void ResetTo(Vector2 position)
    {
        StopMovement();
        rigidbody2D.position = position;
        transform.position = position;
    }

    private void StopMovement()
    {
        isMoving = false;

        if (rigidbody2D == null)
        {
            return;
        }

        rigidbody2D.linearVelocity = Vector2.zero;
        rigidbody2D.angularVelocity = 0f;
        rigidbody2D.simulated = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isMoving)
        {
            return;
        }

        if (collision.gameObject.TryGetComponent(
            out Block block))
        {
            block.TakeDamage(damage);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isMoving)
        {
            return;
        }

        if (!other.CompareTag("ReturnZone"))
        {
            return;
        }

        StopMovement();
        Returned?.Invoke(this);
    }
}