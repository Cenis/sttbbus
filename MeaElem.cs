using System;
using static SttBbusCanAnalyzer.MainWindow;

namespace SttBbusCanAnalyzer
{
    public class MeaElem : IEquatable<MeaElem>
    {

        public string Id { get; set; }
        public string Data { get; set; }
        public byte MEA_Addr { get; set; }
        public byte VT { get; set; }
        public byte MEA_TypeNum { get; set; }
        public string MEA_TypeStr { get; set; }
        public string ConAlias { get; set; }
        public byte[] DataArr { get; set; }
        public byte[] OutputState { get; set; }
        public byte[] InputState { get; set; }
        public byte[] FctAssigned { get; set; }
        public bool[] OutputActive { get; set; }

        public string SwVersion { get; set; }

        public MeaElem()
        {
            //fctAssigned = new byte[8];
            //dataArr = new ushort[8];
        }

        public MeaElem(ushort MEA_TypeNum)
        {
            this.MEA_TypeNum = (byte)MEA_TypeNum;

            switch (MEA_TypeNum)
            {
                case 0: //MEA20
                    DataArr = new byte[6];
                    InputState = new byte[8];
                    OutputActive = new bool[2];
                    FctAssigned = new byte[2];
                    break;
                case 1: //MEA20S
                    DataArr = new byte[6];
                    InputState = new byte[2];
                    OutputActive = new bool[2];
                    FctAssigned = new byte[2];
                    break;
                case 2: //MEA20AT
                    DataArr = new byte[6];
                    InputState = null;
                    OutputActive = new bool[8];
                    FctAssigned = new byte[8];
                    break;
                case 3: //MEA20i
                    DataArr = new byte[7];
                    InputState = new byte[4];
                    OutputActive = new bool[4];
                    FctAssigned = new byte[4];
                    break;
                case 255:
                    DataArr = new byte[10];
                    InputState = new byte[10];
                    OutputActive = new bool[10];
                    FctAssigned = new byte[10];
                    break;
            }
        }

        public static byte MEAType_Str2Num(string meaTypeStr)
        {
            return meaTypeStr switch
            {
                "MEA20" or "MEA20a" => 0,
                "MEA20S" or "MEA20m" => 1,
                "MEA20-AT" => 2,
                "MEA20i" => 3,
                "MD" => 255,
                _ => 0,
            };
        }


        public void ResetInputs()
        {

            switch (MEA_TypeNum)
            {
                case 0: //MEA20
                case 1: //MEA20S
                case 3: //MEA20i
                    for (byte i = 0; i < InputState.Length; i++)
                    {
                        InputState[i] = 2; //2 means open circuit --> not connected 
                    }
                    break;
                case 255: //MD
                    for (byte i = 0; i < InputState.Length; i++)
                    {
                        InputState[i] = 0; //0 means open circuit --> not connected 
                    }
                    break;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj is not MeaElem objAsPart) return false;
            else return Equals(objAsPart);
        }

        public override int GetHashCode()
        {
            return MEA_Addr;
        }

        public bool Equals(MeaElem other)
        {
            if (other == null) return false;
            return MEA_Addr.Equals(other.MEA_Addr);
        }

        public void ActivateFct(byte fctID, bool activate)
        {
            ioDescriptor fctOutput = new();
            for (byte i = 0; i < FctAssigned.Length; i++)
            {
                if (FctAssigned[i] == fctID)
                {
                    OutputActive[i] = activate;
                    //if ((MEA_Addr == 1) && (VT == 1) && (activate) && (i == 0))
                    /*
                    if(mDoor != null)
                    {
                        try
                        {
                            fctOutput = new ioDescriptor() { meaAddr = MEA_Addr, vt = VT, channelID = Convert.ToByte(i + 1) };
                            if (mDoor.output.Equals(fctOutput))
                            {
                                mDoor.activateOutput(activate);
                            }
                        }
                        catch (NullReferenceException) { }
                    }
                    */
                }
            }
        }

        public void SetInput(byte inputID, byte newState)
        {

            //if (MEA_TypeNum == 0)
            //{
            InputState[inputID - 1] = newState;
            //}

        }

        public byte GetCanData(byte[] canDataBytes)
        {
            byte msgLen = 6;

            switch (MEA_TypeNum)
            {
                case 0: //MEA20               
                    DataArr[0] = Convert.ToByte((InputState[0]) | (InputState[1] << 4));
                    DataArr[1] = Convert.ToByte((InputState[2]) | (InputState[3] << 4));
                    DataArr[2] = Convert.ToByte((InputState[4]) | (InputState[5] << 4));
                    DataArr[3] = Convert.ToByte((InputState[6]) | (InputState[7] << 4));
                    DataArr[4] = 0x9A;
                    DataArr[5] = (byte)(0x1B | (Convert.ToByte(OutputActive[0]) << 6) | (Convert.ToByte(OutputActive[1]) << 7));
                    break;

                case 1: //MEA20S
                    DataArr[0] = Convert.ToByte((InputState[0]) | (InputState[1] << 4));
                    DataArr[1] = 0x22;   //Always 0x22 for MEA20S
                    DataArr[2] = 0x22;   //Always 0x22 for MEA20S
                    DataArr[3] = 0x22;   //Always 0x22 for MEA20S
                    DataArr[4] = 0xCC;
                    DataArr[5] = (byte)(0x1B | (Convert.ToByte(OutputActive[0]) << 6) | (Convert.ToByte(OutputActive[1]) << 7));
                    break;

                case 2: //MEA20AT
                    DataArr[0] = 0xCC;
                    DataArr[1] = 0xCC;
                    DataArr[2] = 0xCC;
                    DataArr[3] = 0xCC;
                    DataArr[4] = (byte)((Convert.ToByte(OutputActive[0]) << 0) | (Convert.ToByte(OutputActive[1]) << 1) |
                                        (Convert.ToByte(OutputActive[2]) << 2) | (Convert.ToByte(OutputActive[3]) << 3) |
                                        (Convert.ToByte(OutputActive[4]) << 4) | (Convert.ToByte(OutputActive[5]) << 5) |
                                        (Convert.ToByte(OutputActive[6]) << 6) | (Convert.ToByte(OutputActive[7]) << 7));
                    DataArr[5] = 0x1B;
                    break;

                case 3: //MEA20i
                    msgLen = 7;
                    DataArr[0] = Convert.ToByte((InputState[0]) | (InputState[1] << 4));
                    DataArr[1] = Convert.ToByte((InputState[2]) | (InputState[3] << 4));
                    DataArr[2] = 0x22;   //Always 0x22 for MEA20i
                    DataArr[3] = 0xA9;
                    DataArr[4] = 0xCC;
                    DataArr[5] = (byte)(0x1B | (Convert.ToByte(OutputActive[0]) << 6) | (Convert.ToByte(OutputActive[1]) << 7));
                    DataArr[6] = (byte)((Convert.ToByte(OutputActive[2]) << 0) | (Convert.ToByte(OutputActive[3]) << 1));
                    break;

                default: //not a valid MEA type, return message length 0
                    msgLen = 0;
                    return msgLen;
            }

            if (canDataBytes != null)
            {
                for (byte i = 0; i < msgLen; i++)
                {
                    canDataBytes[i] = DataArr[i];
                }
            }
            else
            {
                Console.WriteLine("Data is null");
            }

            return msgLen;
        }

        public byte GetCanData(byte[] canDataBytes, byte md_chID)
        {
            byte msgLen;

            switch (MEA_TypeNum)
            {
                case 255:  //MD30
                    msgLen = 2;
                    DataArr[0] = md_chID;
                    DataArr[1] = Convert.ToByte((InputState[md_chID - 1]));
                    break;

                default:   //If function was called for anything else than type MD30
                    msgLen = 0;
                    return msgLen;
            }

            if (canDataBytes != null)
            {
                for (byte i = 0; i < msgLen; i++)
                {
                    canDataBytes[i] = DataArr[i];
                }
            }
            else
            {
                Console.WriteLine("Data is null");
            }

            return msgLen;
        }
    }
}
