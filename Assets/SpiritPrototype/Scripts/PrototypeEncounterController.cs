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
        [SerializeField] private Sprite[] relicPortraits = new Sprite[3];
        [SerializeField] private SpiritEncounter[] encounters;
        [SerializeField, Min(.5f)] private float encounterDistance = 1.4f;
        private readonly List<PrototypeTeamMember> team = new List<PrototypeTeamMember>();
        private readonly Dictionary<string, Sprite> portraits = new Dictionary<string, Sprite>();
        private PrototypeBattle battle;
        private SpiritEncounter target, nearby;
        private Vector3 startPosition;
        private int activeIndex, seals = 6;
        private bool teamOpen, forcedReplacement, movementBlocked;
        private bool relicOpen;
        private PrototypeRelicLine relics = new PrototypeRelicLine();
        private int lastActionFrame = -1;
        private bool wasMovementEnabled;
        private GUIStyle label, title, button, small;
        private Texture2D background;

        private void Start()
        {
            if (player == null || encounters == null)
            { Debug.LogError("Configura los encuentros desde Tools > Espiritus > 4.", this); enabled = false; return; }
            startPosition = player.transform.position;
            team.Add(PrototypeTeamMember.Create("Ascua", 20));
            portraits["Ascua"] = allyPortrait;
            foreach (var encounter in encounters)
                if (encounter != null) portraits[encounter.species] = encounter.portrait;
        }

        private void Update()
        {
            if (relicOpen)
            {
                if (Pressed(7) || Pressed(9)) CloseRelics();
                return;
            }
            if (teamOpen)
            {
                for (int i = 1; i <= 4; i++)
                    if (Pressed(i)) { ChooseMember(i - 1); break; }
                if (!forcedReplacement && (Pressed(6) || Pressed(7))) CloseTeam();
                return;
            }
            if (battle != null)
            {
                if (battle.Outcome != PrototypeOutcome.Ongoing)
                { if (Pressed(5)) Finish(); return; }
                if (Pressed(8)) { OpenTeam(false); return; }
                for (int i = 1; i <= 4; i++)
                    if (Pressed(i)) { Act((PrototypeAction)(i - 1)); break; }
                return;
            }
            if (Pressed(6)) { OpenTeam(false); return; }
            if (Pressed(9)) { OpenRelics(); return; }
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
            PrototypeTeamMember member = team[activeIndex];
            battle = new PrototypeBattle(member.Health, target.maxHealth, seals,
                member.MaxHealth, member.Damage, member.Rupture, member.Containment, relics, member.IsBrasa);
            BlockMovement();
        }

        private void Act(PrototypeAction action)
        {
            if (lastActionFrame == Time.frameCount || battle == null) return;
            if (!battle.Act(action, team.Count < 4, Random.value)) return;
            lastActionFrame = Time.frameCount;
            if (action == PrototypeAction.Attack && battle.LastCalculation.Length > 0)
                Debug.Log("Reliquias: " + battle.LastCalculation);
            SyncAfterTurn();
        }

        private void SyncAfterTurn()
        {
            team[activeIndex].Health = battle.AllyHealth;
            seals = battle.Seals;
            if (battle.Outcome == PrototypeOutcome.Defeat)
                foreach (var member in team)
                    if (member.Alive) { OpenTeam(true); break; }
        }

        private void BlockMovement()
        {
            if (movementBlocked) return;
            wasMovementEnabled = player.enabled;
            player.enabled = false;
            movementBlocked = true;
        }

        private void UnblockMovement()
        {
            if (!movementBlocked) return;
            player.enabled = wasMovementEnabled;
            movementBlocked = false;
        }

        private void OpenTeam(bool forced)
        { teamOpen = true; forcedReplacement = forced; BlockMovement(); }

        private void OpenRelics()
        {
            if (battle != null || teamOpen) return;
            relicOpen = true; BlockMovement();
        }

        private void CloseRelics()
        { relicOpen = false; if (battle == null && !teamOpen) UnblockMovement(); }

        private void CloseTeam()
        {
            if (forcedReplacement) return;
            teamOpen = false;
            if (battle == null) UnblockMovement();
        }

        private void ChooseMember(int index)
        {
            if (index < 0 || index >= team.Count || index == activeIndex || !team[index].Alive) return;
            if (lastActionFrame == Time.frameCount) return;
            PrototypeTeamMember member = team[index];
            if (battle != null)
            {
                bool accepted = forcedReplacement
                    ? battle.ReplaceFallenAlly(member.Health, member.MaxHealth, member.Damage, member.Rupture, member.Containment, member.IsBrasa)
                    : battle.SwitchAlly(member.Health, member.MaxHealth, member.Damage, member.Rupture, member.Containment, member.IsBrasa);
                if (!accepted) return;
            }
            activeIndex = index;
            lastActionFrame = Time.frameCount;
            teamOpen = false; forcedReplacement = false;
            if (battle != null) SyncAfterTurn();
            else UnblockMovement();
        }

        private void Finish()
        {
            if (battle == null || battle.Outcome == PrototypeOutcome.Ongoing) return;
            if (battle.Outcome == PrototypeOutcome.Defeat)
            {
                seals = 6;
                relics = new PrototypeRelicLine();
                team.Clear(); team.Add(PrototypeTeamMember.Create("Ascua", 20)); activeIndex = 0;
                foreach (SpiritEncounter encounter in encounters)
                    if (encounter != null) encounter.gameObject.SetActive(true);
                var body = player.GetComponent<Rigidbody2D>();
                body.position = startPosition; body.linearVelocity = Vector2.zero;
                player.transform.position = startPosition;
            }
            else
            {
                if (battle.Outcome == PrototypeOutcome.Captured)
                    team.Add(PrototypeTeamMember.Create(target.species, battle.EnemyHealth));
                target.gameObject.SetActive(false);
            }
            battle = null; target = null; nearby = null;
            teamOpen = false; forcedReplacement = false;
            relicOpen = false;
            UnblockMovement();
        }

        private void OnDisable()
        {
            if (player != null) UnblockMovement();
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
            bool previousEnabled = GUI.enabled;
            float scale = Mathf.Max(.01f, Mathf.Min(Screen.width / 960f, Screen.height / 600f));
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 960 * scale) / 2,
                (Screen.height - 600 * scale) / 2, 0), Quaternion.identity, new Vector3(scale, scale, 1));
            try
            {
                GUI.enabled = previousEnabled && !teamOpen && !relicOpen;
                if (battle == null) DrawExploration();
                else DrawBattle();
                GUI.enabled = previousEnabled;
                if (teamOpen) DrawTeam();
                if (relicOpen) DrawRelics();
            }
            finally { GUI.matrix = previousMatrix; GUI.enabled = previousEnabled; }
        }

        private void DrawExploration()
        {
            PrototypeTeamMember member = team[activeIndex];
            GUI.DrawTexture(new Rect(12, 12, 936, 100), background);
            GUI.Label(new Rect(28, 20, 900, 26), member.Species + "  " + member.Health + "/" + member.MaxHealth + "     Sellos  " + seals +
                "     Equipo  " + team.Count + "/4", label);
            var names = new List<string>();
            foreach (var entry in team) names.Add(entry.Species + " " + entry.Health + "/" + entry.MaxHealth);
            GUI.Label(new Rect(28, 49, 900, 25), string.Join("  /  ", names), small);
            if (GUI.Button(new Rect(28, 78, 220, 28), "Tab - Ver equipo", small)) OpenTeam(false);
            if (GUI.Button(new Rect(300, 78, 240, 28), "R - Ordenar reliquias", small)) OpenRelics();
            DrawRelicStrip(440);
            if (nearby != null)
            {
                GUI.DrawTexture(new Rect(180, 536, 600, 50), background);
                GUI.Label(new Rect(198, 548, 570, 30), "E  -  Encontrar a " + nearby.species, label);
            }
        }

        private void DrawBattle()
        {
            PrototypeTeamMember member = team[activeIndex];
            GUI.DrawTexture(new Rect(0, 0, 960, 600), background);
            GUI.Label(new Rect(30, 20, 740, 42), "Encuentro con " + target.species, title);
            GUI.Label(new Rect(770, 28, 160, 30), "Ronda " + battle.Round, label);
            portraits.TryGetValue(member.Species, out Sprite portrait);
            Portrait(portrait, new Rect(135, 78, 180, 170));
            Portrait(target.portrait, new Rect(645, 78, 180, 170));
            GUI.Label(new Rect(70, 255, 400, 30), member.Species + "  " + battle.AllyHealth + "/" + member.MaxHealth, label);
            GUI.Label(new Rect(565, 255, 365, 30), target.species + "  " + battle.EnemyHealth + "/" + battle.EnemyMaxHealth, label);
            GUI.Label(new Rect(565, 288, 365, 28), battle.Exposed ? "EXPUESTO - Vinculo " + battle.CaptureChance + "%"
                : "Estabilidad " + battle.Stability + "/10", label);
            GUI.Label(new Rect(70, 288, 390, 28), "Sellos " + battle.Seals + "     Equipo " + team.Count + "/4", label);
            if (battle.Outcome == PrototypeOutcome.Ongoing && GUI.Button(new Rect(70, 320, 280, 30), "5 - Cambiar espiritu", small)) OpenTeam(false);
            GUI.Label(new Rect(30, 355, 900, 43), battle.Message, small);
            DrawRelicStrip(400);
            if (battle.Outcome != PrototypeOutcome.Ongoing)
            {
                string text = battle.Outcome == PrototypeOutcome.Defeat ? "Enter - Reiniciar prueba" : "Enter - Volver a explorar";
                if (GUI.Button(new Rect(230, 500, 500, 66), text, button)) Finish();
                return;
            }
            if (GUI.Button(new Rect(30, 486, 210, 80), "1 Atacar\n" + battle.PredictedAttackDamage + " dano / " + member.Rupture + " ruptura", button)) Act(PrototypeAction.Attack);
            if (GUI.Button(new Rect(260, 486, 210, 80), "2 Contener\n1 dano / " + member.Containment + " ruptura", button)) Act(PrototypeAction.Contain);
            if (GUI.Button(new Rect(490, 486, 210, 80), "3 Defender\nReduce el golpe", button)) Act(PrototypeAction.Defend);
            bool previous = GUI.enabled;
            GUI.enabled = previous && battle.Exposed && battle.Seals > 0 && team.Count < 4;
            if (GUI.Button(new Rect(720, 486, 210, 80), "4 Vincular\nConsume un sello", button)) Act(PrototypeAction.Capture);
            GUI.enabled = previous;
            GUI.Label(new Rect(30, 572, 900, 25), "Proximo ataque: " + battle.PredictedAttackTrace, small);
        }

        private void RelicPortrait(PrototypeRelic relic, Rect rect)
        {
            int index = (int)relic;
            if (relicPortraits != null && index < relicPortraits.Length) Portrait(relicPortraits[index], rect);
        }

        private void DrawRelicStrip(float y)
        {
            var tiny = new GUIStyle(small) { fontSize = 14 };
            for (int i = 0; i < 5; i++)
            {
                float x = 30 + i * 182;
                GUI.DrawTexture(new Rect(x, y, 172, 72), background);
                GUI.Box(new Rect(x, y, 172, 72), GUIContent.none);
                GUI.Label(new Rect(x + 6, y + 4, 24, 23), (i + 1).ToString(), tiny);
                if (i >= relics.Count)
                { GUI.Label(new Rect(x + 45, y + 28, 110, 25), "Vacio", tiny); continue; }
                RelicPortrait(relics[i], new Rect(x + 28, y + 2, 43, 43));
                GUI.Label(new Rect(x + 7, y + 46, 164, 24), PrototypeRelicLine.Name(relics[i]), tiny);
            }
        }

        private void DrawRelics()
        {
            GUI.DrawTexture(new Rect(40, 70, 880, 480), background);
            GUI.Label(new Rect(65, 90, 820, 45), "Ordena tus reliquias", title);
            GUI.Label(new Rect(65, 140, 820, 45), "Se calculan de izquierda a derecha. La fila se bloquea al combatir.", small);
            for (int i = 0; i < relics.Count; i++)
            {
                float y = 200 + i * 88;
                RelicPortrait(relics[i], new Rect(65, y, 55, 55));
                GUI.Label(new Rect(135, y, 540, 27), (i + 1) + ". " + PrototypeRelicLine.Name(relics[i]), label);
                GUI.Label(new Rect(135, y + 30, 555, 50), relics.Description(i), small);
                bool previous = GUI.enabled;
                GUI.enabled = previous && i > 0;
                if (GUI.Button(new Rect(715, y + 10, 65, 45), "<", button)) { relics.Move(i, -1); GUI.enabled = previous; break; }
                GUI.enabled = previous && i + 1 < relics.Count;
                if (GUI.Button(new Rect(795, y + 10, 65, 45), ">", button)) { relics.Move(i, 1); GUI.enabled = previous; break; }
                GUI.enabled = previous;
            }
            relics.Damage(10, true, 4, 20, out string example);
            GUI.Label(new Rect(65, 473, 820, 26), "Ejemplo Brasa contra enemigo herido: " + example, small);
            if (GUI.Button(new Rect(600, 510, 260, 30), "Esc / R - Volver", button)) CloseRelics();
        }

        private void DrawTeam()
        {
            GUI.DrawTexture(new Rect(100, 65, 760, 480), background);
            GUI.Label(new Rect(125, 85, 710, 44), forcedReplacement ? "Elige un reemplazo vivo" : "Tu equipo", title);
            GUI.Label(new Rect(125, 132, 710, 42), battle == null
                ? "Elegir lider fuera del combate es gratis. No recupera vida."
                : forcedReplacement ? "El reemplazo por baja no recibe otro ataque inmediato."
                : "Cambiar consume tu turno. El enemigo golpea al entrante.", small);
            for (int i = 0; i < team.Count; i++)
            {
                PrototypeTeamMember member = team[i];
                float y = 186 + i * 70;
                portraits.TryGetValue(member.Species, out Sprite portrait);
                Portrait(portrait, new Rect(125, y, 58, 58));
                GUI.Label(new Rect(198, y, 320, 28), member.Species + "  " + member.Health + "/" + member.MaxHealth, label);
                GUI.Label(new Rect(198, y + 29, 350, 25), "Dano " + member.Damage + " | Contencion " + member.Containment, small);
                bool previous = GUI.enabled;
                GUI.enabled = previous && i != activeIndex && member.Alive;
                string text = i == activeIndex ? "Activo" : !member.Alive ? "Incapacitado" : (i + 1) + " - Elegir";
                if (GUI.Button(new Rect(570, y + 8, 235, 45), text, button)) ChooseMember(i);
                GUI.enabled = previous;
            }
            if (!forcedReplacement && GUI.Button(new Rect(570, 485, 235, 40), "Esc - Volver", button)) CloseTeam();
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
                case 6: return k.tabKey.wasPressedThisFrame;
                case 7: return k.escapeKey.wasPressedThisFrame;
                case 8: return k.digit5Key.wasPressedThisFrame;
                case 9: return k.rKey.wasPressedThisFrame;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            KeyCode[] keys = {KeyCode.E, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4,
                KeyCode.Return, KeyCode.Tab, KeyCode.Escape, KeyCode.Alpha5, KeyCode.R};
            return Input.GetKeyDown(keys[key]);
#endif
            return false;
        }
    }
}
