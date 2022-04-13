using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CodeCrownTool
{
    /// <summary>
    /// DCC Special Quest extractor
    /// </summary>
    public static class SpecialQuestExtractor
    {
        static readonly Dictionary<string, long> QUEST_OFFSETS = new Dictionary<string, long>
        {
            ["26FE8ED953F2F1E47C9BD366D6A52079C39A2D96973454368813D08CBAA354A7"] = 0x3b648, // DCC Special Quest 02
        };

        /// <summary>
        /// Extracts quest file from DCC Special Quest Downloader.
        /// </summary>
        /// <param name="stream">The stream to extract from.</param>
        /// <returns>The extracted quest data, or <c>null</c> if downloader is not recognized.</returns>
        public static byte[] Extract(Stream stream)
        {
            string hash;
            using (var sha = SHA256.Create())
            {
                hash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
            }

            if (QUEST_OFFSETS.TryGetValue(hash, out var offset))
            {
                BinaryReader br = new BinaryReader(stream);
                stream.Seek(offset, SeekOrigin.Begin);
                return br.ReadBytes(0x100000);
            }
            else
            {
                return null;
            }
        }
    }
}
