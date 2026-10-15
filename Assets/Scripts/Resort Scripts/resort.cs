
using UnityEngine;

public class resort : MonoBehaviour
{
    public sundials sundialsManager;

    private void OnTriggerEnter2D(Collider2D other)
    {
        tourist visitor = other.GetComponent<tourist>();

        if (visitor != null)
        {
            sundialsManager.AddSundial(visitor.sundialValue);

            Destroy(visitor.gameObject);
        }
    }
}
