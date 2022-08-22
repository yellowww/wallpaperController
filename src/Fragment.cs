using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace desktopWallpaperController
{
    class Fragment
    {
        public PointF position = new Point(200,200);
        public float xVel;
        public float yVel;
        public Point[] polygon = Array.Empty<Point>();
        public Color color = Color.White;
        public float size;
        public float farthestLeftVert;
        public float farthestUpVert;
        public float farthestRightVert;
        public float alpha = 1;
        public float alphaFade;
        
        public static void generatePolygon(Fragment frag)
        {          
            Random rnd = new Random();
            frag.alphaFade = (float)rnd.Next(100, 300) / 18000f;
            frag.farthestLeftVert = (float)Double.PositiveInfinity;
            frag.farthestUpVert = (float)Double.PositiveInfinity;
            frag.farthestRightVert = (float)Double.NegativeInfinity;
            int verticies = rnd.Next(3,7);
            frag.size = (float)rnd.Next(400, 900) / 75f;
            frag.polygon = new Point[verticies];
            float direction = rnd.Next(0,360);
            for(int i=0;i<verticies; i++)
            {
                float vertScale = ((float)rnd.Next(100, 500) / 350f) * frag.size;
                frag.polygon[i] = new Point(
                    (int)(MathF.Cos(direction * 0.0174533f) * vertScale),
                    (int)(MathF.Sin(direction * 0.0174533f) * vertScale));
                if (frag.polygon[i].X < frag.farthestLeftVert) frag.farthestLeftVert = frag.polygon[i].X;
                if (frag.polygon[i].X > frag.farthestRightVert) frag.farthestRightVert = frag.polygon[i].X;
                if (frag.polygon[i].Y < frag.farthestUpVert) frag.farthestUpVert = frag.polygon[i].Y;
                direction += 360 / verticies;
            }
        }
    }
}
