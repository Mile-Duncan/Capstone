using System.Collections.Generic;
using TMPro;
using Unity.Mathematics.Geometry;
using UnityEngine;
using UnityEngine.UI;
public class ContractMenuUI : GameUIControler
{
    private static readonly System.Random Rng = new System.Random(); 
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private Button Option1;
    [SerializeField] private Button Option2;
    [SerializeField] private Button Option3;
    [SerializeField] private TextMeshProUGUI Title;

    public struct Options
    {
        public POI location;
        public int time;
        public int reward;
        public int units;
    }
    
    Options[] currentOptions;

    private Dictionary<POI, Options[]> _currentActiveOptions = new Dictionary<POI, ContractMenuUI.Options[]>();
    void Start()
    {
        Option1.onClick.AddListener(OnClickOption1);
        Option2.onClick.AddListener(OnClickOption2);
        Option3.onClick.AddListener(OnClickOption3);
        
        PlayerMovment.BreakEvent.AddListener(DisplayContractMenu);
        gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void DisplayContractMenu()
    {
        if(CurrentState != UIState.Move) return;
        DisableButtons();
        
        
        RaycastHit ray = PlayerMovment.Instance.GetMousePositionInWorld();
        POI location = ray.collider.gameObject.GetComponent<POI>();
        if (location == null) return;
        gameObject.SetActive(true);
        Options[] options = CreateContracts(location);
        
        Title.text = location.ContractText;

        Option1.GetComponentInChildren<TextMeshProUGUI>().text = "Take " + options[0].units + " shipments of cargo in less then " + POI.FormatTime(options[0].time) + " for $"+options[0].reward;
        Option2.GetComponentInChildren<TextMeshProUGUI>().text = "Take " + options[1].units + " shipments of cargo in less then " + POI.FormatTime(options[1].time) + " for $"+options[1].reward;
        Option3.GetComponentInChildren<TextMeshProUGUI>().text = "Take " + options[2].units + " shipments of cargo in less then " + POI.FormatTime(options[2].time) + " for $"+options[2].reward;

    }

    private Options[] CreateContracts(POI location)
    {
        if(_currentActiveOptions.ContainsKey(location))return _currentActiveOptions[location];
        Options[] newOptions = new Options[3];
        for (int i = 0; i < 3; i++)
        {
            newOptions[i].location = location;
            newOptions[i].units = Rng.Next(location.MinUnits, location.MaxUnits + 1);
            int timePerUnit = Rng.Next(location.MinTime, location.MaxTime + 1);
            newOptions[i].time = timePerUnit*newOptions[i].units;
            newOptions[i].reward = Rng.Next(location.MinReward, location.MaxReward + 1)*newOptions[i].units * location.MaxTime/timePerUnit;
        }
        _currentActiveOptions.Add(location,newOptions);
        currentOptions = newOptions;
        return newOptions;
    }

    void OnClickOption1()
    {
        StartContract(currentOptions[0]);
    }

    void OnClickOption2()
    {
        StartContract(currentOptions[1]);

    }

    void OnClickOption3()
    {
        StartContract(currentOptions[2]);

    }

    void StartContract(Options options)
    {
        _currentActiveOptions.Remove(options.location);
        options.location.NewContract(options.units, options.time, options.reward);
        
        this.gameObject.SetActive(false);
        
        EnableButtons();
    }
}
