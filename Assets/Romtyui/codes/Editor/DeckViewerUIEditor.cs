using UnityEditor;

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
    // Button Text
    // =========================================================

    private SerializedProperty drawPileButtonText;
    private SerializedProperty discardPileButtonText;
    private SerializedProperty exhaustPileButtonText;
    private SerializedProperty handButtonText;

    // =========================================================
    // Button Image
    // =========================================================

    private SerializedProperty drawPileButtonImage;
    private SerializedProperty discardPileButtonImage;
    private SerializedProperty exhaustPileButtonImage;
    private SerializedProperty handButtonImage;

    // =========================================================
    // Tab Text Label
    // =========================================================

    private SerializedProperty drawPileLabel;
    private SerializedProperty discardPileLabel;
    private SerializedProperty exhaustPileLabel;
    private SerializedProperty handLabel;

    // =========================================================
    // Tab Colors
    // =========================================================

    private SerializedProperty activeTabColor;
    private SerializedProperty inactiveTabColor;
    private SerializedProperty activeTextColor;
    private SerializedProperty inactiveTextColor;

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
        // Button Text
        // =====================================================

        drawPileButtonText = serializedObject.FindProperty("drawPileButtonText");
        discardPileButtonText = serializedObject.FindProperty("discardPileButtonText");
        exhaustPileButtonText = serializedObject.FindProperty("exhaustPileButtonText");
        handButtonText = serializedObject.FindProperty("handButtonText");

        // =====================================================
        // Button Image
        // =====================================================

        drawPileButtonImage = serializedObject.FindProperty("drawPileButtonImage");
        discardPileButtonImage = serializedObject.FindProperty("discardPileButtonImage");
        exhaustPileButtonImage = serializedObject.FindProperty("exhaustPileButtonImage");
        handButtonImage = serializedObject.FindProperty("handButtonImage");

        // =====================================================
        // Tab Text Label
        // =====================================================

        drawPileLabel = serializedObject.FindProperty("drawPileLabel");
        discardPileLabel = serializedObject.FindProperty("discardPileLabel");
        exhaustPileLabel = serializedObject.FindProperty("exhaustPileLabel");
        handLabel = serializedObject.FindProperty("handLabel");

        // =====================================================
        // Tab Colors
        // =====================================================

        activeTabColor = serializedObject.FindProperty("activeTabColor");
        inactiveTabColor = serializedObject.FindProperty("inactiveTabColor");
        activeTextColor = serializedObject.FindProperty("activeTextColor");
        inactiveTextColor = serializedObject.FindProperty("inactiveTextColor");

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

        EditorGUILayout.PropertyField(battleDeck);
        EditorGUILayout.PropertyField(panelRoot);

        EditorGUILayout.Space(8f);

        DrawSectionTitle("Display Mode");

        EditorGUILayout.PropertyField(displayMode);

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

        EditorGUILayout.PropertyField(classicRoot);
        EditorGUILayout.PropertyField(contentRoot);
        EditorGUILayout.PropertyField(cardPrefab);
    }

    private void DrawBookPagedSettings()
    {
        DrawSectionTitle("Book Paged Mode");

        EditorGUILayout.PropertyField(bookRoot);
        EditorGUILayout.PropertyField(cardsPerPage);
        EditorGUILayout.PropertyField(bookCardSlots, true);

        EditorGUILayout.Space(4f);

        EditorGUILayout.PropertyField(previousPageButton);
        EditorGUILayout.PropertyField(nextPageButton);
        EditorGUILayout.PropertyField(pageText);
        EditorGUILayout.PropertyField(emptyMessageRoot);

        EditorGUILayout.Space(4f);

        EditorGUILayout.PropertyField(hidePageButtonsWhenSinglePage);
        EditorGUILayout.PropertyField(resetPageWhenSwitchTab);
    }

    private void DrawCenterTitleSettings()
    {
        DrawSectionTitle("Center Title");

        EditorGUILayout.PropertyField(showCenterTitle);

        if (showCenterTitle.boolValue)
        {
            EditorGUILayout.PropertyField(titleText);
            EditorGUILayout.PropertyField(countText);
        }
    }

    private void DrawButtonSettings()
    {
        DrawSectionTitle("Buttons");

        EditorGUILayout.PropertyField(drawPileButton);
        EditorGUILayout.PropertyField(discardPileButton);
        EditorGUILayout.PropertyField(exhaustPileButton);
        EditorGUILayout.PropertyField(handButton);
        EditorGUILayout.PropertyField(closeButton);

        EditorGUILayout.Space(4f);

        DrawSectionTitle("Button Text");

        EditorGUILayout.PropertyField(drawPileButtonText);
        EditorGUILayout.PropertyField(discardPileButtonText);
        EditorGUILayout.PropertyField(exhaustPileButtonText);
        EditorGUILayout.PropertyField(handButtonText);

        EditorGUILayout.Space(4f);

        DrawSectionTitle("Button Image");

        EditorGUILayout.PropertyField(drawPileButtonImage);
        EditorGUILayout.PropertyField(discardPileButtonImage);
        EditorGUILayout.PropertyField(exhaustPileButtonImage);
        EditorGUILayout.PropertyField(handButtonImage);
    }

    private void DrawTabSettings()
    {
        DrawSectionTitle("Tab Text Label");

        EditorGUILayout.PropertyField(drawPileLabel);
        EditorGUILayout.PropertyField(discardPileLabel);
        EditorGUILayout.PropertyField(exhaustPileLabel);
        EditorGUILayout.PropertyField(handLabel);

        EditorGUILayout.Space(4f);

        DrawSectionTitle("Tab Colors");

        EditorGUILayout.PropertyField(activeTabColor);
        EditorGUILayout.PropertyField(inactiveTabColor);
        EditorGUILayout.PropertyField(activeTextColor);
        EditorGUILayout.PropertyField(inactiveTextColor);
    }

    private void DrawCardSizeSettings()
    {
        DrawSectionTitle("Card Size In Viewer");

        EditorGUILayout.PropertyField(overrideCardSize);

        if (overrideCardSize.boolValue)
        {
            EditorGUILayout.PropertyField(viewerCardSize);
        }

        EditorGUILayout.PropertyField(viewerCardScale);
    }

    private void DrawViewerInteractionSettings()
    {
        DrawSectionTitle("Viewer Interaction");

        EditorGUILayout.PropertyField(disableDragInViewer);
        EditorGUILayout.PropertyField(disableHoverInViewer);
        EditorGUILayout.PropertyField(viewerCardTooltipSide);
    }

    private void DrawRuntimeDebug()
    {
        DrawSectionTitle("Runtime Debug");

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.PropertyField(currentPageIndex);
            EditorGUILayout.PropertyField(totalPageCount);
        }
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