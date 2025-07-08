using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using UnityEngine.UIElements;
using Utility.Singleton;
using Button = UnityEngine.UI.Button;

public class StageManager : MonoSingleton<StageManager>
{
    public GridManager GridManager;
    public BubbleManager BubbleManager;
    public BubbleLauncher BubbleLauncher;

    [SerializeField]
    private Text _bossHPText;

    [SerializeField]
    int _bossHP = 100;


    [SerializeField]
    private Button _changeBtn;

    private void Awake()
    {
        _changeBtn.SetBtnListnerRemoveAllAndAdd(_ClickChange);
    }

    public void DamageBoss(int cal)
    {
        _bossHP += cal;
        _SetView();

        if(_bossHP<=0)
        {
            _ClearStage();
        }
    }

    private void _SetView()
    {
        _bossHPText.text = $"{_bossHP}";
    }

    private void _ClearStage()
    {

    }

    private void _ClickChange()
    {
        BubbleLauncher.ChangeNextBubble();
    }
}
