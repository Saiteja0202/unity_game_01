using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// Menu: PUBG Game ▶ Setup Complete Scene
public static class GameSceneSetup
{
    // ══════════════════════════════════════════════════════════════════════════
    [MenuItem("PUBG Game/Setup Complete Scene", priority = 0)]
    public static void SetupScene()
    {
        if (!EditorUtility.DisplayDialog("Build PUBG Scene",
            "Clears the current scene and builds a PUBG-style environment with mobile joystick controls.\n\nContinue?",
            "Build", "Cancel"))
            return;

        EnsureFolders();
        ClearScene();

        CreateLighting();
        CreateTerrain();
        CreateEnvironmentProps();
        var player = CreatePlayer();
        SetupCamera(player.transform);
        CreateMobileUI();      // ← joystick + touch-look + buttons
        CreateHUDObject();
        CreateZoneCircle();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "✅ Scene built!  Press Play to test.\n" +
            "Desktop : WASD=move  Shift=run  C=crouch  Space=jump  Mouse=look  Scroll=zoom\n" +
            "Mobile  : Left joystick=move  Right drag=look  JUMP/CROUCH buttons");
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  Mobile UI
    // ══════════════════════════════════════════════════════════════════════════

    static void CreateMobileUI()
    {
        // EventSystem (required for UI interaction)
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();

            // Unity 6 + InputSystem package needs InputSystemUIInputModule.
            // Fall back to StandaloneInputModule if package is absent.
            var inputSysModuleType = System.Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSysModuleType != null)
                esGO.AddComponent(inputSysModuleType);
            else
                esGO.AddComponent<StandaloneInputModule>();
        }

        // ── Canvas ──────────────────────────────────────────────────────────
        var canvasGO = new GameObject("Mobile Controls");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        var provider = canvasGO.AddComponent<MobileInputProvider>();

        // ── Generate joystick sprite assets ─────────────────────────────────
        Sprite bgSprite     = MakeCircleSprite("JoystickBg",     200, new Color(0f,0f,0f,0.45f), new Color(1f,1f,1f,0.55f), 0.06f);
        Sprite handleSprite = MakeCircleSprite("JoystickHandle", 100, new Color(1f,1f,1f,0.70f), new Color(1f,1f,1f,0.90f), 0.08f);
        Sprite btnSprite    = MakeCircleSprite("ButtonBg",        80, new Color(0f,0f,0f,0.50f), new Color(1f,1f,1f,0.50f), 0.07f);
        Sprite lookSprite   = null; // fully transparent

        // ── Left joystick ────────────────────────────────────────────────────
        //   Anchored to bottom-left; 260×260, centre at (160,160) from corner
        var joyBgGO  = MakeImage("MoveJoystickBg", canvasGO.transform,
                           bgSprite, Color.white,
                           AnchorPreset.BottomLeft,
                           new Vector2(160f, 160f), new Vector2(260f, 260f));

        var joyBgRT  = joyBgGO.GetComponent<RectTransform>();

        var handleGO = MakeImage("Handle", joyBgGO.transform,
                           handleSprite, Color.white,
                           AnchorPreset.Center,
                           Vector2.zero, new Vector2(110f, 110f));
        handleGO.GetComponent<Image>().raycastTarget = false;

        var joystick          = joyBgGO.AddComponent<VirtualJoystick>();
        joystick.handle       = handleGO.GetComponent<RectTransform>();

        provider.moveJoystick = joystick;

        // ── Right-side look area (transparent, right 60 % of screen) ────────
        var lookGO  = MakeImage("LookArea", canvasGO.transform,
                          lookSprite, new Color(1f,1f,1f,0f),
                          AnchorPreset.StretchRight,
                          Vector2.zero, Vector2.zero);
        // Stretch anchors: left=0.4 right=1  bottom=0 top=1, leave margins 0
        var lookRT  = lookGO.GetComponent<RectTransform>();
        lookRT.anchorMin = new Vector2(0.38f, 0.08f);
        lookRT.anchorMax = new Vector2(0.80f, 1.00f);
        lookRT.offsetMin = lookRT.offsetMax = Vector2.zero;

        var lookArea          = lookGO.AddComponent<TouchLookArea>();
        provider.lookArea     = lookArea;

        // ── Action buttons (bottom-right) ─────────────────────────────────
        //   JUMP
        var jumpGO = MakeImage("JUMP", canvasGO.transform, btnSprite,
                         new Color(0.20f, 0.75f, 0.25f, 0.90f),
                         AnchorPreset.BottomRight,
                         new Vector2(-100f, 120f), new Vector2(140f, 140f));
        AddLabel(jumpGO.transform, "JUMP", 18, Color.white);
        jumpGO.AddComponent<MobileButtonHandler>().action = MobileButtonHandler.ButtonAction.Jump;

        //   CROUCH
        var crouchGO = MakeImage("CROUCH", canvasGO.transform, btnSprite,
                         new Color(0.85f, 0.60f, 0.10f, 0.90f),
                         AnchorPreset.BottomRight,
                         new Vector2(-270f, 90f), new Vector2(120f, 120f));
        AddLabel(crouchGO.transform, "CROUCH", 16, Color.white);
        crouchGO.AddComponent<MobileButtonHandler>().action = MobileButtonHandler.ButtonAction.CrouchHold;

        // ── Sprint label under joystick ──────────────────────────────────
        var sprintLbl = MakeLabel("SprintHint", canvasGO.transform,
                            "Push full = Sprint",
                            new Vector2(160f, 25f), 18,
                            new Color(1f,1f,1f,0.6f),
                            AnchorPreset.BottomLeft);
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  Lighting
    // ══════════════════════════════════════════════════════════════════════════

    static void CreateLighting()
    {
        var sunGO  = new GameObject("Directional Light");
        var light  = sunGO.AddComponent<Light>();
        light.type      = LightType.Directional;
        light.color     = new Color(1f, 0.95f, 0.80f);
        light.intensity = 1.4f;
        light.shadows   = LightShadows.Soft;
        sunGO.transform.rotation = Quaternion.Euler(52f, -30f, 0f);

        RenderSettings.ambientMode       = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight      = new Color(0.45f, 0.55f, 0.65f);
        RenderSettings.fog               = true;
        RenderSettings.fogMode           = FogMode.Linear;
        RenderSettings.fogStartDistance  = 120f;
        RenderSettings.fogEndDistance    = 500f;
        RenderSettings.fogColor          = new Color(0.72f, 0.78f, 0.84f);
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  Terrain
    // ══════════════════════════════════════════════════════════════════════════

    static void CreateTerrain()
    {
        var td = new TerrainData();
        td.heightmapResolution = 257;
        td.size = new Vector3(500f, 35f, 500f);

        int   res  = td.heightmapResolution;
        float seed = 42.7f;
        var   h    = new float[res, res];
        for (int z = 0; z < res; z++)
            for (int x = 0; x < res; x++)
            {
                float fx = (float)x / res, fz = (float)z / res;
                h[z, x] =
                    Mathf.PerlinNoise(fx * 2.5f + seed, fz * 2.5f + seed) * 0.12f +
                    Mathf.PerlinNoise(fx * 5.0f + seed, fz * 5.0f + seed) * 0.04f +
                    Mathf.PerlinNoise(fx * 10f  + seed, fz * 10f  + seed) * 0.01f;

                float d = Mathf.Sqrt((fx - 0.5f) * (fx - 0.5f) + (fz - 0.5f) * (fz - 0.5f));
                h[z, x] = Mathf.Lerp(0.04f, h[z, x], Mathf.Clamp01((d - 0.06f) * 14f));
            }
        td.SetHeights(0, 0, h);

        Texture2D grassTex = MakeSolidTexture(new Color(0.34f, 0.53f, 0.24f), "GrassTex");
        Texture2D dirtTex  = MakeSolidTexture(new Color(0.52f, 0.40f, 0.27f), "DirtTex");
        SaveAsset(grassTex, "Assets/Generated/Textures/GrassTex.asset");
        SaveAsset(dirtTex,  "Assets/Generated/Textures/DirtTex.asset");

        var gl = new TerrainLayer { diffuseTexture = grassTex, tileSize = new Vector2(12f, 12f) };
        var dl = new TerrainLayer { diffuseTexture = dirtTex,  tileSize = new Vector2(8f,  8f)  };
        SaveAsset(gl, "Assets/Generated/Terrain/GrassLayer.asset");
        SaveAsset(dl, "Assets/Generated/Terrain/DirtLayer.asset");

        td.terrainLayers = new TerrainLayer[] { gl, dl };
        SaveAsset(td, "Assets/Generated/Terrain/TerrainData.asset");

        var tGO     = Terrain.CreateTerrainGameObject(td);
        tGO.name    = "Terrain";
        tGO.transform.position = new Vector3(-250f, 0f, -250f);
        tGO.GetComponent<Terrain>().heightmapPixelError = 6;
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  Environment Props
    // ══════════════════════════════════════════════════════════════════════════

    static void CreateEnvironmentProps()
    {
        var trunkM  = MakeMat(new Color(0.38f, 0.24f, 0.14f), "TreeTrunk");
        var leavesM = MakeMat(new Color(0.18f, 0.48f, 0.10f), "TreeLeaves");
        var wallM   = MakeMat(new Color(0.72f, 0.66f, 0.55f), "Building");
        var roofM   = MakeMat(new Color(0.50f, 0.33f, 0.22f), "Roof");
        var rockM   = MakeMat(new Color(0.55f, 0.52f, 0.48f), "Rock");

        var root = new GameObject("Environment Props").transform;
        var rng  = new System.Random(12345);

        for (int i = 0; i < 100; i++)
        {
            float x = (float)(rng.NextDouble() * 440 - 220);
            float z = (float)(rng.NextDouble() * 440 - 220);
            if (x * x + z * z < 400f) continue;
            SpawnTree(root, new Vector3(x, SampleHeight(x, z), z), trunkM, leavesM, rng);
        }

        Vector3[] bPos = {
            new Vector3( 55,0, 55), new Vector3(-65,0, 42), new Vector3( 35,0,-85),
            new Vector3(-85,0,-65), new Vector3(110,0, 22), new Vector3(-45,0,105),
            new Vector3( 80,0,-40), new Vector3(-30,0,-110)
        };
        foreach (var bp in bPos)
            SpawnBuilding(root, new Vector3(bp.x, SampleHeight(bp.x, bp.z), bp.z), wallM, roofM, rng);

        for (int i = 0; i < 40; i++)
        {
            float x = (float)(rng.NextDouble() * 380 - 190);
            float z = (float)(rng.NextDouble() * 380 - 190);
            SpawnRock(root, new Vector3(x, SampleHeight(x, z), z), rockM, rng);
        }
    }

    static void SpawnTree(Transform p, Vector3 pos, Material tm, Material lm, System.Random rng)
    {
        var root = new GameObject("Tree").transform; root.SetParent(p); root.position = pos;
        float ht = 4f + (float)rng.NextDouble() * 5f;

        var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Trunk"; trunk.transform.SetParent(root);
        trunk.transform.localPosition = new Vector3(0, ht * 0.38f, 0);
        trunk.transform.localScale    = new Vector3(0.28f, ht * 0.38f, 0.28f);
        trunk.GetComponent<Renderer>().sharedMaterial = tm;

        float[] ls = { 2.6f, 2.0f, 1.3f };
        float[] lh = { ht * 0.55f, ht * 0.76f, ht * 0.95f };
        for (int i = 0; i < 3; i++)
        {
            var leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leaf.name = "Leaves"; leaf.transform.SetParent(root);
            leaf.transform.localPosition = new Vector3(0, lh[i], 0);
            leaf.transform.localScale    = Vector3.one * ls[i];
            leaf.GetComponent<Renderer>().sharedMaterial = lm;
            Object.DestroyImmediate(leaf.GetComponent<SphereCollider>());
        }
    }

    static void SpawnBuilding(Transform p, Vector3 pos, Material wm, Material rm, System.Random rng)
    {
        var root = new GameObject("Building").transform; root.SetParent(p); root.position = pos;
        root.eulerAngles = new Vector3(0f, (float)(rng.NextDouble() * 360), 0f);
        float w = 9f + (float)rng.NextDouble() * 7f, d = 8f + (float)rng.NextDouble() * 5f, ht = 3.5f + (float)rng.NextDouble() * 2f;

        var walls = GameObject.CreatePrimitive(PrimitiveType.Cube);
        walls.name = "Walls"; walls.transform.SetParent(root);
        walls.transform.localPosition = new Vector3(0, ht * 0.5f, 0);
        walls.transform.localScale    = new Vector3(w, ht, d);
        walls.GetComponent<Renderer>().sharedMaterial = wm;

        var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.name = "Roof"; roof.transform.SetParent(root);
        roof.transform.localPosition = new Vector3(0, ht + 0.2f, 0);
        roof.transform.localScale    = new Vector3(w + 0.6f, 0.4f, d + 0.6f);
        roof.GetComponent<Renderer>().sharedMaterial = rm;

        var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
        door.name = "Door"; door.transform.SetParent(root);
        door.transform.localPosition = new Vector3(0, 1.1f, d * 0.5f + 0.02f);
        door.transform.localScale    = new Vector3(1.4f, 2.2f, 0.06f);
        door.GetComponent<Renderer>().sharedMaterial = MakeMat(new Color(0.12f, 0.09f, 0.06f), "Door");
    }

    static void SpawnRock(Transform p, Vector3 pos, Material mat, System.Random rng)
    {
        var rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rock.name = "Rock"; rock.transform.SetParent(p); rock.transform.position = pos;
        float s = 0.4f + (float)rng.NextDouble() * 1.2f;
        rock.transform.localScale  = new Vector3(s * (0.7f + (float)rng.NextDouble() * 0.6f),
                                                  s * (0.4f + (float)rng.NextDouble() * 0.5f),
                                                  s * (0.7f + (float)rng.NextDouble() * 0.6f));
        rock.transform.eulerAngles = new Vector3(0, (float)(rng.NextDouble() * 360), 0);
        rock.GetComponent<Renderer>().sharedMaterial = mat;
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  Player
    // ══════════════════════════════════════════════════════════════════════════

    static GameObject CreatePlayer()
    {
        var player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = new Vector3(0f, 8f, 0f);

        var cc      = player.AddComponent<CharacterController>();
        cc.height   = 1.8f; cc.radius = 0.3f;
        cc.center   = new Vector3(0f, 0.9f, 0f);
        cc.stepOffset = 0.35f; cc.slopeLimit = 50f; cc.skinWidth = 0.02f;

        player.AddComponent<PlayerController>();

        var visual = new GameObject("Visual").transform;
        visual.SetParent(player.transform); visual.localPosition = Vector3.zero;

        var skinM    = MakeMat(new Color(0.80f, 0.65f, 0.50f), "Skin");
        var clothM   = MakeMat(new Color(0.32f, 0.30f, 0.22f), "Clothes");
        var vestM    = MakeMat(new Color(0.48f, 0.44f, 0.32f), "Vest");
        var bootsM   = MakeMat(new Color(0.18f, 0.14f, 0.10f), "Boots");
        var helmM    = MakeMat(new Color(0.28f, 0.26f, 0.20f), "Helmet");
        var gunM     = MakeMat(new Color(0.12f, 0.12f, 0.12f), "Gun");

        var torso    = Limb("Torso",  visual,  new Vector3(0f,   1.08f, 0f), new Vector3(0.52f, 0.66f, 0.28f), vestM);
        var head     = Limb("Head",   visual,  new Vector3(0f,   1.64f, 0f), Vector3.one * 0.34f, skinM, PrimitiveType.Sphere);
        /*var helmet=*/Limb("Helmet", head,    new Vector3(0f,   0.10f, 0f), new Vector3(1.15f, 0.75f, 1.10f), helmM, PrimitiveType.Sphere);

        var laPivot  = Pivot("LArmPivot", visual, new Vector3(-0.36f, 1.28f, 0f));
        /*var la =  */ Limb("LeftArm",  laPivot, new Vector3(0f, -0.26f, 0f), new Vector3(0.14f, 0.50f, 0.14f), clothM, PrimitiveType.Capsule);

        var raPivot  = Pivot("RArmPivot", visual, new Vector3( 0.36f, 1.28f, 0f));
        /*var ra =  */ Limb("RightArm", raPivot, new Vector3(0f, -0.26f, 0f), new Vector3(0.14f, 0.50f, 0.14f), clothM, PrimitiveType.Capsule);
        /*var gun = */ Limb("Gun",      raPivot, new Vector3(0.06f, -0.60f, 0.25f), new Vector3(0.06f, 0.08f, 0.55f), gunM);

        var llPivot  = Pivot("LLegPivot", visual, new Vector3(-0.14f, 0.78f, 0f));
        /*var ll =  */ Limb("LeftLeg",   llPivot, new Vector3(0f, -0.28f, 0f), new Vector3(0.17f, 0.52f, 0.17f), clothM, PrimitiveType.Capsule);
        /*var lb =  */ Limb("LeftBoot",  llPivot, new Vector3(0f, -0.56f, 0f), new Vector3(0.19f, 0.14f, 0.22f), bootsM);

        var rlPivot  = Pivot("RLegPivot", visual, new Vector3( 0.14f, 0.78f, 0f));
        /*var rl =  */ Limb("RightLeg",  rlPivot, new Vector3(0f, -0.28f, 0f), new Vector3(0.17f, 0.52f, 0.17f), clothM, PrimitiveType.Capsule);
        /*var rb =  */ Limb("RightBoot", rlPivot, new Vector3(0f, -0.56f, 0f), new Vector3(0.19f, 0.14f, 0.22f), bootsM);

        var anim      = player.AddComponent<ProceduralAnimator>();
        anim.body     = torso;  anim.head     = head;
        anim.leftArm  = laPivot; anim.rightArm = raPivot;
        anim.leftLeg  = llPivot; anim.rightLeg = rlPivot;

        return player;
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  Camera
    // ══════════════════════════════════════════════════════════════════════════

    static void SetupCamera(Transform playerTransform)
    {
        var camGO   = new GameObject("Main Camera");
        camGO.tag   = "MainCamera";

        var cam             = camGO.AddComponent<Camera>();
        cam.fieldOfView     = 68f;
        cam.nearClipPlane   = 0.15f;
        cam.farClipPlane    = 700f;
        camGO.AddComponent<AudioListener>();

        var urpType = System.Type.GetType(
            "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
        if (urpType != null) camGO.AddComponent(urpType);

        var tpc              = camGO.AddComponent<ThirdPersonCamera>();
        tpc.target           = playerTransform;
        tpc.distance         = 3.5f;
        tpc.shoulderOffset   = new Vector3(0.55f, 1.65f, 0f);
        tpc.collisionMask    = LayerMask.GetMask("Default");

        camGO.transform.position = playerTransform.position + new Vector3(0.55f, 1.65f, -3.5f);
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  HUD & Zone
    // ══════════════════════════════════════════════════════════════════════════

    static void CreateHUDObject() => new GameObject("Game HUD").AddComponent<GameHUD>();

    static void CreateZoneCircle()
    {
        int   seg = 64; float radius = 180f;
        var   go  = new GameObject("Zone Circle");
        var   lr  = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.loop = true;
        lr.positionCount = seg;
        lr.startWidth = lr.endWidth = 1.2f;
        var zoneShader = Shader.Find("Universal Render Pipeline/Unlit")
                      ?? Shader.Find("Sprites/Default")
                      ?? Shader.Find("Unlit/Color")
                      ?? Shader.Find("Standard");
        var mat = new Material(zoneShader);
        mat.color = new Color(0.3f, 0.7f, 1f, 0.85f);
        lr.material = mat;
        var pos = new Vector3[seg];
        for (int i = 0; i < seg; i++)
        {
            float a = (float)i / seg * Mathf.PI * 2f;
            float x = Mathf.Cos(a) * radius, z = Mathf.Sin(a) * radius;
            pos[i] = new Vector3(x, SampleHeight(x, z) + 1.5f, z);
        }
        lr.SetPositions(pos);
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  UI helpers
    // ══════════════════════════════════════════════════════════════════════════

    enum AnchorPreset { BottomLeft, BottomRight, Center, StretchRight }

    static GameObject MakeImage(string name, Transform parent, Sprite sprite, Color color,
                                 AnchorPreset anchor, Vector2 anchoredPos, Vector2 size)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);

        var img   = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color  = color;
        if (sprite == null) img.raycastTarget = (anchor == AnchorPreset.StretchRight);

        var rt = go.GetComponent<RectTransform>();
        switch (anchor)
        {
            case AnchorPreset.BottomLeft:
                rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero;
                break;
            case AnchorPreset.BottomRight:
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);
                break;
            case AnchorPreset.Center:
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                break;
            case AnchorPreset.StretchRight:
                rt.anchorMin = new Vector2(0.4f, 0f);
                rt.anchorMax = Vector2.one;
                rt.pivot     = new Vector2(0.5f, 0.5f);
                break;
        }
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = size;
        return go;
    }

    static void AddLabel(Transform parent, string text, int fontSize, Color color)
    {
        var go  = new GameObject("Label");
        go.transform.SetParent(parent, false);

        var txt             = go.AddComponent<Text>();
        txt.text            = text;
        txt.fontSize        = fontSize;
        txt.fontStyle       = FontStyle.Bold;
        txt.alignment       = TextAnchor.MiddleCenter;
        txt.color           = color;
        txt.raycastTarget   = false;

        var rt         = go.GetComponent<RectTransform>();
        rt.anchorMin   = Vector2.zero;
        rt.anchorMax   = Vector2.one;
        rt.offsetMin   = rt.offsetMax = Vector2.zero;
    }

    static GameObject MakeLabel(string name, Transform parent, string text,
                                  Vector2 anchoredPos, int fontSize, Color color, AnchorPreset anchor)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);

        var txt           = go.AddComponent<Text>();
        txt.text          = text;
        txt.fontSize      = fontSize;
        txt.alignment     = TextAnchor.MiddleCenter;
        txt.color         = color;
        txt.raycastTarget = false;

        var rt = go.GetComponent<RectTransform>();
        if (anchor == AnchorPreset.BottomLeft)
            { rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero; }
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = new Vector2(220f, 30f);
        return go;
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  Asset / primitive helpers
    // ══════════════════════════════════════════════════════════════════════════

    static Transform Pivot(string n, Transform p, Vector3 pos)
    {
        var go = new GameObject(n).transform;
        go.SetParent(p); go.localPosition = pos; return go;
    }

    static Transform Limb(string n, Transform p, Vector3 pos, Vector3 scale,
                           Material mat, PrimitiveType type = PrimitiveType.Cube)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = n; go.transform.SetParent(p);
        go.transform.localPosition = pos; go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        var col = go.GetComponent<Collider>(); if (col) Object.DestroyImmediate(col);
        return go.transform;
    }

    static float SampleHeight(float x, float z) =>
        Terrain.activeTerrain != null ? Terrain.activeTerrain.SampleHeight(new Vector3(x, 0, z)) : 0f;

    static Material MakeMat(Color color, string assetName)
    {
        string path    = $"Assets/Generated/Materials/{assetName}.mat";
        var    existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing) return existing;
        var shader = Shader.Find("Universal Render Pipeline/Lit")
                  ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                  ?? Shader.Find("Standard")
                  ?? Shader.Find("Diffuse");
        if (shader == null) { Debug.LogError($"No shader found for material '{assetName}'"); return null; }
        var mat = new Material(shader) { color = color };
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static Texture2D MakeSolidTexture(Color color, string name)
    {
        var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        var px  = new Color[64 * 64]; for (int i = 0; i < px.Length; i++) px[i] = color;
        tex.SetPixels(px); tex.Apply(); tex.name = name; return tex;
    }

    /// Saves a PNG circle texture and reimports it as a Sprite.
    static Sprite MakeCircleSprite(string name, int size,
                                    Color fill, Color border, float borderFrac)
    {
        string dir  = "Assets/Generated/Textures";
        string path = $"{dir}/{name}.png";

        var   tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var   px  = new Color[size * size];
        float c   = size * 0.5f;
        float outerR = c, innerR = c - size * borderFrac;

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                px[y * size + x] = d <= innerR ? fill : d <= outerR ? border : Color.clear;
            }
        tex.SetPixels(px); tex.Apply();

        File.WriteAllBytes(Path.GetFullPath(path), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.Refresh();

        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType          = TextureImporterType.Sprite;
            importer.alphaIsTransparency  = true;
            importer.filterMode           = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static void SaveAsset(Object asset, string path)
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(path)) AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(asset, path);
    }

    static void EnsureFolders()
    {
        string[] paths = {
            "Assets/Generated",
            "Assets/Generated/Materials",
            "Assets/Generated/Terrain",
            "Assets/Generated/Textures"
        };
        foreach (var p in paths)
            if (!AssetDatabase.IsValidFolder(p))
                AssetDatabase.CreateFolder(
                    Path.GetDirectoryName(p).Replace('\\','/'),
                    Path.GetFileName(p));
    }

    static void ClearScene()
    {
        foreach (var go in UnityEngine.SceneManagement.SceneManager
            .GetActiveScene().GetRootGameObjects())
            Object.DestroyImmediate(go);
    }
}
