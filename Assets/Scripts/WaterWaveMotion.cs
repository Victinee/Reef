using UnityEngine;

public class WaterWaveMotion : MonoBehaviour
{
    public float scrollSpeedX = 0.02f;
    public float scrollSpeedY = 0.03f;
    private Renderer rend;

    void Start()
    {
        rend = GetComponent<Renderer>();
    }

    void Update()
    {
        if (rend != null && rend.sharedMaterial != null)
        {
            float offsetX = Time.time * scrollSpeedX;
            float offsetY = Time.time * scrollSpeedY;
            rend.sharedMaterial.SetTextureOffset("_BaseMap", new Vector2(offsetX, offsetY));
            if (rend.sharedMaterial.HasProperty("_BumpMap"))
            {
                rend.sharedMaterial.SetTextureOffset("_BumpMap", new Vector2(offsetX, offsetY));
            }
        }
    }
}
