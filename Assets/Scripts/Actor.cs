using UnityEngine;

public class Actor : MonoBehaviour
{
    public bool isPlayer;
    public bool dead;

    public void Kill(Vector2 dir)
    {
        if (dead) return;
        dead = true;

        var weapons = GetComponent<WeaponUser>();
        if (weapons != null && weapons.HasWeapon)
            weapons.Drop(dir.sqrMagnitude > 0.01f ? dir : (Vector2)transform.up, false);

        Gore.Burst(transform.position, dir, isPlayer ? 18 : 12);
        Sfx.Play(Sfx.Hit, 0.9f, Random.Range(0.85f, 1.15f));

        var corpse = Art.Body(isPlayer ? "PlayerCorpse" : "Corpse", transform.position, transform.parent);
        var sr = corpse.AddComponent<SpriteRenderer>();
        sr.sprite = Art.Circle;
        sr.color = isPlayer ? new Color(0.55f, 0.5f, 0.45f) : Art.BloodDark;
        sr.sortingOrder = 4;
        corpse.transform.localScale = Vector3.one * Random.Range(0.7f, 0.95f);
        corpse.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        if (isPlayer) GameRoot.OnPlayerKilled();
        else GameRoot.OnEnemyKilled(transform.position);

        Destroy(gameObject);
    }
}

public static class Gore
{
    public static void Burst(Vector2 origin, Vector2 dir, int count)
    {
        if (dir.sqrMagnitude < 0.01f) dir = Random.insideUnitCircle;
        dir.Normalize();
        var parent = GameRoot.Instance != null ? GameRoot.Instance.transform : null;

        for (var i = 0; i < count; i++)
        {
            var spread = Quaternion.Euler(0f, 0f, Random.Range(-55f, 55f)) * (Vector3)dir;
            var p = origin + (Vector2)spread * Random.Range(0.1f, 1.4f);
            var go = Art.Body("Blood", p, parent);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Random.value > 0.35f ? Art.Circle : Art.Square;
            sr.color = Color.Lerp(Art.Blood, Art.BloodDark, Random.value);
            sr.sortingOrder = 3;
            var s = Random.Range(0.08f, 0.28f);
            go.transform.localScale = new Vector3(s * Random.Range(1f, 2.2f), s, 1f);
            go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        }
    }
}
