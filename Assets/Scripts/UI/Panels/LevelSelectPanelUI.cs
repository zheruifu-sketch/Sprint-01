using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Nenn.InspectorEnhancements.Runtime.Attributes;

[DisallowMultipleComponent]
public class LevelSelectPanelUI : PanelUIBase
{
    [Serializable]
    private class LevelButtonBinding
    {
        [LabelText("关卡序号")]
        [SerializeField] private int levelNumber = 1;
        [LabelText("按钮")]
        [SerializeField] private Button button;

        public int LevelNumber => Mathf.Max(1, levelNumber);
        public Button Button => button;

        public void AutoBind(Transform root)
        {
            if (root == null)
            {
                return;
            }

            if (button == null)
            {
                Transform buttonTransform = root.Find($"Card/LevelButtons/Level{LevelNumber}Button");
                if (buttonTransform != null)
                {
                    button = buttonTransform.GetComponent<Button>();
                }
            }
        }
    }

    [Header("References")]
    [LabelText("返回按钮")]
    [SerializeField] private Button backButton;
    [LabelText("关卡按钮")]
    [SerializeField] private List<LevelButtonBinding> levelButtons = new List<LevelButtonBinding>();

    public event Action<int> LevelSelected;
    public event Action BackRequested;

    protected override void Reset()
    {
        base.Reset();
        AutoBind();
    }

    protected override void Awake()
    {
        base.Awake();
        AutoBind();
        BindButtons();
    }

    public override void Initialize()
    {
        AutoBind();
        BindButtons();
    }

    public void SetContent(int availableLevelCount)
    {
        for (int i = 0; i < levelButtons.Count; i++)
        {
            LevelButtonBinding binding = levelButtons[i];
            if (binding == null || binding.Button == null)
            {
                continue;
            }

            bool isAvailable = binding.LevelNumber <= Mathf.Max(0, availableLevelCount);
            binding.Button.gameObject.SetActive(isAvailable);
            binding.Button.interactable = isAvailable;
        }
    }

    private void BindButtons()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(HandleBackClicked);
            backButton.onClick.AddListener(HandleBackClicked);
        }

        for (int i = 0; i < levelButtons.Count; i++)
        {
            LevelButtonBinding binding = levelButtons[i];
            if (binding == null || binding.Button == null)
            {
                continue;
            }

            binding.Button.onClick.RemoveAllListeners();
            int selectedLevelNumber = binding.LevelNumber;
            binding.Button.onClick.AddListener(() => HandleLevelClicked(selectedLevelNumber));
        }
    }

    private void HandleLevelClicked(int levelNumber)
    {
        SoundEffectPlayback.Play(SoundEffectId.Click);
        LevelSelected?.Invoke(Mathf.Max(1, levelNumber));
    }

    private void HandleBackClicked()
    {
        SoundEffectPlayback.Play(SoundEffectId.Click);
        BackRequested?.Invoke();
    }

    private void AutoBind()
    {
        backButton = backButton != null ? backButton : FindButton("Card/BackButton");

        for (int i = 0; i < levelButtons.Count; i++)
        {
            levelButtons[i]?.AutoBind(transform);
        }
    }

    private Button FindButton(string path)
    {
        Transform child = transform.Find(path);
        return child != null ? child.GetComponent<Button>() : null;
    }

}
