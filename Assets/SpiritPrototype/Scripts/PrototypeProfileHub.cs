using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SpiritPrototype
{
    [DefaultExecutionOrder(200)]
    [DisallowMultipleComponent]
    public sealed class PrototypeProfileHub : MonoBehaviour
    {
        [SerializeField] private GameObject dungeonRoot;
        [SerializeField] private GameObject hubRoot;
        [SerializeField] private PrototypeEncounterController controller;
        [SerializeField] private PrototypeExpeditionController expedition;
        [SerializeField] private PlayerMovement hubPlayer;
        [SerializeField] private Transform portal;
        [SerializeField] private Transform museum;
        private enum View { Profiles, Hub, Portal, Museum, Expedition, Pause, ConfirmNew }
        private View view;
        private PrototypeProfileStore store;
        private PrototypeProfileData profile;
        private readonly PrototypeProfileData[] slots = new PrototypeProfileData[3];
        private readonly string[] slotNotice = new string[3];
        private readonly bool[] blocked = new bool[3];
        private readonly string[] newNames = {"Explorador 1", "Explorador 2", "Explorador 3"};
        private readonly Dictionary<string, Sprite> portraits = new Dictionary<string, Sprite>();
        private Vector2 museumScroll;
        private int selectedSlot = -1, codexIndex;
        private bool ready, restoring;
        public bool DebugActive { get; private set; }
        private bool debugPanelOpen;
        private PrototypeProfileData normalProfile;
        public PrototypeEncounterController DebugController => controller;
        public bool DebugCanOpen => ready && profile != null && (view == View.Hub || view == View.Expedition && controller.DebugCanOpen && !expedition.IsComplete);
        public string DebugAccessMessage => !ready || profile == null ? "Elige primero un perfil."
            : !DebugCanOpen ? "Cierra el menu actual o termina de elegir un reemplazo antes de abrir DEBUG."
            : status;
        public bool DebugInExpedition => view == View.Expedition;
        public bool BeginDebugSession()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (DebugActive) return DebugCanOpen;
            if (!DebugCanOpen) return false;
            bool fromHub = view == View.Hub;
            string opponent = !fromHub && !controller.CanCheckpoint ? controller.DebugCurrentSpecies : null;
            if (fromHub) { if (!Persist()) return false; }
            else if (controller.CanCheckpoint && !SaveCheckpoint()) return false;
            var copy = store.Load(selectedSlot, out string notice, out bool blocked);
            if (blocked || copy == null || !fromHub && copy.run == null)
            { status = "No se pudo preparar DEBUG. " + notice; return false; }
            normalProfile = profile; profile = copy;
            DebugActive = true; controller.DebugEnabled = true;
            if (fromHub)
            {
                if (copy.run == null) NewRun();
                else ContinueRun();
            }
            else
            {
                restoring = true;
                expedition.Restore(copy.run); // Detach the live floor graph from the normal profile.
                restoring = false;
                if (opponent != null) controller.DebugRestartWorldFight(opponent);
            }
            status = opponent != null ? "DEBUG: combate reiniciado desde el ultimo guardado, en una copia temporal."
                : "DEBUG: pruebas temporales, guardado real suspendido.";
            return true;
#else
            return false;
#endif
        }
        public void DebugOverlay(bool open)
        {
            debugPanelOpen = open; controller.SetDebugOverlay(open);
            if (view == View.Hub) hubPlayer.enabled = !open;
        }
        public void EndDebugSession()
        {
            if (!DebugActive) return;
            DebugOverlay(false); controller.DebugResetTransient(); controller.DebugEnabled = false;
            profile = normalProfile; normalProfile = null; DebugActive = false;
            ShowHub(); status = "Depuracion finalizada. Tu expedicion real sigue guardada en el portal.";
        }
        private string status = "";
        private GUIStyle label, title, small, button;
        private Texture2D panel;

        private void Start()
        {
            if (dungeonRoot == null || hubRoot == null || controller == null || expedition == null ||
                hubPlayer == null || portal == null || museum == null || !controller.enabled || !expedition.enabled)
            { Debug.LogError("No se iniciaron los perfiles. Revisa las referencias de la escena y los errores anteriores de Console.", this); enabled = false; return; }
            store = new PrototypeProfileStore(Path.Combine(Application.persistentDataPath, "SpiritPrototype", "Profiles"));
            foreach (var sprite in dungeonRoot.GetComponentsInChildren<SpriteRenderer>(true))
                if (PrototypeSpeciesCatalog.Known(sprite.name)) portraits[sprite.name] = sprite.sprite;
            controller.EncounterBegan += EncounterBegan;
            controller.EncounterResolved += EncounterResolved;
            controller.StateChanged += Changed;
            controller.TrialFailed += Failed;
            expedition.StateChanged += Changed;
            expedition.ReturnToHubRequested += ShowHub;
            ready = true;
            controller.SetExternalPause(false);
            dungeonRoot.SetActive(false);
            hubRoot.SetActive(true);
            SetView(View.Profiles);
            ReloadProfiles();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var debug = gameObject.GetComponent<PrototypeDebugPanel>();
            if (debug == null) debug = gameObject.AddComponent<PrototypeDebugPanel>();
            debug.Configure(this, controller);
#endif
        }

        private void ReloadProfiles()
        {
            for (int i = 0; i < 3; i++) slots[i] = store.Load(i, out slotNotice[i], out blocked[i]);
        }

        private void SetView(View next)
        {
            view = next;
            hubPlayer.enabled = next == View.Hub;
        }

        private bool Persist()
        {
            if (DebugActive) { status = "DEBUG: cambios temporales; no se escriben en el perfil real."; return true; }
            if (profile == null || selectedSlot < 0) return false;
            try { store.Save(selectedSlot, profile); status = "Progreso guardado."; return true; }
            catch (Exception error) when (error is IOException || error is InvalidDataException || error is UnauthorizedAccessException || error is ArgumentException || error is NotSupportedException)
            {
                status = "No se pudo guardar. " + error.Message;
                Debug.LogError(status, this);
                return false;
            }
        }

        private void SelectProfile(int slot)
        {
            if (blocked[slot]) return;
            var chosen = slots[slot] ?? PrototypeProfileData.Create(newNames[slot]);
            if (!chosen.IsValid()) { status = "Escribe un nombre de 1 a 24 caracteres."; return; }
            selectedSlot = slot; profile = chosen;
            if (slots[slot] == null && !Persist()) { selectedSlot = -1; profile = null; return; }
            slots[slot] = profile;
            status = slotNotice[slot].Length > 0 ? slotNotice[slot] : "Bienvenido a tu refugio.";
            ShowHub();
        }

        private void ShowHub()
        {
            controller.SetExternalPause(false);
            dungeonRoot.SetActive(false);
            hubRoot.SetActive(true);
            hubPlayer.transform.position = new Vector3(-1, -2, 0);
            var body = hubPlayer.GetComponent<Rigidbody2D>();
            body.position = new Vector2(-1, -2); body.linearVelocity = Vector2.zero;
            SetView(View.Hub);
        }

        private void SwitchProfiles()
        {
            if (DebugActive) { EndDebugSession(); return; }
            if (!Persist()) return;
            profile = null; selectedSlot = -1;
            ReloadProfiles();
            SetView(View.Profiles);
        }

        private void NewRun()
        {
            restoring = true;
            hubRoot.SetActive(false);
            dungeonRoot.SetActive(true);
            SetView(View.Expedition);
            controller.RestartTrial();
            profile.expeditions++;
            profile.run = new PrototypeRunSave();
            restoring = false;
            SaveCheckpoint();
        }

        private void ContinueRun()
        {
            if (profile.run == null) return;
            restoring = true;
            hubRoot.SetActive(false); dungeonRoot.SetActive(true);
            SetView(View.Expedition);
            expedition.Restore(profile.run);
            restoring = false;
        }

        private bool SaveCheckpoint()
        {
            if (restoring || profile == null || profile.run == null || !controller.CanCheckpoint ||
                !dungeonRoot.activeSelf || expedition.IsComplete) return false;
            var seen = profile.run.seenThisRun;
            var state = expedition.Capture();
            state.seenThisRun = new List<string>(seen);
            profile.RecordProgress(state);
            profile.run = state;
            return Persist();
        }

        private void Changed()
        {
            if (!restoring && view == View.Expedition) SaveCheckpoint();
        }

        private void EncounterBegan(SpiritEncounter enemy)
        {
            if (profile != null && profile.RecordSeen(enemy.species, expedition.EncounterKey(enemy))) Persist();
        }

        private void EncounterResolved(SpiritEncounter enemy, bool captured)
        {
            if (profile == null || profile.run == null) return;
            if (captured) profile.Entry(enemy.species).captures++;
            if (expedition.IsComplete)
            {
                var state = expedition.Capture();
                profile.RecordProgress(state, true);
                profile.victories++;
                profile.run = null;
                Persist();
            }
            else SaveCheckpoint();
        }

        private void Failed()
        {
            if (profile == null) return;
            profile.defeats++; profile.run = null;
            Persist();
            ShowHub();
        }

        private void Update()
        {
            if (!ready) return;
            if (view == View.Expedition)
            {
                if (Escape() && expedition.CanSuspend)
                { controller.SetExternalPause(true); SetView(View.Pause); }
                return;
            }
            if (view == View.Pause)
            {
                if (Escape()) Resume();
                return;
            }
            if ((view == View.Portal || view == View.Museum || view == View.ConfirmNew) && Escape())
            { SetView(View.Hub); return; }
            if (view != View.Hub || !Interact()) return;
            if (Near(portal)) SetView(View.Portal);
            else if (Near(museum)) { codexIndex = 0; SetView(View.Museum); }
        }

        private void Resume() { controller.SetExternalPause(false); SetView(View.Expedition); }
        private bool Near(Transform station)
        { return (station.position - hubPlayer.transform.position).sqrMagnitude <= 1.65f * 1.65f; }

        private void OnApplicationQuit()
        {
            if (ready && (view == View.Expedition || view == View.Pause)) SaveCheckpoint();
        }
        private void OnApplicationPause(bool paused)
        {
            if (ready && paused && (view == View.Expedition || view == View.Pause)) SaveCheckpoint();
        }
        private void OnDestroy()
        {
            if (controller != null)
            {
                controller.EncounterBegan -= EncounterBegan;
                controller.EncounterResolved -= EncounterResolved;
                controller.StateChanged -= Changed;
                controller.TrialFailed -= Failed;
            }
            if (expedition != null)
            {
                expedition.StateChanged -= Changed;
                expedition.ReturnToHubRequested -= ShowHub;
            }
            if (panel != null) Destroy(panel);
        }

        private static bool Escape()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Escape);
#else
            return false;
#endif
        }
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

        private void Styles()
        {
            if (label != null) return;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label = new GUIStyle(GUI.skin.label) {font = font, fontSize = 20, wordWrap = true};
            label.normal.textColor = new Color(.9f, .94f, 1);
            small = new GUIStyle(label) {fontSize = 16};
            title = new GUIStyle(label) {fontSize = 30, fontStyle = FontStyle.Bold};
            button = new GUIStyle(GUI.skin.button) {font = font, fontSize = 19, wordWrap = true};
            panel = new Texture2D(1, 1);
            panel.SetPixel(0, 0, new Color(.025f, .04f, .065f, .98f)); panel.Apply();
        }

        private void OnGUI()
        {
            if (!ready) return;
            Styles();
            var previous = GUI.matrix;
            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && !debugPanelOpen;
            float scale = Mathf.Max(.01f, Mathf.Min(Screen.width / 960f, Screen.height / 600f));
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 960 * scale) / 2,
                (Screen.height - 600 * scale) / 2, 0), Quaternion.identity, new Vector3(scale, scale, 1));
            try
            {
                if (view == View.Expedition)
                {
                    if (controller.CanExplore) GUI.Label(new Rect(20, 568, 240, 26), "Esc - Pausa / volver al refugio", small);
                    return;
                }
                if (view == View.Hub) { DrawHub(); return; }
                GUI.DrawTexture(new Rect(35, 55, 890, 500), panel);
                if (view == View.Profiles) DrawProfiles();
                else if (view == View.Museum) DrawMuseum();
                else if (view == View.Portal) DrawPortal();
                else if (view == View.Pause) DrawPause();
                else DrawConfirmNew();
            }
            finally { GUI.matrix = previous; GUI.enabled = previousEnabled; }
        }

        private void DrawProfiles()
        {
            GUI.Label(new Rect(60, 75, 820, 45), "Elige tu perfil", title);
            GUI.Label(new Rect(60, 124, 820, 40), "Cada perfil tiene su propio museo y su expedicion guardada.", small);
            for (int i = 0; i < 3; i++)
            {
                float x = 60 + i * 285;
                GUI.Box(new Rect(x, 180, 270, 285), GUIContent.none);
                GUI.Label(new Rect(x + 15, 195, 240, 30), "Perfil " + (i + 1), label);
                if (slots[i] == null)
                {
                    if (!blocked[i]) newNames[i] = GUI.TextField(new Rect(x + 15, 240, 240, 34), newNames[i], 24);
                    GUI.Label(new Rect(x + 15, 290, 240, 110), blocked[i] ? slotNotice[i] : "Un nuevo refugio y un museo por descubrir.", small);
                }
                else
                {
                    var data = slots[i];
                    GUI.Label(new Rect(x + 15, 235, 240, 40), data.name, label);
                    GUI.Label(new Rect(x + 15, 280, 240, 120), "Expediciones: " + data.expeditions +
                        "\nVictorias: " + data.victories + "\n" + (data.bestFloor > 0 ? "Mejor: P" + data.bestFloor + " / S" + data.bestFloorRoom : "Sala legado: " + data.bestRoom + "/4") + "\n" +
                        (data.run != null ? "Expedicion pendiente" : "En el refugio"), small);
                }
                bool old = GUI.enabled; GUI.enabled = old && !blocked[i];
                if (GUI.Button(new Rect(x + 15, 412, 240, 40), slots[i] == null ? "Crear y entrar" : "Entrar al refugio", button))
                { SelectProfile(i); GUI.enabled = old; return; }
                GUI.enabled = old;
            }
            GUI.Label(new Rect(60, 485, 830, 55), status, small);
        }

        private void DrawHub()
        {
            GUI.DrawTexture(new Rect(12, 8, 936, 48), panel);
            GUI.Label(new Rect(25, 17, 620, 30), profile.name + "  |  Refugio  |  Victorias " + profile.victories, label);
            if (GUI.Button(new Rect(755, 17, 175, 30), DebugActive ? "Salir de DEBUG" : "Cambiar perfil", button)) { SwitchProfiles(); return; }
            GUI.DrawTexture(new Rect(12, 554, 936, 36), panel);
            GUI.Label(new Rect(24, 560, 910, 27), status, small);
            string prompt = Near(portal) ? "E - Portal de expediciones" : Near(museum) ? "E - Museo / codice de espiritus" : "WASD / flechas - Caminar";
            GUI.DrawTexture(new Rect(230, 505, 500, 34), panel);
            GUI.Label(new Rect(243, 510, 474, 27), prompt, small);
            // Labels are anchored to the stations rather than covering the room.
            StationLabel(portal, "PORTAL"); StationLabel(museum, "MUSEO");
        }

        private void StationLabel(Transform station, string name)
        {
            var camera = Camera.main;
            if (camera == null) return;
            Vector3 pixel = camera.WorldToScreenPoint(station.position + Vector3.up * 1.1f);
            float scale = Mathf.Max(.01f, Mathf.Min(Screen.width / 960f, Screen.height / 600f));
            float x = (pixel.x - (Screen.width - 960 * scale) / 2) / scale;
            float y = (Screen.height - pixel.y - (Screen.height - 600 * scale) / 2) / scale;
            GUI.Label(new Rect(x - 75, y - 15, 150, 30), name, new GUIStyle(small) {alignment = TextAnchor.MiddleCenter});
        }

        private void DrawPortal()
        {
            GUI.Label(new Rect(65, 80, 820, 45), "Portal de expediciones", title);
            GUI.Label(new Rect(65, 140, 820, 65), profile.run == null ? "No tienes una expedicion pendiente. Ascua te espera."
                : (profile.run.generatedFloor ? "Piso 1, sala " + (profile.run.room + 1) + "/5." : "Expedicion anterior, sala " + (profile.run.room + 1) + "/4.") + " Tu equipo conserva su vida y sus reliquias.", label);
            if (profile.run != null && GUI.Button(new Rect(220, 235, 520, 65), "Continuar expedicion", button)) { ContinueRun(); return; }
            if (GUI.Button(new Rect(220, 320, 520, 65), "Nueva expedicion", button))
            {
                if (profile.run == null) NewRun(); else SetView(View.ConfirmNew);
                return;
            }
            if (GUI.Button(new Rect(220, 440, 520, 45), "Esc - Volver al refugio", button)) SetView(View.Hub);
        }

        private void DrawConfirmNew()
        {
            GUI.Label(new Rect(65, 90, 820, 45), "Comenzar otra expedicion", title);
            GUI.Label(new Rect(85, 180, 790, 140), "Se abandonara la expedicion pendiente de este perfil.\n\nEl museo y tus estadisticas permanentes se conservaran.", label);
            if (GUI.Button(new Rect(85, 375, 370, 60), "Abandonar y comenzar otra", button)) { NewRun(); return; }
            if (GUI.Button(new Rect(495, 375, 370, 60), "Conservar la actual", button)) SetView(View.Portal);
        }

        private void DrawPause()
        {
            GUI.Label(new Rect(65, 85, 820, 45), "Expedicion en pausa", title);
            GUI.Label(new Rect(65, 155, 820, 85), "Puedes volver a casa y continuar esta expedicion desde el portal.\nDurante un combate se conserva el estado anterior al encuentro.", label);
            if (GUI.Button(new Rect(220, 270, 520, 60), "Esc - Seguir explorando", button)) { Resume(); return; }
            if (GUI.Button(new Rect(220, 350, 520, 60), "Guardar y volver al refugio", button))
            { if (SaveCheckpoint()) ShowHub(); return; }
            GUI.Label(new Rect(65, 450, 820, 70), status, small);
        }

        private void DrawMuseum()
        {
            GUI.Label(new Rect(65, 75, 820, 45), "Museo de espiritus", title);
            GUI.Label(new Rect(65, 123, 820, 35), "Historial permanente de " + profile.name + ". No almacena aliados para otras partidas.", small);
            museumScroll = GUI.BeginScrollView(new Rect(65, 175, 290, 300), museumScroll,
                new Rect(0, 0, 270, PrototypeSpeciesCatalog.Names.Length * 42));
            for (int i = 0; i < PrototypeSpeciesCatalog.Names.Length; i++)
            {
                string species = PrototypeSpeciesCatalog.Names[i];
                var entry = FindEntry(species);
                string name = entry != null && entry.seen > 0 ? PrototypeSpeciesCatalog.Name(species) : "???";
                if (GUI.Button(new Rect(0, i * 42, 270, 36), (i == codexIndex ? "> " : "") + name, button)) codexIndex = i;
            }
            GUI.EndScrollView();
            codexIndex = Mathf.Max(0, System.Math.Min(codexIndex, PrototypeSpeciesCatalog.Names.Length - 1));
            string selected = PrototypeSpeciesCatalog.Names[codexIndex];
            var record = FindEntry(selected);
            bool seen = record != null && record.seen > 0;
            bool captured = record != null && record.captures > 0;
            bool starter = selected == "Ascua";
            GUI.Label(new Rect(375, 170, 490, 40), seen ? PrototypeSpeciesCatalog.Name(selected) : "Espiritu desconocido", title);
            portraits.TryGetValue(selected, out Sprite sprite);
            if (PrototypeContent.Spirit(selected).FrameAt(Time.unscaledTime) != null) sprite = PrototypeContent.Spirit(selected).FrameAt(Time.unscaledTime);
            if (sprite != null)
            {
                Color old = GUI.color; if (!seen) GUI.color = Color.black;
                GUI.DrawTexture(new Rect(730, 220, 130, 130), sprite.texture, ScaleMode.ScaleToFit, true);
                GUI.color = old;
            }
            string discovery = captured ? "Capturado" : seen ? starter ? "Visto - aliado inicial" : "Visto" : "Desconocido";
            GUI.Label(new Rect(375, 225, 345, 210), "Estado: " + discovery +
                "\nRareza: " + (seen ? PrototypeSpeciesCatalog.Rarity(selected) : "???") +
                "\nEncuentros: " + (seen ? record.seen.ToString() : "-") +
                "\nCapturas: " + (seen ? record.captures.ToString() : "-") +
                "\nMejor avance: " + (record != null && record.bestFloor > 0 ? "P" + record.bestFloor + " / S" + record.bestFloorRoom : record != null && record.bestRoom > 0 ? "Legado " + record.bestRoom + "/4" : "-") +
                "\nVictorias en equipo: " + (record != null ? record.completions.ToString() : "-"), label);
            if (captured || starter)
            {
                var stats = PrototypeTeamMember.Create(selected, 1);
                GUI.Label(new Rect(375, 425, 490, 50), "Vida base " + stats.MaxHealth + " | Ataque " + stats.Damage + " | Contencion " + stats.Containment, small);
            }
            if (GUI.Button(new Rect(570, 490, 300, 40), "Esc - Volver al refugio", button)) SetView(View.Hub);
        }

        private PrototypeCodexEntry FindEntry(string species)
        {
            foreach (var entry in profile.codex) if (entry.species == species) return entry;
            return null;
        }
    }
}
