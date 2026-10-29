using System.Collections.Generic;
using UnityEngine;

namespace SpiritPrototype
{
    public sealed class PrototypeFloorGeometry
    {
        private readonly Transform root;
        private readonly List<GameObject> blocks = new List<GameObject>();
        private readonly SpriteRenderer source;
        public PrototypeFloorGeometry(Transform dungeon)
        {
            var pillar = dungeon.Find("Objetos/Pillar");
            source = pillar != null ? pillar.GetComponent<SpriteRenderer>() : null;
            root = new GameObject("Obstaculos del piso generado").transform;
            root.SetParent(dungeon);
        }
        public void Apply(PrototypeFloorRoom room)
        {
            int count = room != null && source != null && source.sprite != null ? room.blocks.Count : 0;
            for (int i = 0; i < count; i++)
            {
                if (i >= blocks.Count)
                {
                    var go = new GameObject("Obstaculo " + i); go.transform.SetParent(root);
                    var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = source.sprite;
                    renderer.sharedMaterial = source.sharedMaterial; renderer.sortingOrder = 1;
                    Vector3 size = source.sprite.bounds.size;
                    go.transform.localScale = new Vector3(1 / Mathf.Max(.001f, size.x), 1 / Mathf.Max(.001f, size.y), 1);
                    go.AddComponent<BoxCollider2D>().size = (Vector2)(size * .96f);
                    blocks.Add(go);
                }
                blocks[i].SetActive(true);
                blocks[i].transform.position = new Vector3(room.blocks[i].x, room.blocks[i].y, 0);
            }
            for (int i = count; i < blocks.Count; i++) blocks[i].SetActive(false);
        }
    }
}
