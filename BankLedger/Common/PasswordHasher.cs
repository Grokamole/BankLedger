// Copyright 2019 Joseph Miller

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace BankLedger.Common
{
    /// <summary>
    /// Used for password hashing with salt.
    /// </summary>
    static public class PasswordHasher
    {
        /// <summary>
        /// Gets CSPRNG random bytes.
        /// </summary>
        /// <param name="numBytes">The number of bytes to fill.</param>
        /// <param name="randomBytes">Recieves the random bytes.</param>
        static public void GetRandomCryptoBytes(int numBytes, out byte[] randomBytes)
        {
            randomBytes = new byte[numBytes];
            PasswordHasher.randomServiceProvider.GetBytes(randomBytes);
        }

        /// <summary>
        /// Get a salt hashed password for password storage.
        /// </summary>
        /// <param name="password">The password to hash.</param>
        /// <returns>A string represented </returns>
        static public HashedPassword GetSaltHashedPassword(string password, string salt=null)
        {
            if (null == password)
            {
                return new HashedPassword("", "");
            }
            byte[] saltBytes;
            if (null == salt)
            {
                GetRandomCryptoBytes(KEY_LENGTH, out saltBytes);
            }
            else
            {
                saltBytes = Convert.FromBase64String(salt);
            }
            List<byte> saltedPassword = new List<byte>();
            saltedPassword.AddRange(Encoding.UTF8.GetBytes(password));
            saltedPassword.AddRange(saltBytes);
            byte[] digestMessage = SHA512.Create().ComputeHash(saltedPassword.ToArray());
            return new HashedPassword(Convert.ToBase64String(digestMessage), Convert.ToBase64String(saltBytes));
        }

        /// <summary>
        /// Holds salt and hashed password.
        /// </summary>
        public class HashedPassword: IEquatable<HashedPassword>
        {
            /// <summary>
            /// Sets the salt and password.
            /// </summary>
            /// <param name="password">The hashed password to save.</param>
            /// <param name="salt">The salt of the hashed password to save.</param>
            public HashedPassword(string password, string salt)
            {
                Salt = salt;
                Password = password;
            }

            /// <summary>
            /// Checks if the hashed password (only) is the same.
            /// </summary>
            /// <param name="other">The HashedPassword to check for equality.</param>
            /// <returns>true if the hashed password matches. false otherwise.</returns>
            public bool Equals(HashedPassword other)
            {
                return (other.Password == this.Password);
            }

            /// <summary>
            /// The hashed password's salt.
            /// </summary>
            public string Salt { get; }

            /// <summary>
            /// The hashed password. 
            /// </summary>
            public string Password { get; }
        }

        /// <summary>
        /// The number of bytes in the key length.
        /// </summary>
        private const int KEY_LENGTH = 64;

        /// <summary>
        /// A secure RNG provider for sessionID generation
        /// </summary>
        private static readonly RNGCryptoServiceProvider randomServiceProvider = new RNGCryptoServiceProvider();
    }
}
