using System.Linq;
using UnityEngine;
using Verse;

namespace NanameDoors;

internal class FenceGateMoverGraphics : DefModExtension
{
    public FenceGateMoverGraphics(string path)
    {
        var textures = ContentFinder<Texture2D>.GetAllInFolder(path)
            .OrderByDescending(t => t.name.EndsWith("Front")).ToList();
        for (var i = 0; i < 2; i++)
        {
            graphics[i] = GraphicDatabase.Get<Graphic_Single>($"{path}/{textures.ElementAt(i).name}");
        }
    }

    public readonly Graphic[] graphics = new Graphic[2];
}
