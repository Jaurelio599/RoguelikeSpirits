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
        private int room, victories, captures;
        private bool started, completed, rewarded, sanctuaryUsed;
        private float startedAt;
        private GUIStyle text, title, button;
        private Texture2D panel;
        private readonly string[] names = {"Sala de los ecos", "Camara de las raices", "Santuario", "Guardian del Umbral"};
        private string notice = "";

        private void Start()
        {
            if (controller == null || player == null || door == null || sanctuary == null || boss == null ||
                allEncounters == null || firstRoom == null || firstRoom.Length != 2 || secondRoom == null || secondRoom.Length != 2)
            { Debug.LogError("Configura la expedicion con Tools > Espiritus > 6.", this); enabled = false; return; }
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
            room = 0; victories = 0; captures = 0; completed = false;
            sanctuaryUsed = false; startedAt = Time.unscaledTime;
            notice = "Resuelve los dos encuentros para abrir la salida.";
            controller.SetExternalPause(false);
            ApplyRoom();
        }

        private SpiritEncounter[] CurrentEnemies()
        {
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
                foreach (var sprite in floor.GetComponentsInChildren<SpriteRenderer>()) sprite.color = colors[room];
            controller.WarpPlayer(new Vector3(-2, -2, 0));
            PaintDoor();
        }

        private void PaintDoor()
        {
            var sprite = door.GetComponent<SpriteRenderer>();
            if (sprite != null) sprite.color = Cleared() ? new Color(.5f, 1f, 1f) : Color.white;
        }

        private void Resolved(SpiritEncounter enemy, bool captured)
        {
            bool belongs = false;
            foreach (var current in CurrentEnemies()) if (current == enemy) belongs = true;
            if (!belongs) return;
            victories++; if (captured) captures++;
            if (!Cleared()) return;
            if (room == 3)
            {
                completed = true;
                controller.SetExternalPause(true);
                notice = "Has derrotado al Guardian del Umbral.";
                return;
            }
            if (!rewarded)
            {
                controller.GrantSeals(2); rewarded = true;
                notice = "Sala completada. Recibes 2 sellos. Ve a la puerta norte y pulsa E.";
            }
            PaintDoor();
        }

        private void Update()
        {
            if (!started || completed || !controller.CanExplore || !Interact()) return;
            if (room == 2 && !sanctuaryUsed && Near(sanctuary, 1.65f))
            {
                controller.RestoreTeam(); sanctuaryUsed = true;
                notice = "El santuario cura y reanima a todo el equipo. Solo puedes usarlo una vez.";
                return;
            }
            if (!Near(door, 1.65f)) return;
            if (!Cleared()) { notice = "La puerta sigue cerrada. Resuelve los encuentros pendientes."; return; }
            if (room >= 3) return;
            room++;
            notice = room == 2 ? "Acercate al circulo de piedra y pulsa E para recuperar el equipo."
                : room == 3 ? "El jefe no es capturable. Romper su estabilidad cancela una respuesta."
                : "Nuevos encuentros. La vida, el equipo, los sellos y las reliquias se conservan.";
            ApplyRoom();
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

        private void OnGUI()
        {
            if (!started || (!completed && !controller.CanExplore)) return;
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
                if (completed)
                {
                    GUI.DrawTexture(new Rect(90, 120, 780, 350), panel);
                    GUI.Label(new Rect(130, 155, 700, 50), "Expedicion completada", title);
                    GUI.Label(new Rect(130, 225, 700, 90), notice + "\nEncuentros resueltos: " + victories +
                        "   Capturas: " + captures, text);
                    if (GUI.Button(new Rect(290, 365, 380, 65), "Comenzar otra expedicion", button)) controller.RestartTrial();
                    return;
                }
                // One slim objective row, below the player status bar.
                var compact = new GUIStyle(text) { fontSize = 14, wordWrap = false };
                GUI.DrawTexture(new Rect(12, 48, 936, 32), panel);
                GUI.Label(new Rect(24, 53, 410, 23), "Sala " + (room + 1) + "/4 - " + names[room], compact);
                int remaining = 0;
                foreach (var enemy in CurrentEnemies())
                    if (enemy != null && enemy.gameObject.activeSelf) remaining++;
                string objective = room == 2 ? sanctuaryUsed ? "Equipo recuperado. Ve a la puerta norte."
                    : "E junto al santuario para recuperar al equipo."
                    : remaining == 0 ? "Salida abierta: E junto a la puerta norte."
                    : room == 3 ? "Derrota al jefe. Rompe su guardia para frenarlo."
                    : "Encuentros pendientes: " + remaining + "/2";
                GUI.Label(new Rect(450, 53, 480, 23), objective, compact);
                string prompt = room == 2 && Near(sanctuary, 1.65f) && !sanctuaryUsed
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
