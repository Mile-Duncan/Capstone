using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;

public class GameUIControler : MonoBehaviour
{
    protected static Button trackButton;
    protected static Button semaphoreButton;
    protected static Button trainButton;
    protected static Button closeButton;

    public static UIState CurrentState { get;protected set; }

    protected static bool Initialized = false;
    protected static TrackPlaceUI trackPlaceUI;
    protected static SemaphorePlaceUI semaphorePlaceUI;

    
    public enum UIState
    {
        Move,
        Track,
        Train,
        Signal
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected void Start()
    {
        if(Initialized)return;
        Initialized = true;
        trackPlaceUI = Instantiate(Resources.Load("UI/TrackPlaceUI"), transform).GetComponent<TrackPlaceUI>();
        semaphorePlaceUI = Instantiate(Resources.Load("UI/SemaphorePlaceUI"), transform).GetComponent<SemaphorePlaceUI>();

    }

    protected static void DisableButtons()
    {
        trackButton.enabled = false;
        semaphoreButton.enabled = false;
        trainButton.enabled = false;
        closeButton.enabled = true;
    }
    
    protected static void EnableButtons()
    {
        trackButton.enabled = true;
        semaphoreButton.enabled = true;
        trainButton.enabled = true;
        closeButton.enabled = false;
    }

    protected static void OnTrackButtonClicked()
    {
        trackPlaceUI.gameObject.SetActive(true);
        
    }
    
    protected static void OnSemaphoreButtonClicked()
    {
        semaphorePlaceUI.gameObject.SetActive(true);
        
    }
    
    protected static void OnCloseButtonClicked()
    {
        trackPlaceUI.gameObject.SetActive(false);
        semaphorePlaceUI.gameObject.SetActive(false);
        
        EnableButtons();
        CurrentState = UIState.Move;
    }
}
