using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using UnityEngine.UIElements;
using Utility.Singleton;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;

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

    [SerializeField]
    private Text _shootChanceText;

    [SerializeField]
    int _shootChance = 25;

    [SerializeField]
    private Button _catBtn;

    [SerializeField]
    private Image _catImg;

    private int _catGauge = 0;

    private void Awake()
    {
        _SetBtnListner();
        _SetView();
    }

    private void _SetBtnListner()
    {
        _changeBtn.SetBtnListnerRemoveAllAndAdd(_ClickChange);
        _catBtn.SetBtnListnerRemoveAllAndAdd(_ClickCat);
    }

    private void _SetView()
    {
        _SetView_BossHP();
        _SetView_BubbleChance();
        _SetView_Cat();
    }

    private void _SetView_BossHP()
    {
        _bossHPText.text = $"{_bossHP}";
    }

    public void ProgressGridProcess()
    {
        _SetBtnInteractable(false);
    }

    public void CompleteGridProcess()
    {
        ReloadBubble();
        _SetBtnInteractable(true);
    }

    private void _SetBtnInteractable(bool set)
    {
        _catBtn.interactable = set;
        _changeBtn.interactable = set;
    }

    public void DamageBoss(int cal)
    {
        _bossHP += cal;
        _SetView_BossHP();

        if (_bossHP <= 0)
        {
            _ClearStage();
        }
    }

    private void _ClearStage()
    {

    }

    private void _FailStage()
    {

    }

    private void _ClickChange()
    {
        BubbleLauncher.ChangeNextBubble();
    }

    public void ShootBubble()
    {
        _SetBtnInteractable(false);
        _shootChance--;
        _SetView_BubbleChance();
    }

    private void _SetView_BubbleChance()
    {
        _shootChanceText.text = $"{_shootChance}";
    }

    public void RemoveBubble()
    {
        BubbleLauncher.RemoveCurrentBubble();
    }

    public void ChangeBubble()
    {
        BubbleLauncher.ChangeCurrentBubble();
    }

    public void ReloadBubble()
    {
        if(_shootChance == 0)
        {
            _FailStage();
            return;
        }

        BubbleLauncher.SpawnNewBubble();
    }

    private void _ClickCat()
    {
        if(_catGauge + 25 >=100)
        {
            _catGauge = 0;
            ChangeBubble();
            _SetBtnInteractable(false);
        }
        else
        {
            _catGauge += 25;
            ShootBubble();
            RemoveBubble();
            ReloadBubble();
        }

        _SetView_Cat();
    }

    private void _SetView_Cat()
    {
        _catImg.fillAmount = _catGauge * 0.01f;
    }
}
