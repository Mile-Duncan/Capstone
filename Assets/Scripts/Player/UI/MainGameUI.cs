using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;

public class MainGameUI : GameUIControler
{
    [SerializeField]Button _trackButton;
    [SerializeField]Button _semaphoreButton;
    [SerializeField]Button _trainButton;
    [SerializeField]Button _closeButton;
    new void Start()
    {
        trackButton = _trackButton;
        semaphoreButton = _semaphoreButton;
        trainButton = _trainButton;
        closeButton = _closeButton;
        
        trackButton.onClick.AddListener(OnTrackButtonClicked);
        semaphoreButton.onClick.AddListener(OnSemaphoreButtonClicked);
        closeButton.onClick.AddListener(OnCloseButtonClicked);

        base.Start();
    }
    
}
