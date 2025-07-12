using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
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

    [Header("보스관련 설정")]
    [SerializeField]
    private Text _bossHPText;

    [SerializeField]
    int _bossHP = 100;
    [SerializeField]
    int _damagae = 10;

    [Header("버블 교체 설정")]
    [SerializeField]
    private Button _changeBtn;

    [SerializeField]
    private Text _shootChanceText;

    [Header("쏘기 횟수 설정")]
    [SerializeField]
    int _shootChance = 25;

    [Header("주먹밥 폭탄 설정")]
    [SerializeField]
    private Button _catBtn;

    [SerializeField]
    private Image _catImg;

    [SerializeField]
    List<GameObject> _movingGo;

    [Header("임시용 UI")]
    [SerializeField]
    private GameObject _successPop;
    [SerializeField]
    private Button _successBtn;
    [SerializeField]
    private GameObject _failPop;
    [SerializeField]
    private Button _failBtn;
    [SerializeField]
    private Button _retryBtn;

    Dictionary<int, int> _rowCount = new Dictionary<int, int>();

    [Header("첫 Row수 입력")]
    [SerializeField]
    private int _maxRow;
    private int _firstMaxRow;

    private int _catGauge = 0;

    public bool CanSpawnBomb { get => _nowBomb < _enableBomb && _nowNormal >= _goalBomb; }
    private int _enableBomb = 3;
    private int _nowBomb = 0;
    private int _goalBomb = 7;
    private int _nowNormal = 0;

    private void Awake()
    {
        _SetData();
        _SetBtnListner();
        _SetView();
    }

    private void _SetBtnListner()
    {
        _changeBtn.SetBtnListnerRemoveAllAndAdd(_ClickChange);
        _catBtn.SetBtnListnerRemoveAllAndAdd(_ClickCat);
        _successBtn.SetBtnListnerRemoveAllAndAdd(_ClickRetry);
        _failBtn.SetBtnListnerRemoveAllAndAdd(_ClickRetry);
        _retryBtn.SetBtnListnerRemoveAllAndAdd(_ClickRetry);
    }

    private void _SetData()
    {
        _firstMaxRow = _maxRow;
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
        _SetCamPos();
    }

    private void _SetBtnInteractable(bool set)
    {
        _catBtn.interactable = set;
        _changeBtn.interactable = set;
        _retryBtn.interactable = set;
    }

    public void DamageBoss()
    {
        _bossHP -= _damagae;
        _SetView_BossHP();

        if (_bossHP <= 0)
        {
            _ClearStage();
        }
    }

    private void _ClearStage()
    {
        _successPop.SetActive(true);
    }

    private void _FailStage()
    {
        _failPop.SetActive(true);
    }

    private void _ClickChange()
    {
        BubbleLauncher.SwitchCurrentAndNext();
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
        BubbleLauncher.ChangeCurrentBubbleToBomb();
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
            ShootBubble();
            ChangeBubble();
        }
        else
        {
            _catGauge += 25;
            ShootBubble();
            _SetBtnInteractable(true);
            RemoveBubble();
            ReloadBubble();
        }

        _SetView_Cat();
    }

    private void _SetView_Cat()
    {
        _catImg.fillAmount = _catGauge * 0.01f;
    }

    public void AddNowBombCount(int add)
    {
        _nowBomb += add;

        if(add > 0)
        {
            _nowNormal = 0;
        }
    }

    public void AddNowNormalCount(int add)
    {
        _nowNormal += add;
    }

    public void ChangeRowCount(int row, int count)
    {
        if(_rowCount.ContainsKey(row+1) == false)
        {
            _rowCount.Add(row+1, count);
        }
        else
        {
            _rowCount[row+1] += count;
        }
    }

    private void _SetCamPos()
    {
        int beforeMax = _maxRow;
        _maxRow = Mathf.Max(_rowCount.Max(x => x.Value > 0 ? x.Key : 0) , _firstMaxRow);

        float different = (beforeMax - _maxRow) * 0.75f;
        AddPosition(different);
        Camera.main.transform.position =
            new Vector3(
                Camera.main.transform.position.x,
                Camera.main.transform.position.y + different,
                Camera.main.transform.position.z
                );

        void AddPosition(float different)
        {
            Vector3 tempPos = new Vector3(0, different, 0);
            GridManager.MIN_AXIS_Y += (different);
            BubbleLauncher.transform.position += tempPos;
            BubbleLauncher.AddBubblePos(tempPos);
            foreach (var it in _movingGo)
            {
                it.transform.position += tempPos;
            }
        }
    }

    private void _ClickRetry()
    {
        // 1. 현재 활성화된 씬의 빌드 인덱스를 가져옵니다.
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

        // 2. 해당 인덱스의 씬을 다시 로드합니다.
        SceneManager.LoadScene(currentSceneIndex);
    }
}
