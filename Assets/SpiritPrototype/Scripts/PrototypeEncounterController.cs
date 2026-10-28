using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SpiritPrototype
{
    [DisallowMultipleComponent]
    public sealed class PrototypeEncounterController : MonoBehaviour
    {
        [SerializeField] private PlayerMovement player;
        [SerializeField] private Sprite allyPortrait;
        [SerializeField] private SpiritEncounter[] encounters;
        [SerializeField, Min(.5f)] private float encounterDistance = 1.4f;
        private readonly List<string> team = new List<string>();
        private PrototypeBattle battle;
        private SpiritEncounter target, nearby;
        private Vector3 startPosition;
        private int health = PrototypeBattle.AllyMaxHealth, seals = 6;
        private int lastActionFrame = -1;
        private bool wasMovementEnabled;
        private GUIStyle label, title, button, small;
        private Texture2D background;

        private void Start()
        {
            if (player == null || encounters == null)
            { Debug.LogError("Configura los encuentros desde Tools > Espiritus > 4.", this); enabled = false; return; }
            startPosition = player.transform.position;
            team.Add("Ascua");
        }

        private void Update()
        {
            if (battle != null)
            {
                if (battle.Outcome != PrototypeOutcome.Ongoing)
                { if (Pressed(5)) Finish(); return; }
                for (int i = 1; i <= 4; i++)
                    if (Pressed(i)) { Act((PrototypeAction)(i - 1)); break; }
                return;
            }
            nearby = null;
            float best = encounterDistance * encounterDistance;
            foreach (SpiritEncounter encounter in encounters)
            {
                if (encounter == null || !encounter.gameObject.activeInHierarchy) continue;
                float distance = (encounter.transform.position - player.transform.position).sqrMagnitude;
                if (distance < best) { best = distance; nearby = encounter; }
            }
            if (nearby != null && Pressed(0)) Begin(nearby);
        }

        private void Begin(SpiritEncounter encounter)
        {
            target = encounter;
            battle = new PrototypeBattle(health, target.maxHealth, seals);
            wasMovementEnabled = player.enabled;
            player.enabled = false;
        }

        private void Act(PrototypeAction action)
        {
            if (lastActionFrame == Time.frameCount || battle == null) return;
            if (!battle.Act(action, team.Count < 4, Random.value)) return;
            lastActionFrame = Time.frameCount;
            health = battle.AllyHealth; seals = battle.Seals;
        }

        private void Finish()
        {
            if (battle == null || battle.Outcome == PrototypeOutcome.Ongoing) return;
            if (battle.Outcome == PrototypeOutcome.Defeat)
            {
                health = PrototypeBattle.AllyMaxHealth; seals = 6;
                team.Clear(); team.Add("Ascua");
                foreach (SpiritEncounter encounter in encounters)
                    if (encounter != null) encounter.gameObject.SetActive(true);
                var body = player.GetComponent<Rigidbody2D>();
                body.position = startPosition; body.linearVelocity = Vector2.zero;
                player.transform.position = startPosition;
            }
            else
            {
                if (battle.Outcome == PrototypeOutcome.Captured) team.Add(target.species);
                target.gameObject.SetActive(false);
            }
            battle = null; target = null; nearby = null;
            player.enabled = wasMovementEnabled;
        }

        private void OnDisable()
        {
            if (player != null && battle != null) player.enabled = wasMovementEnabled;
        }

        private void OnDestroy()
        {
            if (background != null) Destroy(background);
        }

        private void Styles()
        {
            if (label != null) return;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label = new GUIStyle(GUI.skin.label) { font = font, fontSize = 20, wordWrap = true };
            label.normal.textColor = new Color(.88f, .93f, 1f);
            title = new GUIStyle(label) { fontSize = 30, fontStyle = FontStyle.Bold };
            small = new GUIStyle(label) { fontSize = 16 };
            button = new GUIStyle(GUI.skin.button) { font = font, fontSize = 19, wordWrap = true };
            background = new Texture2D(1, 1);
            background.SetPixel(0, 0, new Color(.025f, .045f, .075f, .98f));
            background.Apply();
        }

        private void OnGUI()
        {
            if (player == null || team.Count == 0) return;
            Styles();
            Matrix4x4 previousMatrix = GUI.matrix;
            float scale = Mathf.Max(.01f, Mathf.Min(Screen.width / 960f, Screen.height / 600f));
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 960 * scale) / 2,
                (Screen.height - 600 * scale) / 2, 0), Quaternion.identity, new Vector3(scale, scale, 1));
            try
            {
                if (battle == null) DrawExploration();
                else DrawBattle();
            }
            finally { GUI.matrix = previousMatrix; }
        }

        private void DrawExploration()
        {
            GUI.DrawTexture(new Rect(12, 12, 936, 72), background);
            GUI.Label(new Rect(28, 20, 900, 26), "Ascua  " + health + "/20     Sellos  " + seals +
                "     Equipo  " + team.Count + "/4", label);
            GUI.Label(new Rect(28, 49, 900, 25), string.Join("  /  ", team), small);
            if (nearby != null)
            {
                GUI.DrawTexture(new Rect(180, 536, 600, 50), background);
                GUI.Label(new Rect(198, 548, 570, 30), "E  -  Encontrar a " + nearby.species, label);
            }
        }

        private void DrawBattle()
        {
            GUI.DrawTexture(new Rect(0, 0, 960, 600), background);
            GUI.Label(new Rect(30, 20, 740, 42), "Encuentro con " + target.species, title);
            GUI.Label(new Rect(770, 28, 160, 30), "Ronda " + battle.Round, label);
            Portrait(allyPortrait, new Rect(115, 110, 210, 210));
            Portrait(target.portrait, new Rect(635, 110, 210, 210));
            GUI.Label(new Rect(70, 330, 330, 30), "Ascua  " + battle.AllyHealth + "/20", label);
            GUI.Label(new Rect(565, 330, 365, 30), target.species + "  " + battle.EnemyHealth + "/" + battle.EnemyMaxHealth, label);
            GUI.Label(new Rect(565, 363, 365, 28), battle.Exposed ? "EXPUESTO - Vinculo " + battle.CaptureChance + "%"
                : "Estabilidad " + battle.Stability + "/10", label);
            GUI.Label(new Rect(70, 363, 390, 28), "Sellos " + battle.Seals + "     Equipo " + team.Count + "/4", label);
            GUI.Label(new Rect(30, 408, 900, 60), battle.Message, label);
            if (battle.Outcome != PrototypeOutcome.Ongoing)
            {
                string text = battle.Outcome == PrototypeOutcome.Defeat ? "Enter - Reiniciar prueba" : "Enter - Volver a explorar";
                if (GUI.Button(new Rect(230, 500, 500, 66), text, button)) Finish();
                return;
            }
            if (GUI.Button(new Rect(30, 486, 210, 80), "1 Atacar\n5 dano / 2 ruptura", button)) Act(PrototypeAction.Attack);
            if (GUI.Button(new Rect(260, 486, 210, 80), "2 Contener\n1 dano / 5 ruptura", button)) Act(PrototypeAction.Contain);
            if (GUI.Button(new Rect(490, 486, 210, 80), "3 Defender\nReduce el golpe", button)) Act(PrototypeAction.Defend);
            bool previous = GUI.enabled;
            GUI.enabled = previous && battle.Exposed && battle.Seals > 0 && team.Count < 4;
            if (GUI.Button(new Rect(720, 486, 210, 80), "4 Vincular\nConsume un sello", button)) Act(PrototypeAction.Capture);
            GUI.enabled = previous;
        }

        private static void Portrait(Sprite sprite, Rect rect)
        { if (sprite != null) GUI.DrawTexture(rect, sprite.texture, ScaleMode.ScaleToFit, true); }

        private static bool Pressed(int key)
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard k = Keyboard.current;
            if (k == null) return false;
            switch (key)
            {
                case 0: return k.eKey.wasPressedThisFrame;
                case 1: return k.digit1Key.wasPressedThisFrame;
                case 2: return k.digit2Key.wasPressedThisFrame;
                case 3: return k.digit3Key.wasPressedThisFrame;
                case 4: return k.digit4Key.wasPressedThisFrame;
                case 5: return k.enterKey.wasPressedThisFrame;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            KeyCode[] keys = {KeyCode.E, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Return};
            return Input.GetKeyDown(keys[key]);
#endif
            return false;
        }
    }
}
