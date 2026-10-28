using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpiritPrototype.Editor
{
    public static class SpiritEncounterSetup
    {
        [MenuItem("Tools/Espiritus/4 Activar encuentros y combate")]
        private static void Setup()
        {
            GameObject root = GameObject.Find("Habitacion temporal");
            PlayerMovement player = root != null ? root.GetComponentInChildren<PlayerMovement>(true) : null;
            if (player == null)
            {
                EditorUtility.DisplayDialog("Falta el jugador", "Abre la habitacion y activa primero el movimiento con el paso 3.", "Aceptar");
                return;
            }
            var enemies = new List<SpiritEncounter>();
            string[] names = {"Velo", "Raicero", "Campanilla", "Luto", "Farol"};
            var candidates = new List<SpriteRenderer>();
            foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
                foreach (string name in names)
                    if (renderer.gameObject.name == name) candidates.Add(renderer);
            if (candidates.Count == 0)
            {
                EditorUtility.DisplayDialog("Faltan espiritus", "No se encontraron los espiritus de la habitacion temporal.", "Aceptar");
                return;
            }
            Sprite ally = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SpiritPrototype/Generated/Characters/Ascua.png");
            if (ally == null) { EditorUtility.DisplayDialog("Falta Ascua", "Prepara los sprites con el paso 1.", "Aceptar"); return; }
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Activar encuentros del prototipo");
            foreach (var renderer in candidates)
            {
                var marker = renderer.GetComponent<SpiritEncounter>();
                if (marker == null) marker = Undo.AddComponent<SpiritEncounter>(renderer.gameObject);
                Undo.RecordObject(marker, "Configurar encuentro");
                marker.species = renderer.gameObject.name; marker.portrait = renderer.sprite;
                marker.maxHealth = 18;
                enemies.Add(marker);
            }
            var controller = root.GetComponent<PrototypeEncounterController>();
            if (controller == null) controller = Undo.AddComponent<PrototypeEncounterController>(root);
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("player").objectReferenceValue = player;
            serialized.FindProperty("allyPortrait").objectReferenceValue = ally;
            var list = serialized.FindProperty("encounters"); list.arraySize = enemies.Count;
            for (int i = 0; i < enemies.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = enemies[i];
            serialized.ApplyModifiedProperties();
            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeGameObject = root;
            EditorUtility.DisplayDialog("Encuentros preparados", "Guarda con Ctrl+S. En Play acercate a un espiritu y pulsa E.\nAcciones con raton o teclas 1 a 4.", "Aceptar");
        }

        [MenuItem("Tools/Espiritus/4 Activar encuentros y combate", true)]
        private static bool CanSetup() { return !EditorApplication.isPlayingOrWillChangePlaymode; }
    }
}
