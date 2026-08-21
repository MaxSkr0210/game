using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public GameObject alertMark;

    const float ViewDistance = 8.2f;
    const float ViewAngle = 68f;
    const float PatrolSpeed = 2.6f;
    const float CombatSpeed = 4.1f;
    const float TurnSpeed = 420f;

    Rigidbody2D _rb;
    WeaponUser _weapons;
    Actor _actor;
    Vector2 _spawn;
    Vector2 _target;
    Vector2 _lastKnown;
    float _nextWander;
    float _react;
    float _alertUntil;
    bool _combat;
    bool _investigating;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _weapons = GetComponent<WeaponUser>();
        _actor = GetComponent<Actor>();
        _spawn = _rb.position;
        _target = _spawn;
        Face(Vector2.down);
    }

    void Update()
    {
        if (_actor.dead || GameRoot.State != RunState.Playing)
        {
            Body.SetVelocity(_rb, Vector2.zero);
            return;
        }

        if (alertMark != null)
            alertMark.SetActive(_combat || Time.time < _alertUntil);

        var player = GameRoot.Player;
        if (player == null) return;

        var toPlayer = (Vector2)player.position - _rb.position;
        var seen = CanSee(player.position);

        if (seen)
        {
            if (!_combat)
            {
                _combat = true;
                _react = 0.22f;
                _alertUntil = Time.time + 1.5f;
                Sfx.Play(Sfx.Alert, 0.35f, Random.Range(0.95f, 1.2f));
            }

            _lastKnown = player.position;
            _investigating = false;
        }

        if (_combat)
        {
            Combat(toPlayer, seen);
            return;
        }

        if (_investigating)
        {
            MoveTowards(_lastKnown, PatrolSpeed * 1.25f);
            Face(_lastKnown - _rb.position);
            if (Vector2.Distance(_rb.position, _lastKnown) < 0.45f)
                _investigating = false;
            return;
        }

        Patrol();
    }

    void Combat(Vector2 toPlayer, bool seen)
    {
        var stats = _weapons.Stats;
        var dist = toPlayer.magnitude;
        var dir = dist > 0.001f ? toPlayer / dist : (Vector2)transform.up;

        Face(dir);
        if (_react > 0f)
        {
            _react -= Time.deltaTime;
            Body.SetVelocity(_rb, Vector2.zero);
            return;
        }

        if (stats.melee || _weapons.id == WeaponId.None || _weapons.id == WeaponId.Knife)
        {
            MoveTowards(_lastKnown, CombatSpeed);
            if (seen && dist < stats.meleeRange + 0.15f)
                _weapons.TryShoot(_rb.position, dir);
            return;
        }

        if (dist > 6.2f) MoveTowards(_lastKnown, CombatSpeed);
        else if (dist < 2.4f) Body.SetVelocity(_rb, -dir * CombatSpeed * 0.7f);
        else Body.SetVelocity(_rb, Vector2.zero);

        if (seen && Vector2.Angle(transform.up, dir) < 12f)
            _weapons.TryShoot(_rb.position, dir);
    }

    void Patrol()
    {
        if (Time.time >= _nextWander || Vector2.Distance(_rb.position, _target) < 0.3f)
        {
            _target = PickPatrolPoint();
            _nextWander = Time.time + Random.Range(1.2f, 2.8f);
        }

        MoveTowards(_target, PatrolSpeed);
        var move = _target - _rb.position;
        if (move.sqrMagnitude > 0.05f) Face(move);
    }

    Vector2 PickPatrolPoint()
    {
        for (var i = 0; i < 10; i++)
        {
            var p = _spawn + Random.insideUnitCircle * 4.2f;
            if (!Blocked(p, 0.38f)) return p;
        }

        return _spawn;
    }

    void MoveTowards(Vector2 point, float speed)
    {
        var d = point - _rb.position;
        if (d.sqrMagnitude < 0.0001f)
        {
            Body.SetVelocity(_rb, Vector2.zero);
            return;
        }

        Body.SetVelocity(_rb, d.normalized * speed);
    }

    void Face(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.0001f) return;
        var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        var next = Mathf.MoveTowardsAngle(_rb.rotation, angle, TurnSpeed * Time.deltaTime);
        _rb.rotation = next;
    }

    bool CanSee(Vector2 target)
    {
        var to = target - _rb.position;
        var dist = to.magnitude;
        if (dist > ViewDistance || dist < 0.05f) return false;
        if (Vector2.Angle(transform.up, to) > ViewAngle * 0.5f) return false;

        var dir = to / dist;
        var hits = Physics2D.RaycastAll(_rb.position + dir * 0.4f, dir, dist - 0.4f);
        for (var i = 0; i < hits.Length; i++)
        {
            var col = hits[i].collider;
            if (col == null) continue;
            if (col.GetComponent<LosBlock>() != null) return false;
            var actor = col.GetComponent<Actor>();
            if (actor != null && actor.isPlayer) return true;
        }

        return false;
    }

    static bool Blocked(Vector2 p, float r)
    {
        var hits = Physics2D.OverlapCircleAll(p, r);
        for (var i = 0; i < hits.Length; i++)
            if (hits[i].GetComponent<LosBlock>() != null)
                return true;
        return false;
    }

    public void Investigate(Vector2 point)
    {
        if (_combat || _actor.dead) return;
        _lastKnown = point;
        _investigating = true;
        _alertUntil = Time.time + 1.2f;
    }

    public static void Hear(Vector2 point, float radius)
    {
        var enemies = GameRoot.FindAll<EnemyAI>();
        for (var i = 0; i < enemies.Length; i++)
        {
            var e = enemies[i];
            if (e == null || e._actor.dead) continue;
            if (Vector2.Distance(e._rb.position, point) <= radius)
                e.Investigate(point);
        }
    }
}
