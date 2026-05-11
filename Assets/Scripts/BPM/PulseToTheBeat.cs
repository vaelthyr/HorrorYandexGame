using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PulseToTheBeat : MonoBehaviour
{
    [SerializeField] private bool _useTestBeat;
    [SerializeField] private float _pulseSize = 1.15f;
    [SerializeField] private float _returnSpeed = 5f;
    private Vector3 _startSize;
    private float timer;


    private void OnEnable()
    {
        BeatManager.OnPulse += Pulse;
    }

    private void OnDisable()
    {
        BeatManager.OnPulse -= Pulse;
    }

    private void OnDestroy()
    {
        BeatManager.OnPulse -= Pulse;
    }

    private void Start()
    {
        _startSize = transform.localScale;
        if (_useTestBeat)
        {
            StartCoroutine(TestBeat());
        }
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, _startSize, Time.deltaTime * _returnSpeed);
    }

    public void Pulse()
    {
        transform.localScale = _startSize * _pulseSize;
    }

    IEnumerator TestBeat()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            Pulse();
        }
    }
}
