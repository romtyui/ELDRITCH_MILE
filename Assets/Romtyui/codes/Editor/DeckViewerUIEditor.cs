using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DeckViewerUI))]
public class DeckViewerUIEditor : Editor
{
    // =========================================================
    // Refs
    // =========================================================

    private SerializedProperty battleDeck;
    private SerializedProperty panelRoot;

    // =========================================================
    // Display Mode
    // =========================================================

    private SerializedProperty displayMode;

    // =========================================================
    // Classic Drag Scroll Mode
    // =========================================================

    private SerializedProperty contentRoot;
    private SerializedProperty cardPrefab;
    private SerializedProperty classicRoot;

    // =========================================================
    // Book Paged Mode
    // =========================================================

    private SerializedProperty bookRoot;
    private SerializedProperty cardsPerPage;
    private SerializedProperty bookCardSlots;
    private SerializedProperty previousPageButton;
    private SerializedProperty nextPageButton;
    private SerializedProperty pageText;
    private SerializedProperty emptyMessageRoot;
    private SerializedProperty hidePageButtonsWhenSinglePage;
    private SerializedProperty resetPageWhenSwitchTab;

    // =========================================================
    // Page Button Visual
    // =========================================================

    private SerializedProperty previousPageButtonImage;
    private SerializedProperty nextPageButtonImage;
    private SerializedProperty pageButtonActiveColor;
    private SerializedProperty pageButtonInactiveColor;

    // =========================================================
    // Center Title
    // =========================================================

    private SerializedProperty showCenterTitle;
    private SerializedProperty titleText;
    private SerializedProperty countText;

    // =========================================================
    // Buttons
    // =========================================================

    private SerializedProperty drawPileButton;
    private SerializedProperty discardPileButton;
    private SerializedProperty exhaustPileButton;
    private SerializedProperty handButton;
    private SerializedProperty closeButton;

    // =========================================================
    // Tab Button Animation
    // =========================================================

    private SerializedProperty activeTabYOffset;
    private SerializedProperty tabMoveDuration;
    private SerializedProperty tabAnimationUseUnscaledTime;

    // =========================================================
    // Button Text
    // =========================================================

    private SerializedProperty drawPileButtonText;
    private SerializedProperty discardPileButtonText;
    private SerializedProperty exhaustPileButtonText;
    private SerializedProperty handButtonText;

    // =========================================================
    // Button Count Text
    // =========================================================

    private SerializedProperty drawPileButtonCountText;
    private SerializedProperty discardPileButtonCountText;
    private SerializedProperty exhaustPileButtonCountText;
    private SerializedProperty handButtonCountText;

    // =========================================================
    // Tab Text Label
    // =========================================================

    private SerializedProperty drawPileLabel;
    private SerializedProperty discardPileLabel;
    private SerializedProperty exhaustPileLabel;
    private SerializedProperty handLabel;

    // =========================================================
    // Card Size In Viewer
    // =========================================================

    private SerializedProperty overrideCardSize;
    private SerializedProperty viewerCardSize;
    private SerializedProperty viewerCardScale;

    // =========================================================
    // Viewer Interaction
    // =========================================================

    private SerializedProperty disableDragInViewer;
    private SerializedProperty disableHoverInViewer;
    private SerializedProperty viewerCardTooltipSide;

    // =========================================================
    // Runtime
    // =========================================================

    private SerializedProperty currentPageIndex;
    private SerializedProperty totalPageCount;

    private void OnEnable()
    {
        // =====================================================
        // Refs
        // =====================================================

        battleDeck = serializedObject.FindProperty("battleDeck");
        panelRoot = serializedObject.FindProperty("panelRoot");

        // =====================================================
        // Display Mode
        // =====================================================

        displayMode = serializedObject.FindProperty("displayMode");

        // =====================================================
        // Classic Drag Scroll Mode
        // =====================================================

        contentRoot = serializedObject.FindProperty("contentRoot");
        cardPrefab = serializedObject.FindProperty("cardPrefab");
        classicRoot = serializedObject.FindProperty("classicRoot");

        // =====================================================
        // Book Paged Mode
        // =====================================================

        bookRoot = serializedObject.FindProperty("bookRoot");
        cardsPerPage = serializedObject.FindProperty("cardsPerPage");
        bookCardSlots = serializedObject.FindProperty("bookCardSlots");
        previousPageButton = serializedObject.FindProperty("previousPageButton");
        nextPageButton = serializedObject.FindProperty("nextPageButton");
        pageText = serializedObject.FindProperty("pageText");
        emptyMessageRoot = serializedObject.FindProperty("emptyMessageRoot");
        hidePageButtonsWhenSinglePage = serializedObject.FindProperty("hidePageButtonsWhenSinglePage");
        resetPageWhenSwitchTab = serializedObject.FindProperty("resetPageWhenSwitchTab");

        // =====================================================
        // Page Button Visual
        // =====================================================

        previousPageButtonImage = serializedObject.FindProperty("previousPageButtonImage");
        nextPageButtonImage = serializedObject.FindProperty("nextPageButtonImage");
        pageButtonActiveColor = serializedObject.FindProperty("pageButtonActiveColor");
        pageButtonInactiveColor = serializedObject.FindProperty("pageButtonInactiveColor");

        // =====================================================
        // Center Title
        // =====================================================

        showCenterTitle = serializedObject.FindProperty("showCenterTitle");
        titleText = serializedObject.FindProperty("titleText");
        countText = serializedObject.FindProperty("countText");

        // =====================================================
        // Buttons
        // =====================================================

        drawPileButton = serializedObject.FindProperty("drawPileButton");
        discardPileButton = serializedObject.FindProperty("discardPileButton");
        exhaustPileButton = serializedObject.FindProperty("exhaustPileButton");
        handButton = serializedObject.FindProperty("handButton");
        closeButton = serializedObject.FindProperty("closeButton");

        // =====================================================
        // Tab Button Animation
        // =====================================================

        activeTabYOffset = serializedObject.FindProperty("activeTabYOffset");
        tabMoveDuration = serializedObject.FindProperty("tabMoveDuration");
        tabAnimationUseUnscaledTime = serializedObject.FindProperty("tabAnimationUseUnscaledTime");

        // =====================================================
        // Button Text
        // =====================================================

        drawPileButtonText = serializedObject.FindProperty("drawPileButtonText");
        discardPileButtonText = serializedObject.FindProperty("discardPileButtonText");
        exhaustPileButtonText = serializedObject.FindProperty("exhaustPileButtonText");
        handButtonText = serializedObject.FindProperty("handButtonText");

        // =====================================================
        // Button Count Text
        // =====================================================

        drawPileButtonCountText = serializedObject.FindProperty("drawPileButtonCountText");
        discardPileButtonCountText = serializedObject.FindProperty("discardPileButtonCountText");
        exhaustPileButtonCountText = serializedObject.FindProperty("exhaustPileButtonCountText");
        handButtonCountText = serializedObject.FindProperty("handPileButtonCountText");

        if (handButtonCountText == null)
            handButtonCountText = serializedObject.FindProperty("handButtonCountText");

        // =====================================================
        // Tab Text Label
        // =====================================================

        drawPileLabel = serializedObject.FindProperty("drawPileLabel");
        discardPileLabel = serializedObject.FindProperty("discardPileLabel");
        exhaustPileLabel = serializedObject.FindProperty("exhaustPileLabel");
        handLabel = serializedObject.FindProperty("handLabel");

        // =====================================================
        // Card Size In Viewer
        // =====================================================

        overrideCardSize = serializedObject.FindProperty("overrideCardSize");
        viewerCardSize = serializedObject.FindProperty("viewerCardSize");
        viewerCardScale = serializedObject.FindProperty("viewerCardScale");

        // =====================================================
        // Viewer Interaction
        // =====================================================

        disableDragInViewer = serializedObject.FindProperty("disableDragInViewer");
        disableHoverInViewer = serializedObject.FindProperty("disableHoverInViewer");
        viewerCardTooltipSide = serializedObject.FindProperty("viewerCardTooltipSide");

        // =====================================================
        // Runtime
        // =====================================================

        currentPageIndex = serializedObject.FindProperty("currentPageIndex");
        totalPageCount = serializedObject.FindProperty("totalPageCount");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawSectionTitle("Refs");

        DrawProperty(battleDeck);
        DrawProperty(panelRoot);

        EditorGUILayout.Space(8f);

        DrawSectionTitle("Display Mode");

        DrawProperty(displayMode);

        DeckViewerDisplayMode currentDisplayMode =
            (DeckViewerDisplayMode)displayMode.enumValueIndex;

        EditorGUILayout.Space(8f);

        switch (currentDisplayMode)
        {
            case DeckViewerDisplayMode.ClassicDragScroll:
                DrawClassicDragScrollSettings();
                break;

            case DeckViewerDisplayMode.BookPaged:
                DrawBookPagedSettings();
                break;
        }

        EditorGUILayout.Space(8f);

        DrawCenterTitleSettings();

        EditorGUILayout.Space(8f);

        DrawButtonSettings();

        EditorGUILayout.Space(8f);

        DrawTabSettings();

        EditorGUILayout.Space(8f);

        DrawCardSizeSettings();

        EditorGUILayout.Space(8f);

        DrawViewerInteractionSettings();

        EditorGUILayout.Space(8f);

        DrawRuntimeDebug();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawClassicDragScrollSettings()
    {
        DrawSectionTitle("Classic Drag Scroll Mode");

        DrawProperty(classicRoot);
        DrawProperty(contentRoot);
        DrawProperty(cardPrefab);
    }

    private void DrawBookPagedSettings()
    {
        DrawSectionTitle("Book Paged Mode");

        DrawProperty(bookRoot);
        DrawProperty(cardsPerPage);
        DrawProperty(bookCardSlots, true);

        EditorGUILayout.Space(4f);

        DrawProperty(previousPageButton);
        DrawProperty(nextPageButton);
        DrawProperty(pageText);
        DrawProperty(emptyMessageRoot);

        EditorGUILayout.Space(4f);

        DrawProperty(hidePageButtonsWhenSinglePage);
        DrawProperty(resetPageWhenSwitchTab);

        EditorGUILayout.Space(8f);

        DrawSectionTitle("Page Button Visual");

        DrawProperty(previousPageButtonImage);
        DrawProperty(nextPageButtonImage);
        DrawProperty(pageButtonActiveColor);
        DrawProperty(pageButtonInactiveColor);
    }

    private void DrawCenterTitleSettings()
    {
        DrawSectionTitle("Center Title");

        DrawProperty(showCenterTitle);

        if (showCenterTitle != null && showCenterTitle.boolValue)
        {
            DrawProperty(titleText);
            DrawProperty(countText);
        }
    }

    private void DrawButtonSettings()
    {
        DrawSectionTitle("Buttons");

        DrawProperty(drawPileButton);
        DrawProperty(discardPileButton);
        DrawProperty(exhaustPileButton);
        DrawProperty(handButton);
        DrawProperty(closeButton);

        EditorGUILayout.Space(8f);

        DrawSectionTitle("Button Text");

        DrawProperty(drawPileButtonText);
        DrawProperty(discardPileButtonText);
        DrawProperty(exhaustPileButtonText);
        DrawProperty(handButtonText);

        EditorGUILayout.Space(8f);

        DrawSectionTitle("Button Count Text");

        DrawProperty(drawPileButtonCountText);
        DrawProperty(discardPileButtonCountText);
        DrawProperty(exhaustPileButtonCountText);
        DrawProperty(handButtonCountText);

        EditorGUILayout.Space(8f);

        DrawSectionTitle("Tab Button Animation");

        DrawProperty(activeTabYOffset);
        DrawProperty(tabMoveDuration);
        DrawProperty(tabAnimationUseUnscaledTime);
    }

    private void DrawTabSettings()
    {
        DrawSectionTitle("Tab Text Label");

        DrawProperty(drawPileLabel);
        DrawProperty(discardPileLabel);
        DrawProperty(exhaustPileLabel);
        DrawProperty(handLabel);
    }

    private void DrawCardSizeSettings()
    {
        DrawSectionTitle("Card Size In Viewer");

        DrawProperty(overrideCardSize);

        if (overrideCardSize != null && overrideCardSize.boolValue)
        {
            DrawProperty(viewerCardSize);
        }

        DrawProperty(viewerCardScale);
    }

    private void DrawViewerInteractionSettings()
    {
        DrawSectionTitle("Viewer Interaction");

        DrawProperty(disableDragInViewer);
        DrawProperty(disableHoverInViewer);
        DrawProperty(viewerCardTooltipSide);
    }

    private void DrawRuntimeDebug()
    {
        DrawSectionTitle("Runtime Debug");

        using (new EditorGUI.DisabledScope(true))
        {
            DrawProperty(currentPageIndex);
            DrawProperty(totalPageCount);
        }
    }

    private void DrawProperty(
        SerializedProperty property,
        bool includeChildren = false
    )
    {
        if (property == null)
            return;

        EditorGUILayout.PropertyField(
            property,
            includeChildren
        );
    }

    private void DrawSectionTitle(string title)
    {
        EditorGUILayout.Space(4f);

        EditorGUILayout.LabelField(
            title,
            EditorStyles.boldLabel
        );
    }
}