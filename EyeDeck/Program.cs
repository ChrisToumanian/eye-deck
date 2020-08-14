using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EyeDeck
{
    class Program
    {
        static void Main(string[] args)
        {
            DeckLink.DeckLinkStream decklink = new DeckLink.DeckLinkStream(1);
            decklink.Initialize();

            int pixel;
            bool fullVideo = false;
            bool fullGraphics = false;
            bool raceTag = false;
            bool doubleBox = false;
            bool lowerThird = false;
            bool racing = false;

            while (true)
            {
                Thread.Sleep(50);

                fullVideo = false;
                fullGraphics = false;
                raceTag = false;
                doubleBox = false;
                lowerThird = false;
                racing = false;

                Console.Clear();

                // Race Tag
                if (decklink.PixelMatchesColor(decklink.frameBuffer, 64200, 142, 48, 115) || decklink.PixelMatchesColor(decklink.frameBuffer, 64200, 142, 48, 116))
                {
                    raceTag = true;
                    Console.WriteLine("Race Tag: TRUE");
                }
                else
                    Console.WriteLine("Race Tag: FALSE");
                decklink.PrintColor(decklink.frameBuffer, 64200);

                // Full Graphics
                if (decklink.PixelMatchesColor(decklink.frameBuffer, 25620, 141, 35, 118))
                {
                    fullGraphics = true;
                    Console.WriteLine("Full Graphics: TRUE");
                }
                else
                    Console.WriteLine("Full Graphics: FALSE");
                decklink.PrintColor(decklink.frameBuffer, 25620);

                // Lower Third
                if (decklink.PixelMatchesColor(decklink.frameBuffer, 832030, 140, 61, 119))
                {
                    lowerThird = true;
                    Console.WriteLine("Lower Third: TRUE");
                }
                else
                    Console.WriteLine("Lower Third: FALSE");
                decklink.PrintColor(decklink.frameBuffer, 832030);

                // Full Video
                if (!decklink.PixelMatchesColor(decklink.frameBuffer, 256150, 131, 216, 126))
                {
                    fullVideo = true;
                    Console.WriteLine("Video Under Race Tag: TRUE");
                }
                else
                    Console.WriteLine("Video Under Race Tag: FALSE");
                decklink.PrintColor(decklink.frameBuffer, 256150);

                // Double Box
                if (decklink.PixelMatchesColor(decklink.frameBuffer, 705200, 143, 48, 115))
                {
                    doubleBox = true;
                    Console.WriteLine("Double Box: TRUE");
                }
                else
                    Console.WriteLine("Double Box: FALSE");
                decklink.PrintColor(decklink.frameBuffer, 705200);

                // Racing
                if (raceTag && !lowerThird && fullVideo && !fullGraphics)
                {
                    racing = true;
                    Console.WriteLine("Racing: TRUE");
                }
                else
                    Console.WriteLine("Racing: FALSE");
            }
        }
    }
}
