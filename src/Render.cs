using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Threading;
using System.Diagnostics;


namespace desktopWallpaperController
{
    static class Render
    {
        static Random rnd;
        static Pen grassPen;
        static int grassNodes = 3;
        static int grassSpacing = 30;

        static float grassFadeDistance = 2000;
        static float grassFadeDistanceX = 3500;

        public static int DISPLAY_WIDTH = 3840;
        public static int DISPLAY_HEIGHT = 1080;
        static string imageDirectory = "C:\\Users\\me\\source\\repos\\desktopWallpaperController\\desktopWallpaperController\\images";

        public static GrassBlade[,] allGrass;
        public static List<GrassBlade> updateQueue = new List<GrassBlade>();
        public static List<Point[]> queueNeighbors = new List<Point[]>();

        static PerformanceCounter[] cpuUtil = new PerformanceCounter[20];
        static float[] cpuUtilValues = new float[21]; // 20: average

        static float[] GPUData;
        static float[] RAMData;

        static PerformanceCounter ramAval;

        static Color[] fragmentColorFade = new Color[]
        {
            Color.FromArgb(255,21,1,0),
            Color.FromArgb(255,61,17,3),
            Color.FromArgb(255,253,111,47),
            Color.FromArgb(255,31,8,24),            
            Color.FromArgb(255,36,32,24)
        };

        static float[] fragmentColorLerpP = new float[]
        {
            0,
            0.296f,
            0.43f,
            0.5f,
            1f

        };

        static List<Fragment> allFragments = new List<Fragment>();

        static Region invalidFragmentRegion;


        static Rectangle[] invalidGrassMovement = new Rectangle[]
        {
            new Rectangle(1500, 540, 343/2, 593/2),
            new Rectangle(1800, 480, (int)(172 / 1.5f), (int)(334 / 1.5f)),
            new Rectangle(1300, 480, (int)(165 / 3.5f), (int)(333 / 3.5f)),
            new Rectangle(2650, 440, (int)(407 / 3.5f), (int)(394 / 3.5f)),
            //new Rectangle(2780, 520, 293 / 6, 110 / 6),
            new Rectangle(3130, 815, 303, 292),
            new Rectangle(3600, 500, (int)(255 / 3.5f), (int)(286 / 3.5f)),
            new Rectangle(3158, 450, (int)(315 / 1.8f), (int)(445 / 1.8f)),
            new Rectangle(3370, 370, (int)(206 / 3f), (int)(691 / 3f))
        };
        static Point[] invalidGrassPos = new Point[]
        {
             new Point(1570,795),
             new Point(1835,670),
             new Point(1320,560),
             new Point(2710,540),
             //new Point(2800, 535),
             new Point(3626, 565),
             new Point(3244, 660),
             new Point(3400, 597)
        };
        static float[][] invalidGrasssRad = new float[][] // 0: radius, 1: y-strech
        {
            new float[] {92,1.2f},
            new float[] {50,1.5f},
            new float[] {30, 2.3f},
            new float[] {60, 3f},
           // new float[] {35, 4f},
           new float[] {50, 2f},
           new float[] {110, 2.5f},
           new float[] {37, 2f}
        };
        static PerlinNoise grassNoise;

        static Graphics wallpaperGraphics;

        public static void initiate()
        {
            grassNoise = new PerlinNoise(new Random());
            rnd = new Random();
            grassPen = new Pen(Color.FromArgb(255, 110, 95, 38));
            grassPen.Width = 3.2F;
            grassPen.LineJoin = System.Drawing.Drawing2D.LineJoin.Bevel;

            System.Drawing.Drawing2D.GraphicsPath invalidPath = new System.Drawing.Drawing2D.GraphicsPath();
            invalidPath.StartFigure();
            invalidPath.AddLine(new Point(1305, 520), new Point(1305, DISPLAY_HEIGHT));
            invalidPath.AddLine(new Point(1305, DISPLAY_HEIGHT), new Point(0, DISPLAY_HEIGHT));
            invalidPath.AddLine(new Point(0, DISPLAY_HEIGHT), new Point(0, 905));
            invalidPath.AddLine(new Point(0, 905), new Point(1305, 520));
            invalidFragmentRegion = new Region(invalidPath);

            for(int i=0;i<cpuUtil.Length;i++)
            {
                cpuUtil[i] = new PerformanceCounter("Processor", "% Processor Time", i.ToString());
            }
            ramAval = new PerformanceCounter("Memory", "Available MBytes");

            GPUData = HardwareMonitor.GetGPU();
            RAMData = HardwareMonitor.GetRAM();

        }
        static bool isVisible;
        public static void doFrame(Graphics g)
        {
            

            wallpaperGraphics = g;
            fullRepaint(g);
            int iteration = 0;
            long lastDate = -1;
            long lastVisiblityCheck = -1;
            long timeOfVisibilityChange = -1;
            bool cancelRepaint = false;
            isVisible = WindowHandle.checkVisibility();
            while (true) // game loop
            {
                if (getMS() - lastVisiblityCheck > 400)
                {
                    bool lastFrameVisible = isVisible;
                    checkVisibilityOnThread();
                    if (lastFrameVisible != isVisible)
                    {             
                        timeOfVisibilityChange = getMS();
                    } 
                    lastVisiblityCheck = getMS();

                }
                if (isVisible)
                {
                    cancelRepaint = false;
                    if (iteration%2==0)
                    {
                        MouseController.mouseFrame();
                        renderBladeQueue();
                    }
                    if(getMS()-lastDate>400)
                    {
                        renderGUI(g);
                        lastDate = getMS();
                    }
                    renderGUIFragments(g, iteration == 0);
                    spawnFragments(rnd.Next(2,4));
                    Thread.Sleep(25);
                } else
                {
                    Thread.Sleep(200);
                    if (getMS() - timeOfVisibilityChange > 30000 && !cancelRepaint)
                    {
                        cancelRepaint = true;
                        fullRepaint(g);
                    }
                }

                iteration++;
                
            }
        }

        static void fullRepaint(Graphics g)
        {
            renderBackground(g);
            renderField(g);
            addPostProcessing(g);
            renderGUIBack(g);
            renderObjects(g);
        }

        static long getMS()
        {
            return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }
            
        static void renderGUI(Graphics g)
        {
            getSystemData();
            updateHardwareData();

            Bitmap bmp = new Bitmap(1305, DISPLAY_HEIGHT);
            Graphics bitmapGraphics = Graphics.FromImage(bmp);
            DateTime now = DateTime.Now;
            Font timeFont = new Font("Verdana", 56.0f);
            Font dateFont = new Font("Verdana", 35.0f);
            Font consolasLarge = new Font("Consolas", 26.0f);
            Font consolasMed = new Font("Consolas", 21.0f);
            Font consolasMedMed = new Font("Consolas", 19.0f);
            Font consolasMedSmall = new Font("Consolas", 17.0f);
            Font consolas = new Font("Consolas", 16.0f);
            Font CPUCoreFont = new Font("Consolas", 13.0f);
            Brush textBrush = new SolidBrush(Color.FromArgb(200,240,240,240));
            Brush darkerTextBrush0 = new SolidBrush(Color.FromArgb(170, 240, 240, 240));
            Brush darkerTextBrush1 = new SolidBrush(Color.FromArgb(140, 240, 240, 240));
            Brush accentBrush = new SolidBrush(Color.FromArgb(200, 55, 115, 204));
            Brush backBrush = new SolidBrush(Color.FromArgb(255, 6, 3, 9));
            Pen greyPen = new Pen(Color.FromArgb(155, 240, 240, 240));
            Pen accentPen = new Pen(Color.FromArgb(155, 55, 115, 204));
            greyPen.Width = 2f;
            accentPen.Width = 5f;
            string dayNumber = now.ToString("dd");
            if (dayNumber[dayNumber.Length - 1] == '1') dayNumber += "st";
            else if (dayNumber[dayNumber.Length - 1] == '2') dayNumber += "nd";
            else if (dayNumber[dayNumber.Length - 1] == '3') dayNumber += "rd";
            else dayNumber += "th";
            StringFormat textFormatting = new StringFormat();
            StringFormat CPUtextFormatting = new StringFormat();
            CPUtextFormatting.Alignment = StringAlignment.Center;
            string dateString = now.ToString("h:mm tt");
            bitmapGraphics.FillRectangle(backBrush, 0, 0, 800, 600);
            bitmapGraphics.FillRectangle(backBrush, 800, 0, 400, 500);

            bitmapGraphics.DrawString(dateString, timeFont, textBrush, 70, 60, textFormatting);
            bitmapGraphics.DrawString(now.ToString("dddd, MMMM ") + dayNumber, dateFont, darkerTextBrush0, 462, 70, textFormatting);
            bitmapGraphics.DrawLine(accentPen, new Point(70, 156), new Point((int)lerp(70, dateString.Length*50+70, (float)now.Second / 60f), 156));

            bitmapGraphics.DrawString("CPU "+ MathF.Round(cpuUtilValues[20]) + "%", consolasLarge, darkerTextBrush0, 65, 230, textFormatting);
            bitmapGraphics.DrawString("Intel i9 10850k", consolas, accentBrush, 220, 242, textFormatting);
            
            for(int i=0;i<4;i++)
            {
                for(int j=0;j<5;j++)
                {
                    float x = j * 70 + 72;
                    float y = i * 50 + 280;
                    float thisUtil = cpuUtilValues[i * 4 + j];
                    int thisAlpha = (int)(thisUtil*5.26f);
                    if (thisAlpha > 255) thisAlpha = 255;
                    if (thisAlpha < 30) thisAlpha = 30;
                    greyPen.Color = Color.FromArgb(thisAlpha, greyPen.Color.R, greyPen.Color.G, greyPen.Color.B);
                    Brush thisBrush = new SolidBrush(Color.FromArgb(thisAlpha, 55, 115, 204));
                    bitmapGraphics.DrawRectangle(greyPen, x, y, 50, 40);
                    float height = lerp(0, 38, thisUtil / 100f) - 1;
                    bitmapGraphics.FillRectangle(thisBrush, x + 1, y + 39 - height, 48, height);

                    thisAlpha = (int)(thisUtil * 5.26f);
                    if (thisAlpha > 255) thisAlpha = 255;
                    if (thisAlpha < 15) thisAlpha = 15;
                    Brush thisTextBrush = new SolidBrush(Color.FromArgb(thisAlpha, 240, 240, 240));
                    bitmapGraphics.DrawString(MathF.Round(thisUtil) + "%", CPUCoreFont, thisTextBrush, new PointF(x+25, y+15), CPUtextFormatting);
                }
            }
            greyPen.Color = Color.FromArgb(155, 240, 240, 240);

            bitmapGraphics.DrawString("GPU", consolasLarge, darkerTextBrush0, 485, 230, textFormatting);
            bitmapGraphics.DrawString("NVIDIA Geforce RTX 3070ti", consolas, accentBrush, 565, 242, textFormatting);

            bitmapGraphics.DrawString("3D: "+MathF.Round(GPUData[0])+"%", consolasMed, darkerTextBrush1, 500, 300, textFormatting);
            accentPen.Width = 2f;
            float underlineLength = lerp(500, 630, GPUData[0] / 100f);
            bitmapGraphics.DrawLine(accentPen, new Point(500, 335), new Point((int)underlineLength, 335));


            bitmapGraphics.DrawString("Cuda: " + MathF.Round(GPUData[2]) + "%", consolasMed, darkerTextBrush1, 680, 300, textFormatting);
            underlineLength = lerp(680, 850, GPUData[2] / 100f);
            bitmapGraphics.DrawLine(accentPen, new Point(680, 335), new Point((int)underlineLength, 335));

            bitmapGraphics.DrawString("Encode: " + MathF.Round(GPUData[3]) + "%", consolasMedSmall, darkerTextBrush1, 500, 350, textFormatting);
            underlineLength = lerp(503, 660, GPUData[3] / 100f);
            bitmapGraphics.DrawLine(accentPen, new Point(503, 380), new Point((int)underlineLength, 380));


            bitmapGraphics.DrawString("Decode: " + MathF.Round(GPUData[4]) + "%", consolasMedSmall, darkerTextBrush1, 680, 350, textFormatting);
            underlineLength = lerp(680, 850, GPUData[4] / 100f);
            bitmapGraphics.DrawLine(accentPen, new Point(680, 380), new Point((int)underlineLength, 380));

            bitmapGraphics.DrawString("Copy: " + MathF.Round(GPUData[1]) + "%", consolasMedSmall, darkerTextBrush1, 500, 390, textFormatting);
            underlineLength = lerp(500, 630, GPUData[1] / 100f);
            bitmapGraphics.DrawLine(accentPen, new Point(500, 420), new Point((int)underlineLength, 420));

            float formattedTemp = (GPUData[8]-20f) / 65f;
            if (formattedTemp > 1) formattedTemp = 1;
            if (formattedTemp < 0) formattedTemp = 0;
            Pen tempPen = new Pen(ColorFromHSV(
                255,
                1,
                1,
                lerp(0,255, formattedTemp)
                ));

            tempPen.Width = 3f;
            bitmapGraphics.DrawString(MathF.Round(GPUData[8]) + "°", consolasMedSmall, darkerTextBrush1, 755, 390, CPUtextFormatting);
            underlineLength = lerp(680, 850, formattedTemp);
            bitmapGraphics.DrawLine(tempPen, new Point(680, 420), new Point((int)underlineLength, 420));
            if (GPUData[6] > GPUData[5]) GPUData[6] = GPUData[5];
            float rectLength = lerp(0, 292, GPUData[6] / GPUData[5])-1f;
            float endPos = lerp(524, 816, GPUData[6] / GPUData[5]) -24f;
            bitmapGraphics.DrawLine(greyPen, new Point(524, 439), new Point(816, 439));
            bitmapGraphics.DrawLine(greyPen, new Point(524, 486), new Point(816, 486));
            bitmapGraphics.DrawArc(greyPen, new Rectangle(499, 438, 48, 48), 90, 180);
            bitmapGraphics.DrawArc(greyPen, new Rectangle(792, 438, 48, 48), 270, 180);
            Brush GPUMEMBrush = new SolidBrush(Color.FromArgb((int)(GPUData[6] / GPUData[5] * 255), 55, 115, 204));
            bitmapGraphics.FillPie(GPUMEMBrush, new Rectangle(499, 439, 47, 47), 90, 180);
            bitmapGraphics.FillPie(GPUMEMBrush, new Rectangle((int)endPos-1, 439, 47, 47), 270, 180);
            bitmapGraphics.FillRectangle(GPUMEMBrush, 523, 440, MathF.Ceiling(rectLength), 45);
            CPUtextFormatting.LineAlignment = StringAlignment.Center;
            bitmapGraphics.DrawString(MathF.Round(GPUData[6] / 102.4f) / 10 + "GB / " + MathF.Round(GPUData[5] / 102.4f) / 10+"GB used", consolasMedMed, darkerTextBrush1, 670, 463, CPUtextFormatting);

            bitmapGraphics.DrawString("RAM", consolasLarge, darkerTextBrush0, 940, 230, textFormatting);
            bitmapGraphics.DrawString("4 x 16GB 3600mhz", consolas, accentBrush, 1020, 242, textFormatting);
            greyPen.Width = 4f;
            bitmapGraphics.DrawArc(greyPen, new Rectangle(998, 298, 174, 174), 0, 360);
            float RAMBrushA = RAMData[0] / (RAMData[0] + RAMData[1])*1.4f;
            if (RAMBrushA > 1) RAMBrushA = 1;
            Brush RAMBrush = new SolidBrush(Color.FromArgb((int)(RAMBrushA * 255), 55, 115, 204));
            bitmapGraphics.FillPie(RAMBrush, new Rectangle(1000, 300, 170, 170), 270, RAMData[0] / (RAMData[0] + RAMData[1]) * 360);
            string RAMText = MathF.Round(RAMData[0]) + "GB / " + MathF.Round(RAMData[0] + RAMData[1]) + "GB";
            bitmapGraphics.DrawString(RAMText, consolas, darkerTextBrush1, 1085, 385, CPUtextFormatting);

            g.DrawImageUnscaled(bmp, 0, 0);
        }

        static void getSystemData()
        {
            float sum = 0;
            for (int i = 0; i < cpuUtil.Length; i++)
            {
                cpuUtilValues[i] = cpuUtil[i].NextValue();
                sum += cpuUtilValues[i];
            }
            cpuUtilValues[cpuUtilValues.Length - 1] = sum / cpuUtil.Length;

        }

        static void updateHardwareData()
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                HardwareMonitor.computer.Accept(new UpdateVisitor());
                GPUData = HardwareMonitor.GetGPU();
                RAMData = HardwareMonitor.GetRAM();
            }).Start();
        }

        static void checkVisibilityOnThread()
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                isVisible = WindowHandle.checkVisibility();
            }).Start();
        }
        static void renderGUIBack(Graphics g)
        {
            Brush backgroundBrush = new SolidBrush(Color.FromArgb(255,6,3,9));
            g.FillRectangle(backgroundBrush, 0, 0, 1305, 520);
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            path.StartFigure();
            path.AddLine(new Point(0, 520), new Point(1305, 520));
            path.AddLine(new Point(1305, 520), new Point(0, 905));
            path.AddLine(new Point(0, 970), new Point(0, 520));
            g.FillPath(backgroundBrush, path);
        }

        static void renderGUIFragments(Graphics g, bool isFirst)
        {
            Bitmap bmp = new Bitmap(1305, DISPLAY_HEIGHT);
            Graphics bitmapGraphics = Graphics.FromImage(bmp);
            bitmapGraphics.ExcludeClip(invalidFragmentRegion);

            for (int i = 0; i < allFragments.Count; i++)
            {
                if(!isFirst)
                {
                    Brush backBrush = new SolidBrush(Color.FromArgb(255, 6, 3, 9));
                    PointF[] oldVerts = applyPositionToFragment(allFragments[i]);
                    bitmapGraphics.FillPolygon(backBrush, oldVerts);

                    updateFragment(allFragments[i]);
                }
                if(allFragments[i].alpha<0.05)
                {
                    allFragments.RemoveAt(i);
                    i--;
                } else
                {
                    Brush fragBrush = new SolidBrush(applyAlpha(allFragments[i]));
                    PointF[] newVerts = applyPositionToFragment(allFragments[i]);
                    bitmapGraphics.FillPolygon(fragBrush, newVerts);
                }

            }


            g.DrawImageUnscaled(bmp, 0, 0);
        }

        static PointF[] applyPositionToFragment(Fragment fragment)
        {
            PointF[] mutatedVerts = new PointF[fragment.polygon.Length];
            for(int i=0;i<fragment.polygon.Length;i++)
            {
                mutatedVerts[i] = new PointF(fragment.polygon[i].X + fragment.position.X, fragment.polygon[i].Y + fragment.position.Y);
            }
            return mutatedVerts;
        }

        static void spawnFragments(int amount)
        {
            for(int i=0;i<amount;i++)
            {

                int yPos, xPos;
                float xVel, yVel=0;
                bool xShift;
                if (rnd.Next(0, 12) < 0) yPos = rnd.Next(0, 520);
                else yPos = rnd.Next(520, 970);
                if (yPos > 520)
                {
                    
                    xPos = (int)lerp(1305, -200, (float)(yPos - 520) / 450f);
                    float relitivePercent = (float)(yPos - 560) / 440f;
                    if (relitivePercent > 1) relitivePercent  = 1;
                    float velocity = (float)rnd.Next(100, 300) / -110f;
                    xVel = velocity*1.3f;
                    yVel = velocity/lerp(4,0,relitivePercent);
                    xShift = false;
                }
                else
                {
                    xPos = 1305;
                    xShift = true;
                    xVel = (float)rnd.Next(100, 300) / -80f;

                }
                if (xPos < 0) spawnFragments(1);
                else
                {
                    Fragment thisFragment = new Fragment();
                    Fragment.generatePolygon(thisFragment);
                    Point position = new Point(xPos, yPos);                   
                    thisFragment.position = position;
                    thisFragment.position.X -= (int)thisFragment.farthestLeftVert;
                    thisFragment.xVel = xVel;
                    thisFragment.color = getFragColor(yPos);
                    thisFragment.yVel = yVel;
                    if (!xShift) thisFragment.position.Y -= (int)thisFragment.farthestUpVert/2;
                    allFragments.Add(thisFragment);
                }

            }
        }

        static void updateFragment(Fragment thisFrag)
        {
            thisFrag.position = new PointF(thisFrag.position.X + thisFrag.xVel, thisFrag.position.Y + thisFrag.yVel);
            thisFrag.xVel /= 1.015f;
            thisFrag.yVel /= 1.015f;
            thisFrag.alpha -= thisFrag.alphaFade;
        }

        static Color applyAlpha(Fragment thisFrag)
        {
            Color rgb = thisFrag.color;
            if (thisFrag.alpha < 0) thisFrag.alpha = 0; 
            return Color.FromArgb((int)(thisFrag.alpha * 255f), rgb.R, rgb.G, rgb.B);
        }

        static Color getFragColor(int y)
        {
            float p = (float)y / (float)DISPLAY_HEIGHT;
            int maxIndex=1, minIndex;
            for (int i = 0; i < fragmentColorLerpP.Length; i++)
            {
                if (fragmentColorLerpP[i] > p) {maxIndex = i; break; }
            }
            minIndex = maxIndex - 1;
            float PMIN = fragmentColorLerpP[minIndex] * DISPLAY_HEIGHT, PMAX = fragmentColorLerpP[maxIndex] * DISPLAY_HEIGHT;
            float localP = (y - PMIN) / (PMAX - PMIN);
            int brightnessMod = rnd.Next(-10, 10);
            float r = lerp(fragmentColorFade[minIndex].R, fragmentColorFade[maxIndex].R, localP)*2+brightnessMod;
            float g = lerp(fragmentColorFade[minIndex].G, fragmentColorFade[maxIndex].G, localP)*2+brightnessMod;
            float b = lerp(fragmentColorFade[minIndex].B, fragmentColorFade[maxIndex].B, localP)*2+brightnessMod;
            if (r > 255) r = 255;
            if (g > 255) g = 255;
            if (b > 255) b = 255;
            if (r < 0) r = 0;
            if (g < 0) g = 0;
            if (b < 0) b = 0;
            return Color.FromArgb(255, (int)r, (int)g, (int)b);
        }

        public static float lerp(int x, int y, float p)
        {
            return x + (float)(y - x) * p;
        }

        static void renderBackground(Graphics g)
        {
            g.Clear(Color.FromArgb(255, 36, 32, 24));
            //g.FillRectangle(new SolidBrush(Color.FromArgb(255, 50, 14, 59)),0,0,DISPLAY_WIDTH,520);
            Image grassBackground = Image.FromFile(imageDirectory + "\\grassBackground.jpg");
            g.DrawImage(grassBackground, 0, 520, DISPLAY_WIDTH, DISPLAY_HEIGHT - 520);
        }

        static void addPostProcessing(Graphics g)
        {
            Bitmap bmp = new Bitmap(DISPLAY_WIDTH, DISPLAY_HEIGHT);
            Graphics bitmapGraphics = Graphics.FromImage(bmp);
            Image skyBackground = Image.FromFile(imageDirectory+"\\skyBackground.jpg");
            bitmapGraphics.DrawImage(skyBackground, 0,0,DISPLAY_WIDTH,520);
            g.DrawImageUnscaled(bmp, 0, 0);
        }

        static void renderObjects(Graphics g)
        {
            Image image0 = Image.FromFile(imageDirectory + "\\objectImages\\0t.png");
            g.DrawImage(image0, 1500, 540, 343/2, 593/2);

            Image image1 = Image.FromFile(imageDirectory + "\\objectImages\\1t.png");
            g.DrawImage(image1, 1300, 480, 165 / 3.5f, 333 / 3.5f);

            Image image2 = Image.FromFile(imageDirectory + "\\objectImages\\2t.png");
            g.DrawImage(image2, 1800, 480, 172 / 1.5f, 334 / 1.5f);

            Image image3 = Image.FromFile(imageDirectory + "\\objectImages\\3t.png");
            g.DrawImage(image3, 2650, 440, 407 / 3.5f, 394 / 3.5f);

            //Image image4 = Image.FromFile(imageDirectory + "\\objectImages\\4t.png");
            //g.DrawImage(image4, 2780, 520, 293 / 6f, 110 / 6f);

            Image image5 = Image.FromFile(imageDirectory + "\\objectImages\\5t.png");
            g.DrawImage(image5, 3150, 815, 283 / 1f, 292 / 1f);

            Image image6 = Image.FromFile(imageDirectory + "\\objectImages\\6t.png");
            g.DrawImage(image6, 3600, 500, 255 / 3.5f, 286 / 3.5f);

            Image image7 = Image.FromFile(imageDirectory + "\\objectImages\\7t.png");
            g.DrawImage(image7, 3158, 450, 315 / 1.8f, 445 / 1.8f);

            Image image8 = Image.FromFile(imageDirectory + "\\objectImages\\8t.png");
            g.DrawImage(image8, 3370, 370, 206 / 3f, 691 / 3f);
        }

        static void renderField(Graphics g)
        {
            Bitmap bmp = new Bitmap(DISPLAY_WIDTH, DISPLAY_HEIGHT);
            Graphics bitmapGraphics = Graphics.FromImage(bmp);

            allGrass = new GrassBlade[(int)MathF.Ceiling((float)(DISPLAY_WIDTH * 2+1000) / (float)grassSpacing), (int)MathF.Ceiling((float)DISPLAY_HEIGHT * 2 / (float)grassSpacing)];
            
            for (int i=-DISPLAY_WIDTH+1000; i<DISPLAY_WIDTH+2000; i+=grassSpacing)
            {
                for (int j=1;j<DISPLAY_HEIGHT*2+1;j+=grassSpacing)
                {
                    System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();

                    int x = i + rnd.Next(0, grassSpacing)+500;
                    int y = j + rnd.Next(0, grassSpacing);
                    grassPen.Width = 8f / ((float)y / 300f + 1f);
                    
                    Point position = project3d((float)x,(float)y,(float)y);
                    GrassBlade thisBlade = new GrassBlade();
                    if (checkValidGrassPosition(position,1))
                    {
                        float colorMultiplyer = (float)(grassNoise.noise((double)x / 750d, (double)y / 750d, 30.4d, 2d)+1f) / 6f + 0.66f;
                        grassPen.Color = getColorFromPos(position.X, y,colorMultiplyer);
                        float bend = (float)grassNoise.noise((double)x/500d, (double)y/500d, 1d, 2d)*50f;
                        float length = (int)(80f / ((float)y / 300f + 1f));
                        renderGrassBlade(path, position, bend, length, grassNodes);
                        bitmapGraphics.DrawPath(grassPen, path);

                        thisBlade.position = position;
                        thisBlade.absolutePosition = new Point(x, y);
                        thisBlade.nodes = grassNodes;
                        thisBlade.bend = bend;
                        thisBlade.length = length;
                        thisBlade.force = 0f;
                        thisBlade.width = grassPen.Width;
                        thisBlade.color = grassPen.Color;
                        thisBlade.backColor = getBackColorFromPos(position.X,y);
                    } else
                    {
                        thisBlade.position = position;
                        thisBlade.exists = false;
                    }

                    
                    allGrass[(i + DISPLAY_WIDTH - 1000) /grassSpacing, (j - 1)/grassSpacing] = thisBlade;
                }
            }
            
            g.DrawImageUnscaled(bmp, 0, 0);
        }

        public static bool checkValidMovementPosition(Point pos)
        {
            for(int i=0;i< invalidGrassMovement.Length;i++)
            {
                if (pos.X >= invalidGrassMovement[i].X-80 && pos.Y >= invalidGrassMovement[i].Y-50 && pos.X <= invalidGrassMovement[i].Right+50 && pos.Y <= invalidGrassMovement[i].Bottom+50) return false;
            }
            return true;
        }
        public static bool checkValidGrassPosition(Point pos, float multiplyer)
        {
            for (int i = 0; i < invalidGrassPos.Length; i++)
            {
                Point thisPos = invalidGrassPos[i];
                float distance = MathF.Sqrt(MathF.Pow(pos.X - thisPos.X,2) + MathF.Pow(pos.Y - thisPos.Y,2)*1.75f*invalidGrasssRad[i][1]);
                if (distance<invalidGrasssRad[i][0]*multiplyer) return false;
            }
            return true;
        }

        static Color getBackColorFromPos(float x, float z)
        {
            float distance = (z / grassFadeDistance)/1f;
            if (distance > 1) distance = 1;
            float r = 36f * (1 - (distance)) + 31f * ((distance));
            float g = 32f * (1 - (distance)) + 8f * ((distance));
            float b = 24f * (1 - (distance)) + 24f * ((distance));
            return Color.FromArgb(255, (int)(r), (int)(g), (int)(b));
        }

        static Color getColorFromPos(float x, float z, float multiplyer)
        {
            float distance = (z / grassFadeDistance + x / grassFadeDistanceX) / 2f;
            if (distance > 1) distance = 1;
            float r = 97f * (1 - (distance)) + 50f * ((distance));
            float g = 82f * (1 - (distance)) + 14f * ((distance));
            float b = 31f * (1 - (distance)) + 59f * ((distance));
            return Color.FromArgb(255, (int)(r * multiplyer), (int)(g * multiplyer), (int)(b * multiplyer));
        }

        public static Point project3d(float x, float y, float z)
        {
            if (z <0.01f) z = 0.01f;
            return new Point((int)(x / (z / 750f + 1f))+DISPLAY_WIDTH/2, DISPLAY_HEIGHT-(int)(y / (z / 750f + 1f)));
        }

        static void renderGrassBlade(System.Drawing.Drawing2D.GraphicsPath path, Point position, float bend, float length,int nodes)
        {
            bend *= 0.0174533f;
            float segmentLength = (length / (float)nodes);
            path.StartFigure();
            Point lastPoint = position;
            for(int i=0;i<nodes;i++)
            {
                Point p0 = lastPoint;
                Point p1 = lastPoint;
                p1.X += (int)(MathF.Cos(((bend*i) - 1.571f)) * segmentLength);
                p1.Y += (int)(MathF.Sin(((bend*i) - 1.571f)) * segmentLength);
                path.AddLine(p0, p1);
                lastPoint = p1;
            }
        }

        public static void renderFromBlade(GrassBlade blade, Graphics g)
        {
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            grassPen.Color = blade.color;
            grassPen.Width = blade.width;
            renderGrassBlade(path, blade.position, blade.bend, blade.length, blade.nodes);
            g.DrawPath(grassPen, path);
        }

        public static void renderActiveBlade(GrassBlade blade, Graphics g)
        {
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            grassPen.Color = blade.backColor;
            grassPen.Width = blade.width+1;
            renderGrassBlade(path, blade.position, blade.bend, blade.length, blade.nodes);
            g.DrawPath(grassPen, path);
            MouseController.updateBlade(blade);
            renderFromBlade(blade, g);

        }

        public static void renderAllNeighbors(Point[] allNeighbors, List<string> denyDraw, Graphics g)
        {
            for (int i = 0; i < allNeighbors.Length; i++)
            {
                string thisPointString = allNeighbors[i].X + ":" + allNeighbors[i].Y;
                if (!denyDraw.Contains(thisPointString))
                {
                    denyDraw.Add(thisPointString);
                    Point thisBlade = allNeighbors[i];
                    renderFromBlade(allGrass[thisBlade.X, thisBlade.Y], g);
                }
            }
        }

        static void renderBladeQueue()
        {
           // Bitmap bmp = new Bitmap(DISPLAY_WIDTH, DISPLAY_HEIGHT);
            //Graphics bitmapGraphics = Graphics.FromImage(bmp);
            for (int i=0;i<updateQueue.Count;i++)
            {
                renderActiveBlade(updateQueue[i], wallpaperGraphics);
            }
            List<string> denyDraw = new List<string>();
            for (int i = 0; i < updateQueue.Count; i++)
            {
                renderAllNeighbors(queueNeighbors[i], denyDraw, wallpaperGraphics);
                if (MathF.Abs(updateQueue[i].force) < 0.1)
                {
                    updateQueue[i].force = 0;
                    updateQueue.RemoveAt(i);
                    queueNeighbors.RemoveAt(i);
                    i--;
                }
            }
            for(int i=0;i<updateQueue.Count-60;i++)
            {
                updateQueue[i].force = 0;
                updateQueue.RemoveAt(i);
                queueNeighbors.RemoveAt(i);
                i--;
            }
            //wallpaperGraphics.DrawImageUnscaled(bmp, 0, 0);
        }

        public static Color ColorFromHSV(double hue, double saturation, double value, float alpha)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            if (hi == 0)
                return Color.FromArgb((int)alpha, v, t, p);
            else if (hi == 1)
                return Color.FromArgb((int)alpha, q, v, p);
            else if (hi == 2)
                return Color.FromArgb((int)alpha, p, v, t);
            else if (hi == 3)
                return Color.FromArgb((int)alpha, p, q, v);
            else if (hi == 4)
                return Color.FromArgb((int)alpha, t, p, v);
            else
                return Color.FromArgb((int)alpha, v, p, q);
        }
    }
}
