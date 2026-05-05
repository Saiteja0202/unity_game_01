using UnityEngine;

/// PUBG-style HUD drawn with OnGUI — no Canvas dependency required.
public class GameHUD : MonoBehaviour
{
    [Header("Player Stats")]
    public float maxHealth  = 100f;
    public float maxBoost   = 100f;
    public float health     = 100f;
    public float boost      = 65f;

    [Header("Ammo")]
    public int currentAmmo = 30;
    public int reserveAmmo = 150;

    [Header("Zone")]
    public int playersAlive = 64;

    // Runtime
    private GUIStyle labelStyle;
    private GUIStyle bigLabel;
    private GUIStyle barBg;
    private bool stylesReady;

    private float zoneSeconds = 180f;
    private float demoBoostTimer;
    private PlayerController cachedPC;

    void Start()
    {
        cachedPC = FindFirstObjectByType<PlayerController>();
    }

    void Update()
    {
        demoBoostTimer += Time.deltaTime * 0.5f;
        boost = 65f + Mathf.Sin(demoBoostTimer) * 10f;
    }

    void InitStyles()
    {
        if (stylesReady) return;

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        bigLabel = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        barBg = new GUIStyle(GUI.skin.box)
        {
            normal = { background = MakeTex(1, 1, new Color(0f, 0f, 0f, 0.5f)) }
        };

        stylesReady = true;
    }

    void OnGUI()
    {
        InitStyles();

        float sw = Screen.width;
        float sh = Screen.height;

        DrawCrosshair(sw, sh);
        DrawHealthBoostBar(sw, sh);
        DrawAmmoPanel(sw, sh);
        DrawZoneTimer(sw, sh);
        DrawPlayerCount(sw, sh);
        DrawCompass(sw, sh);
        DrawStateDebug(sw, sh);
    }

    void DrawCrosshair(float sw, float sh)
    {
        float cx = sw * 0.5f, cy = sh * 0.5f;
        float len = 10f, gap = 5f, thick = 2f;
        Color c = new Color(1f, 1f, 1f, 0.85f);

        DrawRect(cx - len - gap, cy - thick * 0.5f, len, thick, c);
        DrawRect(cx + gap,        cy - thick * 0.5f, len, thick, c);
        DrawRect(cx - thick * 0.5f, cy - len - gap, thick, len, c);
        DrawRect(cx - thick * 0.5f, cy + gap,        thick, len, c);
        DrawRect(cx - thick, cy - thick, thick * 2f, thick * 2f, new Color(1, 1, 1, 0.5f));
    }

    void DrawHealthBoostBar(float sw, float sh)
    {
        float panelW = 300f, panelH = 70f;
        float px = 20f, py = sh - panelH - 20f;

        DrawRect(px - 8f, py - 8f, panelW + 16f, panelH + 16f, new Color(0f, 0f, 0f, 0.45f));

        GUI.Label(new Rect(px, py, 100f, 22f), "HEALTH", labelStyle);
        DrawBar(px, py + 22f, panelW, 18f, health / maxHealth, new Color(0.9f, 0.2f, 0.2f, 0.9f));

        GUI.Label(new Rect(px, py + 46f, 100f, 16f), "BOOST", new GUIStyle(labelStyle) { fontSize = 13 });
        DrawBar(px, py + 58f, panelW, 8f, boost / maxBoost, new Color(0.2f, 0.9f, 0.5f, 0.9f));
    }

    void DrawAmmoPanel(float sw, float sh)
    {
        float panelW = 160f, panelH = 55f;
        float px = sw - panelW - 20f, py = sh - panelH - 20f;

        DrawRect(px - 8f, py - 8f, panelW + 16f, panelH + 16f, new Color(0f, 0f, 0f, 0.45f));

        GUI.Label(new Rect(px, py, panelW, 32f), currentAmmo.ToString(), bigLabel);
        GUI.Label(new Rect(px + 75f, py + 10f, 80f, 22f),
            $"/ {reserveAmmo}", new GUIStyle(labelStyle) { normal = { textColor = new Color(0.7f, 0.7f, 0.7f) } });
        GUI.Label(new Rect(px, py + 34f, panelW, 18f),
            "ASSAULT RIFLE", new GUIStyle(labelStyle) { fontSize = 12, normal = { textColor = new Color(0.6f, 0.8f, 1f) } });
    }

    void DrawZoneTimer(float sw, float sh)
    {
        zoneSeconds = 180f;
        int mins = (int)(zoneSeconds / 60f);
        int secs = (int)(zoneSeconds % 60f);
        string timerText = $"ZONE  {mins}:{secs:00}";

        float w = 200f, h = 30f;
        float px = (sw - w) * 0.5f, py = 14f;

        DrawRect(px - 8f, py - 4f, w + 16f, h + 8f, new Color(0f, 0f, 0f, 0.45f));
        GUI.Label(new Rect(px, py, w, h), timerText,
            new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(1f, 0.75f, 0.2f) } });
    }

    void DrawPlayerCount(float sw, float sh)
    {
        float w = 100f, h = 28f;
        float px = sw - w - 20f, py = 14f;

        DrawRect(px - 8f, py - 4f, w + 16f, h + 8f, new Color(0f, 0f, 0f, 0.45f));
        GUI.Label(new Rect(px, py, w, h), $"# {playersAlive} alive",
            new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleCenter });
    }

    void DrawCompass(float sw, float sh)
    {
        float compassW = 320f, compassH = 22f;
        float px = (sw - compassW) * 0.5f, py = 50f;

        DrawRect(px - 4f, py - 2f, compassW + 8f, compassH + 4f, new Color(0f, 0f, 0f, 0.4f));

        string[] dirs = { "N", "NE", "E", "SE", "S", "SW", "W", "NW", "N", "NE", "E" };
        float[] angles = { 0, 45, 90, 135, 180, 225, 270, 315, 360, 405, 450 };

        float camYaw = 0f;
        var cam = Camera.main;
        if (cam != null) camYaw = cam.transform.eulerAngles.y;

        for (int i = 0; i < dirs.Length; i++)
        {
            float offset = (angles[i] - camYaw) / 180f * (compassW * 0.5f);
            float tx = (sw * 0.5f) + offset - 12f;
            if (tx < px || tx > px + compassW - 24f) continue;

            bool cardinal = (i % 2 == 0);
            GUI.Label(new Rect(tx, py, 24f, compassH), dirs[i],
                new GUIStyle(labelStyle)
                {
                    fontSize = cardinal ? 13 : 10,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = cardinal ? Color.white : new Color(0.7f, 0.7f, 0.7f) }
                });
        }

        DrawRect(sw * 0.5f - 1f, py - 4f, 2f, compassH + 8f, new Color(1f, 0.3f, 0.3f, 0.9f));
    }

    void DrawStateDebug(float sw, float sh)
    {
        var pc = cachedPC;
        if (pc == null) return;

        string state = $"State: {pc.CurrentState}  Speed: {pc.HorizontalSpeed:F1}";
        GUI.Label(new Rect(20f, 16f, 260f, 22f), state,
            new GUIStyle(labelStyle) { fontSize = 13, normal = { textColor = new Color(0.8f, 1f, 0.8f) } });
    }

    void DrawBar(float x, float y, float w, float h, float fill, Color color)
    {
        DrawRect(x, y, w, h, new Color(0f, 0f, 0f, 0.6f));
        if (fill > 0f)
            DrawRect(x + 1f, y + 1f, (w - 2f) * Mathf.Clamp01(fill), h - 2f, color);
    }

    static void DrawRect(float x, float y, float w, float h, Color color)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
        GUI.color = old;
    }

    static Texture2D MakeTex(int w, int h, Color col)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] px = new Color[w * h];
        for (int i = 0; i < px.Length; i++) px[i] = col;
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }
}
