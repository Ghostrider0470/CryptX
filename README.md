# CryptX
## Allows you to quickly generate secure passwords and tokens in three steps

### Installation:
NuGet Package:
| Description |  Link |
| ------ | ------ |
| NuGet Page | https://www.nuget.org/packages/CryptX/ |
| Download NuGet Package | https://www.nuget.org/api/v2/package/CryptX |

### Usage:
> #### 1st Option: (Quick and Easy)
```C#
// Step one: Instantiate the generator
KeyGenerator generator = new KeyGenerator();

// Step one: Generate the key
generator.GenerateKey();

// Step three: Obtain the key
var key = generator.GetKey();
```
> #### 2nd Option: (Parametarized approach)
The constructor has optional parameters
```C#
// Step one: Instantiate the generator
KeyGenerator generator = new KeyGenerator(
includeLowercase: true, // default
includeUppercase:true,  // default
includeNumeric: true,  // default
includeSpecial:true,  // default
keyLength: 12);  // default

// Step one: Generate the key
generator.GenerateKey();

// Step three: Obtain the key
var key = generator.GetKey();
```

> #### Additional Options: 
 > 
 Propeties:
 ```C#
   // Returns the number of the included character sets which will be used in key generation
   generator.IncludesCount
   ```
 ```C#
   // Gets the number of the unique characters in the all the charsets combined
   generator.NumberOfUniqueChars
   ```
   
 ```C#
   // Sets or Gets the value which represents the length of the key that will be generated
   generator.KeyLength
   ```
   
