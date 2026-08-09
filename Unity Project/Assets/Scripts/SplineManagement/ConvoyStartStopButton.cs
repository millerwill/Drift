using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ConvoyStartStopButton : MonoBehaviour
{
    [SerializeField] private ConvoySplineController convoyController;
    [SerializeField] private TMP_Text buttonText;

    [Header("Labels")]
    [SerializeField] private string startLabel = "Start Convoy";
    [SerializeField] private string stopLabel = "Stop Convoy";
    [SerializeField] private string waitingLabel = "Choose Route";
    [SerializeField] private string finishedLabel = "Route Finished";

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (buttonText == null)
            buttonText = GetComponentInChildren<TMP_Text>();

        button.onClick.AddListener(HandleButtonClicked);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(HandleButtonClicked);
    }

    private void Update()
    {
        UpdateButtonAppearance();
    }

    private void HandleButtonClicked()
    {
        if (convoyController == null)
            return;

        convoyController.ToggleConvoyMovement();
        UpdateButtonAppearance();
    }

    private void UpdateButtonAppearance()
    {
        if (convoyController == null)
        {
            button.interactable = false;

            if (buttonText != null)
                buttonText.text = "No Convoy";

            return;
        }

        if (convoyController.RouteFinished)
        {
            button.interactable = false;

            if (buttonText != null)
                buttonText.text = finishedLabel;

            return;
        }

        button.interactable = true;

        if (buttonText == null)
            return;

        if (convoyController.WaitingForChoice)
        {
            buttonText.text = waitingLabel;
        }
        else if (convoyController.MovementEnabled)
        {
            buttonText.text = stopLabel;
        }
        else
        {
            buttonText.text = startLabel;
        }
    }
}