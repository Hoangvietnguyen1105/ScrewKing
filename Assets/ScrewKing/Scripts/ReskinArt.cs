using System.Collections.Generic;
using UnityEngine;

namespace ScrewKing
{
    // Keep gameplay color IDs intact; the supplied screw filenames use a different order.
    public static class ReskinArt
    {
        static readonly int[] ScrewFiles = { 0, 1, 2, 3, 4, 5, 6, 11, 10, 9, 8, 7 };
        static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
        public const string UI = "NewArt/screw ingame/";
        public static string Screw(int color) => "NewArt/box_nap_dinh/d" + ScrewFiles[Mathf.Clamp(color, 1, 11)];
        public static string Box(int color) => "NewArt/box_nap_dinh/c" + color;
        public static string Lid(int color) => "NewArt/box_nap_dinh/g" + color;
        public static string Resolve(string name)
        {
            if (name.StartsWith("Screws/d") && int.TryParse(name.Substring(8), out int screw)) return Screw(screw);
            if (name.StartsWith("Boxes/c")) return "NewArt/box_nap_dinh/" + name.Substring(6);
            if (name == "Screws/ScrewHole") return UI + "Layer 5 copy 4";
            return "Art/" + name;
        }
        public static Sprite Sprite(string path)
        {
            if (!Sprites.TryGetValue(path, out var sprite))
            {
                sprite = Resources.Load<Sprite>(path);
                Sprites[path] = sprite;
            }
            return sprite;
        }
        public static void FitScrew(SpriteRenderer renderer)
        {
            // Match the original 54px head and 75px input area, regardless of export resolution.
            if (renderer.sprite) renderer.transform.localScale = Vector3.one * (.54f / renderer.sprite.bounds.size.x);
        }
    }
}
