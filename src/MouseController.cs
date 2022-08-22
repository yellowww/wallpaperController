using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Text;


namespace desktopWallpaperController
{
    static class MouseController
    {

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out Point pos);
        static Point lastMousePos;

        public static void mouseFrame()
        {

            Point mousePos = new Point();
            GetCursorPos(out mousePos);
            if (lastMousePos == null) lastMousePos = mousePos;
            if(mousePos.Y>520)
            {
                float mouseMovement = (lastMousePos.X - mousePos.X) / -1.5f;

                Point grassIndex = getCoordinatesFromMouse(mousePos);
                Point grassPos = Render.allGrass[grassIndex.X, grassIndex.Y].position;
                if (Render.checkValidMovementPosition(grassPos) && MathF.Abs(mouseMovement)>10)
                {
                    GrassBlade thisBlade = Render.allGrass[grassIndex.X, grassIndex.Y];
                    thisBlade.force = mouseMovement;
                    Render.updateQueue.Add(thisBlade);
                    Render.queueNeighbors.Add(getNeighbors(grassIndex.X, grassIndex.Y,4, thisBlade.bend));
                    Point[] affectedNeighbors = getAllNeighbors(grassIndex.X, grassIndex.Y,2);
                    for(int i=0;i<affectedNeighbors.Length;i++)
                    {
                        GrassBlade thisNeighbor = Render.allGrass[affectedNeighbors[i].X, affectedNeighbors[i].Y];
                        thisNeighbor.force = mouseMovement;
                        if(Render.checkValidMovementPosition(thisNeighbor.position))
                        {
                            Render.updateQueue.Add(thisNeighbor);
                            Render.queueNeighbors.Add(getNeighbors(thisNeighbor.position.X, thisNeighbor.position.Y, 4, thisNeighbor.bend));
                        }
                    }
                }

            }

            lastMousePos = mousePos;
        }

        static Point[] getAllNeighbors(int x, int y, int rad)
        {
            Point[] neighbors = new Point[rad*rad];
            for(int i=x-rad/2;i<x+rad/2;i++)
            {
                for(int j=y-rad/2;j<y+rad/2;j++)
                {
                    if(i>0 && j>0 && i<Render.allGrass.GetLength(0) && j<Render.allGrass.GetLength(1))
                    {
                        int localI = i - (x - rad / 2), localJ = j - (y - rad / 2);
                        neighbors[localI * rad + localJ] = new Point(i,j);
                    }
                }
            }
            return neighbors;
        }

        static Point[] getNeighborsRight(int x, int y, int rad)
        {
            Point[] neighbors = new Point[(int)MathF.Ceiling((float)(rad * rad) / 2f)];
            for (int i = x; i < x + rad / 2; i++)
            {
                for (int j = y - rad / 2; j < y + rad / 2; j++)
                {
                    if (i > 0 && j > 0 && i < Render.allGrass.GetLength(0) && j < Render.allGrass.GetLength(1))
                    {
                        int localI = i - x, localJ = j - (y - rad / 2);
                        neighbors[localI * rad + localJ] = new Point(i, j);
                    }
                }
            }
            return neighbors;
        }

        static Point[] getNeighborsLeft(int x, int y, int rad)
        {
            Point[] neighbors = new Point[(int)MathF.Ceiling((float)(rad * rad) / 2f)];
            for (int i = x - rad / 2; i < x; i++)
            {
                for (int j = y - rad / 2; j < y + rad / 2; j++)
                {
                    if (i > 0 && j > 0 && i < Render.allGrass.GetLength(0) && j < Render.allGrass.GetLength(1))
                    {
                        int localI = i - (x - rad / 2), localJ = j - (y - rad / 2);
                        neighbors[localI * rad + localJ] = new Point(i, j);
                    }
                }
            }
            return neighbors;
        }

        static Point[] getNeighbors(int x, int y, int rad, float bend)
        {
            if (bend >= 0) return getNeighborsRight(x, y, rad);
            else return getNeighborsLeft(x, y, rad);
        }


        static Point getCoordinatesFromMouse(Point pos)
        {
            Point transformed = new Point(pos.X + 1080+850, pos.Y);
            int min = 0, max = Render.allGrass.GetLength(0)-1;
            int lastAvg=-1,avg=0;
            while (lastAvg != avg)
            {
                lastAvg = avg;
                avg = (min + max) / 2;
                
                if(Render.allGrass[avg,0].position.X>transformed.X)
                {
                    max = avg;
                } else if(Render.allGrass[avg, 0].position.X < transformed.X)
                {
                    min = avg;
                } else
                {
                    min = avg;
                    max = avg;
                }
            }
            int grassI = min;
            min = 0;
            lastAvg = -1;
            avg = 0;
            max = Render.allGrass.GetLength(1) - 1;
            while (lastAvg != avg)
            {
                lastAvg = avg;
                avg = (min + max) / 2;
                if (Render.allGrass[grassI, avg].position.Y < transformed.Y)
                {
                    max = avg;
                }
                else if (Render.allGrass[grassI, avg].position.Y >  transformed.Y)
                {
                    min = avg;
                } else
                {
                    min = avg;
                    max = avg;
                }
            }
            int grassJ = min;
            min = 0;
            max = Render.allGrass.GetLength(0) - 1;
            lastAvg = -1;
            avg = 0;
            while (lastAvg != avg)
            {
                lastAvg = avg;
                avg = (min + max) / 2;

                if (Render.allGrass[avg, grassJ].position.X > transformed.X)
                {
                    max = avg;
                }
                else if (Render.allGrass[avg, grassJ].position.X < transformed.X)
                {
                    min = avg;
                }
                else
                {
                    min = avg;
                    max = avg;
                }
            }
            grassI = min;
            return new Point(grassI, grassJ);
        }

        public static void updateBlade(GrassBlade blade)
        {
            blade.bend = MathF.Sin(blade.bladeTime)*blade.force;
            blade.force /= 1.1f;
            blade.bladeTime += 0.1f;
        }
    }
}
