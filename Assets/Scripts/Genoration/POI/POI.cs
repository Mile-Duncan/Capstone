using System;
using System.Collections;
using TMPro;
using UnityEngine;

public abstract class POI : MonoBehaviour
{
    protected int UnitsStored;
    protected int UnitsTotal;
    protected int ExperationTime;
    protected int Reward;

    public int MaxUnits {get; protected set;}
    public int MinUnits{get; protected set;}
    public int MaxReward{get; protected set;}
    public int MinReward{get; protected set;}
    public int MaxTime{get; protected set;}
    public int MinTime{get; protected set;}
    public string ContractText{get; protected set;}
    
    protected CarCargo CargoPrefab;
    public const float Range = 50;
    
    [SerializeField] private TextMeshProUGUI TimeText;
    [SerializeField] private TextMeshProUGUI UnitsText;
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Start()
    {
        StartCoroutine(LoadCheck());
    }

    public void NewContract(int units, int time, int reward)
    {
        UnitsTotal = units;
        UnitsStored = units;
        
        ExperationTime = time + (int)Time.fixedTime;
        Reward = reward;

    }
    // Update is called once per frame
    IEnumerator LoadCheck()
    {
        while (true)
        {
            UpdateText();

            yield return new WaitForSeconds(1);
            if (UnitsStored <= 0)
            {
                if (Reward > 0)
                {
                    ScoreHandler.AddCash(Reward);
                    Reward = 0;
                    UnitsTotal = 0;
                }
                continue;
            }

            if (ExperationTime < Time.time)
            {
                Reward = 0;
                UnitsStored = 0;
                UnitsTotal = 0;
            }
            
            foreach (TrainSet train in TrainSet.TrainSets) foreach (TrainCar car in train.Cars)
            {
                LoadLoop(car);
            }
        }
    }

    private void UpdateText()
    {
        float timeRemaining = ExperationTime - Time.time;
        if (timeRemaining >= 0 && UnitsStored > 0)
        {
            TimeText.text = FormatTime(timeRemaining, true);
            UnitsText.text = (UnitsTotal - UnitsStored) + " / " + UnitsTotal;
        }
        else
        {
            TimeText.text = "";
            UnitsText.text = "";
        }
        
    }
    
    public static string FormatTime(float totalSeconds, bool showHours = false)
    {
        // 1. Create a TimeSpan object from the total seconds
        TimeSpan timeSpan = TimeSpan.FromSeconds(totalSeconds);

        int hours = timeSpan.Hours;
        int minutes = timeSpan.Minutes;
        int seconds = timeSpan.Seconds;
        int totalMinutes = (int)timeSpan.TotalMinutes;

        // 2. Determine the appropriate format string

        // If the duration is 1 hour or more, or if showHours is true, use HH:MM:SS
        if (timeSpan.TotalHours >= 1 || showHours)
        {
            // Note: We use TotalHours to capture durations like 25 hours, which TimeSpan.Hours would cap at 1.
            int displayHours = (int)timeSpan.TotalHours;
            return $"{displayHours:D2}:{minutes:D2}:{seconds:D2}";
        }
        else
        {
            // For durations under 1 hour, use MM:SS (using total minutes for durations like 59:30)
            return $"{totalMinutes:D2}:{seconds:D2}";
        }
    }
    protected abstract void LoadLoop(TrainCar car);
}
