using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using DeckLinkAPI;

namespace DeckLink
{
    public enum DeckLinkDisplayMode
    {
        HD1080i5994 = 1214854457,
        HD1080i60 = 1214854704,
        HD1080p5994 = 1215313209,
        HD1080p60 = 1215313456
    }

    public partial class DeckLinkStream : IDeckLinkDeviceNotificationCallback, IDeckLinkInputCallback, IDeckLinkScreenPreviewCallback, IDeckLinkKeyer
    {
        private IDeckLink DeckLink = null;

        private readonly _BMDAudioSampleRate _AudioSampleRate = _BMDAudioSampleRate.bmdAudioSampleRate48kHz;
        private readonly _BMDAudioSampleType _AudioSampleType = _BMDAudioSampleType.bmdAudioSampleType32bitInteger;
        private readonly uint _AudioChannels = 2;

        private IDeckLinkMutableVideoFrame videoFrame = null;
        private _BMDDisplayMode displayMode = _BMDDisplayMode.bmdModeHD720p5994; // display mode
        private _BMDDisplayMode inputDisplayMode = _BMDDisplayMode.bmdModeHD720p5994; // input display mode

        private IDeckLinkOutput deckLinkOutput = null;
        private IDeckLinkInput deckLinkInput = null;
        private IDeckLinkConfiguration deckLinkConfig = null;

        public IntPtr OpenGLBuffer;

        private long displayTime = 0;
        private long displayDuration = 100;
        private long timeScale = 0;

        IDeckLinkKeyer deckLinkKeyer = null;

        private int deckLinkCard = 0;
        private bool cardWorking = false;
        private bool internalKeying = false;
        private int keyingLevel = 255;
        public bool frameReady = true;
        public bool keyerEnabled = false;

        public byte[] frameBuffer;
        public int videoWidth = 1280;
        public int videoHeight = 720;

        // Constructor
        public DeckLinkStream(int card)
        {
            deckLinkCard = card;
            cardWorking = GetHardware();
            cardWorking = GetIDeckLinkInterface();
        }

        // Initialize DeckLink Stream
        public bool Initialize()
        {
            if (!cardWorking)
            {
                return false;
            }

            // DeckLink Input Callback
            deckLinkInput.SetCallback(this);
            deckLinkInput.SetScreenPreviewCallback(this);
            var flags = _BMDVideoInputFlags.bmdVideoInputFlagDefault | _BMDVideoInputFlags.bmdVideoInputEnableFormatDetection;
            var format = _BMDPixelFormat.bmdFormat8BitYUV;
            //var format = _BMDPixelFormat.bmdFormat8BitARGB;
            //var display = _BMDDisplayMode.bmdModeHD1080p5994; // input display mode
            _BMDDisplayModeSupport support;
            IDeckLinkDisplayMode tmp;
            deckLinkInput.DoesSupportVideoMode(inputDisplayMode, format, flags, out support, out tmp);
            if (support != _BMDDisplayModeSupport.bmdDisplayModeSupported)
                throw new Exception("display mode not working: " + support);

            // Keyer
            if (keyerEnabled)
            {
                if (internalKeying)
                {
                    deckLinkKeyer.Enable(0); // 1: External alpha output, 2: Internal
                    deckLinkKeyer.SetLevel((byte)keyingLevel);
                }
                else
                {
                    deckLinkKeyer.Enable(1); // 1: External alpha output, 2: Internal
                }
            }

            // Enable Input Stream
            deckLinkInput.EnableVideoInput(inputDisplayMode, format, flags);
            deckLinkInput.EnableAudioInput(_AudioSampleRate, _AudioSampleType, _AudioChannels);
            deckLinkInput.StartStreams();

            // Enable Video Output
            //deckLinkOutput.EnableVideoOutput(displayMode, _BMDVideoOutputFlags.bmdVideoOutputFlagDefault);

            return true;
        }

        // Draw
        public void Draw()
        {
            deckLinkOutput.DisplayVideoFrameSync(videoFrame);
        }

        // Draw Frame
        public void DrawFrame(IDeckLinkVideoFrame theFrame)
        {
        }

        // Get VideoFrame
        unsafe public void* GetVideoFramePointer()
        {
            deckLinkOutput.CreateVideoFrame(1920, 1080, 1920 * 4, _BMDPixelFormat.bmdFormat8BitBGRA, _BMDFrameFlags.bmdFrameFlagFlipVertical, out videoFrame);
            IntPtr frameBytes;
            videoFrame.GetBytes(out frameBytes);
            return frameBytes.ToPointer();
        }

        // Set Output DisplayMode
        public void SetOutputDisplayMode(DeckLinkDisplayMode mode)
        {
            displayMode = (_BMDDisplayMode)mode;
        }

        // Set Input DisplayMode
        public void SetInputDisplayMode(DeckLinkDisplayMode mode)
        {
            inputDisplayMode = (_BMDDisplayMode)mode;
        }

        // Set Field Flicker Removal
        public void SetFieldFlickerRemoval(bool fieldFlickerRemoval)
        {
            if (fieldFlickerRemoval)
            {
                deckLinkConfig.SetFlag(_BMDDeckLinkConfigurationID.bmdDeckLinkConfigFieldFlickerRemoval, 1);
            }
            else
            {
                deckLinkConfig.SetFlag(_BMDDeckLinkConfigurationID.bmdDeckLinkConfigFieldFlickerRemoval, 0);
            }
        }

        // Loopback Enabled
        public void SetInternalKeying(bool enabled)
        {
            internalKeying = enabled;
        }

        // Keyer Enabled
        public void SetKeyer(bool enabled)
        {
            keyerEnabled = enabled;
        }

        // Set Keyer Opacity Level
        public void SetKeyerOpacity(int level)
        {
            keyingLevel = level;
        }

        // Check DeckLink Hardware
        public bool GetHardware()
        {
            // Create the COM instance
            IDeckLinkIterator deckLinkIterator = new CDeckLinkIterator();
            if (deckLinkIterator == null)
            {
                Console.Write("Deck link drivers are not installed!", "Error");
                return false;
            }

            // Get the DeckLink card
            for (int i = 0; i < deckLinkCard; i++)
            {
                deckLinkIterator.Next(out DeckLink);
            }

            if (DeckLink == null)
            {
                Console.Write("No connected decklink device found", "Error");
                return false;
            }

            string displayName;
            string modelName;

            DeckLink.GetDisplayName(out displayName);
            DeckLink.GetModelName(out modelName);

            // Compatibility: DeckLink 4K Extreme, DeckLink Quad 2
            Console.Write(string.Format("Device chosen: {0}", displayName, modelName));

            return true;
        }

        // Get IDeckLink Interface
        private bool GetIDeckLinkInterface()
        {
            try
            {
                deckLinkOutput = (IDeckLinkOutput)DeckLink;
                deckLinkInput = (IDeckLinkInput)DeckLink;
                deckLinkConfig = (IDeckLinkConfiguration)DeckLink;
                IDeckLinkDisplayModeIterator displayIterator;
                deckLinkInput.GetDisplayModeIterator(out displayIterator);
                var supportedModes = new List<IDeckLinkDisplayMode>();
                deckLinkKeyer = (IDeckLinkKeyer)DeckLink;
            }
            catch
            {
                return false;
            }
            return true;
        }

        // Video Input Frame Arrival
        public void VideoInputFrameArrived(IDeckLinkVideoInputFrame videoFrame, IDeckLinkAudioInputPacket audioPacket)
        {
            IntPtr data;
            int size = videoFrame.GetRowBytes() * videoFrame.GetHeight();

            videoFrame.GetBytes(out data);

            byte[] array = new byte[size];
            Marshal.Copy(data, array, 0, size);

            frameBuffer = array;

            /*int pixel;
            bool fullVideo = false;
            bool fullGraphics = false;
            bool raceTag = false;
            bool doubleBox = false;
            bool lowerThird = false;
            bool racing = false;

            Console.Clear();

            // Race Tag
            if (PixelMatchesColor(array, 64200, 142, 48, 115) || PixelMatchesColor(array, 64200, 142, 48, 116))
            {
                raceTag = true;
                Console.WriteLine("Race Tag: TRUE");
            }
            else
                Console.WriteLine("Race Tag: FALSE");
            PrintColor(array, 64200);

            // Full Graphics
            if (PixelMatchesColor(array, 25620, 141, 35, 118))
            {
                fullGraphics = true;
                Console.WriteLine("Full Graphics: TRUE");
            }
            else
                Console.WriteLine("Full Graphics: FALSE");
            PrintColor(array, 25620);

            // Lower Third
            if (PixelMatchesColor(array, 832030, 140, 61, 119))
            {
                lowerThird = true;
                Console.WriteLine("Lower Third: TRUE");
            }
            else
                Console.WriteLine("Lower Third: FALSE");
            PrintColor(array, 832030);

            // Full Video
            if (!PixelMatchesColor(array, 256150, 131, 216, 126))
            {
                fullVideo = true;
                Console.WriteLine("Full Video: TRUE");
            }
            else
                Console.WriteLine("Full Video: FALSE");
            PrintColor(array, 256150);

            // Double Box
            if (PixelMatchesColor(array, 705200, 143, 48, 115))
            {
                doubleBox = true;
                Console.WriteLine("Double Box: TRUE");
            }
            else
                Console.WriteLine("Double Box: FALSE");
            PrintColor(array, 705200);

            // Racing
            if (raceTag && !lowerThird && fullVideo && !fullGraphics)
            {
                racing = true;
                Console.WriteLine("Racing: TRUE");
            }
            else
                Console.WriteLine("Racing: FALSE"); */
        }

        public bool PixelMatchesColor(byte[] array, int pixel, int y, int u, int v)
        {
            if (array != null)
            {
                if ((array[pixel * 2] == y || array[pixel * 2] == y + 1 || array[pixel * 2] == y - 1) && (array[pixel * 2 + 1] == u || array[pixel * 2 + 1] == u + 1 || array[pixel * 2 + 1] == u - 1) && (array[pixel * 2 + 2] == v || array[pixel * 2 + 2] == v + 1 || array[pixel * 2 + 2] == v - 1))
                    return true;
            }

            return false;
        }

        public void PrintColor(int x, int y)
        {
            int pixel = videoWidth * y + x;

        }

        public void PrintColor(byte[] array, int pixel)
        {
            if (array != null)
            {
                Console.Write(array[pixel * 2] + ", ");
                Console.Write(array[pixel * 2 + 1] + ", ");
                Console.WriteLine(array[pixel * 2 + 2] + "\n");
            }
        }

        // DeckLink Device Arrived
        public void DeckLinkDeviceArrived(IDeckLink deckLinkDevice)
        {
        }

        // DeckLink Device Removed
        public void DeckLinkDeviceRemoved(IDeckLink deckLinkDevice)
        {
        }

        // Video Input Format Changed
        public void VideoInputFormatChanged(_BMDVideoInputFormatChangedEvents notificationEvents, IDeckLinkDisplayMode newDisplayMode, _BMDDetectedVideoInputFormatFlags detectedSignalFlags)
        {
        }

        // DeckLink Keyer
        public void Enable(int isExternal)
        {
        }

        // Set Level
        public void SetLevel(byte level)
        {
        }

        // Ramp Up
        public void RampUp(uint numberOfFrames)
        {
        }

        // Ramp Down
        public void RampDown(uint numberOfFrames)
        {
        }

        // Disable
        public void Disable()
        {
        }
    }
}
