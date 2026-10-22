using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class WorldLevelNode : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Level Configuration")]
    [Tooltip("0 = Nivel 1, 1 = Nivel 2, etc.")]
    public int levelIndex;
    public LevelDefinition levelDefinition;

    [Header("Visuals")]
    [SerializeField] Transform turretTransform;
    [SerializeField] SpriteRenderer turretRenderer;
    [SerializeField] Sprite normalSprite;
    [SerializeField] Sprite destroyedSprite;

    [Header("Name Box")]
    [SerializeField] GameObject nameBox;
    [SerializeField] TMP_Text nameText;
    [SerializeField] SpriteRenderer nameBoxRenderer;

    [Header("Hover & Interaction")]
    [SerializeField] float hoverScaleMultiplier = 1.25f;
    [SerializeField] float tweenDuration = 0.2f;

    [Header("Locked State")]
    [SerializeField] Color lockedColor = new Color(0.4f, 0.4f, 0.4f, 0.7f);
    [SerializeField] Color normalColor = Color.white;

    Action<WorldLevelNode> onSelectCallback;
    Action<string> onHoverDescriptionCallback;

    Vector3 initialScale;
    bool isUnlocked;
    bool isCompleted;
    bool isHovered;

    void Awake()
    {
        if (turretTransform == null)
            turretTransform = turretRenderer != null ? turretRenderer.transform : transform;

        initialScale = turretTransform.localScale;
    }

    public void Setup(
        int index,
        LevelDefinition definition,
        Sprite customTurret,
        Sprite destroyed,
        Action<WorldLevelNode> onSelect,
        Action<string> onHoverDesc = null)
    {
        levelIndex = index;
        levelDefinition = definition;
        if (customTurret != null) normalSprite = customTurret;
        if (destroyed != null) destroyedSprite = destroyed;

        onSelectCallback = onSelect;
        onHoverDescriptionCallback = onHoverDesc;

        RefreshState();
    }

    public void RefreshState()
    {
        isUnlocked = LevelProgress.IsUnlocked(levelIndex);
        isCompleted = LevelProgress.IsCompleted(levelIndex);

        // 1. Sprite: si está completado (ganado), cambiar a destroyedSprite
        if (isCompleted && destroyedSprite != null)
        {
            turretRenderer.sprite = destroyedSprite;
        }
        else if (normalSprite != null)
        {
            turretRenderer.sprite = normalSprite;
        }

        // 2. Color / Tint
        if (turretRenderer != null)
        {
            turretRenderer.color = isUnlocked ? normalColor : lockedColor;
        }

        // 3. Name Box
        string title = levelDefinition != null && !string.IsNullOrEmpty(levelDefinition.displayName)
            ? levelDefinition.displayName
            : $"NIVEL {levelIndex + 1}";

        if (nameText != null)
        {
            if (isCompleted)
            {
                nameText.text = $"{title}";
                nameText.color = new Color(0.8f, 1f, 0.8f, 1f);
            }
            else if (isUnlocked)
            {
                nameText.text = title;
                nameText.color = Color.white;
            }
            else
            {
                nameText.text = "BLOQUEADO";
                nameText.color = new Color(0.7f, 0.7f, 0.7f, 0.8f);
            }
        }

        if (nameBoxRenderer != null)
        {
            nameBoxRenderer.color = isUnlocked ? normalColor : lockedColor;
        }
    }

    // --- Hover Effects ---
    public void OnPointerEnter(PointerEventData eventData) => HandleHoverEnter();
    public void OnPointerExit(PointerEventData eventData) => HandleHoverExit();
    public void OnPointerClick(PointerEventData eventData) => HandleClick();

    void OnMouseEnter() => HandleHoverEnter();
    void OnMouseExit() => HandleHoverExit();
    void OnMouseDown() => HandleClick();

    void HandleHoverEnter()
    {
        if (isHovered) return;
        isHovered = true;

        if (isUnlocked)
        {
            // Escala más grande para dar la sensación de selección
            LeanTween.cancel(turretTransform.gameObject);
            LeanTween.scale(turretTransform.gameObject, initialScale * hoverScaleMultiplier, tweenDuration)
                .setEaseOutBack();

            if (nameBox != null)
            {
                LeanTween.cancel(nameBox);
                LeanTween.scale(nameBox, Vector3.one * 1.12f, tweenDuration).setEaseOutBack();
            }
        }
        else
        {
            // Pequeña sacudida si está bloqueado
            LeanTween.cancel(turretTransform.gameObject);
            LeanTween.rotateZ(turretTransform.gameObject, 6f, 0.08f)
                .setEasePunch()
                .setOnComplete(() => turretTransform.rotation = Quaternion.identity);
        }

        if (onHoverDescriptionCallback != null && levelDefinition != null)
        {
            string desc = isUnlocked ? levelDefinition.description : "Supera los niveles anteriores para desbloquear.";
            onHoverDescriptionCallback(desc);
        }
    }

    void HandleHoverExit()
    {
        if (!isHovered) return;
        isHovered = false;

        LeanTween.cancel(turretTransform.gameObject);
        LeanTween.scale(turretTransform.gameObject, initialScale, tweenDuration)
            .setEaseOutQuad();
        turretTransform.rotation = Quaternion.identity;

        if (nameBox != null)
        {
            LeanTween.cancel(nameBox);
            LeanTween.scale(nameBox, Vector3.one, tweenDuration).setEaseOutQuad();
        }

        if (onHoverDescriptionCallback != null)
        {
            onHoverDescriptionCallback("");
        }
    }

    void HandleClick()
    {
        if (!isUnlocked)
        {
            // Feedback de bloqueo
            LeanTween.cancel(turretTransform.gameObject);
            LeanTween.rotateZ(turretTransform.gameObject, 10f, 0.15f)
                .setEasePunch()
                .setOnComplete(() => turretTransform.rotation = Quaternion.identity);
            return;
        }

        // Efecto punch de confirmación al hacer click
        LeanTween.cancel(turretTransform.gameObject);
        LeanTween.scale(turretTransform.gameObject, initialScale * (hoverScaleMultiplier * 1.1f), 0.1f)
            .setEaseOutQuad()
            .setOnComplete(() =>
            {
                onSelectCallback?.Invoke(this);
            });
    }

    void OnDisable()
    {
        if (turretTransform != null)
        {
            LeanTween.cancel(turretTransform.gameObject);
            turretTransform.localScale = initialScale;
            turretTransform.rotation = Quaternion.identity;
        }
    }
}
