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
    Vector3 baseScale;
    bool isUnlocked;
    bool isCompleted;
    bool isHovered;
    bool pendingRevealLock;

    void Awake()
    {
        if (turretTransform == null)
            turretTransform = turretRenderer != null ? turretRenderer.transform : transform;

        initialScale = baseScale = turretTransform.localScale;
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

    public void SetPendingReveal(bool hiddenAsLocked)
    {
        pendingRevealLock = hiddenAsLocked;
        RefreshState();
    }

    public void RevealUnlocked()
    {
        pendingRevealLock = false;
        RefreshState();

        if (turretTransform != null)
        {
            LeanTween.cancel(turretTransform.gameObject);
            turretTransform.localScale = initialScale * 0.85f;
            LeanTween.scale(turretTransform.gameObject, initialScale * 1.22f, 0.14f)
                .setEaseOutQuad()
                .setOnComplete(() =>
                {
                    if (turretTransform != null)
                        LeanTween.scale(turretTransform.gameObject, initialScale, 0.22f).setEaseOutBack();
                });
        }

        if (nameBox != null)
        {
            LeanTween.cancel(nameBox);
            nameBox.transform.localScale = Vector3.one * 0.85f;
            LeanTween.scale(nameBox, Vector3.one * 1.15f, 0.14f)
                .setEaseOutQuad()
                .setOnComplete(() =>
                {
                    if (nameBox != null)
                        LeanTween.scale(nameBox, Vector3.one, 0.2f).setEaseOutBack();
                });
        }
    }

    public void RefreshState()
    {
        isUnlocked = LevelProgress.IsUnlocked(levelIndex) && !pendingRevealLock;
        isCompleted = LevelProgress.IsCompleted(levelIndex);

        // 1. Sprite: si está completado (ganado), cambiar a destroyedSprite
        if (isCompleted && destroyedSprite != null)
        {
            turretRenderer.sprite = destroyedSprite;
            // Scale the destroyed bunker sprite to match the width of the island it replaces
            float fit = normalSprite != null && destroyedSprite.bounds.size.x > 0f
                ? normalSprite.bounds.size.x / destroyedSprite.bounds.size.x : 1f;
            initialScale = baseScale * fit;
            turretTransform.localScale = initialScale;
        }
        else if (normalSprite != null)
        {
            turretRenderer.sprite = normalSprite;
            initialScale = baseScale;
            turretTransform.localScale = initialScale;
        }

        // 2. Color / Tint
        if (turretRenderer != null)
        {
            turretRenderer.color = isUnlocked ? normalColor : lockedColor;
        }

        // 3. Name Box
        string title = levelDefinition != null && !string.IsNullOrEmpty(levelDefinition.displayName)
            ? levelDefinition.displayName
            : $"LEVEL {levelIndex + 1}";

        if (nameText != null)
        {
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = 1f;
            if (nameText.fontSizeMax <= 0f)
                nameText.fontSizeMax = nameText.fontSize > 0f ? nameText.fontSize : 36f;
            nameText.overflowMode = TextOverflowModes.Ellipsis;

            if (isCompleted)
            {
                nameText.text = title;
                nameText.color = Color.white;
            }
            else if (isUnlocked)
            {
                nameText.text = title;
                nameText.color = Color.white;
            }
            else
            {
                nameText.text = "LOCKED";
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
            SoundBank.Play(Sfx.InfoBox);
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
            string desc = isUnlocked ? levelDefinition.description : "Complete previous levels to unlock.";
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

        SoundBank.Play(Sfx.CardPlace);
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
