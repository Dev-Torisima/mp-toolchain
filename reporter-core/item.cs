// ----------------------------
// 「Imqutive Codes」から抜粋
// ----------------------------

using System;
using System.Linq;

namespace Item
{
    public static class BitChanger
    {
        public static byte ToByte(bool value)
        {
            if (value) return 1;
            else return 0;
        }

        public static bool ToBool(byte value)
        {
            return value is 1;
        }

        public static byte[] ToBytes(byte value)
        {
            return new byte[] { value };
        }

        public static byte[] ToBytes(bool value)
        {
            return new byte[] { ToByte(value) };
        }

        public static byte[] ToBytes(short value, ByteOrder type)
        {
            if (type == ByteOrder.Auto) return BitConverter.GetBytes(value);
            else if (BitConverter.IsLittleEndian)
            {
                if (type == ByteOrder.LittleEndian) return BitConverter.GetBytes(value);
                else return BitConverter.GetBytes(value).Reverse().ToArray();
            }
            else
            {
                if (type == ByteOrder.BigEndian) return BitConverter.GetBytes(value);
                else return BitConverter.GetBytes(value).Reverse().ToArray();
            }
        }

        public static byte[] ToBytes(ushort value, ByteOrder type)
        {
            if (type == ByteOrder.Auto) return BitConverter.GetBytes(value);
            else if (BitConverter.IsLittleEndian)
            {
                if (type == ByteOrder.LittleEndian) return BitConverter.GetBytes(value);
                else return BitConverter.GetBytes(value).Reverse().ToArray();
            }
            else
            {
                if (type == ByteOrder.BigEndian) return BitConverter.GetBytes(value);
                else return BitConverter.GetBytes(value).Reverse().ToArray();
            }
        }

        public static byte[] ToBytes(int value, ByteOrder type)
        {
            if (type == ByteOrder.Auto) return BitConverter.GetBytes(value);
            else if (BitConverter.IsLittleEndian)
            {
                if (type == ByteOrder.LittleEndian) return BitConverter.GetBytes(value);
                else return BitConverter.GetBytes(value).Reverse().ToArray();
            }
            else
            {
                if (type == ByteOrder.BigEndian) return BitConverter.GetBytes(value);
                else return BitConverter.GetBytes(value).Reverse().ToArray();
            }
        }

        public static byte[] ToBytes(uint value, ByteOrder type)
        {
            if (type == ByteOrder.Auto) return BitConverter.GetBytes(value);
            else if (BitConverter.IsLittleEndian)
            {
                if (type == ByteOrder.LittleEndian) return BitConverter.GetBytes(value);
                else return BitConverter.GetBytes(value).Reverse().ToArray();
            }
            else
            {
                if (type == ByteOrder.BigEndian) return BitConverter.GetBytes(value);
                else return BitConverter.GetBytes(value).Reverse().ToArray();
            }
        }

        public static byte[] ToBytes(long value, ByteOrder type)
        {
            if (type == ByteOrder.Auto) return BitConverter.GetBytes(value);
            else if (BitConverter.IsLittleEndian)
            {
                if (type == ByteOrder.LittleEndian) return BitConverter.GetBytes(value);
                else return BitConverter.GetBytes(value).Reverse().ToArray();
            }
            else
            {
                if (type == ByteOrder.BigEndian) return BitConverter.GetBytes(value);
                else return BitConverter.GetBytes(value).Reverse().ToArray();
            }
        }

        public static byte[] ToBytes(ulong value, ByteOrder type)
        {
            if (type == ByteOrder.Auto) return BitConverter.GetBytes(value);
            else if (BitConverter.IsLittleEndian)
            {
                if (type == ByteOrder.LittleEndian) return BitConverter.GetBytes(value);
                else return BitConverter.GetBytes(value).Reverse().ToArray();
            }
            else
            {
                if (type == ByteOrder.BigEndian) return BitConverter.GetBytes(value);
                else return BitConverter.GetBytes(value).Reverse().ToArray();
            }
        }

        public static byte[] ToBytes(float value, ByteOrder type)
        {
            if (type == ByteOrder.Auto) return BitConverter.GetBytes(value);
            else if (BitConverter.IsLittleEndian)
            {
                if (type == ByteOrder.LittleEndian) return BitConverter.GetBytes(value);
                else return BitConverter.GetBytes(value).Reverse().ToArray();
            }
            else
            {
                if (type == ByteOrder.BigEndian) return BitConverter.GetBytes(value);
                else return BitConverter.GetBytes(value).Reverse().ToArray();
            }
        }

        public static byte[] ToBytes(Half value, ByteOrder type)
        {
            if (type == ByteOrder.Auto) return BitConverter.GetBytes(value);
            else if (BitConverter.IsLittleEndian)
            {
                if (type == ByteOrder.LittleEndian) return BitConverter.GetBytes(value);
                else return BitConverter.GetBytes(value).Reverse().ToArray();
            }
            else
            {
                if (type == ByteOrder.BigEndian) return BitConverter.GetBytes(value);
                else return BitConverter.GetBytes(value).Reverse().ToArray();
            }
        }

        public static byte[] ToBytes(double value, ByteOrder type)
        {
            if (type == ByteOrder.Auto) return BitConverter.GetBytes(value);
            else if (BitConverter.IsLittleEndian)
            {
                if (type == ByteOrder.LittleEndian) return BitConverter.GetBytes(value);
                else return BitConverter.GetBytes(value).Reverse().ToArray();
            }
            else
            {
                if (type == ByteOrder.BigEndian) return BitConverter.GetBytes(value);
                else return BitConverter.GetBytes(value).Reverse().ToArray();
            }
        }


        public static sbyte ToInt8(byte[] value)
        {
            if ((byte)(value[0] & 0b10000000) is 0)
            {
                return (sbyte)((byte)(value[0] & 0b01111111));
            }
            else
            {
                return (sbyte)(-1 * (sbyte)((byte)(value[0] & 0b01111111)));
            }
        }

        public static sbyte ToInt8(byte value)
        {
            if ((byte)(value & 0b10000000) is 0)
            {
                return (sbyte)((byte)(value & 0b01111111));
            }
            else
            {
                return (sbyte)(-1 * (sbyte)((byte)(value & 0b01111111)));
            }
        }

        public static sbyte ToInt8(byte[] value, int index)
        {
            if ((byte)(value[index] & 0b10000000) is 0)
            {
                return (sbyte)((byte)(value[index] & 0b01111111));
            }
            else
            {
                return (sbyte)(-1 * (sbyte)((byte)(value[index] & 0b01111111)));
            }
        }

        public static byte ToUInt8(byte[] value)
        {
            return value[0];
        }

        public static short ToInt16(byte[] value, ByteOrder type)
        {
            if (type == ByteOrder.Auto) return BitConverter.ToInt16(value);
            else if (BitConverter.IsLittleEndian)
            {
                if (type == ByteOrder.LittleEndian) return BitConverter.ToInt16(value);
                else return BitConverter.ToInt16(value.Reverse().ToArray());
            }
            else
            {
                if (type == ByteOrder.BigEndian) return BitConverter.ToInt16(value);
                else return BitConverter.ToInt16(value.Reverse().ToArray());
            }
        }

        public static int ToInt32(byte[] value, ByteOrder type)
        {
            if (type == ByteOrder.Auto) return BitConverter.ToInt32(value);
            else if (BitConverter.IsLittleEndian)
            {
                if (type == ByteOrder.LittleEndian) return BitConverter.ToInt32(value);
                else return BitConverter.ToInt32(value.Reverse().ToArray());
            }
            else
            {
                if (type == ByteOrder.BigEndian) return BitConverter.ToInt32(value);
                else return BitConverter.ToInt32(value.Reverse().ToArray());
            }
        }

        public static long ToInt64(byte[] value, ByteOrder type)
        {
            if (type == ByteOrder.Auto) return BitConverter.ToInt64(value);
            else if (BitConverter.IsLittleEndian)
            {
                if (type == ByteOrder.LittleEndian) return BitConverter.ToInt64(value);
                else return BitConverter.ToInt64(value.Reverse().ToArray());
            }
            else
            {
                if (type == ByteOrder.BigEndian) return BitConverter.ToInt64(value);
                else return BitConverter.ToInt64(value.Reverse().ToArray());
            }
        }

        public static ushort ToUInt16(byte[] value, ByteOrder type)
        {
            if (type == ByteOrder.Auto) return BitConverter.ToUInt16(value);
            else if (BitConverter.IsLittleEndian)
            {
                if (type == ByteOrder.LittleEndian) return BitConverter.ToUInt16(value);
                else return BitConverter.ToUInt16(value.Reverse().ToArray());
            }
            else
            {
                if (type == ByteOrder.BigEndian) return BitConverter.ToUInt16(value);
                else return BitConverter.ToUInt16(value.Reverse().ToArray());
            }
        }

        public static uint ToUInt32(byte[] value, ByteOrder type)
        {
            if (type == ByteOrder.Auto) return BitConverter.ToUInt32(value);
            else if (BitConverter.IsLittleEndian)
            {
                if (type == ByteOrder.LittleEndian) return BitConverter.ToUInt32(value);
                else return BitConverter.ToUInt32(value.Reverse().ToArray());
            }
            else
            {
                if (type == ByteOrder.BigEndian) return BitConverter.ToUInt32(value);
                else return BitConverter.ToUInt32(value.Reverse().ToArray());
            }
        }

        public static ulong ToUInt64(byte[] value, ByteOrder type)
        {
            if (type == ByteOrder.Auto) return BitConverter.ToUInt64(value);
            else if (BitConverter.IsLittleEndian)
            {
                if (type == ByteOrder.LittleEndian) return BitConverter.ToUInt64(value);
                else return BitConverter.ToUInt64(value.Reverse().ToArray());
            }
            else
            {
                if (type == ByteOrder.BigEndian) return BitConverter.ToUInt64(value);
                else return BitConverter.ToUInt64(value.Reverse().ToArray());
            }
        }

        public static Half ToHalf(byte[] value, ByteOrder type)
        {
            if (type == ByteOrder.Auto) return BitConverter.ToHalf(value);
            else if (BitConverter.IsLittleEndian)
            {
                if (type == ByteOrder.LittleEndian) return BitConverter.ToHalf(value);
                else return BitConverter.ToHalf(value.Reverse().ToArray());
            }
            else
            {
                if (type == ByteOrder.BigEndian) return BitConverter.ToHalf(value);
                else return BitConverter.ToHalf(value.Reverse().ToArray());
            }
        }

        public static Single ToSingle(byte[] value, ByteOrder type)
        {
            if (type == ByteOrder.Auto) return BitConverter.ToSingle(value);
            else if (BitConverter.IsLittleEndian)
            {
                if (type == ByteOrder.LittleEndian) return BitConverter.ToSingle(value);
                else return BitConverter.ToSingle(value.Reverse().ToArray());
            }
            else
            {
                if (type == ByteOrder.BigEndian) return BitConverter.ToSingle(value);
                else return BitConverter.ToSingle(value.Reverse().ToArray());
            }
        }

        public static Double ToDouble(byte[] value, ByteOrder type)
        {
            if (type == ByteOrder.Auto) return BitConverter.ToDouble(value);
            else if (BitConverter.IsLittleEndian)
            {
                if (type == ByteOrder.LittleEndian) return BitConverter.ToDouble(value);
                else return BitConverter.ToDouble(value.Reverse().ToArray());
            }
            else
            {
                if (type == ByteOrder.BigEndian) return BitConverter.ToDouble(value);
                else return BitConverter.ToDouble(value.Reverse().ToArray());
            }
        }

        public static byte ToUInt8(byte[] value, int startindex)
        {
            return value[startindex];
        }

        public static short ToInt16(byte[] value, ByteOrder type, int startindex)
        {
            if (type == ByteOrder.Auto) return BitConverter.ToInt16(value, startindex);
            else if (BitConverter.IsLittleEndian)
            {
                if (type == ByteOrder.LittleEndian) return BitConverter.ToInt16(value, startindex);
                else return BitConverter.ToInt16(value.Reverse().ToArray(), startindex);
            }
            else
            {
                if (type == ByteOrder.BigEndian) return BitConverter.ToInt16(value, startindex);
                else return BitConverter.ToInt16(value.Reverse().ToArray(), startindex);
            }
        }

        public static int ToInt32(byte[] value, ByteOrder type, int startindex)
        {
            if (type == ByteOrder.Auto) return BitConverter.ToInt32(value, startindex);
            else if (BitConverter.IsLittleEndian)
            {
                if (type == ByteOrder.LittleEndian) return BitConverter.ToInt32(value, startindex);
                else return BitConverter.ToInt32(value.Reverse().ToArray(), startindex);
            }
            else
            {
                if (type == ByteOrder.BigEndian) return BitConverter.ToInt32(value, startindex);
                else return BitConverter.ToInt32(value.Reverse().ToArray(), startindex);
            }
        }

        public static long ToInt64(byte[] value, ByteOrder type, int startindex)
        {
            if (type == ByteOrder.Auto) return BitConverter.ToInt64(value, startindex);
            else if (BitConverter.IsLittleEndian)
            {
                if (type == ByteOrder.LittleEndian) return BitConverter.ToInt64(value, startindex);
                else return BitConverter.ToInt64(value.Reverse().ToArray(), startindex);
            }
            else
            {
                if (type == ByteOrder.BigEndian) return BitConverter.ToInt64(value, startindex);
                else return BitConverter.ToInt64(value.Reverse().ToArray(), startindex);
            }
        }

        public static ushort ToUInt16(byte[] value, ByteOrder type, int startindex)
        {
            if (type == ByteOrder.Auto) return BitConverter.ToUInt16(value, startindex);
            else if (BitConverter.IsLittleEndian)
            {
                if (type == ByteOrder.LittleEndian) return BitConverter.ToUInt16(value, startindex);
                else return BitConverter.ToUInt16(value.Reverse().ToArray(), startindex);
            }
            else
            {
                if (type == ByteOrder.BigEndian) return BitConverter.ToUInt16(value, startindex);
                else return BitConverter.ToUInt16(value.Reverse().ToArray(), startindex);
            }
        }

        public static uint ToUInt32(byte[] value, ByteOrder type, int startindex)
        {
            if (type == ByteOrder.Auto) return BitConverter.ToUInt32(value, startindex);
            else if (BitConverter.IsLittleEndian)
            {
                if (type == ByteOrder.LittleEndian) return BitConverter.ToUInt32(value, startindex);
                else return BitConverter.ToUInt32(value.Reverse().ToArray(), startindex);
            }
            else
            {
                if (type == ByteOrder.BigEndian) return BitConverter.ToUInt32(value, startindex);
                else return BitConverter.ToUInt32(value.Reverse().ToArray(), startindex);
            }
        }

        public static ulong ToUInt64(byte[] value, ByteOrder type, int startindex)
        {
            if (type == ByteOrder.Auto) return BitConverter.ToUInt64(value, startindex);
            else if (BitConverter.IsLittleEndian)
            {
                if (type == ByteOrder.LittleEndian) return BitConverter.ToUInt64(value, startindex);
                else return BitConverter.ToUInt64(value.Reverse().ToArray(), startindex);
            }
            else
            {
                if (type == ByteOrder.BigEndian) return BitConverter.ToUInt64(value, startindex);
                else return BitConverter.ToUInt64(value.Reverse().ToArray(), startindex);
            }
        }

        public static Half ToHalf(byte[] value, ByteOrder type, int startindex)
        {
            if (type == ByteOrder.Auto) return BitConverter.ToHalf(value, startindex);
            else if (BitConverter.IsLittleEndian)
            {
                if (type == ByteOrder.LittleEndian) return BitConverter.ToHalf(value, startindex);
                else return BitConverter.ToHalf(value.Reverse().ToArray(), startindex);
            }
            else
            {
                if (type == ByteOrder.BigEndian) return BitConverter.ToHalf(value, startindex);
                else return BitConverter.ToHalf(value.Reverse().ToArray(), startindex);
            }
        }

        public static Single ToSingle(byte[] value, ByteOrder type, int startindex)
        {
            if (type == ByteOrder.Auto) return BitConverter.ToSingle(value, startindex);
            else if (BitConverter.IsLittleEndian)
            {
                if (type == ByteOrder.LittleEndian) return BitConverter.ToSingle(value, startindex);
                else return BitConverter.ToSingle(value.Reverse().ToArray(), startindex);
            }
            else
            {
                if (type == ByteOrder.BigEndian) return BitConverter.ToSingle(value, startindex);
                else return BitConverter.ToSingle(value.Reverse().ToArray(), startindex);
            }
        }

        public static Double ToDouble(byte[] value, ByteOrder type, int startindex)
        {
            if (type == ByteOrder.Auto) return BitConverter.ToDouble(value, startindex);
            else if (BitConverter.IsLittleEndian)
            {
                if (type == ByteOrder.LittleEndian) return BitConverter.ToDouble(value, startindex);
                else return BitConverter.ToDouble(value.Reverse().ToArray(), startindex);
            }
            else
            {
                if (type == ByteOrder.BigEndian) return BitConverter.ToDouble(value, startindex);
                else return BitConverter.ToDouble(value.Reverse().ToArray(), startindex);
            }
        }
    }

    public enum ByteOrder : byte
    {
        Auto = 0,
        LittleEndian = 1,
        BigEndian = 2,
    }
}
