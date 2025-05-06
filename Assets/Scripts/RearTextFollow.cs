using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RearTextFollower : MonoBehaviour
{
    public Transform parentTarget;
    private Rigidbody rb;

    [Header("�̵� ����")]
    public float followForce = 500f;
    public float bounceForce = 300f;
    public float maxSpeed = 500f;

    [Header("ƨ�� �� ���� �ð�")]
    public float recoveryDelay = 0.5f;

    private bool isBouncing = false;
    private float bounceTimer = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (parentTarget == null)
        {
            parentTarget = transform.parent;
        }
    }

    void FixedUpdate()
    {
        if (isBouncing)
        {
            bounceTimer -= Time.fixedDeltaTime;
            if (bounceTimer <= 0f)
            {
                isBouncing = false;
            }
        }

        if (!isBouncing && parentTarget != null)
        {
            Vector3 dir = (parentTarget.position - transform.position).normalized;
            rb.AddForce(dir * followForce, ForceMode.Force);

            if (rb.velocity.magnitude > maxSpeed)
            {
                rb.velocity = rb.velocity.normalized * maxSpeed;
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Contour"))
        {
            Vector3 impactDir = (transform.position - collision.contacts[0].point).normalized;

            rb.AddForce(impactDir * bounceForce, ForceMode.Force); // Force�� ������ �б�

            isBouncing = true;
            bounceTimer = recoveryDelay;
        }
    }
}
