using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpiritPrototype.Editor
{
    public static class SpiritProfileSetup
    {
        [MenuItem("Tools/Espiritus/7 Preparar perfiles y refugio")]
        private static void Setup()
        {
            if (Object.FindFirstObjectByType<PrototypeProfileHub>(FindObjectsInactive.Include) != null)
            {
                EditorUtility.DisplayDialog("Ya preparado", "Esta escena ya tiene perfiles y refugio. Guarda y pulsa Play.", "Aceptar");
                return;
            }
            GameObject dungeon = GameObject.Find("Habitacion temporal");
            var controller = dungeon != null ? dungeon.GetComponent<PrototypeEncounterController>() : null;
            var expedition = dungeon != null ? dungeon.GetComponent<PrototypeExpeditionController>() : null;
            var player = dungeon != null ? dungeon.GetComponentInChildren<PlayerMovement>(true) : null;
            var floor = dungeon != null ? dungeon.transform.Find("Suelo") : null;
            var walls = dungeon != null ? dungeon.transform.Find("Muros con colision") : null;
            if (controller == null || expedition == null || player == null || floor == null || walls == null)
            {
                EditorUtility.DisplayDialog("Abre tu habitacion", "Necesitas la escena HabitacionTemporal con la expedicion y los cofres que ya funcionan.", "Aceptar");
                return;
            }
            Sprite portalSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SpiritPrototype/Generated/Environment/Sanctuary.png");
            Sprite museumSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SpiritPrototype/Generated/Characters/Chest.png");
            if (portalSprite == null || museumSprite == null)
            { EditorUtility.DisplayDialog("Faltan sprites", "No se encontraron Sanctuary.png y Chest.png del paquete original.", "Aceptar"); return; }
            if (GameObject.Find("Refugio temporal") != null)
            { EditorUtility.DisplayDialog("Nombre ocupado", "Ya existe Refugio temporal sin el controlador. Renombra ese objeto antes de continuar.", "Aceptar"); return; }

            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Preparar perfiles y refugio");
            var hub = new GameObject("Refugio temporal");
            Undo.RegisterCreatedObjectUndo(hub, "Crear refugio");
            var hubFloor = Object.Instantiate(floor.gameObject, hub.transform);
            hubFloor.name = "Suelo del refugio";
            Undo.RegisterCreatedObjectUndo(hubFloor, "Crear suelo");
            foreach (var sprite in hubFloor.GetComponentsInChildren<SpriteRenderer>()) sprite.color = new Color(1f, .87f, .7f);
            var hubWalls = Object.Instantiate(walls.gameObject, hub.transform);
            hubWalls.name = "Muros del refugio";
            Undo.RegisterCreatedObjectUndo(hubWalls, "Crear muros");
            var hubActor = Object.Instantiate(player.gameObject, hub.transform);
            hubActor.name = "Explorador del refugio";
            hubActor.transform.position = new Vector3(-1, -2, 0);
            hubActor.SetActive(true);
            var movement = hubActor.GetComponent<PlayerMovement>(); movement.enabled = true;
            Undo.RegisterCreatedObjectUndo(hubActor, "Crear explorador");
            Material material = player.GetComponent<SpriteRenderer>().sharedMaterial;
            Transform portal = Station(hub.transform, "Portal", portalSprite, material, new Vector3(3, 1, 0), 1.5f, new Color(.8f, .7f, 1));
            Transform museum = Station(hub.transform, "Museo", museumSprite, material, new Vector3(-4, 1, 0), 1.1f, Color.white);
            // A short row of display pedestals makes room for future museum exhibits.
            Sprite pillar = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SpiritPrototype/Generated/Environment/Pillar.png");
            if (pillar != null)
                for (int i = 0; i < 3; i++) Station(hub.transform, "Vitrina " + (i + 1), pillar, material, new Vector3(-2 + i * 2, 2.8f, 0), .7f, new Color(.9f, .85f, .7f));
            hub.SetActive(false);
            var managerObject = new GameObject("Perfiles y refugio");
            Undo.RegisterCreatedObjectUndo(managerObject, "Crear gestor de perfiles");
            var manager = Undo.AddComponent<PrototypeProfileHub>(managerObject);
            var data = new SerializedObject(manager);
            Set(data, "dungeonRoot", dungeon); Set(data, "hubRoot", hub);
            Set(data, "controller", controller); Set(data, "expedition", expedition);
            Set(data, "hubPlayer", movement); Set(data, "portal", portal); Set(data, "museum", museum);
            data.ApplyModifiedProperties();
            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(dungeon.scene);
            Selection.activeGameObject = managerObject;
            EditorUtility.DisplayDialog("Perfiles preparados", "Guarda con Ctrl+S y pulsa Play. Elige un perfil. Camina al museo o al portal y pulsa E. No necesitas otra escena.", "Aceptar");
        }

        private static void Set(SerializedObject data, string field, Object value)
        { data.FindProperty(field).objectReferenceValue = value; }

        private static Transform Station(Transform parent, string name, Sprite sprite, Material material, Vector3 position, float size, Color color)
        {
            var station = new GameObject(name);
            station.transform.SetParent(parent); station.transform.position = position;
            var renderer = station.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite; renderer.sharedMaterial = material;
            renderer.color = color; renderer.sortingOrder = 1;
            float extent = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            station.transform.localScale = Vector3.one * (size / Mathf.Max(.001f, extent));
            var collider = station.AddComponent<BoxCollider2D>(); collider.size = sprite.bounds.size * .7f;
            Undo.RegisterCreatedObjectUndo(station, "Crear " + name);
            return station.transform;
        }

        [MenuItem("Tools/Espiritus/Guardado/Abrir carpeta de perfiles")]
        private static void RevealProfiles()
        {
            string directory = System.IO.Path.Combine(Application.persistentDataPath, "SpiritPrototype", "Profiles");
            System.IO.Directory.CreateDirectory(directory);
            EditorUtility.RevealInFinder(directory);
        }

        [MenuItem("Tools/Espiritus/7 Preparar perfiles y refugio", true)]
        private static bool CanSetup() { return !EditorApplication.isPlayingOrWillChangePlaymode; }
    }
}
