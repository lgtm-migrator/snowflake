﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Snowflake.Romfile;
using Snowflake.Service;

namespace Snowflake.FileSignatures.SONY_PSX_ISO
{
    public sealed class SonyPSXISOFileSignature : FileSignature
    {
        [ImportingConstructor]
        public SonyPSXISOFileSignature([Import("coreInstance")] ICoreService coreInstance)
            : base(Assembly.GetExecutingAssembly(), coreInstance)
        {
            this.HeaderOffset = 1024*128;
            this.HeaderSignature = Encoding.UTF8.GetBytes("PLAYSTATION");
            
        }

        public override long HeaderOffset { get; }
        public override byte[] HeaderSignature { get; }

        public override bool HeaderSignatureMatches(string fileName)
        {
            using (FileStream isoStream = File.Open(fileName, FileMode.Open))
            {
                byte[] buffer = new byte[this.HeaderOffset]; // read the first 256 KiB
                isoStream.Read(buffer, 0, buffer.Length);
                string code = Encoding.UTF8.GetString(buffer);
                return code.Contains(Encoding.UTF8.GetString(this.HeaderSignature));
            }
        }

        public override string GetGameId(string fileName)
        {
            using (FileStream isoStream = File.Open(fileName, FileMode.Open))
            {
                byte[] buffer = new byte[this.HeaderOffset]; // read the first 10 kilobytes
                isoStream.Read(buffer, 0, buffer.Length);

                string raw =
                    new String(
                        Encoding.UTF8.GetString(buffer)
                            .Replace("\0", "")
                            .Where(
                                c =>
                                    char.IsLetter(c) || char.IsDigit(c) || char.IsPunctuation(c) || char.IsWhiteSpace(c))
                            .ToArray());
                string match = Regex.Match(raw,
                "(SLUS|SLES|SLPM|SLED|SLPS|SCUS|SCES|SCED|SCPS|ESPM|SIPS|PBPX|SLKA)_[0-9][0-9][0-9].[0-9][0-9];", RegexOptions.IgnoreCase).Value;
                return match.Replace(";", "").Replace(".", "");
            }
        }
    }
}
