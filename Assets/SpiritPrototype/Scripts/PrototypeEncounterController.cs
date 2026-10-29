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
        private PrototypeRelicLine relics; // Catalog assets must load in Start, never in a MonoBehaviour initializer.
        private bool debugOverlay, debugTestEncounter;
        public bool DebugEnabled { get; set; }
        public string DebugCurrentSpecies => target != null ? target.species : null;
        public bool DebugRestartWorldFight(string species)
        {
            if (!DebugEnabled || battle != null || string.IsNullOrEmpty(species)) return false;
            foreach (var encounter in encounters)
                if (encounter != null && encounter.species == species && encounter.gameObject.activeSelf)
                { Begin(encounter); return true; }
            return false;
        }
        public PrototypeBattle DebugBattle => DebugEnabled ? battle : null;
        public IReadOnlyList<PrototypeTeamMember> DebugTeam => team.AsReadOnly();
        public bool DebugCanOpen => !externalPause && !teamOpen && !relicOpen;
        private bool externalPause;
        public PrototypeRelicLine Relics => relics;
        public bool CanExplore => battle == null && !teamOpen && !relicOpen && !externalPause && !debugOverlay;
        public event System.Action<SpiritEncounter, bool> EncounterResolved;
        public event System.Action TrialRestarted;
        public event System.Action<SpiritEncounter> EncounterBegan;
        public event System.Action StateChanged;
        public event System.Action TrialFailed;
        public bool CanCheckpoint => battle == null;
        public int MenuClosedFrame { get; private set; } = -1;
        private int lastActionFrame = -1;
        private bool wasMovementEnabled;
        private GUIStyle label, title, button, small;
        private Texture2D background;

        public SpiritEncounter[] CatalogEncounters => encounters;
        private void EnsureCatalogActors()
        {
            var list = new List<SpiritEncounter>();
            foreach (var actor in encounters) if (actor != null && !list.Contains(actor)) list.Add(actor);
            SpiritEncounter template = list.Count > 0 ? list[0] : null;
            foreach (var definition in PrototypeContent.Catalog.spirits)
            {
                SpiritEncounter actor = null;
                foreach (var candidate in list) if (candidate.species == definition.id) { actor = candidate; break; }
                if (actor == null)
                {
                    if (template == null) throw new System.InvalidOperationException("No hay un encuentro base para crear actores del catalogo.");
                    var go = Object.Instantiate(template.gameObject, template.transform.parent);
                    go.name = definition.id; actor = go.GetComponent<SpiritEncounter>(); list.Add(actor);
                }
                actor.ApplyDefinition(definition);
            }
            encounters = list.ToArray();
        }
        private void Start()
        {
            if (player == null || encounters == null)
            { Debug.LogError("Configura los encuentros desde Tools > Espiritus > 4.", this); enabled = false; return; }
            try { EnsureCatalogActors(); relics = new PrototypeRelicLine(); }
            catch (System.Exception error) { Debug.LogError("Catalogo: " + error.Message, this); enabled = false; return; }
            startPosition = player.transform.position;
            team.Add(PrototypeTeamMember.Create("Ascua", 20));
            portraits["Ascua"] = PrototypeContent.Spirit("Ascua").sprite != null ? PrototypeContent.Spirit("Ascua").sprite : allyPortrait;
            foreach (var encounter in encounters)
                if (encounter != null) portraits[encounter.species] = encounter.portrait;
        }

        private void Update()
        {
            if (externalPause || debugOverlay) return;
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
            if (!debugTestEncounter) EncounterBegan?.Invoke(encounter);
            target = encounter;
            PrototypeTeamMember member = team[activeIndex];
            battle = new PrototypeBattle(member.Health, target.maxHealth, seals,
                member.MaxHealth, member.Damage, member.Rupture, member.Containment, relics, member.IsBrasa,
                target.attackDamage, target.capturable, target.maxStability);
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
        { if (externalPause || debugOverlay) return; teamOpen = true; forcedReplacement = forced; BlockMovement(); }

        private void OpenRelics()
        {
            if (battle != null || teamOpen || externalPause) return;
            relicOpen = true; BlockMovement();
        }

        private void CloseRelics()
        { relicOpen = false; MenuClosedFrame = Time.frameCount; if (battle == null && !teamOpen) UnblockMovement(); StateChanged?.Invoke(); }

        private void CloseTeam()
        {
            if (forcedReplacement) return;
            teamOpen = false; MenuClosedFrame = Time.frameCount;
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
            else { UnblockMovement(); StateChanged?.Invoke(); }
        }

        private void Finish()
        {
            if (battle == null || battle.Outcome == PrototypeOutcome.Ongoing) return;
            if (battle.Outcome == PrototypeOutcome.Defeat)
            {
                if (debugTestEncounter)
                {
                    var test = target;
                    battle = null; target = null; nearby = null; debugTestEncounter = false;
                    teamOpen = false; forcedReplacement = false;
                    foreach (var member in team) member.Health = member.MaxHealth;
                    UnblockMovement(); Destroy(test.gameObject);
                    return;
                }
                if (TrialFailed != null)
                {
                    battle = null; target = null; nearby = null;
                    teamOpen = false; relicOpen = false; forcedReplacement = false;
                    UnblockMovement();
                    TrialFailed.Invoke();
                }
                else RestartTrial();
                return;
            }
            SpiritEncounter resolved = target;
            bool captured = battle.Outcome == PrototypeOutcome.Captured;
            if (captured) team.Add(PrototypeTeamMember.Create(target.species, battle.EnemyHealth));
            target.gameObject.SetActive(false);
            battle = null; target = null; nearby = null;
            teamOpen = false; forcedReplacement = false;
            relicOpen = false;
            UnblockMovement();
            if (debugTestEncounter) { debugTestEncounter = false; Destroy(resolved.gameObject); }
            else EncounterResolved?.Invoke(resolved, captured);
        }

        public void RestartTrial()
        {
            CleanupDebugOpponent();
            seals = 6; relics = new PrototypeRelicLine();
            team.Clear(); team.Add(PrototypeTeamMember.Create("Ascua", 20)); activeIndex = 0;
            foreach (SpiritEncounter encounter in encounters)
                if (encounter != null) encounter.gameObject.SetActive(true);
            battle = null; target = null; nearby = null;
            teamOpen = false; relicOpen = false; forcedReplacement = false; externalPause = false;
            WarpPlayer(startPosition);
            UnblockMovement();
            TrialRestarted?.Invoke();
        }

        public void WriteTo(PrototypeRunSave state)
        {
            state.team.Clear();
            foreach (var member in team) state.team.Add(new PrototypeMemberSave {species = member.Species, health = member.Health});
            state.activeIndex = activeIndex; state.seals = seals;
            state.relics.Clear();
            for (int i = 0; i < relics.Count; i++) state.relics.Add((int)relics[i]);
            state.playerX = player.transform.position.x; state.playerY = player.transform.position.y;
        }

        public void Restore(PrototypeRunSave state)
        {
            CleanupDebugOpponent();
            battle = null; target = null; nearby = null;
            teamOpen = false; relicOpen = false; forcedReplacement = false;
            team.Clear();
            foreach (var saved in state.team)
            {
                var member = PrototypeTeamMember.Create(saved.species, saved.health);
                member.Health = System.Math.Min(saved.health, member.MaxHealth); // Keep zero; clamp old saves after balance changes.
                team.Add(member);
            }
            activeIndex = state.activeIndex; seals = state.seals;
            var values = new List<PrototypeRelic>();
            foreach (int value in state.relics) values.Add((PrototypeRelic)value);
            relics = new PrototypeRelicLine(values);
            externalPause = false; UnblockMovement();
            WarpPlayer(new Vector3(state.playerX, state.playerY, 0));
        }

        private void CleanupDebugOpponent()
        {
            if (debugTestEncounter && target != null) Destroy(target.gameObject);
            debugTestEncounter = false;
        }
        public void DebugResetTransient()
        {
            if (!DebugEnabled) return;
            CleanupDebugOpponent(); battle = null; target = null; nearby = null;
            teamOpen = false; forcedReplacement = false; relicOpen = false; externalPause = false;
            UnblockMovement();
        }
        public void SetDebugOverlay(bool open)
        {
            debugOverlay = open;
            if (open) BlockMovement();
            else { MenuClosedFrame = Time.frameCount; if (battle == null && !teamOpen && !relicOpen && !externalPause) UnblockMovement(); }
        }
        public bool DebugResolve(bool capture)
        {
            if (!DebugEnabled || battle == null || !battle.DebugResolve(capture, team.Count < 4)) return false;
            SyncAfterTurn(); Finish();
            if (debugOverlay) BlockMovement();
            return true;
        }
        public void DebugHealTeam()
        {
            if (!DebugEnabled) return;
            foreach (var member in team) member.Health = member.MaxHealth;
            if (battle != null) { battle.DebugHeal(); teamOpen = false; forcedReplacement = false; }
        }
        public void DebugAddSeals()
        {
            if (!DebugEnabled) return;
            seals += 25; if (battle != null) battle.DebugSeals(25);
        }
        public bool DebugMember(string species, int replace)
        {
            if (!DebugEnabled || battle != null || !PrototypeSpeciesCatalog.Known(species) || !PrototypeSpeciesCatalog.CanBeAlly(species)) return false;
            var member = PrototypeTeamMember.Create(species, int.MaxValue);
            if (team.Count < 4) team.Add(member);
            else if (replace >= 0 && replace < team.Count) team[replace] = member;
            else return false;
            return true;
        }
        public bool DebugLeader(int index)
        {
            if (!DebugEnabled || battle != null || index < 0 || index >= team.Count || !team[index].Alive) return false;
            activeIndex = index; return true;
        }
        public bool DebugRemoveMember(int index)
        {
            if (!DebugEnabled || battle != null || team.Count <= 1 || index < 0 || index >= team.Count) return false;
            team.RemoveAt(index); if (activeIndex > index) activeIndex--; else if (activeIndex >= team.Count) activeIndex = 0;
            if (!team[activeIndex].Alive) team[activeIndex].Health = team[activeIndex].MaxHealth;
            return true;
        }
        public bool DebugRelic(PrototypeRelic relic, int replace)
        { return DebugEnabled && battle == null && relics.Acquire(relic, replace); }
        public bool DebugMoveRelic(int index, int direction)
        { return DebugEnabled && battle == null && relics.Move(index, direction); }
        public bool DebugRemoveRelic(int index)
        {
            if (!DebugEnabled || battle != null || index < 0 || index >= relics.Count) return false;
            var values = new List<PrototypeRelic>();
            for (int i = 0; i < relics.Count; i++) if (i != index) values.Add(relics[i]);
            relics = new PrototypeRelicLine(values); return true;
        }
        public bool DebugFight(string species)
        {
            if (!DebugEnabled || battle != null || !DebugCanOpen || !team[activeIndex].Alive) return false;
            SpiritEncounter source = null;
            foreach (var encounter in encounters) if (encounter != null && encounter.species == species) { source = encounter; break; }
            if (!PrototypeSpeciesCatalog.Known(species)) return false;
            var definition = PrototypeContent.Spirit(species);
            var test = new GameObject("DEBUG - " + species).AddComponent<SpiritEncounter>();
            test.ApplyDefinition(definition);
            if (test.portrait == null && source != null) test.portrait = source.portrait;
            debugTestEncounter = true;
            Begin(test); return true;
        }

        public void GrantSeals(int amount) { seals += Mathf.Max(0, amount); }

        public void RestoreTeam()
        {
            if (!CanExplore) return;
            foreach (var member in team) member.Health = member.MaxHealth;
        }

        public void SetExternalPause(bool pause)
        {
            externalPause = pause;
            if (pause) BlockMovement();
            else if (battle == null && !teamOpen && !relicOpen) UnblockMovement();
        }

        public void WarpPlayer(Vector3 position)
        {
            var body = player.GetComponent<Rigidbody2D>();
            body.position = position; body.linearVelocity = Vector2.zero;
            player.transform.position = position;
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
                GUI.enabled = previousEnabled && !teamOpen && !relicOpen && !externalPause && !debugOverlay;
                if (battle == null) DrawExploration();
                else DrawBattle();
                GUI.enabled = previousEnabled && !debugOverlay;
                if (teamOpen) DrawTeam();
                if (relicOpen) DrawRelics();
            }
            finally { GUI.matrix = previousMatrix; GUI.enabled = previousEnabled; }
        }

        private void DrawExploration()
        {
            PrototypeTeamMember member = team[activeIndex];
            // Compact border HUD. Expanded team details live only in the Tab menu.
            GUI.DrawTexture(new Rect(12, 6, 936, 38), background);
            GUI.Label(new Rect(24, 12, 570, 27), member.DisplayName + "  " + member.Health + "/" + member.MaxHealth +
                "   |   Sellos " + seals + "   |   Equipo " + team.Count + "/4", small);
            if (GUI.Button(new Rect(620, 11, 135, 27), "Tab - Equipo", small)) OpenTeam(false);
            if (GUI.Button(new Rect(770, 11, 160, 27), "R - Reliquias", small)) OpenRelics();
            DrawExplorationRelics();
            if (nearby != null && !externalPause)
            {
                GUI.DrawTexture(new Rect(240, 506, 480, 32), background);
                GUI.Label(new Rect(252, 510, 456, 25), "E - Encontrar a " + PrototypeSpeciesCatalog.Name(nearby.species), small);
            }
        }

        private void DrawExplorationRelics()
        {
            const float xStart = 280, y = 550, width = 72, gap = 8;
            var number = new GUIStyle(small) { fontSize = 12 };
            for (int i = 0; i < 5; i++)
            {
                float x = xStart + i * (width + gap);
                string name = i < relics.Count ? PrototypeRelicLine.Name(relics[i]) : "Espacio vacio";
                if (GUI.Button(new Rect(x, y, width, 44), new GUIContent("", name))) OpenRelics();
                GUI.Label(new Rect(x + 5, y + 3, 18, 18), (i + 1).ToString(), number);
                if (i < relics.Count) RelicPortrait(relics[i], new Rect(x + 24, y + 6, 30, 30));
                else GUI.Label(new Rect(x + 31, y + 12, 25, 22), "-", number);
            }
        }

        private void DrawBattle()
        {
            PrototypeTeamMember member = team[activeIndex];
            GUI.DrawTexture(new Rect(0, 0, 960, 600), background);
            GUI.Label(new Rect(30, 20, 740, 42), (debugTestEncounter ? "DEBUG contra " : "Encuentro con ") + PrototypeSpeciesCatalog.Name(target.species), title);
            GUI.Label(new Rect(770, 28, 160, 30), "Ronda " + battle.Round, label);
            portraits.TryGetValue(member.Species, out Sprite portrait);
            if (PrototypeContent.Spirit(member.Species).sprite != null) portrait = PrototypeContent.Spirit(member.Species).sprite;
            Portrait(portrait, new Rect(135, 78, 180, 170));
            Portrait(target.portrait, new Rect(645, 78, 180, 170));
            GUI.Label(new Rect(70, 255, 400, 30), member.DisplayName + "  " + battle.AllyHealth + "/" + member.MaxHealth, label);
            GUI.Label(new Rect(565, 255, 365, 30), PrototypeSpeciesCatalog.Name(target.species) + "  " + battle.EnemyHealth + "/" + battle.EnemyMaxHealth, label);
            GUI.Label(new Rect(565, 288, 365, 28), battle.Exposed ? (battle.Capturable ? "EXPUESTO - Vinculo " + battle.CaptureChance + "%" : "GUARDIA ROTA")
                : "Estabilidad " + battle.Stability + "/" + battle.EnemyMaxStability, label);
            GUI.Label(new Rect(70, 288, 390, 28), "Sellos " + battle.Seals + "     Equipo " + team.Count + "/4", label);
            if (battle.Outcome == PrototypeOutcome.Ongoing && GUI.Button(new Rect(70, 320, 280, 30), "5 - Cambiar espiritu", small)) OpenTeam(false);
            GUI.Label(new Rect(30, 355, 900, 43), battle.Message, small);
            DrawRelicStrip(400);
            if (battle.Outcome != PrototypeOutcome.Ongoing)
            {
                string text = battle.Outcome == PrototypeOutcome.Defeat
                    ? TrialFailed != null ? "Enter - Volver al refugio" : "Enter - Reiniciar prueba"
                    : "Enter - Volver a explorar";
                if (GUI.Button(new Rect(230, 500, 500, 66), text, button)) Finish();
                return;
            }
            if (GUI.Button(new Rect(30, 486, 210, 80), "1 Atacar\n" + battle.PredictedAttackDamage + " dano / " + member.Rupture + " ruptura", button)) Act(PrototypeAction.Attack);
            if (GUI.Button(new Rect(260, 486, 210, 80), "2 Contener\n1 dano / " + battle.PredictedContainment + " ruptura", button)) Act(PrototypeAction.Contain);
            if (GUI.Button(new Rect(490, 486, 210, 80), "3 Defender\nReduce el golpe", button)) Act(PrototypeAction.Defend);
            bool previous = GUI.enabled;
            GUI.enabled = previous && battle.Capturable && battle.Exposed && battle.Seals > 0 && team.Count < 4;
            if (GUI.Button(new Rect(720, 486, 210, 80), battle.Capturable ? "4 Vincular\nConsume un sello" : "Jefe\nNo capturable", button)) Act(PrototypeAction.Capture);
            GUI.enabled = previous;
            GUI.Label(new Rect(30, 572, 900, 25), "Proximo ataque: " + battle.PredictedAttackTrace, small);
        }

        private void RelicPortrait(PrototypeRelic relic, Rect rect)
        {
            var definition = PrototypeContent.Relic((int)relic);
            if (definition.icon != null) { Portrait(definition.icon, rect); return; }
            int index = (int)relic;
            if (relicPortraits != null && index < relicPortraits.Length && relicPortraits[index] != null)
                Portrait(relicPortraits[index], rect);
            else GUI.Label(rect, new GUIContent(Symbol(relic), PrototypeRelicLine.Name(relic)), label);
        }

        private static string Symbol(PrototypeRelic value)
        {
            switch (value)
            {
                case PrototypeRelic.Edge: return "+2";
                case PrototypeRelic.Shell: return "-1";
                case PrototypeRelic.Knot: return "N";
                case PrototypeRelic.Dawn: return "+3";
                default: return "*";
            }
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
            GUI.DrawTexture(new Rect(40, 70, 880, 510), background);
            GUI.Label(new Rect(65, 90, 820, 45), "Ordena tus reliquias", title);
            GUI.Label(new Rect(65, 140, 820, 45), "Se calculan de izquierda a derecha. La fila se bloquea al combatir.", small);
            for (int i = 0; i < relics.Count; i++)
            {
                float y = 183 + i * 62;
                RelicPortrait(relics[i], new Rect(65, y, 42, 42));
                GUI.Label(new Rect(135, y, 540, 27), (i + 1) + ". " + PrototypeRelicLine.Name(relics[i]), label);
                GUI.Label(new Rect(135, y + 25, 555, 36), relics.Description(i), small);
                bool previous = GUI.enabled;
                GUI.enabled = previous && i > 0;
                if (GUI.Button(new Rect(715, y + 3, 65, 40), "<", button)) { relics.Move(i, -1); StateChanged?.Invoke(); GUI.enabled = previous; break; }
                GUI.enabled = previous && i + 1 < relics.Count;
                if (GUI.Button(new Rect(795, y + 3, 65, 40), ">", button)) { relics.Move(i, 1); StateChanged?.Invoke(); GUI.enabled = previous; break; }
                GUI.enabled = previous;
            }
            relics.Damage(10, true, 4, 20, out string example);
            GUI.Label(new Rect(65, 503, 820, 26), "Ejemplo Brasa contra enemigo herido: " + example, small);
            if (GUI.Button(new Rect(600, 539, 260, 30), "Esc / R - Volver", button)) CloseRelics();
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
            if (PrototypeContent.Spirit(member.Species).sprite != null) portrait = PrototypeContent.Spirit(member.Species).sprite;
                Portrait(portrait, new Rect(125, y, 58, 58));
                GUI.Label(new Rect(198, y, 320, 28), member.DisplayName + "  " + member.Health + "/" + member.MaxHealth, label);
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
