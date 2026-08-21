using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum RunState
{
    Playing,
    Dead,
    Cleared
}

public class GameRoot : MonoBehaviour
{
    public static GameRoot Instance { get; private set; }
    public static AudioSource Audio { get; private set; }
    public static RunState State { get; private set; }
    public static Transform Player { get; private set; }
    public static int EnemiesLeft { get; private set; }
    public static int Combo { get; private set; }
    public static float ComboLeft { get; private set; }
    public static float Shake { get; set; }
    public static float Flash { get; set; }
    public static float HitPause { get; private set; }

    Transform _world;
    int _comboPeak;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoBoot()
    {
        if (Find<GameRoot>() != null) return;
        var go = new GameObject("GameRoot");
        go.AddComponent<GameRoot>();
    }

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Physics2D.gravity = Vector2.zero;
        Physics2D.queriesHitTriggers = true;
        Physics2D.queriesStartInColliders = false;
        Application.targetFrameRate = 60;
        Time.timeScale = 1f;
        State = RunState.Playing;
        Combo = 0;
        ComboLeft = 0f;
        Shake = 0f;
        Flash = 0f;
        HitPause = 0f;
        _comboPeak = 0;

        Audio = gameObject.AddComponent<AudioSource>();
        Audio.playOnAwake = false;
        Audio.spatialBlend = 0f;

        _world = new GameObject("World").transform;
        _world.SetParent(transform, false);

        var built = LevelBuilder.Build(_world);
        Player = built.player.transform;
        EnemiesLeft = built.enemyCount;

        var cam = Camera.main;
        if (cam == null)
        {
            var camGo = new GameObject("Main Camera");
            cam = camGo.AddComponent<Camera>();
            camGo.tag = "MainCamera";
            camGo.AddComponent<AudioListener>();
        }

        cam.orthographic = true;
        cam.orthographicSize = 8.4f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Art.Bg;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 50f;
        cam.transform.position = new Vector3(Player.position.x, Player.position.y, -10f);

        var view = cam.gameObject.GetComponent<GameView>();
        if (view == null) view = cam.gameObject.AddComponent<GameView>();
        view.Bind(Player);

        Cursor.visible = false;
    }

    void Update()
    {
        if (ComboLeft > 0f)
        {
            ComboLeft -= Time.unscaledDeltaTime;
            if (ComboLeft <= 0f) Combo = 0;
        }

        Shake = Mathf.MoveTowards(Shake, 0f, Time.unscaledDeltaTime * 2.4f);
        Flash = Mathf.MoveTowards(Flash, 0f, Time.unscaledDeltaTime * 3.2f);

        if (HitPause > 0f)
        {
            HitPause -= Time.unscaledDeltaTime;
            Time.timeScale = HitPause > 0f ? 0.07f : 1f;
        }

        if (Input.GetKeyDown(KeyCode.R)) Restart();
        if (State != RunState.Playing && Input.GetKeyDown(KeyCode.Space)) Restart();
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.visible = true;
            Application.Quit();
        }
    }

    public static void OnEnemyKilled(Vector2 point)
    {
        if (State != RunState.Playing) return;
        EnemiesLeft = Mathf.Max(0, EnemiesLeft - 1);
        Combo += 1;
        ComboLeft = 2.4f;
        if (Combo > Instance._comboPeak) Instance._comboPeak = Combo;
        Punch(Combo >= 4 ? 0.32f : 0.2f, Combo >= 4 ? 0.07f : 0.045f);
        Flash = Mathf.Max(Flash, 0.18f);
        EnemyAI.Hear(point, 11f);
        if (EnemiesLeft <= 0)
        {
            State = RunState.Cleared;
            Time.timeScale = 1f;
            Sfx.Play(Sfx.Win, 0.9f);
            Cursor.visible = true;
        }
    }

    public static void OnPlayerKilled()
    {
        if (State != RunState.Playing) return;
        State = RunState.Dead;
        Punch(0.55f, 0.12f);
        Flash = 0.65f;
        Sfx.Play(Sfx.Dead, 0.85f, 0.7f);
        Instance.StartCoroutine(Instance.ShowCursorSoon());
    }

    public static void Punch(float shake, float pause)
    {
        Shake = Mathf.Max(Shake, shake);
        HitPause = Mathf.Max(HitPause, pause);
    }

    public static void Restart()
    {
        Time.timeScale = 1f;
        Cursor.visible = true;
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);
    }

    IEnumerator ShowCursorSoon()
    {
        yield return new WaitForSecondsRealtime(0.4f);
        Cursor.visible = true;
    }

    public static T Find<T>() where T : Object
    {
#if UNITY_6000_0_OR_NEWER
        return Object.FindAnyObjectByType<T>();
#elif UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<T>();
#else
        return Object.FindObjectOfType<T>();
#endif
    }

    public static T[] FindAll<T>() where T : Object
    {
#if UNITY_6000_0_OR_NEWER
        return Object.FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#elif UNITY_2023_1_OR_NEWER
        return Object.FindObjectsByType<T>(FindObjectsSortMode.None);
#else
        return Object.FindObjectsOfType<T>();
#endif
    }
}
