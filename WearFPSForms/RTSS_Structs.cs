using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WearFPSForms
{
    public struct RTSS_Mem
    {
        public uint signature;
        public uint version;
        public uint appEntrySize;
        public uint appArrOffset;
        public uint appArrSize;
        public uint osdEntrySize;
        public uint osdArrOffset;
        public uint osdArrSize;
        public uint osdFrame;

        public String getInfo()
        {
            if (this.signature == 0xDEAD) return "DEAD";
            else
            {
                char[] sig = new char[4];
                for (int i = 0; i < 4; i++)
                {
                    sig[i] = (char)(0x00FF & this.signature >> (24 - 8 * i));
                }
                short vMajor = (short)((version & 0xFFFF0000) >> 16);
                short vMinor = (short)(version & 0x0000FFFF);
                return new string(sig) + " " + vMajor + "." + vMinor;
            }
        }

        public String getSignature()
        {
            if (this.signature == 0xDEAD) return "DEAD";
            else {
                char[] sig = new char[4];
                for (int i = 0; i < 4; i++)
                {
                    sig[i] = (char)(0x00FF & this.signature >> (24 - 8 * i));
                }
                return new string(sig);
            }
        }

        public short getMinor()
        {
            return (short)(version & 0x0000FFFF);
        }

    }

    /*public unsafe struct RTSS_Osd
    {
        public fixed char OSD[256];
        public fixed char OSDOwner[256];
    }*/

    public struct RTSS_Osd
    {
        public byte[] OSD;
        public byte[] OSDOwner;
        public byte[] OSDEx;

        public RTSS_Osd(byte[] _osd, byte[] _osdowner, byte[] _osdex)
        {
            OSD = _osd;
            OSDOwner = _osdowner;
            OSDEx = _osdex;

        }
    }

    public struct RTSS_App
    {
        public uint procID;
        public byte[] procName;
        public uint flags;

        public uint time0;
        public uint time1;
        public uint frames;
        public uint frameTime;

        public uint statFlags;
        public uint statTime0;
        public uint statTime1;
        public uint statFrames;
        public uint statCount;
        public uint statFramerateMin;
        public uint statFramerateAvg;
        public uint statFramerateMax;

        public uint osdX;
        public uint osdY;
        public uint osdPixel;
        public uint osdColor;
        public uint osdFrame;

        public uint screenCapFlags;
        public char[] screenCapPath;

        public uint osdBackground;

        public uint videoFlags;
        public char[] videoPath;
        public uint videoFramerate;
        public uint videoFramesize;
        public uint videoFormat;
        public uint videoQuality;
        public uint videoCapThreads;

        public uint screenCapQuality;
        public uint screenCapThreads;

        public uint audioCapFlags;

        public uint videoCapFlagsEx;

        public uint audioCapFlags2;

        public uint statFrametimeMin;
        public uint statFrameTimeAvg;
        public uint statFrameTimeMax;
        public uint statFrameTimeCount;

        public uint[] statFrameTimeBuf; /* 1024 */
        public uint statFrameTimeBufPos;
        public uint statFrameTimeBuffFramerate;

        public long audioCapPTTEventPush;
        public long audioCapPTTEventRelease;

        public long audioCapPTTEventPush2;
        public long audioCapPTTEventRelease2;
    }
}
