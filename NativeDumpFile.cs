using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NativeGenerator
{
    public class NativeDumpFile
    {
        public class NativeEntry
        {
            public string Name { get; set; }

            public long Hash { get; set; }
            public long FunctionOffset { get; set; }
        }

        public static readonly int Header = 0x5654414E; // 'NATV'

        public int Version { get; set; }

        public List<NativeEntry> Natives { get; set; }

        public NativeEntry this[long hash]
        {
            get { return Natives.FirstOrDefault((n) => (n.Hash == hash)); }
        }

        public static NativeDumpFile Open(string filename)
        {
            using (var fs = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var f = new BinaryReader(fs))
            {
                if (f.ReadInt32() != Header)
                    throw new InvalidOperationException("The specified file is not a native dump.");

                var version = f.ReadInt32();
                var count = f.ReadInt32();

                var natives = new List<NativeEntry>(count);

                if (version == 1)
                {
                    try
                    {
                        // natives come directly after
                        for (int i = 0; i < count; i++)
                        {
                            var hash = f.ReadInt64();
                            var funcOffset = f.ReadInt64();

                            var name = $"$UNK__{hash:X}";

                            natives.Add(new NativeEntry() {
                                Name = name,

                                Hash = hash,
                                FunctionOffset = funcOffset,
                            });
                        }
                    }
                    catch (EndOfStreamException)
                    {
                        throw new InvalidOperationException($"Failed to load native dump file -- expected to read {count} entries but reached the end of the stream.");
                    }

                    // there may still be junk at the end of the file, but we'll ignore it
                }
                else
                {
                    throw new InvalidOperationException($"Unsupported native dump file version ({version}).");
                }

                return new NativeDumpFile() {
                    Natives = natives,
                    Version = version,
                };
            }
        }
    }
}
