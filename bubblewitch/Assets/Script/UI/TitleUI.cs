using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

public class TitleUI : MonoBehaviour
{
    [SerializeField]
    private Button _startBtn;

    private void Awake()
    {
        _startBtn.SetBtnListnerRemoveAllAndAdd(_OnClickStartBtn);
    }

    private void _OnClickStartBtn()
    {
        SceneManager.LoadScene("InGame");
    }
}
