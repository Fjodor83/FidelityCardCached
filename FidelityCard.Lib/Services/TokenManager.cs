using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.XPath;

namespace FidelityCard.Lib.Services;

public static class TokenManager
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890";
    public static string Generate()
    {
        var result = new StringBuilder();

        for (int i = 0; i < Alphabet.Length; i++) {
            var n = RandomNumberGenerator.GetInt32(0, Alphabet.Length);
            result.Append(Alphabet[n]);
        }
        return result.ToString();
    }
}
