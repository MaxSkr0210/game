using UnityEngine;

public class GameView : MonoBehaviour
{
    Transform _player;
    Transform _crosshair;
    Vector3 _shake;

    public void Bind(Transform player)
    {
        _player = player;
        _crosshair = null;
        var sr = Art.MakeSprite("Crosshair", null, Art.Square, Art.Crosshair, new Vector3(0.16f, 0.16f, 1f), 50);
        _crosshair = sr.transform;
    }

    void LateUpdate()
    {
        if (_player == null) return;

        var mouse = ScreenToWorld(Input.mousePosition);
        var look = Vector3.Lerp(_player.position, mouse, 0.12f);
        look.z = -10f;

        var damp = 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime);
        transform.position = Vector3.Lerp(transform.position, look, damp);

        if (GameRoot.Shake > 0.001f)
            _shake = (Vector3)(Random.insideUnitCircle * GameRoot.Shake);
        else
            _shake = Vector3.MoveTowards(_shake, Vector3.zero, Time.unscaledDeltaTime * 8f);

        transform.position += _shake;
        DrawCrosshair(mouse);
    }

    void OnGUI()
    {
        var title = TitleStyle(46, Art.Hot);
        var big = TitleStyle(64, Color.white);
        var body = TitleStyle(22, Art.Neon);
        var dim = TitleStyle(16, new Color(1f, 1f, 1f, 0.55f));

        Shadow(new Rect(28, 22, 640, 52), "NEON MASK", title);

        var weapons = GameRoot.Player != null ? GameRoot.Player.GetComponent<WeaponUser>() : null;
        if (weapons != null)
        {
            var stats = weapons.Stats;
            var ammo = stats.melee || weapons.id == WeaponId.None ? "" : "  " + weapons.ammo + "/" + stats.ammo;
            Shadow(new Rect(28, 70, 640, 36), stats.name + ammo, body);
        }

        var right = TitleStyle(22, Art.Hot);
        right.alignment = TextAnchor.UpperRight;
        Shadow(new Rect(Screen.width - 420, 22, 392, 36), "HOSTILES  " + GameRoot.EnemiesLeft, right);

        if (GameRoot.Combo >= 2 && GameRoot.State == RunState.Playing)
        {
            var combo = TitleStyle(40, Art.Neon);
            combo.alignment = TextAnchor.UpperCenter;
            Shadow(new Rect(0, 88, Screen.width, 48), "x" + GameRoot.Combo, combo);
        }

        dim.alignment = TextAnchor.LowerLeft;
        Shadow(new Rect(28, Screen.height - 78, 900, 54),
            "WASD MOVE    MOUSE AIM    LMB FIRE    RMB / F MELEE    Q THROW    E SWAP    R RESTART", dim);

        if (GameRoot.Flash > 0.01f)
        {
            var flash = GameRoot.State == RunState.Dead
                ? new Color(0.7f, 0.05f, 0.12f, GameRoot.Flash * 0.55f)
                : new Color(1f, 1f, 1f, GameRoot.Flash * 0.22f);
            GUI.color = flash;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        if (GameRoot.State == RunState.Dead)
        {
            big.alignment = TextAnchor.MiddleCenter;
            body.alignment = TextAnchor.MiddleCenter;
            Shadow(new Rect(0, Screen.height * 0.38f, Screen.width, 80), "YOU'RE DEAD", big);
            Shadow(new Rect(0, Screen.height * 0.38f + 78, Screen.width, 40), "R  /  SPACE  TO  RESTART", body);
        }
        else if (GameRoot.State == RunState.Cleared)
        {
            big.normal.textColor = Art.Neon;
            big.alignment = TextAnchor.MiddleCenter;
            body.alignment = TextAnchor.MiddleCenter;
            Shadow(new Rect(0, Screen.height * 0.36f, Screen.width, 80), "LEVEL CLEAR", big);
            Shadow(new Rect(0, Screen.height * 0.36f + 78, Screen.width, 40), "ALL HOSTILES DOWN    R TO RUN IT BACK", body);
        }

        GUI.color = Color.white;
    }

    void DrawCrosshair(Vector3 mouse)
    {
        if (_crosshair == null) return;
        mouse.z = 0f;
        _crosshair.position = mouse;
    }

    static Vector3 ScreenToWorld(Vector3 screen)
    {
        var cam = Camera.main;
        if (cam == null) return screen;
        screen.z = -cam.transform.position.z;
        return cam.ScreenToWorldPoint(screen);
    }

    static GUIStyle TitleStyle(int size, Color color)
    {
        var s = new GUIStyle(GUI.skin.label)
        {
            fontSize = size,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperLeft
        };
        s.normal.textColor = color;
        return s;
    }

    static void Shadow(Rect r, string text, GUIStyle style)
    {
        var c = style.normal.textColor;
        style.normal.textColor = new Color(0f, 0f, 0f, 0.85f);
        GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), text, style);
        style.normal.textColor = c;
        GUI.Label(r, text, style);
    }
}
