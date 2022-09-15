using Freecell.Structures;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Freecell.Identifer
{
    public class FreecellImage
    {
        private static readonly int TEMPLATE_WIDTH;
        private static readonly int TEMPLATE_HEIGHT;
        private static readonly int TEMPLATE_EXAMINE_WIDTH;
        private static readonly int TEMPLATE_WINDOW_SIZE = 4;
        private static readonly int TEMPLATE_EDGE_THRESHOLD = 4;

        static FreecellImage()
        {
            // Load the templates
            var iconTemplateDirectory = Path.Combine(Environment.CurrentDirectory, "Images");

            _iconTemplates = new Dictionary<Card, byte[,]>();
            for (var i = Card.AceHeart; i <= Card.KingClub; i++)
            {
                var path = Path.Combine(iconTemplateDirectory, $"{(int)i}.png");
                using var img = Image.FromFile(path);
                using var directBitmap = new DirectBitmap(img.Width, img.Height);
                using var g = Graphics.FromImage(directBitmap.Bitmap);
                g.DrawImage(img, 0, 0);

                var gray = ToGrayscale(directBitmap);

                // Gaussian
                var blurred = new byte[img.Height, img.Width];
                for (int y = 0; y < img.Height; y++)
                {
                    var y0 = (y + img.Height - 1) % img.Height;
                    var y1 = y;
                    var y2 = (y + 1) % img.Height;
                    for (int x = 0; x < img.Width; x++)
                    {
                        var x0 = (x + img.Width - 1) % img.Width;
                        var x1 = x;
                        var x2 = (x + 1) % img.Width;

                        var sum = gray[y0, x0] + gray[y0, x2] + gray[y2, x0] + gray[y2, x2] + 2 * (gray[y0, x1] + gray[y1, x0] + gray[y1, x2] + gray[y2, x1]) + 4 * gray[y1, x1];

                        blurred[y, x] = (byte)(1.0 * sum / 16 + 0.5);
                    }
                }

                _iconTemplates[i] = blurred;
            }

            TEMPLATE_WIDTH = _iconTemplates.Values.Select(x => x.GetLength(1)).Min();
            TEMPLATE_HEIGHT = _iconTemplates.Values.Select(x => x.GetLength(0)).Min();

            TEMPLATE_EXAMINE_WIDTH = (int)(0.25 * TEMPLATE_WIDTH);
        }

        private static readonly Dictionary<Card, byte[,]> _iconTemplates;

        public int OriginX { get; set; }
        public int OriginY { get; set; }
        public double CardHeight { get; set; }
        public double CardWidth { get; set; }
        public double CardOverlap { get; set; }
        public double CardGap { get; set; }
        public FreecellBoard Board { get; set; }

        public Point GetCardPosition(int row, int col, Point? offset = null)
        {
            var origin = offset ?? Point.Empty;
            if (row > 10) row = 10; // The game starts to smoosh cards onto the screen at this point
            var x = OriginX + col * (CardWidth + CardGap) + CardWidth / 2;
            var y = OriginY + (row - 1) * CardOverlap + CardOverlap / 2;
            if (row == 0)
            {
                y -= CardHeight;
                if (col < 4)
                {
                    x -= CardGap;
                }
                else
                {
                    x += CardGap;
                }
            }

            return new Point((int)x + origin.X, (int)y + origin.Y);
        }

        private static byte[,] ToGrayscale(DirectBitmap img)
        {
            var grayscale = new byte[img.Height, img.Width];
            for (int y = 0; y < img.Height; y++)
            {
                for (int x = 0; x < img.Width; x++)
                {
                    var color = img.GetPixel(x, y);
                    grayscale[y, x] = (byte)(color.R * 0.3 + color.G * 0.59 + color.B * 0.11);
                }
            }
            return grayscale;
        }

        public static FreecellImage ReadImage(DirectBitmap img)
        {
            // Paint a pixel map with a unique number for every white-ish pixel
            var pixelMap = new int[img.Height, img.Width];
            var index = 1;
            for (int y = 0; y < img.Height; y++)
            {
                for (int x = 0; x < img.Width; x++)
                {
                    var pixel = img.GetPixel(x, y);
                    if (pixel.R >= 230 && pixel.G > 230 && pixel.B > 230)
                    {
                        pixelMap[y, x] = index++;
                    }
                }
            }

            // Scan through the pixel map consolidating pixel numbers in 3x3 boxes
            for (int y = 0; y < img.Height; y++)
            {
                for (int x = 0; x < img.Width; x++)
                {
                    if (pixelMap[y, x] == 0) continue;
                    for (int shiftY = 0; shiftY <= 3; shiftY++)
                    {
                        var y2 = y + shiftY;
                        if (y2 < 0 || y2 >= img.Height) continue;
                        for (int shiftX = 0; shiftX <= 3; shiftX++)
                        {
                            var x2 = x + shiftX;
                            if (x2 < 0 || x2 >= img.Width) continue;
                            if (pixelMap[y2, x2] == 0) continue;

                            if (pixelMap[y2, x2] > pixelMap[y, x])
                            {
                                pixelMap[y2, x2] = pixelMap[y, x];
                            }
                            else if (pixelMap[y2, x2] < pixelMap[y, x])
                            {
                                pixelMap[y, x] = pixelMap[y2, x2];
                            }
                        }
                    }
                }
            }

            // Get a list of unique numbers left in the pixel map
            var numbers = new HashSet<int>();
            for (int y = 0; y < img.Height; y++)
            {
                for (int x = 0; x < img.Width; x++)
                {
                    if (pixelMap[y, x] == 0) continue;

                    numbers.Add(pixelMap[y, x]);
                }
            }

            // Find the extent of each region
            var minX = new Dictionary<int, int>();
            var maxX = new Dictionary<int, int>();
            var minY = new Dictionary<int, int>();
            var maxY = new Dictionary<int, int>();
            for (int y = 0; y < img.Height; y++)
            {
                for (int x = 0; x < img.Width; x++)
                {
                    var pix = pixelMap[y, x];
                    int val;
                    if (!minX.TryGetValue(pix, out val) || x < val)
                    {
                        minX[pix] = x;
                    }
                    if (!maxX.TryGetValue(pix, out val) || x > val)
                    {
                        maxX[pix] = x;
                    }
                    if (!minY.TryGetValue(pix, out val) || y < val)
                    {
                        minY[pix] = y;
                    }
                    if (!maxY.TryGetValue(pix, out val) || y > val)
                    {
                        maxY[pix] = y;
                    }
                }
            }

            // Remove regions where the width is not larger than 1/15 of the view and the height is not 1/4 of the view
            foreach (var num in numbers.ToList())
            {
                var width = maxX[num] - minX[num];
                var height = maxY[num] - minY[num];
                if (width * 15 < img.Width && height * 4 < img.Height || height < width)
                {
                    numbers.Remove(num);
                }
            }

            // Remove region where less than 1/2 of the pixels are the same color
            foreach (var num in numbers.ToList())
            {
                var threshold = (maxY[num] - minY[num] + 1) * (maxX[num] - minX[num] + 1) / 2;
                var count = 0;
                for (int y = minY[num]; y <= maxY[num]; y++)
                {
                    for (int x = minX[num]; x <= maxX[num]; x++)
                    {
                        if (pixelMap[y, x] == num) count++;
                    }
                }
                if (count < threshold)
                {
                    numbers.Remove(num);
                }
            }

            // At this point we can eliminate things that look not-freecell-y
            if (numbers.Count != 8) return null;

            var widths = numbers.Select(x => maxX[x] - minX[x] + 1).ToArray();
            var heights = numbers.Select(x => maxY[x] - minY[x] + 1).ToArray();
            var gaps = numbers.OrderBy(x => x).Skip(1).Zip(numbers.OrderBy(x => x).Take(7)).Select(x => minX[x.First] - maxX[x.Second] - 1).ToArray();

            var averageWidth = widths.Average();
            var averageHeightLong = heights.Take(4).Average();
            var averageHeightShort = heights.Skip(4).Take(4).Average();
            var averageGap = gaps.Average();

            if (widths.Any(x => x < 9 * averageWidth / 10 || 10 * averageWidth / 9 < x)) return null;
            if (heights.Take(4).Any(x => x < 9 * averageHeightLong / 10 || 10 * averageHeightLong / 9 < x)) return null;
            if (heights.Skip(4).Take(4).Any(x => x < 9 * averageHeightShort / 10 || 10 * averageHeightShort / 9 < x)) return null;
            if (gaps.Any(x => x < 9 * averageGap / 10 || 10 * averageGap / 9 < x)) return null;

            // Compute basic image properties
            var cardOverlap = averageHeightLong - averageHeightShort;
            var fImg = new FreecellImage()
            {
                OriginX = numbers.Min(x => minX[x]),
                OriginY = (int)(numbers.Select(x => minY[x]).Average() + 0.5),
                CardHeight = (averageHeightLong + averageHeightShort - 11 * cardOverlap) / 2,
                CardWidth = averageWidth,
                CardOverlap = cardOverlap,
                CardGap = averageGap
            };

            // Determine card order
            var scale = TEMPLATE_WIDTH / fImg.CardWidth;
            var boardWidth = fImg.CardWidth * 8 + fImg.CardGap * 7;
            var boardHeight = fImg.CardOverlap * 6 + fImg.CardHeight;
            using var boardImg = new DirectBitmap((int)(boardWidth * scale), (int)(boardHeight * scale));
            using var g = Graphics.FromImage(boardImg.Bitmap);
            g.CompositingMode = CompositingMode.SourceCopy;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            using (var wrapMode = new ImageAttributes())
            {
                wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                g.DrawImage(img.Bitmap, new Rectangle(0, 0, boardImg.Width, boardImg.Height), fImg.OriginX, fImg.OriginY, (int)boardWidth, (int)boardHeight, GraphicsUnit.Pixel, wrapMode);
            }
            var bImgGrayscale = ToGrayscale(boardImg);
            // Fill tuple score dictionary, which is the RMS of manhattan color distances of aligned pixels
            var scores = new Dictionary<(Card, int), int>();
            for (int col = 0; col < 8; col++)
            {
                var originX = (int)(scale * ((fImg.CardWidth + fImg.CardGap) * col) + 0.5);
                var rowMax = col < 4 ? 7 : 6;
                for (int row = 0; row < rowMax; row++)
                {
                    var originY = (int)(scale * (fImg.CardOverlap * row) + 0.5);
                    foreach (var iconTemplate in _iconTemplates)
                    {
                        var minScore = int.MaxValue;
                        for (int winY = originY - TEMPLATE_WINDOW_SIZE; winY < originY + TEMPLATE_WINDOW_SIZE; winY++)
                        {
                            for (int winX = originX - TEMPLATE_WINDOW_SIZE; winX < originX + TEMPLATE_WINDOW_SIZE; winX++)
                            {
                                long sum = 0;
                                int count = 0;
                                for (int tY = TEMPLATE_EDGE_THRESHOLD; tY < TEMPLATE_HEIGHT - TEMPLATE_EDGE_THRESHOLD; tY++)
                                {
                                    for (int tX = TEMPLATE_EDGE_THRESHOLD; tX < TEMPLATE_EXAMINE_WIDTH - TEMPLATE_EDGE_THRESHOLD; tX++)
                                    {
                                        var diff = iconTemplate.Value[tY, tX] - bImgGrayscale[winY + tY, winX + tX];
                                        sum += diff * diff;
                                        count++;
                                    }
                                }
                                var score = (int)Math.Sqrt(sum / count);
                                if (score < minScore)
                                {
                                    minScore = score;
                                }
                            }
                        }
                        scores[(iconTemplate.Key, row * 8 + col)] = minScore;
                    }
                }
            }
            // Now iterate through the dictionary tuples and remove the best ones
            var cards = new Card[52];
            var orderedPairs = new LinkedList<KeyValuePair<(Card, int), int>>(scores.OrderBy(x => x.Value));
            while (orderedPairs.Any())
            {
                var node = orderedPairs.First;
                if (node.Value.Value > 40) return null;
                var minTuple = node.Value.Key;
                cards[minTuple.Item2] = minTuple.Item1;
                do
                {
                    var next = node.Next;
                    if (node.Value.Key.Item1 == minTuple.Item1 || node.Value.Key.Item2 == minTuple.Item2)
                    {
                        orderedPairs.Remove(node);
                    }
                    node = next;
                }
                while (node != null);
            }

            fImg.Board = new FreecellBoard(cards);

            return fImg;
        }
    }
}
