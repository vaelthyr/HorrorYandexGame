using UnityEngine;
using UnityEngine.AddressableAssets;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private AudioClip _clip;
    [Header("Windows")]
    [SerializeField] private GameObject _skinsPanel;
    [SerializeField] private GameObject _mainPanel;
    [SerializeField] private GameObject _noValuePanel;
    [SerializeField] private GameObject _donatePanel;
    [SerializeField] private GameObject _levelsPanel;
    [Header("Skins windows")]
    [SerializeField] private GameObject _buyByValue;
    [SerializeField] private GameObject _buyByMoney;
    [SerializeField] private GameObject _buyByCrystal;
    [SerializeField] private GameObject _viewPartsContent;
    [SerializeField] private GameObject _viewSkinsContent;

    private void Awake()
    {
        _skinsPanel.SetActive(false);
        _noValuePanel.SetActive(false);
        _donatePanel.SetActive(false);
    }
    
    private void Start()
    { 
        //Debug.Log(AddressableManager.instance.AmbientData[1].Asset as AudioClip);
        //MainAudioManager.instance.PlayMainSourceAudio(AddressableManager.instance.AmbientData[1].Asset as AudioClip);
        MainAudioManager.instance.PlayMainSourceAudio(_clip);
    }
    
    public async void ChangeScene(int _levelID)
    {
        await LevelLoader.instance.LoadNewSceneAsync(_levelID);
    }
    
    public void OpenSkins(bool value)
    {
        if (value)
        {
            _skinsPanel.SetActive(true);
            _mainPanel.SetActive(false);
            _levelsPanel.SetActive(false);
        }
        else
        {
            _skinsPanel.SetActive(false);
            _mainPanel.SetActive(true);
            _levelsPanel.SetActive(false);
        }
    }
    public void OpenLevels(bool value)
    {
        if (value)
        {
            _skinsPanel.SetActive(false);
            _mainPanel.SetActive(false);
            _levelsPanel.SetActive(true);
        }
        else
        {
            _skinsPanel.SetActive(false);
            _mainPanel.SetActive(true);
            _levelsPanel.SetActive(false);
        }
    }
}