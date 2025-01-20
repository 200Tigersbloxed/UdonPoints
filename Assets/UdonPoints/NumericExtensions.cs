using System;
using UnityEngine;

namespace UdonPoints
{
    public static class NumericExtensions
    {
        public static double MoneyToDouble(this decimal amount) => (double) amount;
        public static float MoneyToFloat(this decimal amount) => (float) amount;

        public static decimal DoubleToMoney(this double amount)
        {
            if (amount >= (double)decimal.MaxValue) return decimal.MaxValue - 1;
            if (amount <= (double)decimal.MinValue) return decimal.MinValue + 1;
            return (decimal)amount;
        }

        public static decimal FloatToMoney(this float amount)
        {
            if (amount >= (float)decimal.MaxValue) return decimal.MaxValue - 1;
            if (amount <= (float)decimal.MinValue) return decimal.MinValue + 1;
            return (decimal)amount;
        }
        
        public static decimal ClampToMoneyRange(this decimal value)
        {
            if (value >= decimal.MaxValue) return decimal.MaxValue - 1;
            if (value <= decimal.MinValue) return decimal.MinValue + 1;
            return value;
        }

        public static byte[] ToBytes(this decimal amount)
        {
            int[] bits = decimal.GetBits(amount);
            byte[] bytes = new byte[16];
            Buffer.BlockCopy(bits, 0, bytes, 0, 16);
            return bytes;
        }
        
        public static byte[] ToBytes(this decimal[] decimals)
        {
            int decimalCount = decimals.Length;
            byte[] result = new byte[4 + (decimalCount * 16)];
            Buffer.BlockCopy(BitConverter.GetBytes(decimalCount), 0, result, 0, 4);
            for (int i = 0; i < decimalCount; i++)
            {
                byte[] decimalBytes = decimals[i].ToBytes();
                Buffer.BlockCopy(decimalBytes, 0, result, 4 + (i * 16), 16);
            }
            return result;
        }

        public static decimal ToDecimal(this byte[] bytes)
        {
            // This NEEDS to throw an error when it fails
            decimal[] errorme = new decimal[0];
            if (bytes.Length != 16) return errorme[1];
            int[] bits = new int[4];
            Buffer.BlockCopy(bytes, 0, bits, 0, 16);
            return new decimal(bits);
        }
        
        public static decimal[] ToDecimals(this byte[] data)
        {
            int decimalCount = BitConverter.ToInt32(data, 0);
            decimal[] decimals = new decimal[decimalCount];
            for (int i = 0; i < decimalCount; i++)
            {
                byte[] decimalBytes = new byte[16];
                Buffer.BlockCopy(data, 4 + (i * 16), decimalBytes, 0, 16);
                decimals[i] = decimalBytes.ToDecimal();
            }
            return decimals;
        }
        
        public static decimal SafeAdd(decimal left, decimal right)
        {
            if (right > 0 && left > decimal.MaxValue - right) return decimal.MaxValue - 1;
            if (right < 0 && left < decimal.MinValue - right) return decimal.MinValue + 1;
            return left + right;
        }

        public static decimal SafeSubtract(decimal left, decimal right)
        {
            if (right < 0 && left > decimal.MaxValue + right) return decimal.MaxValue - 1;
            if (right > 0 && left < decimal.MinValue + right) return decimal.MinValue + 1;
            return left - right;
        }

        public static decimal SafeMultiply(decimal left, decimal right)
        {
            if (left == 0 || right == 0) return 0;

            if (left > 0 && right > 0 && left > decimal.MaxValue / right) return decimal.MaxValue - 1;
            if (left > 0 && right < 0 && left > decimal.MinValue / right) return decimal.MinValue + 1;
            if (left < 0 && right > 0 && left < decimal.MinValue / right) return decimal.MinValue + 1;
            if (left < 0 && right < 0 && left < decimal.MaxValue / right) return decimal.MaxValue - 1;

            return left * right;
        }

        public static decimal SafeDivide(decimal left, decimal right)
        {
            if (right == 0) return left / 1;
            return left / right;
        }
        
        public static string ConvertToHex(this Color color)
        {
            int red = (int)Math.Round(color.r * 255);
            int green = (int)Math.Round(color.g * 255);
            int blue = (int)Math.Round(color.b * 255);
            int alpha = (int)Math.Round(color.a * 255);
            return $"#{red:X2}{green:X2}{blue:X2}{alpha:X2}";
        }
    }
}