using UnityEngine;
using TMPro;

public class ConvoyResources : MonoBehaviour
{
    [Header("Resources")]
    [SerializeField] private int scrap = 0;

    [Header("UI")]
    [SerializeField] private TMP_Text scrapText;

    public int Scrap => scrap;

    private void Start()
    {
        UpdateUI();
    }

    public void AddScrap(int amount)
    {
        scrap += amount;

        UpdateUI();
    }

    public bool SpendScrap(int amount)
    {
        if (scrap < amount)
        {
            Debug.Log("Not enough scrap.");
            return false;
        }

        scrap -= amount;

        UpdateUI();

        return true;
    }

    private void UpdateUI()
    {
        if (scrapText != null)
        {
            scrapText.text = $"Scrap: {scrap}";
        }
    }
}