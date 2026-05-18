using UnityEngine;
using UnityEngine.UI;

public class StartManager : MonoBehaviour
{
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _settingButton;
    [SerializeField] private Button _endButton;


    private void Awake()
    {
        _startButton.onClick.AddListener(OnStartButtonClicked);
        _settingButton.onClick.AddListener(OnSettingButtonClicked);
        _endButton.onClick.AddListener(OnEndButtonClicked);
    }

    private void OnStartButtonClicked()
    {
        SystemSetting.Instance.ChangeScene("Login");
    }

    private void OnSettingButtonClicked()
    {

    }

    private void OnEndButtonClicked()
    {
        SystemSetting.Instance.QuitGame();
    }
}