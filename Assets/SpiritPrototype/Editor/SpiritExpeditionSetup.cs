using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpiritPrototype.Editor
{
    public static class SpiritExpeditionSetup
    {
        [MenuItem("Tools/Espiritus/6 Preparar expedicion corta")]
        private static void Setup()
        {
            GameObject root = GameObject.Find("Habitacion temporal");
            var controller = root != null ? root.GetComponent<PrototypeEncounterController>() : null;
            var player = root != null ? root.GetComponentInChildren<PlayerMovement>(true) : null;
            if (controller == null || player == null)
            { EditorUtility.DisplayDialog("Falta la habitacion", "Abre la escena que ya tiene equipo, combate y reliquias.", "Aceptar"); return; }
            var members = new Dictionary<string, SpiritEncounter>();
            foreach (var encounter in root.GetComponentsInChildren<SpiritEncounter>(true)) members[encounter.species] = encounter;
            foreach (string name in new[] {"Velo", "Luto", "Raicero", "Farol"})
                if (!members.ContainsKey(name))
                { EditorUtility.DisplayDialog("Falta un encuentro", "No se encontro " + name, "Aceptar"); return; }
            Transform door = null, shrine = null;
            foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.name == "Door") door = renderer.transform;
                if (renderer.name == "Sanctuary") shrine = renderer.transform;
            }
            if (door == null || shrine == null)
            { EditorUtility.DisplayDialog("Faltan objetos", "Se necesitan Door y Sanctuary de la sala original.", "Aceptar"); return; }
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Preparar expedicion corta");
            SpiritEncounter boss;
            if (!members.TryGetValue("Guardian del Umbral", out boss))
            {
                var source = members["Raicero"].GetComponent<SpriteRenderer>();
                var go = new GameObject("Guardian del Umbral"); Undo.RegisterCreatedObjectUndo(go, "Crear jefe temporal");
                go.transform.SetParent(source.transform.parent);
                go.transform.localScale = source.transform.localScale * 1.35f;
                var sprite = Undo.AddComponent<SpriteRenderer>(go);
                sprite.sprite = source.sprite; sprite.sharedMaterial = source.sharedMaterial;
                sprite.sortingOrder = source.sortingOrder; sprite.color = new Color(1f, .65f, .65f);
                boss = Undo.AddComponent<SpiritEncounter>(go);
            }
            Undo.RecordObject(boss, "Configurar jefe");
            boss.species = "Guardian del Umbral"; boss.portrait = boss.GetComponent<SpriteRenderer>().sprite;
            boss.maxHealth = 32; boss.attackDamage = 4; boss.maxStability = 20; boss.capturable = false;
            var all = new List<SpiritEncounter>();
            foreach (var encounter in root.GetComponentsInChildren<SpiritEncounter>(true))
            {
                all.Add(encounter);
                if (encounter == boss) continue;
                Undo.RecordObject(encounter, "Configurar espiritu comun");
                encounter.attackDamage = 3; encounter.maxStability = 10; encounter.capturable = true;
            }
            var expedition = root.GetComponent<PrototypeExpeditionController>();
            if (expedition == null) expedition = Undo.AddComponent<PrototypeExpeditionController>(root);
            var data = new SerializedObject(expedition);
            data.FindProperty("controller").objectReferenceValue = controller;
            data.FindProperty("player").objectReferenceValue = player;
            data.FindProperty("door").objectReferenceValue = door;
            data.FindProperty("sanctuary").objectReferenceValue = shrine;
            data.FindProperty("boss").objectReferenceValue = boss;
            Assign(data.FindProperty("allEncounters"), all);
            Assign(data.FindProperty("firstRoom"), new[] {members["Velo"], members["Luto"]});
            Assign(data.FindProperty("secondRoom"), new[] {members["Raicero"], members["Farol"]});
            data.ApplyModifiedProperties();
            var encounters = new SerializedObject(controller);
            Assign(encounters.FindProperty("encounters"), all);
            encounters.ApplyModifiedProperties();
            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeGameObject = root;
            EditorUtility.DisplayDialog("Expedicion preparada", "Guarda con Ctrl+S y pulsa Play. Resuelve dos encuentros y ve a la puerta norte. Pulsa E para avanzar.", "Aceptar");
        }

        private static void Assign(SerializedProperty property, IEnumerable<SpiritEncounter> items)
        {
            var list = new List<SpiritEncounter>(items); property.arraySize = list.Count;
            for (int i = 0; i < list.Count; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
        }

        [MenuItem("Tools/Espiritus/6 Preparar expedicion corta", true)]
        private static bool CanSetup() { return !EditorApplication.isPlayingOrWillChangePlaymode; }
    }
}
