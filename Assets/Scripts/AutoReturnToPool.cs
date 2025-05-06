using UnityEngine;

public class AutoReturnToPool : MonoBehaviour
{

    // ������ ���� 0.1 , �Ϲ� �׵θ� - 0.5
    public float lifetime = 1.0f;

    private void OnEnable()
    {
        Invoke("Return", lifetime);
    }

    void Return()
    {
        ObjectPoolManager pool = FindObjectOfType<ObjectPoolManager>();
        pool.ReturnToPool(this.gameObject);
    }
}
