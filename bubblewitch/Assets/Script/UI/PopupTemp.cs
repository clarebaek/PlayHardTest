using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class PopupTemp : MonoBehaviour
{
    [SerializeField]
    private Button _delayBtn;
    [SerializeField]
    private float _delayTime;
    private void OnEnable()
    {
        _delayBtn.gameObject.SetActive(false);
        _StartDelay();
    }

    async void _StartDelay()
    {
        await Task.Delay(TimeSpan.FromSeconds(_delayTime));
        _delayBtn.gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        _delayBtn.gameObject.SetActive(false);
    }
}
