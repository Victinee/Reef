using UnityEngine;
using TMPro;

public class CurrencyManager : MonoBehaviour
{
    public int sundial = 0;
    public TMP_Text sundialText;

    public void AddSundial(int amount)
    {
        sundial += amount;

        sundialText.text = sundial.ToString();
    }
}