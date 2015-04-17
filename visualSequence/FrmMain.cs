using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace visualSequence
{
    public struct seqChain
    {
        public int x;
        public int y;
        public int z;
    }

    public partial class FrmMain : Form
    {
        private const int max_x = 639;
        private const int max_y = 479;


        private const int size_x = 640;
        private const int size_y = 480;


        private Bitmap bm = new Bitmap(1, 1);

        private List<seqChain> chain = new List<seqChain>();
        private List<seqChain> guesschain = new List<seqChain>();
        private List<int> chainInts = new List<int>();

        public FrmMain()
        {
            InitializeComponent();

            Graphics bg = Graphics.FromImage(bm);
            bg.Clear(Color.White);
            bg.Dispose();
        }

        private void btnGenerate_Click(object sender, EventArgs e)
        {
            lblOutside.Text = string.Empty;
            if (chainInts.Any())
            {
                GenerateImage();
                lblOutside.Text = chainInts.Count.ToString();
            }
            else
            {
                lblOutside.Text = "No values loaded.";
            }
        }

        private void btnRandom_Click(object sender, EventArgs e)
        {
            lblOutside.Text = string.Empty;
            GenerateSeqNumsInts();
            GenerateImage();
            lblOutside.Text = chainInts.Count.ToString();
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            var filename = tbFolder.Text;
            LoadSeqNumsFromFile(filename);
        }

        private void GenerateImage()
        {
            Bitmap img = new Bitmap(size_x, size_y, PixelFormat.Format32bppRgb);

            Graphics g = Graphics.FromImage(img);

            //g.Clear(Color.Black);

            int seq3th = 0, seq2th = 0, seq1th = 0;

            double x1, y1, z1;
            double pha = 0.2;
            int x, y;

            double usex = Math.Sin(pha);
            double usey = Math.Cos(pha);

            // Middle of sceen.
            int half_x = (max_x/2);
            int half_y = (max_y/2);
            int outside = 0;

            byte[] pixels = new byte[img.Height*img.Width];

            foreach (int c in chainInts)
            {
                x1 = (c - seq1th)*(max_x/65535f/65535f);
                y1 = (seq1th - seq2th)*(max_y/65535f/65535f);
                z1 = (seq2th - seq3th)*(max_y/65535f/65535f);

                x = (int) (x1*usey + z1*usex);
                y = (int) (y1 + (z1*usey - x1*usex)/2);

                x += half_x;
                y += half_y;

                outside++;

                // Skip first 4 and make sure it is inside of picture
                if (outside > 4 && x <= max_x && y <= max_y && x >= 0 && y >= 0)
                {
                    // transformera in i byte arrayn.
                    int plats = x*y;

                    if ((pixels[plats] + 5) < 255)
                        pixels[plats] += 5;
                    else
                        pixels[plats] = 127;
                }

                // Switch up old seqences
                seq3th = seq2th;
                seq2th = seq1th;
                seq1th = c;
            }

            LockUnlockBits(img, pixels);

            picBox1.Image = img;
            g.Dispose();
        }

        private void GenerateSeqNumsInts()
        {
            Random r = new Random();

            chainInts = new List<int>();

            for (int i = 0; i < 50000; i++)
            {
                chainInts.Add(r.Next(0, Int32.MaxValue));
            }
        }

        private void LoadSeqNumsFromFile(string filename)
        {
            if (!File.Exists(filename))
                return;

            chainInts = new List<int>();
            using (StreamReader sr = new StreamReader(filename))
            {
                string s = sr.ReadLine();
                while (s != null)
                {
                    int a = Convert.ToInt32(s);
                    chainInts.Add(a);
                    s = sr.ReadLine();
                }
            }
        }

        private void LoadSeqChain()
        {
            for (int i = 3; i < chainInts.Count - 1; i++)
            {
                seqChain aChain = new seqChain();
                aChain.x = chainInts[i] - chainInts[i - 1];
                aChain.y = chainInts[i - 1] - chainInts[i - 2];
                aChain.z = chainInts[i - 2] - chainInts[i - 3];

                chain.Add(aChain);
            }

        }

        private void Guess3d(int seqT_3, int seqT_2, int seqT_1, int radius)
        {
            int ny = seqT_1 - seqT_2;
            int nz = seqT_2 - seqT_3;

            lblOutside.Text = string.Format("Reconstructed: x={guess set} y={0} z={1} (radius={2})", ny, nz, radius);

            guesschain = new List<seqChain>();

            for (int i = 3; i < chain.Count - 1; i++)
            {
                int distance = Math.Abs(chain[i].z - nz) + Math.Abs(chain[i].y - ny);
                if (distance <= radius)
                {
                    System.Diagnostics.Debug.WriteLine(
                        string.Format("{0} {1}", distance, chain[i].x)
                        );

                    guesschain.Add(chain[i]);
                }
            }
        }

        private void LockUnlockBits(Bitmap bmp, byte[] imgValues)
        {
            // Lock the bitmap's bits.  
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, bmp.PixelFormat);

            unsafe
            {
                // Get the address of the first line.
                // IntPtr ptr = bmpData.Scan0;
                //Sets the pointer to the first pixel
                byte* ptr = (byte*) bmpData.Scan0;

                int bytesPerPixel = BytesPerPixel(bmp.PixelFormat);
                int remainOffset = bmpData.Stride - bmpData.Width*bytesPerPixel;

                //Loops over each pixel
                for (int y = 0; y < bmpData.Height; y++)
                {
                    for (int x = 0; x < bmpData.Width; x++)
                    {
                        byte colByte = imgValues[x*y];

                        for (int pp = 0; pp < bytesPerPixel; pp++)
                        {
                            ptr[pp] = colByte;
                        }

                        ptr += bytesPerPixel;
                    }
                    //Corrects for the potential offset
                    ptr += remainOffset;
                }
            }

            // Unlock the bits.
            bmp.UnlockBits(bmpData);
        }

        public static int BytesPerPixel(PixelFormat format)
        {
            switch (format)
            {
                case PixelFormat.Format16bppRgb555:
                case PixelFormat.Format16bppRgb565:
                case PixelFormat.Format16bppGrayScale:
                    return 2;

                case PixelFormat.Format24bppRgb:
                    return 3;

                case PixelFormat.Format32bppRgb:
                case PixelFormat.Format32bppArgb:
                case PixelFormat.Format32bppPArgb:
                    return 4;

                case PixelFormat.Format48bppRgb:
                    return 6;

                case PixelFormat.Format64bppArgb:
                case PixelFormat.Format64bppPArgb:
                    return 8;

                default:
                    throw new ArgumentNullException("format");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
                tbFolder.Text = openFileDialog1.FileName;
        }

    }
}
