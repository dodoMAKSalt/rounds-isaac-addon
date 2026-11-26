using System;
using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using HarmonyLib;
using InControl;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

// Настоящие механики предметов Isaac: лазеры, взрывы, самонаведение, яд/огонь, орбитали,
// поглощение удара и активные способности на клавишу. Всё привязано к картам «RS_i_*».
namespace RoundsSettingsMod
{
    // ---------- подсчёт карт игрока ----------
    public static class Pw
    {
        class Cache { public int Count; public CardInfo Last; public Dictionary<string, int> D = new Dictionary<string, int>(); }
        static readonly Dictionary<Player, Cache> cache = new Dictionary<Player, Cache>();
        static readonly Dictionary<string, int> empty = new Dictionary<string, int>();

        static string Strip(string n) { return n.Replace("(Clone)", "").Trim(); }

        static Dictionary<string, int> Counts(Player p)
        {
            if (p == null || p.data == null || p.data.currentCards == null) return empty;
            var cs = p.data.currentCards;
            Cache c;
            if (!cache.TryGetValue(p, out c)) { c = new Cache(); cache[p] = c; c.Count = -1; }
            CardInfo last = cs.Count > 0 ? cs[cs.Count - 1] : null;
            if (c.Count != cs.Count || c.Last != last)
            {
                c.D.Clear();
                for (int i = 0; i < cs.Count; i++)
                {
                    var ci = cs[i]; if (ci == null) continue;
                    string n = Strip(ci.gameObject.name);
                    if (!n.StartsWith("RS_")) continue;
                    n = n.Substring(3);
                    int v; c.D.TryGetValue(n, out v); c.D[n] = v + 1;
                }
                c.Count = cs.Count; c.Last = last;
            }
            return c.D;
        }

        public static int N(Player p, string id) { int v; return Counts(p).TryGetValue(id, out v) ? v : 0; }
        public static bool Any(Player p) { return Counts(p).Count > 0; }
        public static bool Alive(Player p) { return p != null && p.data != null && !p.data.dead && p.data.isPlaying; }

        public static List<Player> Enemies(Player me)
        {
            var l = new List<Player>();
            if (PlayerManager.instance == null) return l;
            foreach (var p in PlayerManager.instance.players)
                if (p != null && p != me && Alive(p) && p.TeamID != me.TeamID) l.Add(p);
            return l;
        }

        public static float Mass(Player p) { try { float m = Traverse.Create(p.data.playerVel).Field("mass").GetValue<float>(); return m > 0f ? m : 1f; } catch { return 1f; } }
        public static void Impulse(Player p, Vector2 f) { Traverse.Create(p.data.playerVel).Method("AddForce", new Type[] { typeof(Vector2), typeof(ForceMode2D) }, new object[] { f, ForceMode2D.Impulse }).GetValue(); }

        public static bool BattleOn { get { return GameManager.instance != null && GameManager.instance.battleOngoing; } }
    }

    // ---------- визуальные эффекты ----------
    public static class Fx
    {
        public static readonly Color[] Pal = {
            new Color(1f, 0.15f, 0.1f), new Color(0.3f, 0.9f, 1f), new Color(1f, 1f, 1f), new Color(0.7f, 0.3f, 1f),
            new Color(1f, 0.6f, 0.1f), new Color(0.4f, 1f, 0.4f), new Color(0.8f, 0.8f, 0.8f)
        };
        static Material mat;
        static Sprite circle;

        static Material Mat()
        {
            if (mat == null)
            {
                var sh = Shader.Find("Sprites/Default");
                if (sh == null) sh = Shader.Find("Hidden/Internal-Colored");
                mat = new Material(sh);
            }
            return mat;
        }

        public static Sprite Circle()
        {
            if (circle == null)
            {
                int n = 32;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
                for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    tex.SetPixel(x, y, new Color(1, 1, 1, d <= 1f ? Mathf.Clamp01((1f - d) * 6f) : 0f));
                }
                tex.Apply();
                circle = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
            }
            return circle;
        }

        public static void Ring(Vector2 p, float r, int col, float dur)
        {
            try
            {
                var go = new GameObject("RS_Ring");
                var lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = true; lr.material = Mat(); lr.sortingOrder = 300; lr.loop = true; lr.positionCount = 40;
                var a = go.AddComponent<FxAnim>(); a.Setup(lr, true, p, Vector2.zero, r, 0.2f, Pal[Mathf.Clamp(col, 0, Pal.Length - 1)], dur);
            }
            catch (Exception e) { RoundsSettingsPlugin.Warn("Fx.Ring: " + e.Message); }
        }

        public static void Beam(Vector2 a, Vector2 b, float width, int col, float dur)
        {
            try
            {
                var go = new GameObject("RS_Beam");
                var lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = true; lr.material = Mat(); lr.sortingOrder = 300; lr.positionCount = 2;
                var f = go.AddComponent<FxAnim>(); f.Setup(lr, false, a, b, 0f, width, Pal[Mathf.Clamp(col, 0, Pal.Length - 1)], dur);
            }
            catch (Exception e) { RoundsSettingsPlugin.Warn("Fx.Beam: " + e.Message); }
        }
    }

    public class FxAnim : MonoBehaviour
    {
        LineRenderer lr; bool ring; Vector2 p, q; float radius, width, dur, t; Color col;
        public void Setup(LineRenderer l, bool isRing, Vector2 a, Vector2 b, float r, float w, Color c, float d)
        { lr = l; ring = isRing; p = a; q = b; radius = r; width = w; col = c; dur = Mathf.Max(0.05f, d); Apply(0f); }
        void Update() { t += Time.deltaTime; if (t >= dur) { Destroy(gameObject); return; } Apply(t / dur); }
        void Apply(float k)
        {
            var c = col; c.a = 1f - k * k;
            lr.startColor = c; lr.endColor = c;
            if (ring)
            {
                float r = Mathf.Lerp(0.2f, radius, Mathf.Sqrt(k));
                lr.widthMultiplier = width * (1f - k) + 0.05f;
                for (int i = 0; i < lr.positionCount; i++)
                {
                    float a = i / (float)lr.positionCount * Mathf.PI * 2f;
                    lr.SetPosition(i, new Vector3(p.x + Mathf.Cos(a) * r, p.y + Mathf.Sin(a) * r, 0f));
                }
            }
            else
            {
                lr.widthMultiplier = width * (1f - k);
                lr.SetPosition(0, new Vector3(p.x, p.y, 0f)); lr.SetPosition(1, new Vector3(q.x, q.y, 0f));
            }
        }
    }

    // ---------- самонаведение ----------
    public class HomingBullet : MonoBehaviour
    {
        public float Strength; public Player Owner; MoveTransform mt;
        void Start() { mt = GetComponent<MoveTransform>(); }
        void Update()
        {
            if (mt == null || Owner == null || PlayerManager.instance == null) return;
            Vector3 v = mt.velocity; if (v.sqrMagnitude < 1f) return;
            Player best = null; float bd = 30f * 30f;
            foreach (var p in PlayerManager.instance.players)
            {
                if (p == null || p == Owner || !Pw.Alive(p) || p.TeamID == Owner.TeamID) continue;
                float d = (p.transform.position - transform.position).sqrMagnitude;
                if (d < bd) { bd = d; best = p; }
            }
            if (best == null) return;
            Vector3 want = best.transform.position - transform.position; want.z = 0f;
            mt.velocity = Vector3.RotateTowards(v, want.normalized * v.magnitude, Strength * Time.deltaTime, 0f);
        }
    }

    // ---------- орбитали ----------
    class Orb { public Transform T; public float Angle0, Radius, Speed, Dmg, Size; public float[] Next = new float[16]; }
    class OrbitSet { public int Code = -1; public List<Orb> Orbs = new List<Orb>(); public GameObject Root; }

    // ---------- главный обработчик ----------
    public class IsaacRunner : MonoBehaviour, IOnEventCallback
    {
        public static IsaacRunner I;
        const byte EvCode = 181;

        static readonly Dictionary<int, float> shieldUntil = new Dictionary<int, float>();
        static readonly Dictionary<int, float> belialUntil = new Dictionary<int, float>();
        static readonly Dictionary<int, float> tempOrbitUntil = new Dictionary<int, float>();
        static readonly Dictionary<int, int> mantle = new Dictionary<int, int>();
        static readonly Dictionary<int, float> readyAt = new Dictionary<int, float>();
        readonly Dictionary<Player, OrbitSet> orbits = new Dictionary<Player, OrbitSet>();

        class ActiveDef { public string Id, Name; public float Cd; public ActiveDef(string i, string n, float c) { Id = i; Name = n; Cd = c; } }
        static readonly ActiveDef[] Actives = {
            new ActiveDef("i_necronomicon", "Некрономикон", 20f), new ActiveDef("i_belial", "Книга Велиала", 18f),
            new ActiveDef("i_yumheart", "Вкусное сердце", 15f), new ActiveDef("i_shadows", "Книга теней", 25f),
            new ActiveDef("i_bible", "Библия", 14f), new ActiveDef("i_teleport", "Телепорт!", 8f),
            new ActiveDef("i_megablast", "Мега-взрыв", 35f), new ActiveDef("i_shoop", "Шуп-да-Вуп!", 20f),
            new ActiveDef("i_hourglass", "Песочные часы", 22f), new ActiveDef("i_tammy", "Голова Тэмми", 12f),
            new ActiveDef("i_howtojump", "Как прыгать", 6f), new ActiveDef("i_monstermanual", "Монстр-книга", 24f),
            new ActiveDef("i_kamikaze", "Камикадзе", 25f)
        };

        void OnEnable() { I = this; PhotonNetwork.AddCallbackTarget(this); }
        void OnDisable() { PhotonNetwork.RemoveCallbackTarget(this); }

        // ---------- сеть: общие события ----------
        public static void Send(int kind, int pid, params float[] f)
        {
            if (PhotonNetwork.OfflineMode || !PhotonNetwork.InRoom) { Handle(kind, pid, f); return; }
            PhotonNetwork.RaiseEvent(EvCode, new object[] { kind, pid, f }, new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendReliable);
        }

        public void OnEvent(EventData e)
        {
            if (e.Code != EvCode) return;
            try { var a = (object[])e.CustomData; Handle((int)a[0], (int)a[1], (float[])a[2]); }
            catch (Exception ex) { RoundsSettingsPlugin.Warn("Isaac.OnEvent: " + ex.Message); }
        }

        static Player ById(int pid) { return PlayerManager.instance != null ? Traverse.Create(PlayerManager.instance).Method("GetPlayerWithID", new object[] { pid }).GetValue<Player>() : null; }

        static void Handle(int kind, int pid, float[] f)
        {
            Player p = ById(pid);
            switch (kind)
            {
                case 1: shieldUntil[pid] = Time.time + f[0]; if (p != null) Fx.Ring(p.transform.position, 2.2f, 3, 0.6f); break;
                case 2: if (p != null && Pw.Alive(p)) { p.data.healthHandler.Heal(f[0]); Fx.Ring(p.transform.position, 1.6f, 5, 0.5f); } break;
                case 3: if (p != null && Pw.Alive(p)) p.data.stats.RPCA_AddSlow(f[0], false); break;
                case 5: Fx.Ring(new Vector2(f[0], f[1]), f[2], (int)f[3], f[4]); break;
                case 6: Fx.Beam(new Vector2(f[0], f[1]), new Vector2(f[2], f[3]), f[4], (int)f[5], f[6]); break;
                case 8: tempOrbitUntil[pid] = Time.time + f[0]; break;
            }
        }

        public static float BelialUntil(Player p) { float v; return belialUntil.TryGetValue(p.PlayerID, out v) ? v : 0f; }

        public static void ResetPlayer(Player p)
        {
            int pid = p.PlayerID;
            mantle[pid] = Pw.N(p, "i_holymantle");
            shieldUntil.Remove(pid); belialUntil.Remove(pid); tempOrbitUntil.Remove(pid);
            for (int i = 0; i < Actives.Length; i++) readyAt.Remove(pid * 100 + i);
        }

        // поглощение удара: Священная мантия и «Книга теней»
        public static bool Absorb(Player p, Vector2 damage, bool healthRemoval, HealthHandler.DamageSource src)
        {
            if (src != HealthHandler.DamageSource.Player || p == null) return false;
            int pid = p.PlayerID;
            float until;
            if (shieldUntil.TryGetValue(pid, out until) && Time.time < until) return true;
            if (healthRemoval || damage.magnitude < 10f) return false;
            int left;
            if (!mantle.TryGetValue(pid, out left)) { left = Pw.N(p, "i_holymantle"); mantle[pid] = left; }
            if (left > 0)
            {
                mantle[pid] = left - 1;
                Fx.Ring(p.transform.position, 2.4f, 2, 0.5f);
                return true;
            }
            return false;
        }

        // ---------- пули ----------
        static bool warned;
        public static void OnBullet(Gun gun, GameObject bullet)
        {
            if (!IsaacAddonPlugin.On) return;
            var ph = bullet.GetComponent<ProjectileHit>(); if (ph == null) return;
            Player owner = ph.ownPlayer != null ? ph.ownPlayer : gun.player; if (owner == null) return;
            if (!Pw.Any(owner)) return;

            if (Time.time < BelialUntil(owner)) ph.damage *= 1.4f;
            float still = IsaacRunner.Still(owner);
            if (still >= 1.5f && Pw.N(owner, "i_ambush") > 0) ph.damage *= 1.4f;

            // самонаведение
            float homing = Pw.N(owner, "i_spoonbender") * 1.6f + Pw.N(owner, "i_ludovico") * 4.5f + Pw.N(owner, "i_sacredheart") * 2.6f;
            if (still >= 1.5f && Pw.N(owner, "z_hunter") > 0) homing += 3.5f;
            if (homing > 0f) { var hb = bullet.AddComponent<HomingBullet>(); hb.Strength = homing; hb.Owner = owner; }

            // размер пули (после этого ProjectileHit.Start увеличит урон пропорционально)
            float size = 1f;
            if (Pw.N(owner, "i_polyphemus") > 0) size = Mathf.Max(size, 1.6f);
            if (Pw.N(owner, "i_epicfetus") > 0) size = Mathf.Max(size, 1.4f);
            if (Pw.N(owner, "i_ipecac") > 0) size = Mathf.Max(size, 1.25f);
            if (size > 1f) bullet.transform.localScale *= size;

            // лазеры вместо пуль
            int laser = Pw.N(owner, "i_brimstone") > 0 ? 1 : (Pw.N(owner, "i_technology") > 0 ? 2 : (Pw.N(owner, "i_momsknife") > 0 ? 3 : 0));
            if (laser != 0)
            {
                bool mine = Traverse.Create(ph).Field("hasControl").GetValue<bool>();
                var rct = bullet.GetComponent<RayCastTrail>();
                LayerMask mask = rct != null ? rct.mask : (LayerMask)(~0);
                Vector2 o = bullet.transform.position, d = bullet.transform.forward; d.Normalize();
                float dmg = ph.damage * ph.dealDamageMultiplierr;
                if (dmg <= 0.01f) dmg = 25f * Mathf.Max(0.3f, gun.damage);
                float range = 45f, width = 0.5f, mult = 1f; int col = 0;
                if (laser == 1) { mult = 1.5f; width = 0.55f; col = 0; }
                else if (laser == 2) { mult = 0.7f; width = 0.2f; col = 1; }
                else { mult = 1.3f; width = 0.45f; col = 6; range = 4.5f; }
                var hit = DoBeam(owner, o, d, range, mask, dmg * mult, mine);
                Fx.Beam(o, hit, width, col, 0.22f);
                // гасим обычную пулю
                ph.damage = 0f; ph.dealDamageMultiplierr = 0f; ph.bulletCanDealDeamage = false;
                foreach (var r in bullet.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
                if (I != null && ph.view != null && ph.view.IsMine) I.StartCoroutine(I.DelayDestroy(bullet));
                return;
            }

            // взрывы / яд / огонь при попадании
            float exR = 0f, exF = 0f, poison = 0f, burn = 0f;
            int n;
            if ((n = Pw.N(owner, "i_drfetus")) > 0) { exR = Mathf.Max(exR, 3.5f); exF += 0.6f * n; }
            if ((n = Pw.N(owner, "i_epicfetus")) > 0) { exR = Mathf.Max(exR, 6f); exF += 0.8f * n; }
            if ((n = Pw.N(owner, "i_cookbook")) > 0) { exR = Mathf.Max(exR, 3f); exF += 0.5f * n; }
            if ((n = Pw.N(owner, "i_ipecac")) > 0) { exR = Mathf.Max(exR, 3f); exF += 0.4f * n; poison = 0.5f * n; }
            if ((n = Pw.N(owner, "i_firemind")) > 0) burn = 0.4f * n;
            if (exR > 0f || poison > 0f || burn > 0f)
            {
                float fR = exR, fF = Mathf.Min(exF, 1.6f), fP = poison, fB = burn;
                var p = ph;
                ph.AddHitActionWithData(delegate(HitInfo hi)
                {
                    try { OnHit(p, owner, hi, fR, fF, fP, fB); }
                    catch (Exception e) { if (!warned) { warned = true; RoundsSettingsPlugin.Warn("Isaac.OnHit: " + e); } }
                });
            }
        }

        IEnumerator DelayDestroy(GameObject go)
        {
            yield return new WaitForSeconds(0.08f);
            if (go != null) { try { PhotonNetwork.Destroy(go); } catch (Exception) { Destroy(go); } }
        }

        static int beamLogs;

        // «твёрдая» преграда для луча: карта/блоки, но не игроки, пули и эффекты
        static bool IsSolid(Collider2D col)
        {
            if (col == null || col.isTrigger) return false;
            if (col.GetComponentInParent<Player>() != null) return false;
            if (col.GetComponentInParent<ProjectileHit>() != null) return false;
            if (col.GetComponentInParent<SpawnedAttack>() != null) return false;
            return true;
        }

        // считает луч, наносит урон (только на стороне владельца), возвращает конечную точку.
        // Цели ищем геометрически (близость к лучу), а не по маске пули: так урон идёт всегда
        static Vector2 DoBeam(Player owner, Vector2 o, Vector2 d, float range, LayerMask mask, float damage, bool canDamage)
        {
            float end = range;
            var hits = Physics2D.RaycastAll(o, d, range);
            for (int i = 0; i < hits.Length; i++)
            {
                if (!IsSolid(hits[i].collider)) continue;
                if (hits[i].distance < end) end = hits[i].distance;
            }
            int n = 0;
            if (canDamage && damage > 0f)
            {
                var wpn = owner.data.weaponHandler.gun.gameObject;
                foreach (var e in Pw.Enemies(owner))
                {
                    Vector2 c = e.transform.position;
                    float t = Vector2.Dot(c - o, d);
                    if (t < -0.5f || t > end + 0.8f) continue;
                    Vector2 closest = o + d * Mathf.Clamp(t, 0f, end);
                    if ((c - closest).magnitude > 1.25f) continue;
                    e.data.healthHandler.CallTakeDamage(d * damage, c, wpn, owner);
                    n++;
                }
            }
            if (beamLogs < 6)
            {
                beamLogs++;
                RoundsSettingsPlugin.Warn("Isaac beam: урон=" + damage.ToString("0.0") + " canDamage=" + canDamage + " дальность=" + end.ToString("0.0") + " целей=" + n);
            }
            return o + d * end;
        }

        static void OnHit(ProjectileHit ph, Player owner, HitInfo hi, float exR, float exF, float poison, float burn)
        {
            Vector2 pos = hi.point;
            if (exR > 0f) Fx.Ring(pos, exR, 4, 0.35f);
            bool mine = Traverse.Create(ph).Field("hasControl").GetValue<bool>();
            if (!mine || I == null) return;
            float dmg = Mathf.Max(5f, ph.damage * ph.dealDamageMultiplierr);
            Player direct = hi.transform != null ? hi.transform.GetComponentInParent<Player>() : null;
            var wpn = owner.data.weaponHandler.gun.gameObject;
            if (direct != null && direct != owner && direct.TeamID != owner.TeamID)
            {
                if (poison > 0f) I.StartCoroutine(I.Dot(owner, direct, dmg * poison, 4f));
                if (burn > 0f) I.StartCoroutine(I.Dot(owner, direct, dmg * burn, 3f));
            }
            if (exR > 0f)
            {
                foreach (var e in Pw.Enemies(owner))
                {
                    if (e == direct) continue;
                    Vector2 dv = (Vector2)e.transform.position - pos;
                    float dist = Mathf.Max(0f, dv.magnitude - 0.5f);
                    if (dist > exR) continue;
                    Vector2 dir = dv.sqrMagnitude > 0.001f ? dv.normalized : Vector2.up;
                    float dd = dmg * exF * (1f - 0.6f * dist / exR);
                    e.data.healthHandler.CallTakeDamage(dir * dd, e.transform.position, wpn, owner);
                    e.data.healthHandler.CallTakeForce(dir * Pw.Mass(e) * 12f);
                    if (poison > 0f) I.StartCoroutine(I.Dot(owner, e, dmg * poison, 4f));
                }
            }
        }

        IEnumerator Dot(Player owner, Player victim, float total, float dur)
        {
            float interval = 0.5f; int ticks = Mathf.Max(1, Mathf.RoundToInt(dur / interval));
            float per = total / ticks;
            for (int i = 0; i < ticks; i++)
            {
                yield return new WaitForSeconds(interval);
                if (victim == null || owner == null || !Pw.Alive(victim) || !Pw.BattleOn) yield break;
                victim.data.healthHandler.CallTakeDamage(Vector2.up * per, victim.transform.position, null, owner);
            }
        }

        // ---------- активные способности ----------
        static Vector2 Aim(Player p)
        {
            Vector2 a = p.data.aimDirection; if (a.sqrMagnitude < 0.01f) a = Vector2.right; return a.normalized;
        }

        static LayerMask beamMask;
        static bool beamMaskSet;
        static LayerMask BeamMask(Player p)
        {
            if (!beamMaskSet)
            {
                try
                {
                    var rct = p.data.weaponHandler.gun.projectiles[0].objectToSpawn.GetComponent<RayCastTrail>();
                    if (rct != null) { beamMask = rct.mask; beamMaskSet = true; }
                }
                catch (Exception) { }
                if (!beamMaskSet) { beamMask = ~0; beamMaskSet = true; }
            }
            return beamMask;
        }

        void ActiveBeam(Player p, float damage, float width, int col, float range)
        {
            Vector2 o = p.transform.position, d = Aim(p);
            Vector2 end = DoBeam(p, o, d, range, BeamMask(p), damage, true);
            Send(6, p.PlayerID, o.x, o.y, end.x, end.y, width, col, 0.35f);
        }

        IEnumerator MegaBlast(Player p)
        {
            for (int i = 0; i < 6; i++)
            {
                if (!Pw.Alive(p) || !Pw.BattleOn) yield break;
                ActiveBeam(p, 10f, 1.3f, 0, 60f);
                yield return new WaitForSeconds(0.35f);
            }
        }

        void UseActive(Player p, string id)
        {
            var gun = p.data.weaponHandler.gun; Vector2 pos = p.transform.position; Vector2 aim = Aim(p);
            var wpn = gun.gameObject; int pid = p.PlayerID;
            switch (id)
            {
                case "i_necronomicon":
                    Send(5, pid, pos.x, pos.y, 4f, 0, 0.5f);
                    foreach (var e in Pw.Enemies(p))
                    {
                        e.data.healthHandler.CallTakeDamage(Vector2.up * 20f, e.transform.position, wpn, p);
                        Send(5, e.PlayerID, e.transform.position.x, e.transform.position.y, 2.5f, 0, 0.5f);
                    }
                    break;
                case "i_belial": belialUntil[pid] = Time.time + 6f; Fx.Ring(pos, 2.5f, 3, 0.6f); break;
                case "i_yumheart": Send(2, pid, 40f); break;
                case "i_shadows": Send(1, pid, 5f); break;
                case "i_bible":
                    Send(5, pid, pos.x, pos.y, 8f, 2, 0.5f);
                    foreach (var e in Pw.Enemies(p))
                    {
                        Vector2 dv = (Vector2)e.transform.position - pos; if (dv.magnitude > 12f) continue;
                        Vector2 dir = dv.sqrMagnitude > 0.001f ? dv.normalized : Vector2.up;
                        e.data.healthHandler.CallTakeDamage(dir * 8f, e.transform.position, wpn, p);
                        e.data.healthHandler.CallTakeForce(dir * Pw.Mass(e) * 25f);
                    }
                    break;
                case "i_teleport":
                    {
                        float dist = 7f;
                        var hits = Physics2D.RaycastAll(pos, aim, 7f);
                        Array.Sort(hits, delegate(RaycastHit2D a, RaycastHit2D b) { return a.distance.CompareTo(b.distance); });
                        for (int i = 0; i < hits.Length; i++)
                        {
                            var c = hits[i].collider; if (c == null || c.isTrigger || c.GetComponentInParent<Player>() != null) continue;
                            dist = Mathf.Max(0f, hits[i].distance - 0.8f); break;
                        }
                        Vector2 np = pos + aim * dist;
                        Send(5, pid, pos.x, pos.y, 1.8f, 3, 0.4f);
                        p.transform.position = new Vector3(np.x, np.y, p.transform.position.z);
                        var rb = p.GetComponent<Rigidbody2D>(); if (rb != null) rb.position = np;
                        Send(5, pid, np.x, np.y, 1.8f, 3, 0.4f);
                    }
                    break;
                case "i_megablast": StartCoroutine(MegaBlast(p)); break;
                case "i_shoop": ActiveBeam(p, 40f, 1.6f, 4, 60f); break;
                case "i_hourglass":
                    Send(5, pid, pos.x, pos.y, 10f, 1, 0.6f);
                    foreach (var e in Pw.Enemies(p)) { Send(3, e.PlayerID, 0.7f); StartCoroutine(SlowAgain(e.PlayerID)); }
                    break;
                case "i_tammy":
                    {
                        var f = Traverse.Create(gun).Field("forceShootDir");
                        for (int k = 0; k < 8; k++)
                        {
                            float a = k * Mathf.PI / 4f;
                            f.SetValue(new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f));
                            gun.Attack(0f, true, 0.8f, 0f, false);
                        }
                        f.SetValue(Vector3.zero);
                    }
                    break;
                case "i_howtojump":
                    Pw.Impulse(p, Vector2.up * Pw.Mass(p) * 38f);
                    Fx.Ring(pos, 1.5f, 5, 0.4f);
                    break;
                case "i_monstermanual": Send(8, pid, 12f); break;
                case "i_kamikaze":
                    Send(5, pid, pos.x, pos.y, 8f, 4, 0.5f);
                    foreach (var e in Pw.Enemies(p))
                    {
                        Vector2 dv = (Vector2)e.transform.position - pos; float dist = dv.magnitude;
                        if (dist > 8f) continue;
                        Vector2 dir = dv.sqrMagnitude > 0.001f ? dv.normalized : Vector2.up;
                        e.data.healthHandler.CallTakeDamage(dir * (40f * (1f - 0.5f * dist / 8f)), e.transform.position, wpn, p);
                        e.data.healthHandler.CallTakeForce(dir * Pw.Mass(e) * 20f);
                    }
                    p.data.healthHandler.CallTakeDamage(Vector2.up * 25f, pos, wpn, p, false);
                    break;
            }
        }

        IEnumerator SlowAgain(int pid)
        {
            for (int i = 0; i < 3; i++) { yield return new WaitForSeconds(1.2f); Send(3, pid, 0.7f); }
        }

        bool PressedActive(Player p)
        {
            try
            {
                var pa = p.data.playerActions;
                if (pa != null && pa.Device != null) return pa.Device.Action4.WasPressed;
            }
            catch (Exception) { }
            return Input.GetKeyDown(IsaacAddonPlugin.ActiveKey.Value);
        }

        // ---------- «стоишь на месте» ----------
        static readonly Dictionary<int, Vector2> lastPos = new Dictionary<int, Vector2>();
        static readonly Dictionary<int, float> stillT = new Dictionary<int, float>();
        static readonly Dictionary<int, float> healAt = new Dictionary<int, float>();

        public static float Still(Player p) { float v; return stillT.TryGetValue(p.PlayerID, out v) ? v : 0f; }

        void TrackStill(Player p)
        {
            int pid = p.PlayerID;
            Vector2 pos = p.transform.position, prev;
            float dt = Mathf.Max(Time.deltaTime, 0.0001f), t;
            stillT.TryGetValue(pid, out t);
            bool moved = lastPos.TryGetValue(pid, out prev) && (pos - prev).magnitude / dt > 0.6f;
            lastPos[pid] = pos;
            if (!Pw.Alive(p) || !Pw.BattleOn || moved) { stillT[pid] = 0f; return; }
            float before = t; t += dt; stillT[pid] = t;
            bool has = Pw.N(p, "i_ambush") > 0 || Pw.N(p, "z_stoneform") > 0 || Pw.N(p, "z_hunter") > 0;
            if (!has) return;
            float need = Pw.N(p, "z_stoneform") > 0 ? 2f : 1.5f;
            if (before < need && t >= need && p.IsLocal) Fx.Ring(pos, 1.8f, 5, 0.5f); // сигнал: бонус включился
            // каменная поза лечит: владелец раз в 0.5 с шлёт всем событие лечения
            if (t >= 2f && Pw.N(p, "z_stoneform") > 0 && p.data.view.IsMine)
            {
                float next; healAt.TryGetValue(pid, out next);
                if (Time.time >= next) { healAt[pid] = Time.time + 0.5f; Send(2, pid, 1f * Pw.N(p, "z_stoneform")); }
            }
        }

        void Update()
        {
            if (!IsaacAddonPlugin.On) return;
            if (PlayerManager.instance == null) return;
            var players = PlayerManager.instance.players;
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i]; if (p == null || p.data == null) continue;
                TrackStill(p);
                UpdateOrbits(p);
                if (!p.IsLocal || !Pw.Alive(p) || !Pw.BattleOn || !Pw.Any(p)) continue;
                if (!PressedActive(p)) continue;
                for (int k = 0; k < Actives.Length; k++)
                {
                    if (Pw.N(p, Actives[k].Id) == 0) continue;
                    int key = p.PlayerID * 100 + k; float t;
                    if (readyAt.TryGetValue(key, out t) && Time.time < t) continue;
                    readyAt[key] = Time.time + Actives[k].Cd;
                    try { UseActive(p, Actives[k].Id); }
                    catch (Exception e) { RoundsSettingsPlugin.Warn("Isaac.UseActive " + Actives[k].Id + ": " + e); }
                    break;
                }
            }
        }

        // ---------- орбитали ----------
        static float OrbitSpeedSign(int type) { return type == 1 ? -1f : 1f; }

        void UpdateOrbits(Player p)
        {
            int meat = Pw.N(p, "i_cubemeat"), band = Pw.N(p, "i_bandages") * 2, dag = Pw.N(p, "i_dagger");
            float tu; if (tempOrbitUntil.TryGetValue(p.PlayerID, out tu) && Time.time < tu) meat += 3;
            int code = meat * 10000 + band * 100 + dag;
            OrbitSet set;
            if (!orbits.TryGetValue(p, out set)) { set = new OrbitSet(); orbits[p] = set; }
            if (set.Code != code)
            {
                if (set.Root != null) Destroy(set.Root);
                set.Orbs.Clear(); set.Code = code;
                if (code != 0)
                {
                    set.Root = new GameObject("RS_Orbits");
                    for (int i = 0; i < meat; i++) set.Orbs.Add(MakeOrb(set, 0, i, meat, 2.2f, 150f, 14f, 0.6f, new Color(0.75f, 0.12f, 0.12f)));
                    for (int i = 0; i < band; i++) set.Orbs.Add(MakeOrb(set, 1, i, band, 2.7f, -170f, 9f, 0.5f, new Color(0.92f, 0.9f, 0.8f)));
                    for (int i = 0; i < dag; i++) set.Orbs.Add(MakeOrb(set, 2, i, dag, 3.3f, 260f, 18f, 0.4f, new Color(0.75f, 0.85f, 1f)));
                }
            }
            if (set.Orbs.Count == 0) return;
            bool show = Pw.Alive(p) && Pw.BattleOn;
            if (set.Root != null) set.Root.SetActive(show);
            if (!show) return;
            bool mine = p.IsLocal; Vector2 c = p.transform.position; float now = Time.time;
            var enemies = mine ? Pw.Enemies(p) : null;
            for (int i = 0; i < set.Orbs.Count; i++)
            {
                var o = set.Orbs[i];
                float a = (o.Angle0 + now * o.Speed) * Mathf.Deg2Rad;
                Vector2 pos = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * o.Radius;
                o.T.position = new Vector3(pos.x, pos.y, p.transform.position.z);
                if (!mine) continue;
                for (int e = 0; e < enemies.Count; e++)
                {
                    var en = enemies[e]; int id = en.PlayerID & 15;
                    if (now < o.Next[id]) continue;
                    Vector2 dv = (Vector2)en.transform.position - pos;
                    if (dv.magnitude > 0.9f + o.Size * 0.5f) continue;
                    o.Next[id] = now + 0.7f;
                    en.data.healthHandler.CallTakeDamage(dv.normalized * o.Dmg, pos, p.data.weaponHandler.gun.gameObject, p);
                }
            }
        }

        Orb MakeOrb(OrbitSet set, int type, int idx, int count, float radius, float speed, float dmg, float size, Color col)
        {
            var go = new GameObject("RS_Orb");
            go.transform.SetParent(set.Root.transform, false);
            go.transform.localScale = new Vector3(size, size, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Fx.Circle(); sr.color = col; sr.sortingOrder = 250;
            return new Orb { T = go.transform, Angle0 = idx * 360f / Mathf.Max(1, count) + type * 40f, Radius = radius, Speed = speed, Dmg = dmg, Size = size };
        }

        // ---------- подсказка по активкам ----------
        GUIStyle st;
        void OnGUI()
        {
            if (!IsaacAddonPlugin.On || PlayerManager.instance == null || !Pw.BattleOn) return;
            if (st == null) { st = new GUIStyle(GUI.skin.label); st.fontSize = 16; st.fontStyle = FontStyle.Bold; st.normal.textColor = Color.white; }
            float y = Screen.height - 24f;
            foreach (var p in PlayerManager.instance.players)
            {
                if (p == null || p.data == null || !p.IsLocal) continue;
                for (int k = Actives.Length - 1; k >= 0; k--)
                {
                    if (Pw.N(p, Actives[k].Id) == 0) continue;
                    float t; bool wait = readyAt.TryGetValue(p.PlayerID * 100 + k, out t) && Time.time < t;
                    string key = (p.data.playerActions != null && p.data.playerActions.Device != null) ? "Y" : IsaacAddonPlugin.ActiveKey.Value.ToString();
                    string s = "[" + key + "] " + Actives[k].Name + (wait ? " — " + Mathf.CeilToInt(t - Time.time) + " с" : " — готово");
                    st.normal.textColor = wait ? new Color(1f, 1f, 1f, 0.55f) : new Color(0.6f, 1f, 0.6f);
                    GUI.Label(new Rect(16, y, 420, 24), s, st);
                    y -= 22f;
                }
            }
        }
    }

    static class IsaacPatches
    {
        [HarmonyPatch(typeof(Gun), "BulletInit")]
        [HarmonyPostfix]
        static void BulletPost(Gun __instance, GameObject bullet)
        {
            try { IsaacRunner.OnBullet(__instance, bullet); }
            catch (Exception e) { RoundsSettingsPlugin.Warn("Isaac.OnBullet: " + e); }
        }

        [HarmonyPatch(typeof(HealthHandler), "DoDamage")]
        [HarmonyPrefix]
        static bool DamagePre(HealthHandler __instance, ref Vector2 damage, bool healthRemoval, HealthHandler.DamageSource damageSource)
        {
            try
            {
                if (!IsaacAddonPlugin.On) return true;
                var p = __instance.GetComponent<Player>();
                if (IsaacRunner.Absorb(p, damage, healthRemoval, damageSource)) return false;
                if (p != null && !healthRemoval && Pw.N(p, "z_stoneform") > 0 && IsaacRunner.Still(p) >= 2f) damage *= 0.6f;
            }
            catch (Exception e) { RoundsSettingsPlugin.Warn("Isaac.DamagePre: " + e); }
            return true;
        }

        [HarmonyPatch(typeof(HealthHandler), "Revive")]
        [HarmonyPostfix]
        static void RevivePost(HealthHandler __instance)
        {
            try { var p = __instance.GetComponent<Player>(); if (p != null) IsaacRunner.ResetPlayer(p); }
            catch (Exception e) { RoundsSettingsPlugin.Warn("Isaac.RevivePost: " + e); }
        }
    }
}
