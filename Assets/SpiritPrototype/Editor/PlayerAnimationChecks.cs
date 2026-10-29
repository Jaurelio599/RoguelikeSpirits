using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SpiritPrototype.Editor
{
    public static class PlayerAnimationChecks
    {
        [MenuItem("Tools/Espiritus/Diagnostico/Verificar animacion del jugador")]
        public static void Run()
        {
            var clock = new PlayerWalkClock();
            Check(clock.Tick(PlayerFacing.South, false, 1, 8, 1) == 1, "Reposo usa el fotograma elegido.");
            Check(clock.Tick(PlayerFacing.South, true, .125f, 8, 1) == 1, "Velocidad de ocho fotogramas por segundo.");
            Check(clock.Tick(PlayerFacing.South, true, .375f, 8, 1) == 0, "El ciclo vuelve al principio.");
            Check(clock.Tick(PlayerFacing.West, true, .2f, 8, 1) == 0, "Cambiar de direccion reinicia el ciclo.");
            Check(clock.Tick(PlayerFacing.West, false, 2, 8, 1) == 1, "Detenerse conserva la direccion y el reposo.");
            const string sheet = "Assets/SpiritPrototype/Source/PlayerWalkSheet_v01.png";
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Check(ImageConversion.LoadImage(source, File.ReadAllBytes(sheet)), "La hoja se puede leer.");
                var pixels = source.GetPixels32();
                var frames = PlayerSpriteBaker.Analyze(pixels, source.width, source.height, out float scale);
                Check(frames.Length == 16 && scale > 0 && scale < 1, "16 recortes con escala comun.");
                foreach (var frame in frames)
                {
                    var output = PlayerSpriteBaker.Bake(pixels, source.width, frame, scale);
                    int visible = 0;
                    for (int i = 0; i < output.Length; i++) if (output[i].a > 12) visible++;
                    Check(visible > 100 && output[0].a == 0 && output[output.Length - 1].a == 0, "Contenido visible y bordes transparentes.");
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(source); }
            Debug.Log("Espiritus: 16 recortes, escala comun, transparencia y ciclo de animacion correctos.");
        }
        private static void Check(bool value, string message)
        { if (!value) throw new InvalidOperationException("Animacion: " + message); }
    }
}
