using UnityEngine;

public class LosBlock : MonoBehaviour
{
}

public readonly struct BuiltLevel
{
    public readonly PlayerController player;
    public readonly int enemyCount;

    public BuiltLevel(PlayerController player, int enemyCount)
    {
        this.player = player;
        this.enemyCount = enemyCount;
    }
}

public static class LevelBuilder
{
    const float Tile = 1f;

    static readonly string[] Map =
    {
        "################################################################",
        "#....................##........................##..............#",
        "#..P.................##............K...........##..............#",
        "#....................##........................##.......E......#",
        "#..............#######.............F...........##..............#",
        "#..............#...............................##..............#",
        "#..............#....E........##########........##....F.........#",
        "#..............#.............#........#........##..............#",
        "#.....K........#.............#...U....#........................#",
        "#.........................F..#........#........................#",
        "#............................####..####........########........#",
        "#.....F........................................#......#........#",
        "#.....................................K........#..S...#........#",
        "#........E.....................................#......#........#",
        "#...................############...............###..###........#",
        "#...................#..........#...............................#",
        "#...................#....E.....#...........U...................#",
        "#...................#..........#...............................#",
        "#...................############...............................#",
        "#......................................................E.......#",
        "#..U...........................................................#",
        "#..............................................................#",
        "################################################################"
    };

    public static BuiltLevel Build(Transform root)
    {
        var h = Map.Length;
        var w = Map[0].Length;
        PlayerController player = null;
        var enemies = 0;

        var floor = Art.Body("Floor", Vector2.zero, root);
        var floorSr = floor.AddComponent<SpriteRenderer>();
        floorSr.sprite = Art.Square;
        floorSr.color = Art.Floor;
        floorSr.sortingOrder = 0;
        floor.transform.position = new Vector3((w - 1) * 0.5f * Tile, -(h - 1) * 0.5f * Tile, 0f);
        floor.transform.localScale = new Vector3(w * Tile, h * Tile, 1f);

        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                if ((x + y) % 2 == 0 && Map[y][x] != '#')
                {
                    var speckle = Art.Sprite("Tile", root, Art.Square, Art.FloorAlt, new Vector3(Tile, Tile, 1f), 1);
                    speckle.transform.position = World(x, y, h);
                }
            }
        }

        for (var y = 0; y < h; y++)
        {
            var x = 0;
            while (x < w)
            {
                if (Map[y][x] != '#')
                {
                    x++;
                    continue;
                }

                var start = x;
                while (x < w && Map[y][x] == '#') x++;
                var len = x - start;
                SpawnWall(root, start, y, len, 1, h);
            }
        }

        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                var c = Map[y][x];
                var p = World(x, y, h);
                switch (c)
                {
                    case 'P':
                        player = SpawnPlayer(root, p);
                        break;
                    case 'E':
                        SpawnEnemy(root, p, WeaponId.Pistol);
                        enemies++;
                        break;
                    case 'U':
                        SpawnEnemy(root, p, WeaponId.Uzi);
                        enemies++;
                        break;
                    case 'S':
                        SpawnEnemy(root, p, WeaponId.Shotgun);
                        enemies++;
                        break;
                    case 'K':
                        SpawnEnemy(root, p, WeaponId.Knife);
                        enemies++;
                        break;
                    case 'F':
                        SpawnFurniture(root, p);
                        break;
                }
            }
        }

        if (player == null)
            player = SpawnPlayer(root, World(2, 2, h));

        return new BuiltLevel(player, enemies);
    }

    static Vector2 World(int x, int y, int h)
    {
        return new Vector2(x * Tile, (h - 1 - y) * Tile);
    }

    static void SpawnWall(Transform root, int x, int y, int w, int hTiles, int mapH)
    {
        var pos = World(x, y, mapH);
        pos += new Vector2((w - 1) * 0.5f * Tile, 0f);
        var go = Art.Body("Wall", pos, root);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Art.Square;
        sr.color = Art.Wall;
        sr.sortingOrder = 8;
        go.transform.localScale = new Vector3(w * Tile, hTiles * Tile, 1f);

        var box = go.AddComponent<BoxCollider2D>();
        box.size = Vector2.one;
        go.AddComponent<LosBlock>();

        var trim = Art.Sprite("Trim", go.transform, Art.Square, (x + y) % 3 == 0 ? Art.Neon : Art.Hot,
            new Vector3(1f, 0.08f, 1f), 9);
        trim.transform.localPosition = new Vector3(0f, 0.46f, 0f);
        var c = trim.color;
        c.a = 0.7f;
        trim.color = c;
    }

    static void SpawnFurniture(Transform root, Vector2 pos)
    {
        var go = Art.Body("Desk", pos, root);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Art.Square;
        sr.color = Art.Furniture;
        sr.sortingOrder = 6;
        go.transform.localScale = new Vector3(1.35f, 0.7f, 1f);
        var box = go.AddComponent<BoxCollider2D>();
        box.size = Vector2.one;
        go.AddComponent<LosBlock>();
    }

    static PlayerController SpawnPlayer(Transform root, Vector2 pos)
    {
        var go = Art.Body("Player", pos, root);
        var rb = go.AddComponent<Rigidbody2D>();
        Body.Setup(rb, 10f, 1f);

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.34f;

        var actor = go.AddComponent<Actor>();
        actor.isPlayer = true;

        var weapons = go.AddComponent<WeaponUser>();
        weapons.isPlayer = true;

        Art.Sprite("Body", go.transform, Art.Circle, Art.PlayerBody, Vector3.one * 0.86f, 20);
        Art.Sprite("Mask", go.transform, Art.Circle, Art.PlayerMask, Vector3.one * 0.52f, 21)
            .transform.localPosition = new Vector3(0f, 0.06f, 0f);
        Art.Sprite("Visor", go.transform, Art.Square, Art.EnemyVisor, new Vector3(0.42f, 0.1f, 1f), 22)
            .transform.localPosition = new Vector3(0f, 0.1f, 0f);

        var gun = Art.Sprite("Gun", go.transform, Art.Square, Art.Hot, new Vector3(0.16f, 0.46f, 1f), 23);
        gun.transform.localPosition = new Vector3(0.28f, 0.34f, 0f);
        weapons.gunView = gun;
        weapons.muzzle = gun.transform;
        weapons.RefreshView();

        return go.AddComponent<PlayerController>();
    }

    public static EnemyAI SpawnEnemy(Transform root, Vector2 pos, WeaponId weapon)
    {
        var go = Art.Body("Enemy", pos, root);
        var rb = go.AddComponent<Rigidbody2D>();
        Body.Setup(rb, 8f, 1.4f);

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.34f;

        var actor = go.AddComponent<Actor>();
        actor.isPlayer = false;

        var weapons = go.AddComponent<WeaponUser>();
        weapons.isPlayer = false;
        weapons.Equip(weapon, Catalog.Of(weapon).ammo);

        var bodyColor = weapon == WeaponId.Knife ? Art.EnemyKnife : Art.Enemy;
        Art.Sprite("Body", go.transform, Art.Circle, bodyColor, Vector3.one * 0.86f, 18);
        Art.Sprite("Visor", go.transform, Art.Square, Art.EnemyVisor, new Vector3(0.4f, 0.12f, 1f), 19)
            .transform.localPosition = new Vector3(0f, 0.12f, 0f);

        var gun = Art.Sprite("Gun", go.transform, Art.Square, Color.Lerp(bodyColor, Color.black, 0.35f),
            new Vector3(0.14f, 0.42f, 1f), 19);
        gun.transform.localPosition = new Vector3(0.26f, 0.32f, 0f);
        weapons.gunView = gun;
        weapons.muzzle = gun.transform;
        weapons.RefreshView();

        var mark = new GameObject("Alert");
        mark.transform.SetParent(go.transform, false);
        mark.transform.localPosition = new Vector3(0f, 0.7f, 0f);
        var tm = mark.AddComponent<TextMesh>();
        tm.text = "!";
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.characterSize = 0.12f;
        tm.fontSize = 64;
        tm.color = Art.Hot;
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font != null) tm.font = font;
        tm.GetComponent<MeshRenderer>().sortingOrder = 40;
        mark.SetActive(false);

        var ai = go.AddComponent<EnemyAI>();
        ai.alertMark = mark;
        return ai;
    }
}
