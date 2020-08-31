using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Text;

namespace CryptX
{
    // Defines different types of charset
    internal enum CharsetTypes : int
    {
        LowerCaseChars = 0,
        UpperCaseChars = 1,
        NumericalChars = 2,
        SpecialChars = 3,
    }
    // This class is designed to hold the information about what 'chars' belong to a specific Charset 
    internal class Charsets
    {
        public string LowerCaseChars { get; set; } = "abcdefghijklmnopqrstuvwxyz";
        public string UpperCaseChars { get; set; } = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        public string NumericalChars { get; set; } = "1234567890";
        public string SpecialChars { get; set; } = "!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~";
    }
    // This class is used for holding information about what Charset combination has the user specified
    internal class CharsetIncludes
    {
        public bool IncludeLowerCaseChars { get; set; } = true;
        public bool IncludeUpperCaseChars { get; set; } = true;
        public bool IncludeNumericalChars { get; set; } = true;
        public bool IncludeSpecialChars { get; set; } = true;
    }

    // This class is used to hold all the information the user has specified for the key generation
    internal sealed class Configuration
    {
        public CharsetIncludes Includes { get; set; } = new CharsetIncludes();
        public int KeyLength { get; set; } = 12;
        public Charsets Charsets = new Charsets();
    }

    /// <summary>
    /// This class is used for quick and easy generation of cryptographically secure keys
    /// </summary>
    public class KeyGenerator
    {
        private readonly Configuration _configuration = new Configuration();

        private Dictionary<CharsetTypes, char[]> _charDictionary = new Dictionary<CharsetTypes, char[]>();

        private Dictionary<CharsetTypes, double> _probabilities = new Dictionary<CharsetTypes, double>();

        /// <summary>
        /// Key value in bytes
        /// </summary>
        private byte[] _data = null;

        /// <summary>
        /// Key value
        /// </summary>
        private string _key = null;

        /// <summary>
        /// Sets or Gets the value which represents the length of the key that will be generated
        /// </summary>
        public int KeyLength
        {
            get => _configuration.KeyLength;
            set => _configuration.KeyLength = value;
        }


        /// <summary>
        /// Returns the number of the included character sets which will be used in key generation
        /// </summary>
        public int IncludesCount => _charDictionary.Count;

        /// <summary>
        /// <strong>Parameterized KeyGenerator constructor for custom creation of cryptographically secure keys</strong> 
        /// <br/>
        /// Every parameter is optional and has a default value
        /// </summary>
        /// <param name="includeLowercase">Should the key contain lowercase characters.<br/> Defaults to True.</param>
        /// <param name="includeUppercase">Should the key contain uppercase characters.<br/> Defaults to True.</param>
        /// <param name="includeNumeric">Should the key contain numeric characters.<br/> Defaults to True.</param>
        /// <param name="includeSpecial">Should the key contain special characters.<br/> Defaults to True.</param>
        /// <param name="keyLength"> Sets the the length of the key that will be generated.<br/> Defaults to 12.</param>
        public KeyGenerator(
            bool includeLowercase = true,
            bool includeUppercase = true,
            bool includeNumeric = true,
            bool includeSpecial = true,
            byte keyLength = 12
        )
        {
            // Sets the configuration according to the data provided in the properties of the constructor
            SetIncludes(includeLowercase, includeUppercase, includeNumeric, includeSpecial);

            // Sets the key length to the one provided in the constructor parameter
            KeyLength = keyLength;

            CharDictionaryInitialization();
        }

        /// <summary>
        /// Sets the include properties in the configuration
        /// </summary>
        /// <param name="includeLowercase">Should the key contain lowercase characters.<br/> Defaults to True.</param>
        /// <param name="includeUppercase">Should the key contain uppercase characters.<br/> Defaults to True.</param>
        /// <param name="includeNumeric">Should the key contain numeric characters.<br/> Defaults to True.</param>
        /// <param name="includeSpecial">Should the key contain special characters.<br/> Defaults to True.</param>
        public void SetIncludes(bool includeLowercase = true, bool includeUppercase = true, bool includeNumeric = true, bool includeSpecial = true)
        {
            _configuration.Includes.IncludeLowerCaseChars = includeLowercase;
            _configuration.Includes.IncludeUpperCaseChars = includeUppercase;
            _configuration.Includes.IncludeNumericalChars = includeNumeric;
            _configuration.Includes.IncludeSpecialChars = includeSpecial;

        }

        /// <summary>
        /// Initializes the CharDictionary to an empty dictionary and adds only the enabled 
        /// </summary>
        private void CharDictionaryInitialization()
        {
            _charDictionary = new Dictionary<CharsetTypes, char[]>();
            if (_configuration.Includes.IncludeLowerCaseChars)
                _charDictionary.TryAdd(CharsetTypes.LowerCaseChars, _configuration.Charsets.LowerCaseChars.ToCharArray());
            if (_configuration.Includes.IncludeUpperCaseChars)
                _charDictionary.TryAdd(CharsetTypes.UpperCaseChars, _configuration.Charsets.UpperCaseChars.ToCharArray());
            if (_configuration.Includes.IncludeNumericalChars)
                _charDictionary.TryAdd(CharsetTypes.NumericalChars, _configuration.Charsets.NumericalChars.ToCharArray());
            if (_configuration.Includes.IncludeSpecialChars)
                _charDictionary.TryAdd(CharsetTypes.SpecialChars, _configuration.Charsets.SpecialChars.ToCharArray()); ;
        }

        /// <summary>
        /// Sets equal probabilities for each charset included
        /// </summary>
        private void SetUnbiasedProbability()
        {
            _probabilities = new Dictionary<CharsetTypes, double>();
            foreach (KeyValuePair<CharsetTypes, char[]> charSet in _charDictionary)
            {
                _probabilities.TryAdd(charSet.Key, 100.0d / IncludesCount);
            }
        }
        /// <summary>
        /// Sets the penalty for the charset which generated the last character by setting its probability to the provided bias value
        /// <br/>
        /// The rest of the charsets get the same value distributed among them 
        /// </summary>
        /// <param name="charsetTypes"></param>
        /// <param name="biasValue">Value which will be </param>
        private void SetNegativeBiasProbabilityPenalty(CharsetTypes charsetTypes, double biasValue)
        {
            // Gets the absolute value of the biasValue in case of negative numbers
            double bias = Math.Abs(biasValue);

            // Calculated probability is used for logging probabilities
            double calculatedTotal = 0;
            // Gets the enum index of the charset to which the negative bias probability penalty will be applied
            int reducedProbabilityIndex = (int)charsetTypes;

            foreach (KeyValuePair<CharsetTypes, char[]> charSet in _charDictionary)
            {
                // Executes if the current charset has suffered the bias penalty
                double? calculatedProbability = null;
                if ((int)charSet.Key == reducedProbabilityIndex)
                {
                    // Probability is set to the bias value
                    calculatedProbability = bias;
                }
                // Executes if this charset hasn't suffered the bias penalty in this iteration
                else
                {
                    // Calculates the equal bias share for all the other probabilities from the bias percentage
                    // by taking in account the number of total probabilities except the one who lost the percentage due to bias
                    double equalBiasShare = bias / (IncludesCount - 1);

                    // Executes if both last bias values are not null
                    // This improves the probability for this charset as it increases its share
                    //calculatedProbability = equalProbabilityShare + equalBiasShare;
                    calculatedProbability = (_probabilities[charSet.Key] + equalBiasShare);

                }
                _probabilities[charSet.Key] = (double)calculatedProbability;
                calculatedTotal += (double)calculatedProbability;
#if DEBUG
                Console.WriteLine($"{charSet.Key}: {Math.Round((double)calculatedProbability, 2)}%");
#endif
            }
#if DEBUG
            Console.WriteLine($"Total: {Math.Round((double)calculatedTotal, 4)}%");
            Console.WriteLine("###################");
#endif
        }

        public void GenerateKey()
        {
            CharDictionaryInitialization();

            SetUnbiasedProbability();

            // Data initialized to the size of keySize * 4 since chars are 4 bits long 
            Random random = new Random(DateTime.Now.Millisecond);

            byte[] data = new byte[_configuration.KeyLength * 4];

            StringBuilder result = new StringBuilder(0, _configuration.KeyLength);

            using (RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider())
            {
                crypto.GetBytes(data);
            }

            CharsetTypes charset = CharsetTypes.LowerCaseChars;

            for (int keyIndex = 0; keyIndex < _configuration.KeyLength; keyIndex++)
            {
                int percentage = random.Next(0, 100);
                int probabilityOffset = 0;

                foreach (KeyValuePair<CharsetTypes, double> probability in _probabilities)
                {
                    if (percentage <= probabilityOffset + probability.Value)
                    {
                        charset = probability.Key;
                        break;
                    }
                    probabilityOffset += 100 / IncludesCount;

                }

                double biasValue =_probabilities[charset]/2;

                // Last negative bias is set after probabilities were altered
                SetNegativeBiasProbabilityPenalty(charset, biasValue);

                var rnd = BitConverter.ToUInt32(data, keyIndex * 4);
                var idx = rnd % _charDictionary[charset].Length;


                char value = _charDictionary[charset][(int)idx];
                result.Append(value);

            }

            _data = Encoding.ASCII.GetBytes(result.ToString());
            _key = result.ToString();
        }
        public string GetKey()
        {
            return _key;
        }
        public byte[] GetKeyBytes()
        {
            return _data;
        }
        public string Get_UTF8_Encoded_Key()
        {
            return Encoding.UTF8.GetString(_data);
        }
        public string Get_Base64_Encoded_Key()
        {
            return Convert.ToBase64String(_data);
        }
    }
}
