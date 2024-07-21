using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace NanameDoors
{
    internal class FenceGateMoverGraphics : DefModExtension
    {
        public FenceGateMoverGraphics(string path)
        {
            var textures = ContentFinder<Texture2D>.GetAllInFolder(path).OrderByDescending(t => t.name.EndsWith("Front")); 
            for (int i = 0; i < 2; i++)
            {
                this.graphics[i] = GraphicDatabase.Get<Graphic_Single>($"{path}/{textures.ElementAt(i).name}");
            }
        }

        public Graphic[] graphics = new Graphic[2];
    }
}
