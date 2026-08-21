using UnityEngine;

public static class Art
{
    public static readonly Color Bg = Hex("07050a");
    public static readonly Color Floor = Hex("14101c");
    public static readonly Color FloorAlt = Hex("181422");
    public static readonly Color Wall = Hex("0c0912");
    public static readonly Color WallTrim = Hex("ff2bd6");
    public static readonly Color Neon = Hex("3dffe8");
    public static readonly Color Hot = Hex("ff2bd6");
    public static readonly Color Blood = Hex("c4123a");
    public static readonly Color BloodDark = Hex("6a0a22");
    public static readonly Color PlayerBody = Hex("f2e6d4");
    public static readonly Color PlayerMask = Hex("3dffe8");
    public static readonly Color Enemy = Hex("e01b5c");
    public static readonly Color EnemyKnife = Hex("f0a020");
    public static readonly Color EnemyVisor = Hex("14080c");
    public static readonly Color Furniture = Hex("2a2030");
    public static readonly Color Bullet = Hex("fff1a8");
    public static readonly Color Crosshair = Hex("ffffff");

    static Sprite _square;
    static Sprite _circle;

    public static Sprite Square
    {
        get
        {
            if (_square == null) _square = MakeSquare();
            return _square;
        }
    }

    public static Sprite Circle
    {
        get
        {
            if (_circle == null) _circle = MakeCircle(64);
            return _circle;
        }
    }

    public static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex.TrimStart('#'), out var c);
        return c;
    }

    public static SpriteRenderer MakeSprite(string name, Transform parent, Sprite sprite, Color color, Vector3 scale, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localScale = scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }

    public static GameObject Body(string name, Vector2 pos, Transform parent = null)
    {
        var go = new GameObject(name);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        if (parent != null) go.transform.SetParent(parent, true);
        return go;
    }

    static Sprite MakeSquare()
    {
        var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color[64];
        for (var i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        return UnityEngine.Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 8f);
    }

    static Sprite MakeCircle(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        var r = size * 0.5f - 0.6f;
        var cx = size * 0.5f;
        var cy = size * 0.5f;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                var a = Mathf.Clamp01(r - d + 1f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        tex.Apply();
        return UnityEngine.Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}

public static class Sfx
{
    static AudioClip _pistol;
    static AudioClip _uzi;
    static AudioClip _shotgun;
    static AudioClip _melee;
    static AudioClip _hit;
    static AudioClip _pickup;
    static AudioClip _throw;
    static AudioClip _dry;
    static AudioClip _alert;
    static AudioClip _win;
    static AudioClip _dead;

    public static AudioClip Pistol => _pistol != null ? _pistol : (_pistol = Noise(0.12f, 18f, 900f));
    public static AudioClip Uzi => _uzi != null ? _uzi : (_uzi = Noise(0.07f, 22f, 1400f));
    public static AudioClip Shotgun => _shotgun != null ? _shotgun : (_shotgun = Noise(0.22f, 10f, 280f));
    public static AudioClip Melee => _melee != null ? _melee : (_melee = Impact(0.14f, 180f));
    public static AudioClip Hit => _hit != null ? _hit : (_hit = Impact(0.18f, 90f));
    public static AudioClip Pickup => _pickup != null ? _pickup : (_pickup = Tone(0.08f, 880f, 1320f));
    public static AudioClip Throw => _throw != null ? _throw : (_throw = Noise(0.1f, 14f, 400f));
    public static AudioClip Dry => _dry != null ? _dry : (_dry = Tone(0.05f, 220f, 140f));
    public static AudioClip Alert => _alert != null ? _alert : (_alert = Tone(0.16f, 740f, 980f));
    public static AudioClip Win => _win != null ? _win : (_win = Arp());
    public static AudioClip Dead => _dead != null ? _dead : (_dead = Noise(0.4f, 6f, 120f));

    public static void Play(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null || GameRoot.Audio == null) return;
        GameRoot.Audio.pitch = pitch;
        GameRoot.Audio.PlayOneShot(clip, volume);
    }

    static AudioClip Noise(float duration, float decay, float toneHz)
    {
        const int rate = 22050;
        var n = Mathf.CeilToInt(duration * rate);
        var data = new float[n];
        var phase = 0.0;
        var step = toneHz / rate;
        for (var i = 0; i < n; i++)
        {
            var t = i / (float)n;
            var env = Mathf.Exp(-t * decay);
            phase += step;
            var tone = Mathf.Sin((float)(phase * 2.0 * Mathf.PI)) * 0.35f;
            var noise = (Random.value * 2f - 1f) * 0.65f;
            data[i] = (tone + noise) * env;
        }

        return Clip("sfx", data, rate);
    }

    static AudioClip Impact(float duration, float hz)
    {
        const int rate = 22050;
        var n = Mathf.CeilToInt(duration * rate);
        var data = new float[n];
        for (var i = 0; i < n; i++)
        {
            var t = i / (float)rate;
            var env = Mathf.Exp(-t * 28f);
            data[i] = Mathf.Sin(2f * Mathf.PI * hz * t) * env;
            hz *= 0.9992f;
        }

        return Clip("hit", data, rate);
    }

    static AudioClip Tone(float duration, float a, float b)
    {
        const int rate = 22050;
        var n = Mathf.CeilToInt(duration * rate);
        var data = new float[n];
        for (var i = 0; i < n; i++)
        {
            var t = i / (float)rate;
            var u = i / (float)n;
            var hz = Mathf.Lerp(a, b, u);
            var env = Mathf.Sin(u * Mathf.PI);
            data[i] = Mathf.Sin(2f * Mathf.PI * hz * t) * env * 0.45f;
        }

        return Clip("tone", data, rate);
    }

    static AudioClip Arp()
    {
        const int rate = 22050;
        float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
        var n = Mathf.CeilToInt(0.55f * rate);
        var data = new float[n];
        for (var i = 0; i < n; i++)
        {
            var t = i / (float)rate;
            var idx = Mathf.Clamp(Mathf.FloorToInt(t / 0.12f), 0, notes.Length - 1);
            var local = t - idx * 0.12f;
            var env = Mathf.Exp(-local * 8f);
            data[i] = Mathf.Sin(2f * Mathf.PI * notes[idx] * t) * env * 0.4f;
        }

        return Clip("win", data, rate);
    }

    static AudioClip Clip(string name, float[] data, int rate)
    {
        var clip = AudioClip.Create(name, data.Length, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
