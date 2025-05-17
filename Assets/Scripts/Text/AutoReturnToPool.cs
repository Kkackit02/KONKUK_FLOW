using UnityEngine;

public class AutoReturnToPool : MonoBehaviour
{

    // 움직임 감지 0.1 , 일반 테두리 - 0.5
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
