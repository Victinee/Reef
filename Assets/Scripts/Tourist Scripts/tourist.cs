using UnityEngine;

public class tourist : MonoBehaviour
{
    public int sundialValue = 10;
    public float moveSpeed = 2f;

    public Transform resort;

    void Update()
    {
        if (resort != null)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                resort.position,
                moveSpeed * Time.deltaTime
            );
        }
    }
}