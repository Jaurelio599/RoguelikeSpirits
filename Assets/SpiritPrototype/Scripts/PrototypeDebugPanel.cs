using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SpiritPrototype
{
    [DefaultExecutionOrder(300)]
    public sealed class PrototypeDebugPanel : MonoBehaviour
    {
        private PrototypeProfileHub hub;
        private PrototypeEncounterController controller;
        private bool open;
        private Vector2 enemyScroll, allyScroll, relicScroll;
        private int tab, teamSlot, relicSlot;
        private int lastToggleFrame = -1;
        private string accessFeedback = "";
        private float feedbackUntil;
        private string pickedAlly = "Velo", pickedEnemy = "Velo", message = "";
        private PrototypeRelic pickedRelic = PrototypeRelic.Edge;
        private GUIStyle label, title, button, small;
        private Texture2D panel;
        public void Configure(PrototypeProfileHub hub, PrototypeEncounterController controller)
        {
            this.hub = hub; this.controller = controller;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            enabled = true;
#endif
        }
        private void Awake()
        {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            enabled = false;
#endif
        }
        private bool EnsureConfigured()
        {
            if (hub == null) hub = GetComponent<PrototypeProfileHub>();
            if (controller == null && hub != null) controller = hub.DebugController;
            return hub != null && controller != null;
        }
        private void TryToggle()
        {
            if (lastToggleFrame == Time.frameCount) return;
            lastToggleFrame = Time.frameCount;
            if (open) { Close(); return; }
            if (hub.BeginDebugSession()) { open = true; hub.DebugOverlay(true); accessFeedback = ""; }
            else { accessFeedback = hub.DebugAccessMessage; feedbackUntil = Time.unscaledTime + 8; }
        }
        private void Update()
        {
            if (!EnsureConfigured()) return;
            if (Toggle()) TryToggle();
            if (open && Escape()) Close();
        }
        private void Close() { open = false; hub.DebugOverlay(false); }
        private void OnDestroy() { if (panel != null) Destroy(panel); }
        private static bool Toggle()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.F2);
#else
            return false;
#endif
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
        private void Styles()
        {
            if (label != null) return;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label = new GUIStyle(GUI.skin.label) {font = font, fontSize = 18, wordWrap = true}; label.normal.textColor = Color.white;
            title = new GUIStyle(label) {fontSize = 25, fontStyle = FontStyle.Bold};
            small = new GUIStyle(label) {fontSize = 14};
            button = new GUIStyle(GUI.skin.button) {font = font, fontSize = 16, wordWrap = true};
            panel = new Texture2D(1, 1); panel.SetPixel(0, 0, new Color(.04f, .035f, .065f, .99f)); panel.Apply();
        }
        private void OnGUI()
        {
            if (!EnsureConfigured()) return;
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.F2)
            { TryToggle(); Event.current.Use(); }
            Styles();
            var matrix = GUI.matrix; int depth = GUI.depth; bool enabledBefore = GUI.enabled;
            float scale = Mathf.Max(.01f, Mathf.Min(Screen.width / 960f, Screen.height / 600f));
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 960 * scale) / 2,
                (Screen.height - 600 * scale) / 2, 0), Quaternion.identity, new Vector3(scale, scale, 1));
            GUI.depth = -1000; GUI.enabled = true;
            try
            {
                if (!open)
                {
                    Rect accessRect = !controller.CanCheckpoint && controller.DebugCanOpen ? new Rect(650, 320, 260, 30) : new Rect(15, 513, 210, 35);
                    if (GUI.Button(accessRect, hub.DebugActive ? "Abrir DEBUG (temporal)" : "DEBUG / F2", button)) TryToggle();
                    if (accessFeedback.Length > 0 && Time.unscaledTime < feedbackUntil)
                    {
                        GUI.DrawTexture(new Rect(15, 474, 910, 33), panel);
                        GUI.Label(new Rect(25, 479, 890, 26), accessFeedback, small);
                    }
                    return;
                }
                GUI.DrawTexture(new Rect(25, 65, 910, 515), panel);
                GUI.Label(new Rect(45, 78, 850, 32), "DEBUG - los cambios no se guardan en tu perfil", title);
                if (GUI.Button(new Rect(45, 122, 255, 35), "Combate / enemigos", button)) tab = 0;
                if (GUI.Button(new Rect(315, 122, 255, 35), "Equipo", button)) tab = 1;
                if (GUI.Button(new Rect(585, 122, 255, 35), "Reliquias", button)) tab = 2;
                bool expedition = hub.DebugInExpedition;
                GUI.enabled = expedition;
                if (tab == 0) DrawCombat(); else if (tab == 1) DrawTeam(); else DrawRelics();
                GUI.enabled = true;
                GUI.Label(new Rect(45, 482, 850, 37), expedition ? message : "Estas en el refugio. Sal de DEBUG para recuperar tu partida real.", small);
                if (GUI.Button(new Rect(45, 527, 290, 36), "F2 / Esc - Cerrar panel", button)) { Close(); return; }
                if (GUI.Button(new Rect(355, 527, 555, 36), "Finalizar DEBUG y recuperar mi partida", button))
                { Close(); hub.EndDebugSession(); return; }
            }
            finally { GUI.matrix = matrix; GUI.depth = depth; GUI.enabled = enabledBefore; }
        }
        private void DrawCombat()
        {
            var battle = controller.DebugBattle;
            bool baseEnabled = GUI.enabled;
            GUI.Label(new Rect(45, 174, 850, 30), battle == null ? "Elige un rival para una pelea de prueba; no altera los encuentros de la sala."
                : "Ronda " + battle.Round + " | Enemigo " + battle.EnemyHealth + "/" + battle.EnemyMaxHealth + " | Ataque previsto " + battle.PredictedAttackDamage, label);
            GUI.enabled = baseEnabled && battle != null && battle.Outcome == PrototypeOutcome.Ongoing;
            if (GUI.Button(new Rect(45, 218, 265, 42), "Derrotar instantaneamente", button))
            { message = controller.DebugResolve(false) ? "Victoria inmediata. Regresas al mapa." : "No hay combate activo."; return; }
            GUI.enabled = baseEnabled && battle != null && battle.Outcome == PrototypeOutcome.Ongoing && battle.Capturable && controller.DebugTeam.Count < 4;
            if (GUI.Button(new Rect(325, 218, 285, 42), "Capturar sin sello ni fallo", button))
            { message = controller.DebugResolve(true) ? "Capturado. El aliado se anadio al equipo temporal." : "No se pudo capturar."; return; }
            GUI.enabled = baseEnabled;
            if (GUI.Button(new Rect(625, 218, 285, 42), "Curar / reanimar equipo", button)) { controller.DebugHealTeam(); message = "Equipo recuperado."; }
            GUI.enabled = baseEnabled && battle != null && battle.Outcome == PrototypeOutcome.Ongoing;
            if (GUI.Button(new Rect(45, 273, 265, 38), "Enemigo al 40% de vida", button)) battle.DebugEnemy(false, true);
            if (GUI.Button(new Rect(325, 273, 285, 38), "Enemigo con toda su vida", button)) battle.DebugEnemy(false, false);
            if (GUI.Button(new Rect(625, 273, 285, 38), "Romper estabilidad", button)) battle.DebugExpose();
            GUI.enabled = baseEnabled && battle == null;
            enemyScroll = GUI.BeginScrollView(new Rect(45, 335, 870, 88), enemyScroll,
                new Rect(0, 0, 850, ((PrototypeSpeciesCatalog.Names.Length + 3) / 4) * 43));
            for (int i = 0; i < PrototypeSpeciesCatalog.Names.Length; i++)
            {
                string species = PrototypeSpeciesCatalog.Names[i];
                if (GUI.Button(new Rect((i % 4) * 212, (i / 4) * 43, 205, 37), (pickedEnemy == species ? "> " : "") + PrototypeSpeciesCatalog.Name(species), button)) pickedEnemy = species;
            }
            GUI.EndScrollView();
            if (GUI.Button(new Rect(45, 431, 560, 40), "Pelear contra " + PrototypeSpeciesCatalog.Name(pickedEnemy) + " (prueba)", button))
            {
                if (controller.DebugFight(pickedEnemy)) { message = "Combate de prueba. Pulsa F2 para combatir normalmente."; }
                else message = "No disponible: comprueba el lider y que exista ese encuentro en la escena.";
            }
            GUI.enabled = baseEnabled;
            if (GUI.Button(new Rect(625, 431, 285, 40), "+25 sellos", button)) controller.DebugAddSeals();
        }
        private void DrawTeam()
        {
            bool baseEnabled = GUI.enabled;
            GUI.enabled = baseEnabled && controller.DebugBattle == null;
            GUI.Label(new Rect(45, 170, 420, 30), "Equipo temporal: elige un espacio.", small);
            for (int i = 0; i < controller.DebugTeam.Count; i++)
            {
                var member = controller.DebugTeam[i]; float y = 209 + i * 44;
                if (GUI.Button(new Rect(45, y, 420, 37), (i == teamSlot ? "> " : "") + (i + 1) + ". " + member.DisplayName + " " + member.Health + "/" + member.MaxHealth, button)) teamSlot = i;
                if (GUI.Button(new Rect(480, y, 200, 37), "Usar como lider", button)) controller.DebugLeader(i);
                if (GUI.Button(new Rect(695, y, 215, 37), "Quitar", button))
                { message = controller.DebugRemoveMember(i) ? "Integrante retirado." : "Debe quedar al menos un integrante."; return; }
            }
            int index = 0;
            allyScroll = GUI.BeginScrollView(new Rect(45, 393, 870, 80), allyScroll,
                new Rect(0, 0, 850, ((PrototypeSpeciesCatalog.Names.Length + 2) / 3) * 38));
            foreach (string species in PrototypeSpeciesCatalog.Names)
            {
                if (!PrototypeSpeciesCatalog.CanBeAlly(species)) continue;
                if (GUI.Button(new Rect(index % 3 * 283, index / 3 * 38, 275, 33), (pickedAlly == species ? "> " : "") + PrototypeSpeciesCatalog.Name(species), button)) pickedAlly = species;
                index++;
            }
            GUI.EndScrollView();
            if (GUI.Button(new Rect(480, 170, 430, 32), controller.DebugTeam.Count < 4 ? "Agregar " + PrototypeSpeciesCatalog.Name(pickedAlly) : "Reemplazar espacio " + (teamSlot + 1) + " con " + PrototypeSpeciesCatalog.Name(pickedAlly), button))
            { message = controller.DebugMember(pickedAlly, teamSlot) ? "Equipo temporal actualizado." : "No se pudo modificar el equipo."; }
            GUI.enabled = baseEnabled;
        }
        private void DrawRelics()
        {
            bool baseEnabled = GUI.enabled;
            GUI.enabled = baseEnabled && controller.DebugBattle == null;
            GUI.Label(new Rect(45, 170, 420, 30), "Fila temporal: elige un espacio.", small);
            var line = controller.Relics;
            for (int i = 0; i < line.Count; i++)
            {
                float y = 210 + i * 37;
                if (GUI.Button(new Rect(45, y, 420, 32), (relicSlot == i ? "> " : "") + (i + 1) + ". " + PrototypeRelicLine.Name(line[i]), button)) relicSlot = i;
                if (GUI.Button(new Rect(480, y, 75, 32), "<", button)) { controller.DebugMoveRelic(i, -1); return; }
                if (GUI.Button(new Rect(565, y, 75, 32), ">", button)) { controller.DebugMoveRelic(i, 1); return; }
                if (GUI.Button(new Rect(650, y, 260, 32), "Quitar", button)) { controller.DebugRemoveRelic(i); return; }
            }
            int index = 0;
            relicScroll = GUI.BeginScrollView(new Rect(45, 403, 870, 70), relicScroll,
                new Rect(0, 0, 850, ((PrototypeContent.RelicIds.Length + 3) / 4) * 35));
            foreach (PrototypeRelic relic in PrototypeContent.RelicIds)
            {
                if (GUI.Button(new Rect(index % 4 * 212, index / 4 * 35, 205, 30), (pickedRelic == relic ? "> " : "") + PrototypeRelicLine.Name(relic), button)) pickedRelic = relic;
                index++;
            }
            GUI.EndScrollView();
            if (GUI.Button(new Rect(480, 170, 430, 32), line.Count < 5 ? "Agregar reliquia elegida" : "Reemplazar espacio " + (relicSlot + 1), button))
            { message = controller.DebugRelic(pickedRelic, relicSlot) ? "Reliquia aplicada al equipo temporal." : "Ya esta equipada o el espacio no existe."; }
            GUI.enabled = baseEnabled;
        }
    }
}
