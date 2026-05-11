using UnityEngine;

public class TriggerColliderEvent : MonoBehaviour
{
    [SerializeField] private string _triggerTag;
    [SerializeField] private string _newSceneName = "";

    private async void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(_triggerTag))
        {
            return;
        }

        LevelController.instance?.SetEnabledCharacterMovement(false);

        if (string.IsNullOrEmpty(_newSceneName))
        {
            await LevelLoader.instance.LoadNewSceneAsync();
        }
        else
        {
            await LevelLoader.instance.LoadNewSceneAsync(_newSceneName);
        }
    }
}
