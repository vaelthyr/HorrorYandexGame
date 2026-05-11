using UnityEngine;

[RequireComponent(typeof(HealthManager))]
public class EnemyDamageable : MonoBehaviour, IDamageable
{
    [SerializeField] private HealthManager healthManager;
    [SerializeField] private bool destroyOnDeath;
    [SerializeField] private float destroyDelay;

    private void Reset()
    {
        healthManager = GetComponent<HealthManager>();
    }

    private void Awake()
    {
        if (healthManager == null)
        {
            healthManager = GetComponent<HealthManager>();
        }
    }

    private void OnEnable()
    {
        if (healthManager != null)
        {
            healthManager.OnDeath.AddListener(HandleDeath);
        }
    }

    private void OnDisable()
    {
        if (healthManager != null)
        {
            healthManager.OnDeath.RemoveListener(HandleDeath);
        }
    }

    public void Damage(int value)
    {
        if (healthManager == null)
        {
            return;
        }

        healthManager.Damage(value);
    }

    private void HandleDeath()
    {
        if (destroyOnDeath)
        {
            Destroy(gameObject, destroyDelay);
        }
    }
}
