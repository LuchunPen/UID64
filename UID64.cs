/*
Copyright (c) Luchunpen.
Date: 19.04.2016 21:12:44
*/

using System;
using System.Globalization;
using System.IO;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Nano3
{
    [Serializable]
    public struct UID64 : ISerializable, IEquatable<UID64>
    {
        //private static readonly string stringUID = "ABC18FEEF9D97407";
        private static readonly byte[] firstChar = { 160, 176, 192, 208, 224, 240 };
        public static readonly UID64 EMPTY = 0;

        private static object _sync = new object();
        private static RandomXor random;
        private static uint tick = 1;

        private static Stopwatch timer;
        private static long startTick;

        private long _data;
        public long Data { get { return _data; } private set { _data = value; } }

        static UID64()
        {
            startTick = DateTime.Now.Ticks;
            random = new RandomXor(startTick.GetHashCode());
            timer = Stopwatch.StartNew();
        }

        #region DT

        private const long TicksPerMillisecond = 10000;
        private const long TicksPerSecond = TicksPerMillisecond * 1000;
        private const long TicksPerMinute = TicksPerSecond * 60;
        private const long TicksPerHour = TicksPerMinute * 60;
        private const long TicksPerDay = TicksPerHour * 24;
        private const int DaysPerYear = 365;
        private const int DaysPer4Years = DaysPerYear * 4 + 1;       // 1461
        private const int DaysPer100Years = DaysPer4Years * 25 - 1;  // 36524
        private const int DaysPer400Years = DaysPer100Years * 4 + 1; // 146097
        
        private static readonly int[] DaysToMonth365 = { 0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365 };
        private static readonly int[] DaysToMonth366 = { 0, 31, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335, 366 };

        #endregion DT
        public static UID64 CreateNew()
        {
            long ticks = timer.ElapsedTicks + startTick;
            tick++;

            int n = (int)(ticks / TicksPerDay);
            int y400 = n / DaysPer400Years;
            n -= y400 * DaysPer400Years;
            int y100 = n / DaysPer100Years;
            if (y100 == 4) y100 = 3;
            n -= y100 * DaysPer100Years;
            int y4 = n / DaysPer4Years;
            n -= y4 * DaysPer4Years;
            int y1 = n / DaysPerYear;
            if (y1 == 4) y1 = 3;
            int year = y400 * 400 + y100 * 100 + y4 * 4 + y1 + 1;
            n -= y1 * DaysPerYear;
            bool leapYear = y1 == 3 && (y4 != 24 || y100 == 3);
            int m = n >> 5 + 1;
            if (leapYear) { while (n >= DaysToMonth366[m]) m++; }
            else { while (n >= DaysToMonth365[m]) m++; }
            uint l = (uint)((year - 2010) * 31536000 + m * 2592000 + (n + 1) * 86400 + (int)((ticks / TicksPerHour) % 24) * 3600 + (int)((ticks / TicksPerMinute) % 60) * 60 + (int)((ticks / TicksPerSecond) % 60));
            long data = tick;
            if (tick < 256) { int r = random.Next(16777216); data |= (uint)(r << 8); }
            else if (tick < 65536) { int r = random.Next(65536); data |= (uint)(r << 16); }
            else if (tick < 16777216) { int r = random.Next(256); data |= (uint)(r << 24); }
            byte b = (byte)(l >> 24);
            b = (byte)((b & 15) | firstChar[random.Next(6)]);
            data |= (long)(16777215 & l) << 32 | (long)b << 56;

            return new UID64(-data);
        }
        public static UID64 CreateNewSync()
        {
            lock (_sync) { return CreateNew(); }
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext ctx) { info.AddValue("Uid64", Data); }

        public UID64(long n) { _data = n; }
        public UID64(byte[] b)
        {
            _data = ((long)b[0] | (long)b[1] << 8 | (long)b[2] << 16 | (long)b[3] << 24
                    | (long)b[4] << 32 | (long)b[5] << 40 | (long)b[6] << 48 | (long)b[7] << 56);
        }

        public byte[] Serialize()
        {
            byte[] b = new byte[8];
            b[0] = (byte)(_data); b[1] = (byte)(_data >> 8);
            b[2] = (byte)(_data >> 16); b[3] = (byte)(_data >> 24);
            b[4] = (byte)(_data >> 32); b[5] = (byte)(_data >> 40);
            b[6] = (byte)(_data >> 48); b[7] = (byte)(_data >> 56);
            return b;
        }
        public void Serialize(byte[] b, int pos)
        {
            b[pos + 0] = (byte)(_data); b[pos + 1] = (byte)(_data >> 8);
            b[pos + 2] = (byte)(_data >> 16); b[pos + 3] = (byte)(_data >> 24);
            b[pos + 4] = (byte)(_data >> 32); b[pos + 5] = (byte)(_data >> 40);
            b[pos + 6] = (byte)(_data >> 48); b[pos + 7] = (byte)(_data >> 56);
        }
        public void Serialize(BinaryWriter bw)
        {
            bw.Write(_data);
        }
        public void Deserialize(BinaryReader br)
        {
            _data = br.ReadInt64();
        }

        public static UID64 Deserialize(byte[] b)
        {
            UID64 u;
            u._data = ((long)b[0] | (long)b[1] << 8 | (long)b[2] << 16 | (long)b[3] << 24
                    | (long)b[4] << 32 | (long)b[5] << 40 | (long)b[6] << 48 | (long)b[7] << 56);
            return u;
        }
        public static UID64 Deserialize(byte[] b, int _position)
        {
            UID64 u;
            u._data = 0;
            u._data = ((long)b[_position] | (long)b[_position + 1] << 8 | (long)b[_position + 2] << 16 | (long)b[_position + 3] << 24
                        | (long)b[_position + 4] << 32 | (long)b[_position + 5] << 40 | (long)b[_position + 6] << 48 | (long)b[_position + 7] << 56);
            return u;
        }
        public static UID64 LoadFromString(string s)
        {
            UID64 u = EMPTY;
            if (string.IsNullOrEmpty(s) || s == "0" || s.Length < 16) return EMPTY;
            if (!System.Text.RegularExpressions.Regex.IsMatch(s, @"\A\b[0-9a-fA-F]+\b\Z")) return EMPTY;
            if (s.Length == 16) long.TryParse(s, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out u._data);
            return u;
        }

        public static implicit operator string(UID64 value) { return value._data.ToString("X").ToUpper(); }
        public static implicit operator UID64(string value) { return LoadFromString(value); }
        public static implicit operator UID64(long value) { return new UID64(value); }

        public static bool operator !=(UID64 uid1, UID64 uid2) { return uid1._data != uid2._data; }
        public static bool operator ==(UID64 uid1, UID64 uid2) { return uid1._data == uid2._data; }
        public bool Equals(UID64 uid) { return _data == uid._data; }

        public override bool Equals(object obj) { if (obj is UID64) return _data == ((UID64)obj)._data; return false; }
        public override int GetHashCode() { return ((int)_data) ^ (int)(_data >> 32); }
        public override string ToString() { return _data.ToString("X").ToUpper(); }
    }


    public class RandomXor
    {
        //private static readonly string stringUID = "ED0EE19475E78D01";

        private const double intMod = 1.0 / ((double)int.MaxValue + 1.0);
        private const double uintMod = 1.0 / ((double)uint.MaxValue + 1.0);
        private const uint Y = 842502087, Z = 3579807591, W = 273326509;
        private uint x, y, z, w;

        public RandomXor() : this(Environment.TickCount) { }

        public RandomXor(int seed)
        {
            x = (uint)seed; y = Y; z = Z; w = W;
        }

        public int Next()
        {
            uint t = (x ^ (x << 11));
            x = y; y = z; z = w;
            w = (w ^ (w >> 19)) ^ (t ^ (t >> 8));
            int i = (int)w;
            return i < 0 ? -i : i;
        }

        public int Next(int max)
        {
            int m = max < 0 ? -max : max;
            uint t = (x ^ (x << 11));
            x = y; y = z; z = w;
            w = (w ^ (w >> 19)) ^ (t ^ (t >> 8));
            return (int)((intMod * (int)(0x7FFFFFFF & w)) * max);
        }

        public int Next(int min, int max)
        {
            int m = max < 0 ? -max : max;
            uint t = (x ^ (x << 11));
            x = y; y = z; z = w;

            int range = max - min;
            range = range < 0 ? -range : range;
            w = (w ^ (w >> 19)) ^ (t ^ (t >> 8));
            return min + (int)((intMod * (int)(0x7FFFFFFF & w)) * range);
        }
    }
}

