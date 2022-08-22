using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace desktopWallpaperController
{
    class GrassBlade
    {
        public Point position;
        public Point absolutePosition;
        public int nodes;
        public float bend;
        public float length;
        public float force;
        public float width;
        public Color color;
        public Color backColor;
        public float bladeTime = 0;
        public bool exists = true;
    }
}
