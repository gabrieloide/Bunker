using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Template ("molde") for one entry of the level selector; LevelSelectorMenu clones it per level.
public class LevelButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Button button;
    [SerializeField] TMP_Text numberText;
    [SerializeField] TMP_Text nameText;
    [Tooltip("Shown instead of the name while the level is locked")]
    [SerializeField] GameObject lockedMark;

    Action<string> onHover;
    string hoverText;

    public void Bind(int index, LevelDefinition level, bool unlocked, Action onClick, Action<string> hover)
    {
        if (numberText != null) numberText.text = (index + 1).ToString("00");
        if (nameText != null)
        {
            nameText.text = level.displayName;
            nameText.gameObject.SetActive(unlocked);
        }
        if (lockedMark != null) lockedMark.SetActive(!unlocked);

        button.interactable = unlocked;
        button.onClick.RemoveAllListeners();
        if (unlocked) button.onClick.AddListener(() => onClick());

        onHover = hover;
        hoverText = unlocked ? level.description : "Bloqueado: gana el nivel anterior.";
    }

    public void OnPointerEnter(PointerEventData eventData) => onHover?.Invoke(hoverText);
    public void OnPointerExit(PointerEventData eventData) => onHover?.Invoke("");
}
