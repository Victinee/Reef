
using UnityEngine;
using TMPro;

public class sundials : MonoBehaviour
{
    public int sundial = 0;
    public TMP_Text sundialCounter;

    public void AddSundial(int amount)
    {
        sundial += amount;

        if (sundialCounter != null)
        {
            sundialCounter.text = "Sundials: " + sundial;
        }
    }
}
