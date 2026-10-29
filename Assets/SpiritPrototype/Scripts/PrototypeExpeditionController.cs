using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SpiritPrototype
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class PrototypeExpeditionController : MonoBehaviour
    {
        [SerializeField] private PrototypeEncounterController controller;
        [SerializeField] private PlayerMovement player;
        [SerializeField] private Transform door;
        [SerializeField] private Transform sanctuary;
        [SerializeField] private SpiritEncounter[] allEncounters;
        [SerializeField] private SpiritEncounter[] firstRoom;
        [SerializeField] private SpiritEncounter[] secondRoom;
        [SerializeField] private SpiritEncounter boss;
        public event System.Action StateChanged;
        public event System.Action ReturnToHubRequested;
        public bool IsComplete => completed;
        public bool CanSuspend => started && !completed && !rewardOpen && !routeOpen && controller.CanExplore &&
            Time.frameCount != rewardClosedFrame && Time.frameCount != controller.MenuClosedFrame;
        private int rewardClosedFrame = -1;
        private bool generated = true, routeOpen;
        private PrototypeFloorPlan floorPlan;
        private PrototypeFloorGeometry geometry;
        private Transform oldPillar, oldUrn;
        public string EncounterKey(SpiritEncounter enemy) { return generated ? floorPlan.EncounterKey(enemy.species) : enemy.species; }
        private bool SanctuaryRoom => generated ? floorPlan.Current.kind == PrototypeRoomKind.Sanctuary : room == 2;
        private bool BossRoom => generated ? floorPlan.Current.kind == PrototypeRoomKind.Boss : room == 3;
        private bool CombatRoom => !SanctuaryRoom && !BossRoom;
        private Transform chest;
        private SpriteRenderer chestSprite;
        private PrototypeChestReward chestReward = new PrototypeChestReward();
        private readonly System.Random rewardRandom = new System.Random();
        private bool rewardOpen;
        private int rewardChoice = -1;
        private int room, victories, captures;
        private bool started, completed, rewarded, sanctuaryUsed;
        private float startedAt;
        private GUIStyle text, title, button;
        private Texture2D panel;
        private readonly string[] names = {"Sala de los ecos", "Camara de las raices", "Santuario", "Guardian del Umbral"};
        private string notice = "";

        private void Start()
        {
            if (controller != null && !controller.enabled)
            { Debug.LogError("La expedicion no se inicio: revisa los errores anteriores del catalogo o del controlador.", this); enabled = false; return; }
            if (controller == null || player == null || door == null || sanctuary == null || boss == null ||
                allEncounters == null || firstRoom == null || firstRoom.Length != 2 || secondRoom == null || secondRoom.Length != 2)
            { Debug.LogError("Configura la expedicion con Tools > Espiritus > 6.", this); enabled = false; return; }
            allEncounters = controller.CatalogEncounters;
            chest = transform.Find("Objetos/Chest");
            if (chest != null) chestSprite = chest.GetComponent<SpriteRenderer>();
            else Debug.LogWarning("No se encontro Objetos/Chest. Conserva el nombre del cofre de la habitacion original.", this);
            geometry = new PrototypeFloorGeometry(transform);
            oldPillar = transform.Find("Objetos/Pillar"); oldUrn = transform.Find("Objetos/Urn");
            controller.EncounterResolved += Resolved;
            controller.TrialRestarted += Restarted;
            started = true;
            Restarted();
        }

        private void OnDestroy()
        {
            if (controller != null)
            {
                controller.EncounterResolved -= Resolved;
                controller.TrialRestarted -= Restarted;
            }
            if (panel != null) Destroy(panel);
        }

        private void Restarted()
        {
            var pool = new List<string>();
            foreach (var enemy in allEncounters) if (enemy != null && PrototypeContent.Spirit(enemy.species).spawnInDungeon) pool.Add(enemy.species);
            floorPlan = PrototypeFloorPlan.Generate(UnityEngine.Random.Range(0, int.MaxValue), pool);
            generated = true; routeOpen = false;
            rewardOpen = false; rewardChoice = -1;
            room = 0; victories = 0; captures = 0; completed = false;
            sanctuaryUsed = false; startedAt = Time.unscaledTime;
            notice = "Resuelve los dos encuentros para abrir la salida.";
            controller.SetExternalPause(false);
            ApplyRoom();
        }

        private SpiritEncounter[] CurrentEnemies()
        {
            if (generated)
            {
                var active = new List<SpiritEncounter>();
                foreach (string species in floorPlan.Current.enemies)
                    foreach (var actor in allEncounters)
                        if (actor != null && actor.species == species) { active.Add(actor); break; }
                return active.ToArray();
            }
            if (room == 0) return firstRoom;
            if (room == 1) return secondRoom;
            if (room == 3) return new[] {boss};
            return new SpiritEncounter[0];
        }

        private bool Cleared()
        {
            foreach (var enemy in CurrentEnemies())
                if (enemy != null && enemy.gameObject.activeSelf) return false;
            return true;
        }

        private void ApplyRoom()
        {
            rewarded = false;
            chestReward = new PrototypeChestReward();
            rewardChoice = -1;
            if (generated) sanctuaryUsed = false;
            if (chest != null) chest.gameObject.SetActive(CombatRoom);
            sanctuary.gameObject.SetActive(SanctuaryRoom);
            if (oldPillar != null) oldPillar.gameObject.SetActive(!generated);
            if (oldUrn != null) oldUrn.gameObject.SetActive(!generated);
            geometry.Apply(generated ? floorPlan.Current : null);
            foreach (var enemy in allEncounters) if (enemy != null) enemy.gameObject.SetActive(false);
            SpiritEncounter[] active = CurrentEnemies();
            for (int i = 0; i < active.Length; i++)
            {
                active[i].gameObject.SetActive(true);
                active[i].transform.position = active.Length == 1 ? new Vector3(1, 1, 0)
                    : i == 0 ? new Vector3(2, 1, 0) : new Vector3(3, -2, 0);
            }
            Color[] colors = {Color.white, new Color(.8f, 1f, .85f), new Color(.75f, .95f, 1f), new Color(1f, .8f, .8f)};
            Transform floor = transform.Find("Suelo");
            if (floor != null)
                foreach (var sprite in floor.GetComponentsInChildren<SpriteRenderer>()) sprite.color = generated ? SanctuaryRoom ? new Color(.8f, 1f, .9f) : BossRoom ? new Color(1f, .8f, .8f) : Color.white : colors[room];
            controller.WarpPlayer(generated ? new Vector3(0, -3, 0) : new Vector3(-2, -2, 0));
            PaintDoor();
        }

        private void PaintDoor()
        {
            var sprite = door.GetComponent<SpriteRenderer>();
            if (sprite != null) sprite.color = Cleared() ? new Color(.5f, 1f, 1f) : Color.white;
            PaintChest();
        }

        private void PaintChest()
        {
            if (chestSprite != null) chestSprite.color = chestReward.Consumed
                ? new Color(.4f, .4f, .4f) : Cleared() ? new Color(.5f, 1f, .7f) : Color.white;
        }

        private void Resolved(SpiritEncounter enemy, bool captured)
        {
            bool belongs = false;
            foreach (var current in CurrentEnemies()) if (current == enemy) belongs = true;
            if (!belongs) return;
            victories++; if (captured) captures++;
            if (!Cleared()) return;
            if (BossRoom)
            {
                completed = true;
                controller.SetExternalPause(true);
                notice = "Has derrotado al Guardian del Umbral.";
                return;
            }
            if (!rewarded)
            {
                controller.GrantSeals(2); rewarded = true;
                notice = "Sala completada. Recibes 2 sellos. Abre el cofre o avanza por la puerta norte.";
            }
            PaintDoor();
        }

        private void Update()
        {
            if (routeOpen)
            {
                if (CancelReward()) { routeOpen = false; rewardClosedFrame = Time.frameCount; controller.SetExternalPause(false); }
                return;
            }
            if (rewardOpen)
            {
                if (CancelReward()) { CloseReward(); return; }
                return;
            }
            if (!started || completed || !controller.CanExplore || !Interact()) return;
            if (CombatRoom && chest != null && Near(chest, 1.65f))
            {
                if (Cleared() && !chestReward.Consumed)
                {
                    chestReward.Open(controller.Relics, generated ? new System.Random(floorPlan.Current.rewardSeed) : rewardRandom);
                    rewardOpen = true; rewardChoice = -1;
                    controller.SetExternalPause(true);
                    StateChanged?.Invoke();
                }
                return;
            }
            if (SanctuaryRoom && !sanctuaryUsed && Near(sanctuary, 1.65f))
            {
                controller.RestoreTeam(); sanctuaryUsed = true;
                notice = "El santuario cura y reanima a todo el equipo. Solo puedes usarlo una vez.";
                StateChanged?.Invoke();
                return;
            }
            if (!Near(door, 1.65f)) return;
            if (!Cleared()) { notice = "La puerta sigue cerrada. Resuelve los encuentros pendientes."; return; }
            if (generated)
            {
                if (floorPlan.Current.exits.Count == 1) Advance(floorPlan.Current.exits[0]);
                else if (floorPlan.Current.exits.Count > 1) { routeOpen = true; controller.SetExternalPause(true); }
                return;
            }
            if (room >= 3) return;
            room++;
            notice = room == 2 ? "Acercate al circulo de piedra y pulsa E para recuperar el equipo."
                : room == 3 ? "El jefe no es capturable. Romper su estabilidad cancela una respuesta."
                : "Nuevos encuentros. La vida, el equipo, los sellos y las reliquias se conservan.";
            ApplyRoom();
            StateChanged?.Invoke();
        }

        private void Advance(int id)
        {
            if (!floorPlan.MoveTo(id)) return;
            room = floorPlan.Current.depth;
            routeOpen = false; controller.SetExternalPause(false);
            rewardClosedFrame = Time.frameCount;
            ApplyRoom();
            StateChanged?.Invoke();
        }

        private void CloseReward()
        {
            rewardClosedFrame = Time.frameCount;
            rewardOpen = false; rewardChoice = -1;
            controller.SetExternalPause(false);
            PaintChest();
            StateChanged?.Invoke();
        }

        public PrototypeRunSave Capture()
        {
            var state = new PrototypeRunSave {generatedFloor = generated, floor = generated ? floorPlan : null, room = room, victories = victories, captures = captures,
                sanctuaryUsed = sanctuaryUsed, rewarded = rewarded};
            foreach (var enemy in CurrentEnemies())
                if (enemy != null && !enemy.gameObject.activeSelf) state.defeated.Add(enemy.species);
            chestReward.WriteTo(state);
            controller.WriteTo(state);
            return state;
        }

        public void Restore(PrototypeRunSave state)
        {
            generated = state.generatedFloor; floorPlan = generated ? state.floor : null; routeOpen = false;
            room = state.room; victories = state.victories; captures = state.captures;
            sanctuaryUsed = state.sanctuaryUsed; completed = false;
            rewardOpen = false; rewardChoice = -1;
            ApplyRoom();
            rewarded = state.rewarded; sanctuaryUsed = state.sanctuaryUsed;
            foreach (var enemy in CurrentEnemies())
                if (enemy != null && state.defeated.Contains(enemy.species)) enemy.gameObject.SetActive(false);
            chestReward.Restore(state);
            controller.Restore(state);
            PaintDoor();
        }

        private static bool CancelReward()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Escape);
#else
            return false;
#endif
        }

        private bool Near(Transform target, float distance)
        { return (target.position - player.transform.position).sqrMagnitude <= distance * distance; }

        private static bool Interact()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.E);
#else
            return false;
#endif
        }

        private void DrawReward()
        {
            GUI.DrawTexture(new Rect(40, 100, 880, 450), panel);
            GUI.Label(new Rect(65, 120, 830, 45), rewardChoice < 0 ? "Elige una reliquia" : "Elige que reliquia reemplazar", title);
            if (rewardChoice >= 0)
            {
                var selected = chestReward.Options[rewardChoice];
                GUI.Label(new Rect(65, 170, 830, 50), "Recibiras: " + PrototypeRelicLine.Name(selected) + ". El reemplazo conserva su posicion.", text);
                for (int i = 0; i < controller.Relics.Count; i++)
                {
                    if (GUI.Button(new Rect(80, 225 + i * 46, 800, 40), (i + 1) + ". " +
                        PrototypeRelicLine.Name(controller.Relics[i]), button))
                    {
                        if (chestReward.Claim(controller.Relics, rewardChoice, i)) CloseReward();
                        return;
                    }
                }
                if (GUI.Button(new Rect(80, 490, 360, 40), "Volver a las opciones", button)) rewardChoice = -1;
            }
            else
            {
                GUI.Label(new Rect(65, 172, 830, 40), chestReward.Options.Count == 0 ? "No quedan reliquias disponibles en el catalogo para este equipo." : "Elige una. Las otras se descartan. La recompensa no se repite al reabrir.", text);
                for (int i = 0; i < chestReward.Options.Count; i++)
                {
                    var relic = chestReward.Options[i];
                    float x = 65 + i * 280;
                    GUI.Box(new Rect(x, 225, 270, 240), GUIContent.none);
                    GUI.Label(new Rect(x + 12, 238, 246, 55), PrototypeRelicLine.Name(relic), new GUIStyle(text) {fontSize = 22});
                    GUI.Label(new Rect(x + 12, 300, 246, 100), PrototypeRelicLine.Effect(relic), text);
                    if (GUI.Button(new Rect(x + 15, 410, 240, 40), "Elegir", button))
                    {
                        if (controller.Relics.Count >= 5) rewardChoice = i;
                        else if (chestReward.Claim(controller.Relics, i)) CloseReward();
                        return;
                    }
                }
                if (GUI.Button(new Rect(65, 490, 390, 40), "Rechazar recompensa", button))
                { chestReward.Decline(); CloseReward(); return; }
            }
            if (GUI.Button(new Rect(495, 490, 390, 40), "Esc - Decidir despues", button)) CloseReward();
        }

        private void DrawRoutes()
        {
            GUI.DrawTexture(new Rect(55, 120, 850, 380), panel);
            GUI.Label(new Rect(80, 140, 800, 50), "Elige la siguiente sala", title);
            for (int i = 0; i < floorPlan.Current.exits.Count; i++)
            {
                int id = floorPlan.Current.exits[i]; var next = floorPlan.rooms[id];
                float x = 85 + i * 405;
                GUI.Label(new Rect(x, 215, 380, 45), next.Title, title);
                GUI.Label(new Rect(x, 270, 375, 90), next.Preview, text);
                if (GUI.Button(new Rect(x, 370, 375, 55), "Entrar", button)) { Advance(id); return; }
            }
            if (GUI.Button(new Rect(285, 445, 390, 40), "Esc - Quedarme en esta sala", button))
            { routeOpen = false; rewardClosedFrame = Time.frameCount; controller.SetExternalPause(false); }
        }

        private void OnGUI()
        {
            if (!started || (!completed && !rewardOpen && !routeOpen && !controller.CanExplore)) return;
            if (text == null)
            {
                var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text = new GUIStyle(GUI.skin.label) {font = font, fontSize = 17, wordWrap = true};
                text.normal.textColor = Color.white;
                title = new GUIStyle(text) {fontSize = 28, fontStyle = FontStyle.Bold};
                button = new GUIStyle(GUI.skin.button) {font = font, fontSize = 20};
                panel = new Texture2D(1, 1); panel.SetPixel(0, 0, new Color(.025f, .045f, .075f, .98f)); panel.Apply();
            }
            Matrix4x4 old = GUI.matrix;
            float scale = Mathf.Max(.01f, Mathf.Min(Screen.width / 960f, Screen.height / 600f));
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 960 * scale) / 2,
                (Screen.height - 600 * scale) / 2, 0), Quaternion.identity, new Vector3(scale, scale, 1));
            try
            {
                if (routeOpen) { DrawRoutes(); return; }
                if (rewardOpen) { DrawReward(); return; }
                if (completed)
                {
                    GUI.DrawTexture(new Rect(90, 120, 780, 350), panel);
                    GUI.Label(new Rect(130, 155, 700, 50), "Expedicion completada", title);
                    GUI.Label(new Rect(130, 225, 700, 90), notice + "\nEncuentros resueltos: " + victories +
                        "   Capturas: " + captures, text);
                    if (GUI.Button(new Rect(290, 365, 380, 65), ReturnToHubRequested != null ? "Volver al refugio" : "Comenzar otra expedicion", button))
                    {
                        if (ReturnToHubRequested != null) ReturnToHubRequested.Invoke();
                        else controller.RestartTrial();
                    }
                    return;
                }
                // One slim objective row, below the player status bar.
                var compact = new GUIStyle(text) { fontSize = 14, wordWrap = false };
                GUI.DrawTexture(new Rect(12, 48, 936, 32), panel);
                GUI.Label(new Rect(24, 53, 410, 23), generated ? "Piso 1 | Sala " + (room + 1) + "/5 - " + floorPlan.Current.Title : "Sala " + (room + 1) + "/4 - " + names[room], compact);
                int remaining = 0;
                foreach (var enemy in CurrentEnemies())
                    if (enemy != null && enemy.gameObject.activeSelf) remaining++;
                string objective = SanctuaryRoom ? sanctuaryUsed ? "Equipo recuperado. Ve a la puerta norte."
                    : "E junto al santuario para recuperar al equipo."
                    : remaining == 0 ? CombatRoom && !chestReward.Consumed ? "Cofre disponible. E junto al cofre o la salida." : "Salida abierta: E junto a la puerta norte."
                    : BossRoom ? "Derrota al jefe. Rompe su guardia para frenarlo."
                    : "Encuentros pendientes: " + remaining + "/2";
                GUI.Label(new Rect(450, 53, 480, 23), objective, compact);
                string prompt = CombatRoom && chest != null && Near(chest, 1.65f)
                    ? !Cleared() ? "Cofre bloqueado: resuelve los encuentros"
                    : chestReward.Consumed ? "Cofre agotado" : "E - Elegir una reliquia del cofre"
                    : SanctuaryRoom && Near(sanctuary, 1.65f) && !sanctuaryUsed
                    ? "E - Recuperar al equipo" : Near(door, 1.65f)
                    ? Cleared() ? "E - Avanzar a la siguiente sala" : "Salida bloqueada" : "";
                if (prompt.Length > 0)
                {
                    GUI.DrawTexture(new Rect(240, 506, 480, 32), panel);
                    GUI.Label(new Rect(252, 510, 456, 25), prompt, text);
                }
            }
            finally { GUI.matrix = old; }
        }
    }
}
