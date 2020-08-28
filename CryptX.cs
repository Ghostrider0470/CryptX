using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace CryptX
{
    internal enum CryptXCharsetEnum : int
    {
        LowerCaseChars = 0,
        UpperCaseChars = 1,
        NumericalChars = 2,
        SpecialChars = 3,
    }
    internal class CryptXCharset
    {
        public string LowerCaseChars = "abcdefghijklmnopqrstuvwxyz";
        public string UpperCaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        public string NumericalChars = "1234567890";
        public string SpecialChars = "!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~";

    }
    internal class CryptXConfig
    {
        public CryptXCharsetIncludeConfig CharsetConfig { get; set; } = new CryptXCharsetIncludeConfig();
        public byte KeyLength { get; set; } = 12;
    }

    internal class CryptXCharsetIncludeConfig
    {
        public bool IncludeLowerCaseChars { get; set; } = true;
        public bool IncludeUpperCaseChars { get; set; } = true;
        public bool IncludeNumericalChars { get; set; } = true;
        public bool IncludeSpecialChars { get; set; } = true;
    }
    public class CryptXGenerator
    {
        private CryptXConfig _cryptoHashConfig = new CryptXConfig();
        private CryptXCharset _cryptoHashCharset = new CryptXCharset();
        private Dictionary<CryptXCharsetEnum, char[]> _charDictionary = new Dictionary<CryptXCharsetEnum, char[]>();

        private Dictionary<CryptXCharsetEnum, double> probabilities = new Dictionary<CryptXCharsetEnum, double>();

        public CryptXGenerator(
            bool includeLowercase = true,
            bool includeUppercase = true,
            bool includeNumeric = true,
            bool includeSpecial = true,
            [Range(8, 256)]
            byte keyLength = 12
        )
        {
            #region Config Initialization
            _cryptoHashConfig.CharsetConfig.IncludeLowerCaseChars = includeLowercase;
            _cryptoHashConfig.CharsetConfig.IncludeUpperCaseChars = includeUppercase;
            _cryptoHashConfig.CharsetConfig.IncludeNumericalChars = includeNumeric;
            _cryptoHashConfig.CharsetConfig.IncludeSpecialChars = includeSpecial;
            _cryptoHashConfig.KeyLength = keyLength;
            #endregion
            SetUnbiasedProbability();
        }

        private void CharDictionaryInitialization()
        {
            if (_cryptoHashConfig.CharsetConfig.IncludeLowerCaseChars)
                _charDictionary.TryAdd(CryptXCharsetEnum.LowerCaseChars, _cryptoHashCharset.LowerCaseChars.ToCharArray());
            if (_cryptoHashConfig.CharsetConfig.IncludeUpperCaseChars)
                _charDictionary.TryAdd(CryptXCharsetEnum.UpperCaseChars, _cryptoHashCharset.UpperCaseChars.ToCharArray());
            if (_cryptoHashConfig.CharsetConfig.IncludeNumericalChars)
                _charDictionary.TryAdd(CryptXCharsetEnum.NumericalChars, _cryptoHashCharset.NumericalChars.ToCharArray());
            if (_cryptoHashConfig.CharsetConfig.IncludeSpecialChars)
                _charDictionary.TryAdd(CryptXCharsetEnum.SpecialChars, _cryptoHashCharset.SpecialChars.ToCharArray()); ;
        }

        private void SetUnbiasedProbability()
        {
            foreach (KeyValuePair<CryptXCharsetEnum, char[]> charSet in _charDictionary)
            {
                if (!probabilities.TryAdd(charSet.Key, 100.0d / _charDictionary.Count))
                {
                    probabilities[charSet.Key] = 100.0d / _charDictionary.Count;
                }
            }
        }

        private void SetNegativeBiasProbabilityPenalty(CryptXCharsetEnum charsetEnum, double biasValue)
        {
            // Calculated probability is used for logging probabilities
            double calculatedTotal = 0;
            // Gets the enum index of the charset to which the negative bias probability penalty will be applied
            int reducedProbabilityIndex = (int)charsetEnum;

            foreach (KeyValuePair<CryptXCharsetEnum, char[]> charSet in _charDictionary)
            {
                // Executes if the current charset has suffered the bias penalty
                double? calculatedProbability = null;
                if ((int)charSet.Key == reducedProbabilityIndex)
                {
                    // Probability is set to the bias value
                    calculatedProbability = biasValue;

                }
                // Executes if this charset hasn't suffered the bias penalty
                else
                {
                    // Calculates the equal bias share for all the other probabilities from the bias percentage
                    // by taking in account the number of total probabilities except the one who lost the percentage due to bias
                    double equalBiasShare = biasValue / (_charDictionary.Count - 1);

                    // Executes if both last bias values are not null
                    // This improves the probability for this charset as it increases its share
                    //calculatedProbability = equalProbabilityShare + equalBiasShare;
                    calculatedProbability = probabilities[charSet.Key] + equalBiasShare;

                }
                probabilities[charSet.Key] = (double)calculatedProbability;
                calculatedTotal += (double)calculatedProbability;
                Console.WriteLine($"{charSet.Key}: {Math.Round((double)calculatedProbability, 2)}%");
            }

            Console.WriteLine($"Total: {Math.Round((double)calculatedTotal, 4)}%");
            Console.WriteLine("###################");
        }
        public string GenerateUnique()
        {
            CharDictionaryInitialization();

            SetUnbiasedProbability();

            // Data initialized to the size of keySize * 4 since chars are 4 bits long 
            Random random = new Random(DateTime.Now.Millisecond);

            byte[] data = new byte[_cryptoHashConfig.KeyLength * 4];

            StringBuilder result = new StringBuilder(0, _cryptoHashConfig.KeyLength);

            using (RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider())
            {
                crypto.GetBytes(data);
            }

            CryptXCharsetEnum charset = CryptXCharsetEnum.LowerCaseChars;

            for (int keyIndex = 0; keyIndex < _cryptoHashConfig.KeyLength; keyIndex++)
            {
                int percentage = random.Next(0, 100);
                int probabilityOffset = 0;

                foreach (KeyValuePair<CryptXCharsetEnum, double> probability in probabilities)
                {
                    if (percentage <= probabilityOffset + probability.Value)
                    {
                        charset = probability.Key;
                        break;
                    }
                    probabilityOffset += 100 / _charDictionary.Count;

                }

                double biasValue = probabilities[charset] / 2;

                // Last negative bias is set after probabilities were altered
                SetNegativeBiasProbabilityPenalty(charset, biasValue);
                



                var rnd = BitConverter.ToUInt32(data, keyIndex * 4);
                var idx = rnd % _charDictionary[charset].Length;

                char value = _charDictionary[charset][(int)idx];
                result.Append(value);

            }
            return result.ToString();
        }
    }
}
