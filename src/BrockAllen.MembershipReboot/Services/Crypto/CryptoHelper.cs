﻿using System;
using System.Linq;
using BrockAllen.MembershipReboot.Helpers;

namespace BrockAllen.MembershipReboot
{
    internal static class CryptoHelper
    {
        internal const char PasswordHashingIterationCountSeparator = '.';
        internal static Func<int> GetCurrentYear = () => DateTime.Now.Year;

        internal static string Hash(string value)
        {
            return Crypto.Hash(value);
        }
        
        internal static string GenerateSalt()
        {
            return Crypto.GenerateSalt();
        }

        internal static string HashPassword(string password)
        {
            var count = SecuritySettings.Instance.PasswordHashingIterationCount;
            if (count <= 0)
            {
                count = GetIterationsFromYear(GetCurrentYear());
            }
            var result = Crypto.HashPassword(password, count);
            return EncodeIterations(count) + PasswordHashingIterationCountSeparator + result;
        }

        internal static bool VerifyHashedPassword(string hashedPassword, string password)
        {
            if (hashedPassword.Contains(PasswordHashingIterationCountSeparator))
            {
                var parts = hashedPassword.Split(PasswordHashingIterationCountSeparator);
                if (parts.Length != 2) return false;

                int count = DecodeIterations(parts[0]);
                if (count <= 0) return false;

                hashedPassword = parts[1];
                
                return Crypto.VerifyHashedPassword(hashedPassword, password, count);
            }
            else
            {
                return Crypto.VerifyHashedPassword(hashedPassword, password);
            }
        }

        internal static string EncodeIterations(int count)
        {
            return count.ToString("X");
        }

        internal static int DecodeIterations(string prefix)
        {
            int val;
            if (Int32.TryParse(prefix, System.Globalization.NumberStyles.HexNumber, null, out val))
            {
                return val;
            }
            return -1;
        }

        // from OWASP : https://www.owasp.org/index.php/Password_Storage_Cheat_Sheet
        const int StartYear = 2000;
        const int StartCount = 1000;
        internal static int GetIterationsFromYear(int year)
        {
            if (year > StartYear)
            {
                var diff = (year - StartYear) / 2;
                var mul = (int)Math.Pow(2, diff);
                int count = StartCount * mul;
                // if we go negative, then we wrapped (expected in year ~2044). 
                // Int32.Max is best we can do at this point
                if (count < 0) count = Int32.MaxValue;
                return count;
            }
            return StartCount;
        }
    }
}
