using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;

public class LevelLoader : MonoSingleton<LevelLoader>
{
    private bool _uiInitialized;

    [Header("Loading UI")]
    [SerializeField] private Slider loadingBar;
    [SerializeField] private GameObject _loadScreenGM;
    [SerializeField] private Image _loadScreen;

    private bool _isTranslatedLoadScreen = false;
    private bool _isSceneLoading = false;
    private bool _isNextSceneLoaded = false;
    private bool _isSceneActive = false;
    public bool isTranslatedLoadScreen => _isTranslatedLoadScreen;
    public bool IsSceneLoading => _isSceneLoading;
    public bool IsNextSceneLoaded => _isNextSceneLoaded;
    public bool IsSceneActive => _isSceneActive;

    public override void Init()
    {
        InitializeLoadingUi();
    }

    private void Start()
    {
        InitializeLoadingUi();
    }

    private async UniTask SetActiveLoadingUI(bool value)
    {
        InitializeLoadingUi();

        if (_isTranslatedLoadScreen)
        {
            return;
        }
        _isTranslatedLoadScreen = true;

        if (value)
        {
            _loadScreen.gameObject.SetActive(true);
            await _loadScreen.DOFade(1f, 1f).OnComplete(() =>
             {
                 _isTranslatedLoadScreen = false;
                 loadingBar.gameObject.SetActive(true);
             }).AsyncWaitForCompletion();
        }
        else
        {
            loadingBar.gameObject.SetActive(false);
            await _loadScreen.DOFade(0, 1f).OnComplete(() =>
            {
                _isTranslatedLoadScreen = false;
                _loadScreen.gameObject.SetActive(false);

            }).AsyncWaitForCompletion();
        }

    }

    public void FastSetActiveLoadingUI(bool value)
    {
        if (value)
        {
            _loadScreen.DOFade(1, 0);
        }
        else
        {
            _loadScreen.DOFade(0, 0);
        }

    }

    public async UniTask LoadNewSceneAsync(string newBuildSceneName)
    {
        int buildIndex = GetBuildIndexBySceneName(newBuildSceneName);
        if (buildIndex < 0)
        {
            Debug.LogError($"LevelLoader could not find scene '{newBuildSceneName}' in Build Settings.");
            return;
        }

        await LoadProcess(buildIndex);
    }
    
    public async UniTask LoadNewSceneAsync(int newBuildSceneIndex)
    {
        await LoadProcess(newBuildSceneIndex);
    }
    
    public async UniTask LoadNewSceneAsync()
    {
        await LoadProcess(SceneManager.GetActiveScene().buildIndex + 1);
    }

    private async UniTask LoadProcess(int buildIndex)
    {
        if (_isSceneLoading)
        {
            return;
        }
        _isSceneLoading = true;
        if (_loadScreen != null && loadingBar != null)
        {
            await SetActiveLoadingUI(true);
        }

        if (MainAudioManager.instance != null)
        {
            MainAudioManager.instance.PauseMainSourceAudio();
        }
        //await LoadAmbientAsync(AddressableManager.instance.AmbientData[buildIndex]);
        await LoadSceneAsync(buildIndex);
        if (_loadScreen != null && loadingBar != null)
        {
            await SetActiveLoadingUI(false);
        }
        _isSceneLoading = false;
    }

    private async UniTask LoadAmbientAsync(AssetReference reference)
    {
        var operation = reference.LoadAssetAsync<AudioClip>();
        while (!operation.IsDone)
        {
            loadingBar.value = Mathf.Lerp(0, 0.5f, operation.PercentComplete / 0.9f);
            await UniTask.Yield(); 
        }  
    }
    
    private async UniTask LoadSceneAsync(int _buildSceneIndex)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(_buildSceneIndex);
        asyncLoad.allowSceneActivation = false; 
        while (!asyncLoad.isDone)
        { 
            if (asyncLoad.progress >= .9f && !asyncLoad.allowSceneActivation)
            {
                asyncLoad.allowSceneActivation = true;
                _isNextSceneLoaded = true;
            }
            if (loadingBar != null)
            {
                loadingBar.value = Mathf.Lerp(0.5f, 1, asyncLoad.progress / 0.9f);
            }
            await UniTask.Yield(); 
        }
        
        _isSceneActive = true;
    }

    private int GetBuildIndexBySceneName(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string buildSceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (string.Equals(buildSceneName, sceneName, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private void InitializeLoadingUi()
    {
        if (_uiInitialized)
        {
            return;
        }

        if (_loadScreen != null)
        {
            Color color = _loadScreen.color;
            color.a = 0f;
            _loadScreen.color = color;
            _loadScreen.gameObject.SetActive(false);
        }

        if (loadingBar != null)
        {
            loadingBar.value = 0f;
            loadingBar.gameObject.SetActive(false);
        }

        _uiInitialized = true;
    }
}
