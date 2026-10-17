using NasconGJ.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace NasconGJ.EditorTools.Generation
{
    /// <summary>
    /// Builds the in-game HUD and the main-menu canvas. Typography: Cinzel (titles, menu, key caps),
    /// EB Garamond (dialogue, hints, body). Palette: ember orange on purple-black; the guiding light speaks in pale cyan.
    /// </summary>
    public static class UIPrefabGenerator
    {
        public static string HUDPath => $"{GenUtil.UIPrefabDir}/PF_HUD.prefab";
        public static string MainMenuPath => $"{GenUtil.UIPrefabDir}/PF_MainMenuCanvas.prefab";
        public static string CutscenePath => $"{GenUtil.UIPrefabDir}/PF_CutsceneCanvas.prefab";

        private static readonly Color Warm = new(1f, 0.9f, 0.78f, 0.92f);
        private static readonly Color Ember = new(1f, 0.62f, 0.28f, 1f);
        private static readonly Color Muted = new(0.85f, 0.74f, 0.64f, 0.7f);
        private static readonly Color GuideText = new(0.86f, 0.95f, 1f, 1f);
        private static readonly Color GuideLine = new(0.7f, 0.92f, 1f, 0.45f);

        private static TMP_FontAsset title, body, italic;
        private static Sprite rounded, shade, diamond, line, ring, ringOutlineSprite;

        public static void GenerateAll()
        {
            GenUtil.EnsureFolder(GenUtil.UIPrefabDir);
            title = FontLibrary.Title;
            body = FontLibrary.Body;
            italic = FontLibrary.Italic;
            rounded = TextureGenerator.GetSprite(TextureGenerator.RoundedPath);
            shade = TextureGenerator.GetSprite(TextureGenerator.ShadePath);
            diamond = TextureGenerator.GetSprite(TextureGenerator.DiamondPath);
            line = TextureGenerator.GetSprite(TextureGenerator.LinePath);
            ring = TextureGenerator.GetSprite(TextureGenerator.RingPath);
            ringOutlineSprite = TextureGenerator.GetSprite(TextureGenerator.RingOutlinePath);
            BuildHUD();
            BuildMainMenu();
            BuildCutsceneCanvas();
            AssetDatabase.SaveAssets();
        }

        // =====================================================================
        //  HUD
        // =====================================================================

        private static void BuildHUD()
        {
            var root = MakeCanvas("PF_HUD", 10);
            root.GetComponent<UnityEngine.UI.CanvasScaler>().referenceResolution = new Vector2(1600, 900);
            var uiAudio = root.gameObject.AddComponent<AudioSource>();
            uiAudio.playOnAwake = false;
            uiAudio.spatialBlend = 0f;

            // crosshair + transfer ring
            var crosshairHalo = MakeRect("CrosshairHalo", root, Center, Center, Vector2.zero, new Vector2(24, 24));
            MakeImage(crosshairHalo, new Color(0.01f, 0.008f, 0.006f, 0.9f), diamond);
            var cross = MakeRect("Crosshair", root, Center, Center, Vector2.zero, new Vector2(14, 14));
            MakeImage(cross, new Color(1f, 0.78f, 0.42f, 1f), diamond);
            var ringOutline = MakeImage(MakeRect("TransferRingOutline", root, Center, Center, Vector2.zero, new Vector2(72, 72)),
                Color.black, ringOutlineSprite);
            SetupTransferRing(ringOutline);
            var ringImg = MakeImage(MakeRect("TransferRing", root, Center, Center, Vector2.zero, new Vector2(72, 72)),
                Color.white, ring);
            SetupTransferRing(ringImg);

            var prompt = MakeText(MakeRect("Prompt", root, Center, Center, new Vector2(0, -90), new Vector2(1250, 120)), "", body, 36, Warm, TextAlignmentOptions.Top);
            prompt.fontStyle = FontStyles.Bold;

            // fuel meter (bottom-left)
            var fuelRoot = MakeRect("FuelMeter", root, Vector2.zero, Vector2.zero, new Vector2(72, 64), new Vector2(260, 40));
            fuelRoot.pivot = Vector2.zero;
            var fuelGroup = fuelRoot.gameObject.AddComponent<CanvasGroup>();
            var slot = MakeText(MakeRect("SlotText", fuelRoot, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, new Vector2(0, 26)),
                "[1] Torch", title, 20, Warm, TextAlignmentOptions.BottomLeft);
            slot.characterSpacing = 6f;
            slot.rectTransform.pivot = new Vector2(0, 0);
            slot.rectTransform.anchoredPosition = new Vector2(0, -22);
            var fuelBg = MakeRect("Bar", fuelRoot, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 10));
            var fuelBarFlashGroup = fuelBg.gameObject.AddComponent<CanvasGroup>();
            fuelBg.pivot = new Vector2(0.5f, 0);
            MakeImage(fuelBg, new Color(0.1f, 0.05f, 0.04f, 0.7f));
            var fuelFill = MakeRect("Fill", fuelBg, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var fuelFillImg = MakeImage(fuelFill, Ember);
            var coverRoot = MakeRect("CoverMeter", fuelRoot, Vector2.zero, new Vector2(1, 0), new Vector2(0, 16), new Vector2(0, 4));
            coverRoot.pivot = new Vector2(0.5f, 0);
            var coverGroup = coverRoot.gameObject.AddComponent<CanvasGroup>();
            coverGroup.alpha = 0f;
            MakeImage(coverRoot, new Color(0.1f, 0.07f, 0.07f, 0.6f));
            var coverFill = MakeRect("Fill", coverRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            MakeImage(coverFill, new Color(0.95f, 0.85f, 0.7f, 0.85f));

            // centre message
            var message = MakeText(MakeRect("Message", root, Center, Center, new Vector2(0, 150), new Vector2(1300, 180)), "", italic, 48,
                new Color(1f, 0.86f, 0.72f, 1f), TextAlignmentOptions.Center);
            message.alpha = 0f;

            // objective: one quiet line, top-left, always says what to do next
            var objRoot = MakeRect("Objective", root, new Vector2(0, 1), new Vector2(0, 1), new Vector2(72, -66), new Vector2(900, 50));
            objRoot.pivot = new Vector2(0, 1);
            var objGroup = objRoot.gameObject.AddComponent<CanvasGroup>();
            objGroup.blocksRaycasts = false;
            var objGem = MakeRect("Gem", objRoot, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(9, 0), new Vector2(16, 16));
            MakeImage(objGem, Ember, diamond);
            var objText = MakeText(MakeRect("Text", objRoot, new Vector2(0, 0), new Vector2(1, 1), new Vector2(18, 0), new Vector2(-36, 0)), "",
                body, 36, new Color(1f, 0.92f, 0.8f, 1f), TextAlignmentOptions.MidlineLeft);
            objText.fontStyle = FontStyles.Bold;

            // contextual key hint card
            var hint = BuildHintCard(root);

            // dialogue: a soft band, the guiding light's words in pale cyan
            var dlg = MakeRect("DialogueBox", root, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 150), new Vector2(1300, 170));
            dlg.pivot = new Vector2(0.5f, 0);
            var dlgGroup = dlg.gameObject.AddComponent<CanvasGroup>();
            dlgGroup.blocksRaycasts = false;
            MakeImage(MakeRect("Band", dlg, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), new Color(0.01f, 0.01f, 0.02f, 0.88f), line);
            MakeImage(MakeRect("LineTop", dlg, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, new Vector2(0, 2)), GuideLine, line);
            MakeImage(MakeRect("LineBottom", dlg, new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, 2)), GuideLine, line);
            var orn = MakeRect("Ornament", dlg, new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(16, 16));
            MakeImage(orn, new Color(0.8f, 0.95f, 1f, 0.9f), diamond);
            var speaker = MakeText(MakeRect("Speaker", dlg, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, 26), new Vector2(600, 30)), "",
                title, 17, new Color(0.75f, 0.93f, 1f, 0.85f), TextAlignmentOptions.Center);
            speaker.characterSpacing = 10f;
            var bodyText = MakeText(MakeRect("Body", dlg, Vector2.zero, Vector2.one, new Vector2(0, 4), new Vector2(-240, -40)), "...", italic, 34, GuideText, TextAlignmentOptions.Center);
            bodyText.textWrappingMode = TextWrappingModes.Normal;
            var hintText = MakeText(MakeRect("Hint", dlg, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-150, 18), new Vector2(120, 26)),
                "F", title, 16, new Color(0.75f, 0.92f, 1f, 0.7f), TextAlignmentOptions.BottomRight);
            hintText.rectTransform.pivot = new Vector2(1, 0);

            // pause menu
            var pause = MakeRect("PausePanel", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            MakeImage(pause, new Color(0.015f, 0.008f, 0.015f, 0.86f));
            var pauseTitle = MakeText(MakeRect("Title", pause, Center, Center, new Vector2(0, 210), new Vector2(800, 100)), "PAUSED", title, 64, Warm, TextAlignmentOptions.Center);
            pauseTitle.characterSpacing = 16f;
            MakeImage(MakeRect("Divider", pause, Center, Center, new Vector2(0, 150), new Vector2(420, 2)), new Color(1f, 0.62f, 0.28f, 0.6f), line);
            var resume = MenuItem(pause, "Resume", new Vector2(-110, 70), 30, centered: true).button;
            var restart = MenuItem(pause, "Restart Level", new Vector2(-110, 10), 30, centered: true).button;
            var menu = MenuItem(pause, "Main Menu", new Vector2(-110, -50), 30, centered: true).button;
            var quit = MenuItem(pause, "Quit", new Vector2(-110, -110), 30, centered: true).button;

            // fader on top
            var fader = MakeRect("Fader", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            MakeImage(fader, Color.black);
            var faderGroup = fader.gameObject.AddComponent<CanvasGroup>();
            faderGroup.blocksRaycasts = false;
            faderGroup.interactable = false;
            crosshairHalo.SetAsLastSibling();
            cross.SetAsLastSibling();
            fader.SetAsLastSibling();

            var hud = root.gameObject.AddComponent<HUDController>();
            GenUtil.Set(hud, "promptText", prompt);
            GenUtil.Set(hud, "transferRing", ringImg);
            GenUtil.Set(hud, "transferRingOutline", ringOutline);
            GenUtil.Set(hud, "fuelGroup", fuelGroup);
            GenUtil.Set(hud, "fuelFill", fuelFill);
            GenUtil.Set(hud, "fuelFillImage", fuelFillImg);
            GenUtil.Set(hud, "fuelBarFlashGroup", fuelBarFlashGroup);
            GenUtil.Set(hud, "fuelColor", GameplayPrefabGenerator.Grad(
                new[] { new Color(0.8f, 0.12f, 0.05f), new Color(1f, 0.45f, 0.1f), new Color(1f, 0.72f, 0.35f) },
                new[] { 0f, 0.3f, 1f }, new[] { 1f, 1f }, new[] { 0f, 1f }));
            GenUtil.Set(hud, "slotText", slot);
            GenUtil.Set(hud, "coverGroup", coverGroup);
            GenUtil.Set(hud, "coverFill", coverFill);
            GenUtil.Set(hud, "messageText", message);

            var dialogue = root.gameObject.AddComponent<DialogueUI>();
            GenUtil.Set(dialogue, "group", dlgGroup);
            GenUtil.Set(dialogue, "bodyText", bodyText);
            GenUtil.Set(dialogue, "continueHint", hintText);
            GenUtil.Set(dialogue, "blipSource", uiAudio);
            GenUtil.Set(dialogue, "lineClip", AudioGenerator.Load("SFX_GuideChime"));
            GenUtil.Set(dialogue, "speakerText", speaker);

            var objective = root.gameObject.AddComponent<ObjectiveUI>();
            GenUtil.Set(objective, "group", objGroup);
            GenUtil.Set(objective, "root", objRoot);
            GenUtil.Set(objective, "text", objText);
            GenUtil.Set(objective, "chimeSource", uiAudio);
            GenUtil.Set(objective, "chimeClip", AudioGenerator.Load("SFX_UI_Hover"));

            var hintUI = root.gameObject.AddComponent<HintUI>();
            GenUtil.Set(hintUI, "group", hint.group);
            GenUtil.Set(hintUI, "card", hint.card);
            GenUtil.Set(hintUI, "keyText", hint.keyText);
            GenUtil.Set(hintUI, "keyCap", hint.keyCap);
            GenUtil.Set(hintUI, "labelText", hint.label);
            GenUtil.Set(hintUI, "chimeSource", uiAudio);
            GenUtil.Set(hintUI, "chimeClip", AudioGenerator.Load("SFX_GuideChime"));

            var screenFader = root.gameObject.AddComponent<ScreenFader>();
            GenUtil.Set(screenFader, "group", faderGroup);

            var pauseMenu = root.gameObject.AddComponent<PauseMenu>();
            GenUtil.Set(pauseMenu, "panel", pause.gameObject);
            GenUtil.Set(pauseMenu, "resumeButton", resume);
            GenUtil.Set(pauseMenu, "restartButton", restart);
            GenUtil.Set(pauseMenu, "menuButton", menu);
            GenUtil.Set(pauseMenu, "quitButton", quit);

            GenUtil.SavePrefab(root.gameObject, HUDPath);
        }

        private static (CanvasGroup group, RectTransform card, TMP_Text keyText, RectTransform keyCap, TMP_Text label) BuildHintCard(RectTransform root)
        {
            var holder = MakeRect("Hint", root, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 200), new Vector2(10, 76));
            var group = holder.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            var card = MakeRect("Card", holder, Center, Center, Vector2.zero, new Vector2(10, 76));
            MakeImage(card, new Color(0.02f, 0.012f, 0.02f, 0.9f), rounded).type = Image.Type.Sliced;
            var cardOutline = card.gameObject.AddComponent<Outline>();
            cardOutline.effectColor = new Color(1f, 0.62f, 0.28f, 0.8f);
            cardOutline.effectDistance = new Vector2(1.5f, -1.5f);
            var layout = card.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(18, 30, 11, 11);
            layout.spacing = 20f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var fitter = card.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            var keyCap = MakeRect("KeyCap", card, Center, Center, Vector2.zero, new Vector2(58, 54));
            var capImg = MakeImage(keyCap, new Color(0.12f, 0.06f, 0.03f, 0.95f), rounded);
            capImg.type = Image.Type.Sliced;
            var capOutline = keyCap.gameObject.AddComponent<Outline>();
            capOutline.effectColor = new Color(1f, 0.7f, 0.38f, 1f);
            capOutline.effectDistance = new Vector2(2f, -2f);
            var capLayout = keyCap.gameObject.AddComponent<LayoutElement>();
            capLayout.preferredWidth = 58f;
            capLayout.preferredHeight = 54f;
            var keyText = MakeText(MakeRect("Key", keyCap, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "F", title, 28, new Color(1f, 0.82f, 0.5f, 1f), TextAlignmentOptions.Center);
            keyText.fontStyle = FontStyles.Bold;

            var label = MakeText(MakeRect("Label", card, Center, Center, Vector2.zero, new Vector2(400, 54)), "hold to pass the flame", body, 36, new Color(1f, 0.95f, 0.88f, 1f), TextAlignmentOptions.MidlineLeft);
            label.fontStyle = FontStyles.Bold;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            return (group, card, keyText, keyCap, label);
        }

        // =====================================================================
        //  Main menu
        // =====================================================================

        private static void BuildMainMenu()
        {
            var root = MakeCanvas("PF_MainMenuCanvas", 0);
            var uiAudio = root.gameObject.AddComponent<AudioSource>();
            uiAudio.playOnAwake = false;
            var music = root.gameObject.AddComponent<AudioSource>();
            music.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(GenUtil.Root + "/Audio/Music/Theme.mp3");
            music.loop = true;
            music.playOnAwake = true;
            music.volume = 0.55f;

            // legibility shade on the left, scene stays visible on the right
            var shadeRt = MakeRect("LeftShade", root, new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(1250, 0));
            shadeRt.pivot = new Vector2(0, 0.5f);
            MakeImage(shadeRt, new Color(0.01f, 0.005f, 0.01f, 0.95f), shade);

            var left = new Vector2(0, 0.5f);

            // brand
            var titleText = MakeText(MakeRect("Title", root, left, left, new Vector2(150, 230), new Vector2(1500, 150)), "HOLLOW LIGHT", title, 104,
                Color.white, TextAlignmentOptions.Left);
            titleText.textWrappingMode = TextWrappingModes.NoWrap;
            titleText.rectTransform.pivot = new Vector2(0, 0.5f);
            titleText.characterSpacing = 10f;
            titleText.fontStyle = FontStyles.Bold;
            if (FontLibrary.TitleGlow != null) titleText.fontSharedMaterial = FontLibrary.TitleGlow;
            titleText.enableVertexGradient = true;
            titleText.colorGradient = new VertexGradient(new Color(1f, 0.9f, 0.7f), new Color(1f, 0.9f, 0.7f), new Color(0.95f, 0.5f, 0.22f), new Color(0.95f, 0.5f, 0.22f));
            var titleGroup = titleText.gameObject.AddComponent<CanvasGroup>();

            var divider = MakeRect("Divider", root, left, left, new Vector2(156, 150), new Vector2(520, 2));
            divider.pivot = new Vector2(0, 0.5f);
            MakeImage(divider, new Color(1f, 0.62f, 0.28f, 0.75f), line);
            var gem = MakeRect("Crystal", divider, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(60, 0), new Vector2(18, 18));
            MakeImage(gem, new Color(1f, 0.8f, 0.55f, 1f), diamond);


            // menu list
            var menuRoot = MakeRect("Menu", root, left, left, new Vector2(150, -60), new Vector2(520, 300));
            menuRoot.pivot = new Vector2(0, 0.5f);
            var menuGroup = menuRoot.gameObject.AddComponent<CanvasGroup>();
            var begin = MenuItem(menuRoot, "Begin", new Vector2(0, 105), 36);
            var settings = MenuItem(menuRoot, "Settings", new Vector2(0, 40), 36);
            var credits = MenuItem(menuRoot, "Credits", new Vector2(0, -25), 36);
            var quit = MenuItem(menuRoot, "Quit", new Vector2(0, -90), 36);

            // footer
            var footer = MakeRect("Footer", root, Vector2.zero, new Vector2(1, 0), new Vector2(0, 40), new Vector2(0, 30));
            footer.pivot = new Vector2(0.5f, 0);
            var footerGroup = footer.gameObject.AddComponent<CanvasGroup>();
            var jam = MakeText(MakeRect("Jam", footer, new Vector2(0, 0), new Vector2(0, 0), new Vector2(156, 0), new Vector2(700, 30)),
                "NASCON 2026  ·  GAME JAM BUILD", title, 15, new Color(1f, 0.85f, 0.7f, 0.38f), TextAlignmentOptions.BottomLeft);
            jam.rectTransform.pivot = new Vector2(0, 0);
            jam.characterSpacing = 8f;
            var controls = MakeText(MakeRect("Controls", footer, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-60, 0), new Vector2(700, 30)),
                "MOUSE  ·  ENTER TO SELECT  ·  ESC TO GO BACK", title, 14, new Color(1f, 0.85f, 0.7f, 0.3f), TextAlignmentOptions.BottomRight);
            controls.rectTransform.pivot = new Vector2(1, 0);
            controls.characterSpacing = 6f;

            // settings panel
            var settingsPanel = Panel(root, "SettingsPanel", "Settings", out var settingsBody);
            var sens = SliderRow(settingsBody, "Mouse sensitivity", new Vector2(0, 120));
            var vol = SliderRow(settingsBody, "Master volume", new Vector2(0, 50));
            var brush = SliderRow(settingsBody, "Brush strokes", new Vector2(0, -20));
            var fullscreen = ToggleRow(settingsBody, "Fullscreen", new Vector2(0, -90));
            var settingsBack = MenuItem(settingsBody, "Back", new Vector2(-14, -180), 26).button;

            // credits panel
            var creditsPanel = Panel(root, "CreditsPanel", "Credits", out var creditsBody);
            var creditsText = MakeText(MakeRect("Text", creditsBody, Center, Center, new Vector2(0, 20), new Vector2(520, 300)),
                "<size=120%><color=#FFB462>HOLLOW LIGHT</color></size>\n<size=95%>A game made for the NASCON 2026 game jam.</size>\n\n" +
                "<color=#FFB462>Design  ·  Code  ·  Art</color>\n<alpha=#CC>the team</alpha>\n\n" +
                "<size=85%><alpha=#99>Main character modelled with Meshy.\nEverything else generated in code.\nCinzel & EB Garamond, SIL Open Font License.</size>",
                body, 27, Warm, TextAlignmentOptions.Top);
            creditsText.textWrappingMode = TextWrappingModes.Normal;
            var creditsBack = MenuItem(creditsBody, "Back", new Vector2(-14, -180), 26).button;

            // fader on top
            var fader = MakeRect("Fader", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            MakeImage(fader, Color.black);
            var faderGroup = fader.gameObject.AddComponent<CanvasGroup>();
            faderGroup.blocksRaycasts = false;

            var controller = root.gameObject.AddComponent<MainMenuController>();
            GenUtil.Set(controller, "title", titleGroup);
            GenUtil.Set(controller, "footer", footerGroup);
            GenUtil.Set(controller, "divider", divider);
            GenUtil.SetArray(controller, "items", new Object[] { begin.group, settings.group, credits.group, quit.group });
            GenUtil.Set(controller, "beginButton", begin.button);
            GenUtil.Set(controller, "settingsButton", settings.button);
            GenUtil.Set(controller, "creditsButton", credits.button);
            GenUtil.Set(controller, "quitButton", quit.button);
            GenUtil.Set(controller, "settingsBack", settingsBack);
            GenUtil.Set(controller, "creditsBack", creditsBack);
            GenUtil.Set(controller, "menuGroup", menuGroup);
            GenUtil.Set(controller, "settingsPanel", settingsPanel);
            GenUtil.Set(controller, "creditsPanel", creditsPanel);
            GenUtil.Set(controller, "sensitivitySlider", sens.slider);
            GenUtil.Set(controller, "sensitivityValue", sens.value);
            GenUtil.Set(controller, "volumeSlider", vol.slider);
            GenUtil.Set(controller, "volumeValue", vol.value);
            GenUtil.Set(controller, "brushSlider", brush.slider);
            GenUtil.Set(controller, "brushValue", brush.value);
            GenUtil.Set(controller, "fullscreenToggle", fullscreen);
            GenUtil.Set(controller, "uiSource", uiAudio);
            GenUtil.Set(controller, "hoverClip", AudioGenerator.Load("SFX_UI_Hover"));
            GenUtil.Set(controller, "selectClip", AudioGenerator.Load("SFX_UI_Select"));
            GenUtil.Set(controller, "music", music);
            GenUtil.Set(controller, "firstScene", NasconGJ.Core.SceneNames.Prologue);
            var screenFader = root.gameObject.AddComponent<ScreenFader>();
            GenUtil.Set(screenFader, "group", faderGroup);
            GenUtil.Set(screenFader, "fadeInOnStart", 2.2f);

            GenUtil.SavePrefab(root.gameObject, MainMenuPath);
        }

        // =====================================================================
        //  Cutscene overlay
        // =====================================================================

        private static void BuildCutsceneCanvas()
        {
            var root = MakeCanvas("PF_CutsceneCanvas", 20);

            var top = MakeRect("BarTop", root, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, new Vector2(0, 0));
            top.pivot = new Vector2(0.5f, 1);
            MakeImage(top, Color.black);
            var bottom = MakeRect("BarBottom", root, new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, 0));
            bottom.pivot = new Vector2(0.5f, 0);
            MakeImage(bottom, Color.black);

            // one quiet line in the lower bar area
            var captionRt = MakeRect("Caption", root, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 168), new Vector2(1500, 70));
            var captionGroup = captionRt.gameObject.AddComponent<CanvasGroup>();
            // soft dark pool behind the line, so it reads over a bright shot
            var backing = new GameObject("Backing", typeof(RectTransform), typeof(Image));
            backing.transform.SetParent(captionRt, false);
            var brt = (RectTransform)backing.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.sizeDelta = new Vector2(1640f, 88f);
            var braw = backing.GetComponent<Image>();
            braw.color = new Color(0f, 0f, 0f, 0.62f);
            braw.raycastTarget = false;
            var captionTextRt = new GameObject("Text", typeof(RectTransform)).GetComponent<RectTransform>();
            captionTextRt.SetParent(captionRt, false);
            captionTextRt.anchorMin = Vector2.zero; captionTextRt.anchorMax = Vector2.one;
            captionTextRt.offsetMin = captionTextRt.offsetMax = Vector2.zero;
            var caption = MakeText(captionTextRt, "", body, 44, new Color(1f, 0.95f, 0.86f, 1f), TextAlignmentOptions.Center);
            // outline comes from the font material now (FontLibrary.ApplyTextOutline)

            // title card
            var titleRt = MakeRect("Title", root, Center, Center, new Vector2(0, 20), new Vector2(1600, 180));
            var titleGroup = titleRt.gameObject.AddComponent<CanvasGroup>();
            var titleText = MakeText(titleRt, "HOLLOW LIGHT", title, 120, Color.white, TextAlignmentOptions.Center);
            titleText.characterSpacing = 14f;
            titleText.fontStyle = FontStyles.Bold;
            if (FontLibrary.TitleGlow != null) titleText.fontSharedMaterial = FontLibrary.TitleGlow;
            titleText.enableVertexGradient = true;
            titleText.colorGradient = new VertexGradient(new Color(1f, 0.9f, 0.7f), new Color(1f, 0.9f, 0.7f), new Color(0.95f, 0.5f, 0.22f), new Color(0.95f, 0.5f, 0.22f));
            var rule = MakeRect("Rule", titleRt, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, -6), new Vector2(520, 2));
            MakeImage(rule, new Color(1f, 0.62f, 0.28f, 0.75f), line);

            // hold-to-skip, bottom right
            var skipRt = MakeRect("Skip", root, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-70, 46), new Vector2(360, 40));
            skipRt.pivot = new Vector2(1, 0.5f);
            var skipGroup = skipRt.gameObject.AddComponent<CanvasGroup>();
            var fillRt = MakeRect("Ring", skipRt, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-16, 0), new Vector2(30, 30));
            MakeImage(fillRt, new Color(1f, 0.9f, 0.8f, 0.2f), ring);
            var fillImg = MakeImage(MakeRect("Fill", fillRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), Ember, ring);
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Radial360;
            fillImg.fillOrigin = (int)Image.Origin360.Top;
            fillImg.fillAmount = 0f;
            var skipText = MakeText(MakeRect("Label", skipRt, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-48, 0), new Vector2(300, 36)),
                "HOLD SPACE TO SKIP", title, 16, new Color(1f, 0.88f, 0.75f, 0.7f), TextAlignmentOptions.MidlineRight);
            skipText.rectTransform.pivot = new Vector2(1, 0.5f);
            skipText.characterSpacing = 6f;

            var fader = MakeRect("Fader", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            MakeImage(fader, Color.black);
            var faderGroup = fader.gameObject.AddComponent<CanvasGroup>();
            faderGroup.blocksRaycasts = false;

            var cutsceneUI = root.gameObject.AddComponent<NasconGJ.Cutscenes.CutsceneUI>();
            GenUtil.Set(cutsceneUI, "barTop", top);
            GenUtil.Set(cutsceneUI, "barBottom", bottom);
            GenUtil.Set(cutsceneUI, "caption", caption);
            GenUtil.Set(cutsceneUI, "captionGroup", captionGroup);
            GenUtil.Set(cutsceneUI, "titleGroup", titleGroup);
            GenUtil.Set(cutsceneUI, "skipGroup", skipGroup);
            GenUtil.Set(cutsceneUI, "skipFill", fillImg);
            var screenFader = root.gameObject.AddComponent<ScreenFader>();
            GenUtil.Set(screenFader, "group", faderGroup);
            GenUtil.Set(screenFader, "fadeInOnStart", 2.5f);

            GenUtil.SavePrefab(root.gameObject, CutscenePath);
        }

        private static CanvasGroup Panel(RectTransform root, string name, string header, out RectTransform body)
        {
            var panel = MakeRect(name, root, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-170, -30), new Vector2(640, 560));
            panel.pivot = new Vector2(1, 0.5f);
            var group = panel.gameObject.AddComponent<CanvasGroup>();
            var bg = MakeImage(panel, new Color(0.035f, 0.02f, 0.03f, 0.92f), rounded);
            bg.type = Image.Type.Sliced;
            bg.raycastTarget = true;
            var outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.62f, 0.28f, 0.35f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            var h = MakeText(MakeRect("Header", panel, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -52), new Vector2(560, 60)),
                header.ToUpperInvariant(), title, 38, Warm, TextAlignmentOptions.Center);
            h.characterSpacing = 14f;
            MakeImage(MakeRect("Divider", panel, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -94), new Vector2(380, 2)), new Color(1f, 0.62f, 0.28f, 0.6f), line);
            body = MakeRect("Body", panel, Center, Center, new Vector2(0, -20), new Vector2(560, 420));
            return group;
        }

        private static (Slider slider, TMP_Text value) SliderRow(RectTransform parent, string label, Vector2 pos)
        {
            var row = MakeRect(label.Replace(" ", ""), parent, Center, Center, pos, new Vector2(520, 44));
            MakeText(MakeRect("Label", row, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(110, 0), new Vector2(220, 40)), label, body, 26, Warm, TextAlignmentOptions.MidlineLeft);

            var sliderRt = MakeRect("Slider", row, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(345, 0), new Vector2(210, 24));
            var bg = MakeRect("Background", sliderRt, new Vector2(0, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(0, 3));
            MakeImage(bg, new Color(1f, 0.9f, 0.8f, 0.2f));
            var fillArea = MakeRect("Fill Area", sliderRt, new Vector2(0, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(0, 3));
            var fill = MakeRect("Fill", fillArea, Vector2.zero, new Vector2(0, 1), Vector2.zero, Vector2.zero);
            MakeImage(fill, Ember);
            var handleArea = MakeRect("Handle Slide Area", sliderRt, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-16, 0));
            var handle = MakeRect("Handle", handleArea, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(20, 20));
            var handleImg = MakeImage(handle, new Color(1f, 0.8f, 0.55f, 1f), diamond);
            handleImg.raycastTarget = true;
            var slider = sliderRt.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight;
            var colors = slider.colors;
            colors.highlightedColor = new Color(1f, 0.95f, 0.85f);
            colors.selectedColor = new Color(1f, 0.95f, 0.85f);
            slider.colors = colors;

            var value = MakeText(MakeRect("Value", row, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-30, 0), new Vector2(90, 40)), "", title, 18, Muted, TextAlignmentOptions.MidlineRight);
            return (slider, value);
        }

        private static Toggle ToggleRow(RectTransform parent, string label, Vector2 pos)
        {
            var row = MakeRect(label.Replace(" ", ""), parent, Center, Center, pos, new Vector2(520, 44));
            MakeText(MakeRect("Label", row, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(110, 0), new Vector2(220, 40)), label, body, 26, Warm, TextAlignmentOptions.MidlineLeft);
            var box = MakeRect("Box", row, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(255, 0), new Vector2(26, 26));
            var boxImg = MakeImage(box, new Color(1f, 0.66f, 0.32f, 0.15f), rounded);
            boxImg.type = Image.Type.Sliced;
            boxImg.raycastTarget = true;
            var o = box.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(1f, 0.66f, 0.32f, 0.7f);
            var check = MakeRect("Check", box, Center, Center, Vector2.zero, new Vector2(16, 16));
            var checkImg = MakeImage(check, Ember, diamond);
            var toggle = box.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = boxImg;
            toggle.graphic = checkImg;
            return toggle;
        }

        /// <summary>Menu item: crystal marker, Cinzel label, ember underline, MenuButtonFX feel.</summary>
        private static (Button button, CanvasGroup group) MenuItem(RectTransform parent, string label, Vector2 pos, float size, bool centered = false)
        {
            var a = centered ? Center : new Vector2(0, 0.5f);
            var rt = MakeRect("Item_" + label.Replace(" ", ""), parent, a, a, pos, new Vector2(460, size + 22));
            rt.pivot = new Vector2(0, 0.5f);
            var group = rt.gameObject.AddComponent<CanvasGroup>();
            var hit = MakeImage(rt, new Color(0, 0, 0, 0));
            hit.raycastTarget = true;
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;

            var marker = MakeRect("Marker", rt, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(14, 14));
            var markerImg = MakeImage(marker, Ember, diamond);

            var labelRt = MakeRect("Label", rt, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(30, 0), new Vector2(420, size + 10));
            labelRt.pivot = new Vector2(0, 0.5f);
            var text = MakeText(labelRt, label.ToUpperInvariant(), title, size, Muted, TextAlignmentOptions.MidlineLeft);
            text.characterSpacing = 9f;

            var underline = MakeRect("Underline", rt, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(30, -size * 0.55f), new Vector2(0, 2));
            underline.pivot = new Vector2(0, 0.5f);
            MakeImage(underline, new Color(1f, 0.62f, 0.28f, 0.8f), line);

            var fx = rt.gameObject.AddComponent<MenuButtonFX>();
            GenUtil.Set(fx, "label", labelRt);
            GenUtil.Set(fx, "labelText", text);
            GenUtil.Set(fx, "marker", markerImg);
            GenUtil.Set(fx, "underline", underline);
            GenUtil.Set(fx, "underlineWidth", Mathf.Min(260f, label.Length * size * 0.75f));
            return (button, group);
        }

        // =====================================================================
        //  helpers
        // =====================================================================

        private static readonly Vector2 Center = new(0.5f, 0.5f);

        private static RectTransform MakeCanvas(string name, int order)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return (RectTransform)go.transform;
        }

        private static RectTransform MakeRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        private static void SetupTransferRing(Image image)
        {
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Radial360;
            image.fillOrigin = (int)Image.Origin360.Top;
            image.fillAmount = 0f;
        }

        private static Image MakeImage(RectTransform rt, Color color, Sprite sprite = null)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.sprite = sprite;
            img.raycastTarget = false;
            return img;
        }

        private static TextMeshProUGUI MakeText(RectTransform rt, string text, TMP_FontAsset font, float size, Color color, TextAlignmentOptions align)
        {
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            t.richText = true;
            return t;
        }
    }
}
