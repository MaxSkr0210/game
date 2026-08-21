using UnityEngine;

public enum WeaponId
{
    None,
    Knife,
    Pistol,
    Uzi,
    Shotgun
}

public struct WeaponStats
{
    public string name;
    public bool melee;
    public int ammo;
    public float fireDelay;
    public float bulletSpeed;
    public int pellets;
    public float spread;
    public float meleeRange;
    public Color color;
    public AudioClip ShootClip => melee ? Sfx.Melee : name == "UZI" ? Sfx.Uzi : name == "SHOTGUN" ? Sfx.Shotgun : Sfx.Pistol;
}

public static class Catalog
{
    public static WeaponStats Of(WeaponId id)
    {
        switch (id)
        {
            case WeaponId.Knife:
                return new WeaponStats
                {
                    name = "KNIFE", melee = true, ammo = 0, fireDelay = 0.28f, meleeRange = 1.15f,
                    color = Art.EnemyKnife
                };
            case WeaponId.Pistol:
                return new WeaponStats
                {
                    name = "PISTOL", ammo = 13, fireDelay = 0.16f, bulletSpeed = 26f, pellets = 1, spread = 2.5f,
                    meleeRange = 0.9f, color = Art.Hot
                };
            case WeaponId.Uzi:
                return new WeaponStats
                {
                    name = "UZI", ammo = 24, fireDelay = 0.065f, bulletSpeed = 24f, pellets = 1, spread = 9f,
                    meleeRange = 0.9f, color = Art.Neon
                };
            case WeaponId.Shotgun:
                return new WeaponStats
                {
                    name = "SHOTGUN", ammo = 6, fireDelay = 0.55f, bulletSpeed = 22f, pellets = 6, spread = 16f,
                    meleeRange = 1.0f, color = new Color(1f, 0.75f, 0.2f)
                };
            default:
                return new WeaponStats
                {
                    name = "FISTS", melee = true, ammo = 0, fireDelay = 0.3f, meleeRange = 0.8f,
                    color = Art.PlayerBody
                };
        }
    }
}

public class WeaponUser : MonoBehaviour
{
    public bool isPlayer;
    public WeaponId id = WeaponId.None;
    public int ammo;
    public SpriteRenderer gunView;
    public Transform muzzle;
    float _cd;

    public bool HasWeapon => id != WeaponId.None;
    public WeaponStats Stats => Catalog.Of(id);

    void Update()
    {
        if (_cd > 0f) _cd -= Time.deltaTime;
    }

    public void Equip(WeaponId next, int rounds)
    {
        id = next;
        ammo = rounds;
        RefreshView();
    }

    public void RefreshView()
    {
        if (gunView == null) return;
        gunView.enabled = id != WeaponId.None;
        if (id != WeaponId.None) gunView.color = Stats.color;
        gunView.transform.localScale = id == WeaponId.Knife
            ? new Vector3(0.1f, 0.55f, 1f)
            : new Vector3(0.16f, 0.46f, 1f);
    }

    public bool Ready => _cd <= 0f;

    public bool TryShoot(Vector2 origin, Vector2 dir)
    {
        if (!Ready) return false;
        var stats = Stats;
        if (stats.melee || id == WeaponId.None)
        {
            Melee(origin, dir, stats.meleeRange);
            _cd = stats.fireDelay;
            return true;
        }

        if (ammo <= 0)
        {
            Sfx.Play(Sfx.Dry, 0.5f, Random.Range(0.9f, 1.1f));
            _cd = 0.18f;
            return false;
        }

        ammo--;
        _cd = stats.fireDelay;
        dir.Normalize();
        var shots = Mathf.Max(1, stats.pellets);
        for (var i = 0; i < shots; i++)
        {
            var angle = Random.Range(-stats.spread, stats.spread);
            var d = Quaternion.Euler(0f, 0f, angle) * dir;
            Bullet.Spawn(origin + d * 0.45f, d, stats.bulletSpeed, isPlayer, stats.color);
        }

        Sfx.Play(stats.ShootClip, isPlayer ? 0.7f : 0.45f, Random.Range(0.92f, 1.08f));
        MuzzleFlash(origin + dir * 0.5f, stats.color);
        if (isPlayer) GameRoot.Punch(0.08f, 0.012f);
        EnemyAI.Hear(origin, 12f);
        return true;
    }

    public void Bash(Vector2 origin, Vector2 dir)
    {
        if (!Ready) return;
        Melee(origin, dir, Stats.meleeRange);
        _cd = 0.32f;
    }

    public void Throw(Vector2 origin, Vector2 dir)
    {
        if (!HasWeapon) return;
        var dropped = Drop(dir, true);
        if (dropped == null) return;
        dropped.Throw(origin, dir.normalized * 18f, isPlayer);
        Sfx.Play(Sfx.Throw, 0.7f, Random.Range(0.95f, 1.1f));
    }

    public GroundWeapon Drop(Vector2 dir, bool thrown)
    {
        if (!HasWeapon) return null;
        var spawn = (Vector2)transform.position + dir.normalized * 0.55f;
        var pickup = GroundWeapon.Spawn(spawn, id, ammo);
        Equip(WeaponId.None, 0);
        return pickup;
    }

    public bool TryPickup(GroundWeapon ground)
    {
        if (ground == null || ground.flying) return false;
        if (HasWeapon)
        {
            var keepId = id;
            var keepAmmo = ammo;
            Equip(ground.id, ground.ammo);
            ground.id = keepId;
            ground.ammo = keepAmmo;
            ground.Refresh();
        }
        else
        {
            Equip(ground.id, ground.ammo);
            Destroy(ground.gameObject);
        }

        Sfx.Play(Sfx.Pickup, 0.7f);
        return true;
    }

    void Melee(Vector2 origin, Vector2 dir, float range)
    {
        Sfx.Play(Sfx.Melee, 0.8f, Random.Range(0.9f, 1.15f));
        var hits = Physics2D.OverlapCircleAll(origin + dir.normalized * (range * 0.55f), range * 0.55f);
        for (var i = 0; i < hits.Length; i++)
        {
            var actor = hits[i].GetComponent<Actor>();
            if (actor == null || actor.dead || actor.isPlayer == isPlayer) continue;
            actor.Kill(dir);
            GameRoot.Punch(0.28f, 0.06f);
        }
    }

    static void MuzzleFlash(Vector2 pos, Color color)
    {
        var go = Art.Body("Flash", pos);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Art.Circle;
        sr.color = color;
        sr.sortingOrder = 30;
        go.transform.localScale = Vector3.one * 0.35f;
        Object.Destroy(go, 0.04f);
    }
}

public class Bullet : MonoBehaviour
{
    Vector2 _vel;
    bool _fromPlayer;
    float _life = 1.1f;

    public static void Spawn(Vector2 pos, Vector2 dir, float speed, bool fromPlayer, Color color)
    {
        var go = Art.Body("Bullet", pos);
        go.transform.right = dir;
        Art.Sprite("Sprite", go.transform, Art.Square,
            color.a <= 0f ? Art.Bullet : color, new Vector3(0.22f, 0.08f, 1f), 25);

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.12f;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        var b = go.AddComponent<Bullet>();
        b._vel = dir.normalized * speed;
        b._fromPlayer = fromPlayer;
    }

    void Update()
    {
        transform.position += (Vector3)(_vel * Time.deltaTime);
        _life -= Time.deltaTime;
        if (_life <= 0f) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<LosBlock>() != null)
        {
            Destroy(gameObject);
            return;
        }

        var actor = other.GetComponent<Actor>();
        if (actor == null || actor.dead || actor.isPlayer == _fromPlayer) return;
        actor.Kill(_vel);
        Destroy(gameObject);
    }
}

public class GroundWeapon : MonoBehaviour
{
    public WeaponId id;
    public int ammo;
    public bool flying;
    Vector2 _vel;
    bool _fromPlayer;

    public static GroundWeapon Spawn(Vector2 pos, WeaponId id, int ammo)
    {
        var stats = Catalog.Of(id);
        var go = Art.Body("Pickup-" + stats.name, pos);
        Art.Sprite("Sprite", go.transform, Art.Square, stats.color, new Vector3(0.55f, 0.2f, 1f), 7);
        go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.45f;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        var g = go.AddComponent<GroundWeapon>();
        g.id = id;
        g.ammo = ammo;
        return g;
    }

    public void Refresh()
    {
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.color = Catalog.Of(id).color;
        name = "Pickup-" + Catalog.Of(id).name;
    }

    public void Throw(Vector2 origin, Vector2 vel, bool fromPlayer)
    {
        transform.position = origin;
        flying = true;
        _vel = vel;
        _fromPlayer = fromPlayer;
    }

    void Update()
    {
        if (!flying) return;
        transform.position += (Vector3)(_vel * Time.deltaTime);
        transform.Rotate(0f, 0f, 720f * Time.deltaTime);
        _vel *= 0.985f;
        if (_vel.magnitude < 2.2f)
        {
            flying = false;
            _vel = Vector2.zero;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!flying) return;
        if (other.GetComponent<LosBlock>() != null)
        {
            flying = false;
            _vel = Vector2.zero;
            return;
        }

        var actor = other.GetComponent<Actor>();
        if (actor == null || actor.dead) return;
        if (actor.isPlayer && _fromPlayer) return;
        actor.Kill(_vel);
        flying = false;
        _vel = Vector2.zero;
    }
}
