using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpiritPrototype.Editor
{
    public static class SpiritRelicSetup
    {
        [MenuItem("Tools/Espiritus/5 Preparar reliquias iniciales")]
        private static void Setup()
        {
            GameObject root = GameObject.Find("Habitacion temporal");
            var controller = root != null ? root.GetComponent<PrototypeEncounterController>() : null;
            if (controller == null)
            { EditorUtility.DisplayDialog("Falta la habitacion", "Abre la escena que ya tiene encuentros y equipo.", "Aceptar"); return; }
            string folder = "Assets/SpiritPrototype/Generated/Items/";
            string[] names = {"CarbonTallado", "LenteVoraz", "PrismaVecino"};
            var sprites = new Sprite[3];
            for (int i = 0; i < 3; i++)
            {
                sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(folder + names[i] + ".png");
                if (sprites[i] == null)
                { EditorUtility.DisplayDialog("Falta un icono", "Prepara los sprites del primer paquete. Falta " + names[i], "Aceptar"); return; }
            }
            var data = new SerializedObject(controller);
            var portraits = data.FindProperty("relicPortraits"); portraits.arraySize = 3;
            for (int i = 0; i < 3; i++) portraits.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            data.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(root.scene);
            EditorUtility.DisplayDialog("Reliquias preparadas", "Guarda con Ctrl+S. En Play pulsa R para ordenarlas fuera de combate.", "Aceptar");
        }

        [MenuItem("Tools/Espiritus/5 Preparar reliquias iniciales", true)]
        private static bool CanSetup() { return !EditorApplication.isPlayingOrWillChangePlaymode; }
    }
}
