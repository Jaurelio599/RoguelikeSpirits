using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpiritPrototype.Editor
{
    public static class SpiritMovementSetup
    {
        [MenuItem("Tools/Espiritus/3 Activar movimiento en la habitacion")]
        private static void Setup()
        {
            GameObject root = GameObject.Find("Habitacion temporal");
            if (root == null)
            {
                EditorUtility.DisplayDialog("Abre la habitacion",
                    "Abre primero la escena HabitacionTemporal del paquete anterior. No necesitas volver a generarla.", "Aceptar");
                return;
            }
            SpriteRenderer player = null;
            foreach (var candidate in root.GetComponentsInChildren<SpriteRenderer>(true))
                if (candidate.gameObject.name == "Player_South") { player = candidate; break; }
            const string folder = "Assets/SpiritPrototype/Generated/Characters/";
            Sprite south = AssetDatabase.LoadAssetAtPath<Sprite>(folder + "Player_South.png");
            Sprite north = AssetDatabase.LoadAssetAtPath<Sprite>(folder + "Player_North.png");
            if (player == null || south == null || north == null)
            {
                EditorUtility.DisplayDialog("Faltan recursos",
                    "Se necesita Player_South en la habitacion y ambos sprites en Generated/Characters. Prepara el paquete anterior primero.", "Aceptar");
                return;
            }

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Activar movimiento del prototipo");
            GameObject actor = player.gameObject;
            Rigidbody2D body = actor.GetComponent<Rigidbody2D>();
            if (body == null) body = Undo.AddComponent<Rigidbody2D>(actor);
            BoxCollider2D collider = actor.GetComponent<BoxCollider2D>();
            if (collider == null) collider = Undo.AddComponent<BoxCollider2D>(actor);
            PlayerMovement movement = actor.GetComponent<PlayerMovement>();
            bool isNew = movement == null;
            if (isNew) movement = Undo.AddComponent<PlayerMovement>(actor);
            Undo.RecordObject(body, "Configurar fisica");
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.linearDamping = 0f;
            body.angularDamping = 0f;
            Undo.RecordObject(collider, "Colision a los pies");
            Bounds bounds = south.bounds;
            collider.isTrigger = false;
            collider.size = new Vector2(bounds.size.x * .55f, bounds.size.y * .22f);
            collider.offset = new Vector2(bounds.center.x, bounds.min.y + bounds.size.y * .14f);
            var data = new SerializedObject(movement);
            data.FindProperty("south").objectReferenceValue = south;
            data.FindProperty("north").objectReferenceValue = north;
            if (isNew) data.FindProperty("speed").floatValue = 3f;
            data.ApplyModifiedProperties();

            int walls = 0;
            foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                string name = renderer.gameObject.name;
                bool wall = name == "Wall" || name == "Door";
                bool obstacle = name == "Pillar" || name == "Urn" || name == "Chest";
                if (!wall && !obstacle) continue;
                BoxCollider2D blocker = renderer.GetComponent<BoxCollider2D>();
                if (blocker == null) blocker = Undo.AddComponent<BoxCollider2D>(renderer.gameObject);
                Undo.RecordObject(blocker, "Configurar obstaculo");
                blocker.isTrigger = false;
                Bounds b = renderer.sprite.bounds;
                blocker.size = wall ? (Vector2)b.size : new Vector2(b.size.x * .7f, b.size.y * .45f);
                blocker.offset = wall ? (Vector2)b.center : new Vector2(b.center.x, b.min.y + b.size.y * .25f);
                if (wall) walls++;
            }
            EditorSceneManager.MarkSceneDirty(actor.scene);
            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = actor;
            EditorUtility.DisplayDialog("Movimiento preparado",
                "Guarda con Ctrl+S y pulsa Play. Haz clic en Game y usa WASD o flechas.\n" +
                walls + " muros o puertas preparados. El cofre, pilar y vasija tambien bloquean el paso.\n" +
                "La puerta permanece cerrada. Los espiritus aun no inician combates.", "Aceptar");
        }

        [MenuItem("Tools/Espiritus/3 Activar movimiento en la habitacion", true)]
        private static bool CanSetup()
        { return !EditorApplication.isPlayingOrWillChangePlaymode; }
    }
}
