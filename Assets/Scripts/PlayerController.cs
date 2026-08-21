using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public const float Speed = 7.4f;

    Rigidbody2D _rb;
    WeaponUser _weapons;
    Actor _actor;
    Vector2 _input;
    Vector2 _aim = Vector2.up;

    public Vector2 Aim => _aim;
    public WeaponUser Weapons => _weapons;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _weapons = GetComponent<WeaponUser>();
        _actor = GetComponent<Actor>();
    }

    void Update()
    {
        if (_actor.dead) return;

        _input = Vector2.zero;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) _input.x -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) _input.x += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) _input.y -= 1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) _input.y += 1f;
        if (_input.sqrMagnitude > 1f) _input.Normalize();

        var cam = Camera.main;
        if (cam != null)
        {
            var mouse = cam.ScreenToWorldPoint(Input.mousePosition);
            var delta = (Vector2)mouse - _rb.position;
            if (delta.sqrMagnitude > 0.0001f) _aim = delta.normalized;
        }

        _rb.rotation = Mathf.Atan2(_aim.y, _aim.x) * Mathf.Rad2Deg - 90f;

        if (GameRoot.State != RunState.Playing) return;

        var origin = _rb.position;
        if (Input.GetMouseButton(0))
            _weapons.TryShoot(origin, _aim);

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.F))
            _weapons.Bash(origin, _aim);

        if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.G))
            _weapons.Throw(origin, _aim);

        if (Input.GetKeyDown(KeyCode.E))
            TryPickup(false);
        else
            TryPickup(true);
    }

    void FixedUpdate()
    {
        if (_actor.dead || GameRoot.State == RunState.Dead)
        {
            Body.SetVelocity(_rb, Vector2.zero);
            return;
        }

        Body.SetVelocity(_rb, _input * Speed);
    }

    void TryPickup(bool autoOnlyIfEmpty)
    {
        var hits = Physics2D.OverlapCircleAll(_rb.position, 0.75f);
        GroundWeapon best = null;
        var bestDist = float.MaxValue;
        for (var i = 0; i < hits.Length; i++)
        {
            var ground = hits[i].GetComponent<GroundWeapon>();
            if (ground == null || ground.flying) continue;
            var d = ((Vector2)ground.transform.position - _rb.position).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = ground;
            }
        }

        if (best == null) return;
        if (autoOnlyIfEmpty && _weapons.HasWeapon) return;
        _weapons.TryPickup(best);
    }
}
