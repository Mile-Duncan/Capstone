using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ScoreHandler : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI CashText;
    [SerializeField] private TextMeshProUGUI ScoreText;
    private static ScoreHandler Instance;
    
    private static int score;

    private static int cash;
    
    private static LooseScreen looseScreen;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        looseScreen = Resources.Load("UI/GameOverUI").GetComponent<LooseScreen>();
        Instance = this;
        StartCoroutine(UpkeepLoop());
        cash = 10000;
        UpdateVisuals();
    }

    IEnumerator UpkeepLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            cash -= (int)(RailNetwork.GetTotalLength() / 100f);
            cash -= TrainSet.TrainSets.Count*20;
            UpdateVisuals();
        }
    }

    public static int AddScore(int s = 0)
    {
        score += s;
        UpdateVisuals();
        return score;
    }

    public static int AddCash(int c = 0)
    {
        cash += c;
        UpdateVisuals();
        return cash;
    }

    private static void UpdateVisuals()
    {
        Instance.CashText.text = "Cash: $" + cash;
        Instance.ScoreText.text = "Score: " + score;

        if (cash < 0)
        {
            looseScreen.ScoreText.text = Instance.ScoreText.text;
            Instantiate(looseScreen.gameObject);
        }
    }
    
    
}
