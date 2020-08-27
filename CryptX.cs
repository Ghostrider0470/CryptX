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
        Spaces = 4,
    }
    internal class CryptXCharset
    {
        public string LowerCaseChars = "abcdefghijklmnopqrstuvwxyz";
        public string UpperCaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        public string NumericalChars = "1234567890";
        public string SpecialChars = "!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~";
        public string Spaces = " ";

    }
    internal class CryptXConfig
    {
        public CryptXCharsetIncludeConfig CharsetConfig { get; set; } = new CryptXCharsetIncludeConfig();
        public byte KeyLength { get; set; } = 12;
        public byte MaximumConsecutiveChars { get; set; } = 2;
        public double SpaceCharProbability { get; set; } = 0;
    }

    internal class CryptXCharsetIncludeConfig
    {
        public bool IncludeLowerCaseChars { get; set; } = true;
        public bool IncludeUpperCaseChars { get; set; } = true;
        public bool IncludeNumericalChars { get; set; } = true;
        public bool IncludeSpecialChars { get; set; } = true;
        public bool IncludeSpaces { get; set; } = true;
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
            byte keyLength = 12,
            byte maximumConsecutiveChars = 2,
            double spaceCharProbability = 0.0d
            )
        {
            #region Config Initialization
            _cryptoHashConfig.CharsetConfig.IncludeLowerCaseChars = includeLowercase;
            _cryptoHashConfig.CharsetConfig.IncludeUpperCaseChars = includeUppercase;
            _cryptoHashConfig.CharsetConfig.IncludeNumericalChars = includeNumeric;
            _cryptoHashConfig.CharsetConfig.IncludeSpecialChars = includeSpecial;
            _cryptoHashConfig.CharsetConfig.IncludeSpaces = spaceCharProbability > 0.0d;
            _cryptoHashConfig.KeyLength = keyLength;
            _cryptoHashConfig.MaximumConsecutiveChars = maximumConsecutiveChars;
            _cryptoHashConfig.SpaceCharProbability = spaceCharProbability;
            #endregion
            SetUnbiasedProbability();
        }

        private void CharDictionaryInitialization()
        {
            if (_cryptoHashConfig.CharsetConfig.IncludeLowerCaseChars)
                _charDictionary.Add(CryptXCharsetEnum.LowerCaseChars, _cryptoHashCharset.LowerCaseChars.ToCharArray());
            if (_cryptoHashConfig.CharsetConfig.IncludeUpperCaseChars)
                _charDictionary.Add(CryptXCharsetEnum.UpperCaseChars, _cryptoHashCharset.UpperCaseChars.ToCharArray());
            if (_cryptoHashConfig.CharsetConfig.IncludeNumericalChars)
                _charDictionary.Add(CryptXCharsetEnum.NumericalChars, _cryptoHashCharset.NumericalChars.ToCharArray());
            if (_cryptoHashConfig.CharsetConfig.IncludeSpecialChars)
                _charDictionary.Add(CryptXCharsetEnum.SpecialChars, _cryptoHashCharset.SpecialChars.ToCharArray());
            if (_cryptoHashConfig.CharsetConfig.IncludeSpaces)
                _charDictionary.Add(CryptXCharsetEnum.Spaces, _cryptoHashCharset.Spaces.ToCharArray());
        }

        private void SetUnbiasedProbability()
        {
            if (_cryptoHashConfig.CharsetConfig.IncludeSpaces)
            {
                foreach (KeyValuePair<CryptXCharsetEnum, char[]> charSet in _charDictionary)
                {
                    if (!probabilities.TryAdd(charSet.Key, 100.0d / _charDictionary.Count))
                    {
                        if (charSet.Key == CryptXCharsetEnum.Spaces)
                        {
                            probabilities[charSet.Key] = _cryptoHashConfig.SpaceCharProbability;
                        }
                        else
                            probabilities[charSet.Key] = (100.0d - _cryptoHashConfig.SpaceCharProbability) / (_charDictionary.Count - 1);
                    }

                }
            }
            else
            {
                foreach (KeyValuePair<CryptXCharsetEnum, char[]> charSet in _charDictionary)
                {
                    if (!probabilities.TryAdd(charSet.Key, 100.0d / _charDictionary.Count))
                    {
                        probabilities[charSet.Key] = 100.0d / _charDictionary.Count;
                    }
                }
            }

        }

        private double SetNegativeBiasProbabilityPenalty(CryptXCharsetEnum charsetEnum, double biasValue, CryptXCharsetEnum? lastNegativeBiasedCharset, double? lastNegativeBiasedValue)
        {
            // Calculated probability is used for logging probabilities
            double? calculatedProbability = null;

            // Gets the enum index of the charset to which the negative bias probability penalty will be applied
            int reducedProbabilityIndex = (int)charsetEnum;

            // Executes if spaces are included
            if (_cryptoHashConfig.CharsetConfig.IncludeSpaces)
            {
                foreach (KeyValuePair<CryptXCharsetEnum, char[]> charSet in _charDictionary)
                {
                    if ((int)charSet.Key == reducedProbabilityIndex)
                    {
                        // Executes if both last bias values are not null 
                        if (lastNegativeBiasedCharset != null && lastNegativeBiasedValue != null)
                        {
                            // One Third of the last bias reserved to be redistributed to the last negative bias
                            double oneThirdOfLastBiasHalf = (double)lastNegativeBiasedValue / 3;

                            // Two thirds of the last bias value divided to equal parts so that it can be redistributed among other probabilities
                            // excluding current negative bias and the last one
                            double twoThirdsOfLastBiasPartitioned = oneThirdOfLastBiasHalf * 2 / (_charDictionary.Count - 2);

                            if (charSet.Key == (CryptXCharsetEnum)lastNegativeBiasedCharset)
                            {
                                // Probability reduced by current bias penalty and improved by one third of the last bias
                                calculatedProbability = biasValue + oneThirdOfLastBiasHalf;

                            }
                            else
                            {
                                // Probability reduced by current bias penalty and improved by two thirds of the last bias
                                calculatedProbability = biasValue + twoThirdsOfLastBiasPartitioned;
                            }
                        }
                        else
                        {
                            // Probability is set to the bias value
                            calculatedProbability = biasValue;
                        }
                    }
                    else
                    {
                        if (charSet.Key == CryptXCharsetEnum.Spaces)
                        {
                            calculatedProbability = _cryptoHashConfig.SpaceCharProbability + biasValue / (_charDictionary.Count - 1);
                        }
                        else
                        {
                            if (charSet.Key == CryptXCharsetEnum.Spaces)
                            {
                                calculatedProbability = _cryptoHashConfig.SpaceCharProbability;
                            }
                            else
                            {
                                calculatedProbability = (100.0d - _cryptoHashConfig.SpaceCharProbability) / (_charDictionary.Count - 1) + biasValue / (_charDictionary.Count - 1);

                            }

                        }
                    }
                    probabilities[charSet.Key] = (double)calculatedProbability;
                    Console.WriteLine($"{charSet.Key}: {calculatedProbability}%");
                }
            }
            // Executes if spaces aren't included
            else
            {
                foreach (KeyValuePair<CryptXCharsetEnum, char[]> charSet in _charDictionary)
                {
                    // Executes if the current charset has suffered the bias penalty
                    if ((int)charSet.Key == reducedProbabilityIndex)
                    {
                        // Executes if both last bias values are not null 
                        if (lastNegativeBiasedCharset != null && lastNegativeBiasedValue != null)
                        {
                            // One Third of the last bias reserved to be redistributed to the last negative bias
                            double oneThirdOfLastBiasHalf = (double)lastNegativeBiasedValue / 3;

                            // Two thirds of the last bias value divided to equal parts so that it can be redistributed among other probabilities
                            // excluding current negative bias and the last one
                            double twoThirdsOfLastBiasPartitioned = oneThirdOfLastBiasHalf * 2 / (_charDictionary.Count - 2);

                            if (charSet.Key == (CryptXCharsetEnum)lastNegativeBiasedCharset)
                            {
                                // Probability reduced by current bias penalty and improved by one third of the last bias
                                calculatedProbability = biasValue + oneThirdOfLastBiasHalf;
                            }
                            else
                            {
                                // Probability reduced by current bias penalty and improved by two thirds of the last bias
                                calculatedProbability = biasValue + twoThirdsOfLastBiasPartitioned;
                            }
                        }
                        else
                        {
                            // Probability is set to the bias value
                            calculatedProbability = biasValue;
                        }
                    }
                    // Executes if this charset hasn't suffered the bias penalty
                    else
                    {
                        // Calculates the equal probability share from total percentage by taking in account the number of total probabilities
                        double equalProbabilityShare = 100.0d / _charDictionary.Count;

                        // Calculates the equal bias share for all the other probabilities from the bias percentage
                        // by taking in account the number of total probabilities except the one who lost the percentage due to bias
                        double equalBiasShare = biasValue / (_charDictionary.Count - 1);

                        // Executes if both last bias values are not null
                        if (lastNegativeBiasedCharset != null && lastNegativeBiasedValue != null)
                        {
                            // One Third of the last bias reserved to be redistributed to the last negative bias
                            double oneThirdOfLastBiasHalf = (double)lastNegativeBiasedValue / 3;

                            // Two thirds of the last bias value divided to equal parts so that it can be redistributed among other probabilities
                            // excluding current negative bias and the last one
                            double twoThirdsOfLastBiasPartitioned = oneThirdOfLastBiasHalf * 2 / (_charDictionary.Count - 2);

                            // Charset which last suffered the bias penalty only gets one third back
                            if (charSet.Key == (CryptXCharsetEnum)lastNegativeBiasedCharset)
                            {
                                calculatedProbability = equalProbabilityShare + equalBiasShare + oneThirdOfLastBiasHalf;
                            }
                            else
                            {
                                // Charsets which didn't suffer the last bias penalty get two thirds 
                                calculatedProbability = equalProbabilityShare + equalBiasShare + twoThirdsOfLastBiasPartitioned;
                            }
                        }
                        // Executes if either of the last bias values are null
                        else
                        {
                            // This improves the probability for this charset as it increases its share
                            calculatedProbability = equalProbabilityShare + equalBiasShare;
                        }

                    }
                    probabilities[charSet.Key] = (double)calculatedProbability;
                    Console.WriteLine($"{charSet.Key}: {calculatedProbability}%");
                }
            }
            return biasValue;
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

            CryptXCharsetEnum? lastNegativeBiasedCharset = null;

            double? lastNegativeBiasedValue = null;

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

                double biasValue = probabilities[charset] / 4;

                // Last negative bias is set after probabilities were altered
                lastNegativeBiasedValue = SetNegativeBiasProbabilityPenalty(charset, biasValue, lastNegativeBiasedCharset, lastNegativeBiasedValue);
                lastNegativeBiasedCharset = charset;



                var rnd = BitConverter.ToUInt32(data, keyIndex * 4);
                var idx = rnd % _charDictionary[charset].Length;

                char value = _charDictionary[charset][(int)idx];
                result.Append(value);

            }
            return result.ToString();
        }
    }
}
