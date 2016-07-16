using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace TO2NameFix
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            this.Load += MainForm_Load;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            AnalyzeFile(@"dump.mem", @"TO2_encode.txt");
            this.Close();
        }

        struct NameRecord
        {
            public ushort[] codes;
            public string name;
        }

        struct TO2Names
        {
            public NameRecord legion;
            public NameRecord hero;
            public List<NameRecord> members;
        }
        
        NameRecord ReadName(FileStream stream, Dictionary<ushort, char> codes)
        {
            var name = new NameRecord();
            var buffer = new List<byte>();
            var codebuffer = new List<ushort>();

            var position = 0;
            do
            {
                var b = stream.ReadByte();
                buffer.Add((byte)b);
                if (b == 0)
                    break;

                if(position == buffer.Count - 1) // the top one
                {
                    ushort code = buffer[position];
                    if(codes.ContainsKey(code)) // found single character
                    {
                        name.name += codes[code];
                        position += 1;
                        codebuffer.Add(code);
                    }
                }
                else if (position == buffer.Count -2)
                {
                    ushort code = (ushort)(buffer[position] + buffer[position + 1] * 256);
                    if (codes.ContainsKey(code)) // found single character
                    {
                        name.name += codes[code];
                        position += 2;
                        codebuffer.Add(code);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[Error] Wrong Code!!");
                        codebuffer.Add(0);
                        break;
                    }
                }                
            } while (position < 32); // incase we are reading crazy buffer
            name.codes = codebuffer.ToArray();

            return name;
        }

        // generate a CMF file
        const int STRING_LEN = 8;

        int WriteTestNameCMF(ushort[] keys, int pos, StreamWriter writer, int offset)
        {
            int maxChar = Math.Min(STRING_LEN, keys.Length - pos);
            for (int i = 0; i < maxChar; i++)
            {
                var key = keys[pos + i];
                if (key <= 0xFF)
                {
                    writer.WriteLine(string.Format("_L 0x0{0:X7} 0x{1:X8}", offset++, (byte)key));
                }
                else
                {
                    var bytes = BitConverter.GetBytes(key);
                    writer.WriteLine(string.Format("_L 0x0{0:X7} 0x{1:X8}", offset++, bytes[0]));
                    writer.WriteLine(string.Format("_L 0x0{0:X7} 0x{1:X8}", offset++, bytes[1]));
                }
            }

            if(maxChar > 0)
            {
                writer.WriteLine(string.Format("_L 0x0{0:X7} 0x{1:X8}", offset, 0));
                writer.WriteLine("");
            }

            return maxChar;
        }

        int WriteTestCodeCMF(StreamWriter writer, string memfile, ushort[] keys, int start)
        {
            // write test code
            int pos = start;
            using (FileStream stream = new FileStream(memfile, FileMode.Open))
            {
                //pos += WriteTestNameCMF(keys, pos, writer, 0x2D4CBC); // legion
                //pos += WriteTestNameCMF(keys, pos, writer, 0x2D4CD5); // hero

                const int maxMember = 50;
                for (int i = 0; i < maxMember; i++)
                {
                    var offset = 0x2D4CFC + 0x48C * i;
                    stream.Seek(offset, SeekOrigin.Begin); // team members
                    int exist = stream.ReadByte();
                    if (exist == 1)
                        pos += WriteTestNameCMF(keys, pos, writer, offset + 1);
                }
            }

            return pos;
        }

        void AnalyzeFile(string memfile, string codefile)
        {
            // the original names
            TO2Names names = new TO2Names();
            names.members = new List<NameRecord>();

            var OFFSET_LEGION = 0x2D4CBC;
            var OFFSET_HERO = 0x2D4CD5;
            var OFFSET_MEMBERS = 0x2D4CFC;
            var OFFSET_MEMBERS_STEP = 0x48C;

            // the character code mapping
            var codes = ReadCodes(codefile);
            using (FileStream stream = new FileStream(memfile, FileMode.Open))
            {
                stream.Seek(OFFSET_LEGION, SeekOrigin.Begin); // legion
                names.legion = ReadName(stream, codes);

                stream.Seek(OFFSET_HERO, SeekOrigin.Begin); // hero
                names.hero = ReadName(stream, codes);

                const int maxMember = 50;
                for (int i = 0; i < maxMember; i++)
                {
                    stream.Seek(OFFSET_MEMBERS + OFFSET_MEMBERS_STEP * i, SeekOrigin.Begin); // team members
                    int exist = stream.ReadByte();
                    names.members.Add(ReadName(stream, codes));
                }
            }
            
            // read the fixed code list
            var newcodes = ReadCodes("TO2_encode_new.txt");
            var reverse = new Dictionary<char, ushort>();
            foreach(KeyValuePair<ushort, char> kv in newcodes)
            {
                if (reverse.ContainsKey(kv.Value))
                {
                    System.Diagnostics.Debug.WriteLine(string.Format("[Warning] Duplicated Value: {0}", kv.Value));
                    continue;
                }

                reverse.Add(kv.Value, kv.Key);
            }

            // manually add more keys
            //for(int low=0x0A;low<=0x0F;low++)
            //{
            //    for(int high=0x01;high<=0xFF;high++)
            //    {
            //        if (low == 0x0A && high <= 0x8B)
            //            continue;

            //        var newkey = (ushort)(high * 256 + low);
            //        newcodes[newkey] = '*';
            //    }
            //}

            //GenerateCompareList(newcodes, memfile);

            // Convert the original names
            string cmf = "ULJM-05753_NameFix.CMF";
            using (StreamWriter writer = new StreamWriter(cmf, false, Encoding.ASCII))
            {
                writer.WriteLine("_S ULJM-05753");
                writer.WriteLine("_G Tactics Ogre PSP Chinese NameFix");
                writer.WriteLine("");
                writer.WriteLine("_C0 Fix Name");

                TO2Names newnames = new TO2Names();
                newnames.members = new List<NameRecord>();

                newnames.legion = ConvertName(names.legion, reverse);
                WriteTestNameCMF(newnames.legion.codes, 0, writer, OFFSET_LEGION);

                newnames.hero = ConvertName(names.hero, reverse);
                WriteTestNameCMF(newnames.hero.codes, 0, writer, OFFSET_HERO);

                foreach (var member in names.members)
                {
                    newnames.members.Add(ConvertName(member, reverse));
                }

                for(int i=0;i<newnames.members.Count;i++)
                {
                    var name = newnames.members[i];
                    if(!string.IsNullOrEmpty(name.name))
                    {
                        var offset = OFFSET_MEMBERS + i * OFFSET_MEMBERS_STEP + 1;
                        WriteTestNameCMF(name.codes, 0, writer, offset);
                    }
                }
            }
            
        }

        NameRecord ConvertName(NameRecord old, Dictionary<char, ushort> reverse)
        {
            var name = new NameRecord();

            if(!string.IsNullOrEmpty(old.name))
            {
                var namebuffer = new StringBuilder();
                var codebuffer = new List<ushort>();
                foreach (char c in old.name)
                {
                    if (reverse.ContainsKey(c))
                    {
                        var code = reverse[c];
                        if (code <= 0)
                        {
                            System.Diagnostics.Debug.WriteLine(string.Format("[Error] Invalid code {0} for {1}", code, c));
                            continue;
                        }
                        
                        codebuffer.Add(code);
                        namebuffer.Append(c);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine(string.Format("[Error] Cannot find code for {0}", c));
                        codebuffer.Add(0x3F);
                        namebuffer.Append('?');
                    }
                }

                if (codebuffer.Count > 0)
                {
                    codebuffer.Add(0);
                    name.name = namebuffer.ToString();
                    name.codes = codebuffer.ToArray();
                }
            }
            

            return name;
        }

        Dictionary<ushort, char> ReadCodes(string codefile)
        {
            var codes = new Dictionary<ushort, char>();
            using (StreamReader sr = new StreamReader(codefile))
            {
                while(!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    if (line.StartsWith("//"))
                        continue;

                    int split = line.IndexOf('\t');
                    if (split < 0)
                    {
                        split = line.IndexOf('=');
                        if (split < 0)
                            continue;
                    }
                        

                    string strCode = line.Substring(0, split);
                    string strCharacter = line.Substring(split + 1);
                    if (strCode.Length <= 0 || strCharacter.Length <= 0)
                        continue;

                    ushort code = ushort.Parse(strCode, System.Globalization.NumberStyles.HexNumber);
                    if(code > 0xFF)
                    {
                        var bytes = BitConverter.GetBytes(code);
                        code = (ushort)(bytes[0] * 256 + bytes[1]);
                    }                    
                    char character = strCharacter.Length > 1 ? strCharacter[1] : strCharacter[0];
                    if(codes.ContainsKey(code))
                    {
                        System.Diagnostics.Debug.WriteLine(string.Format("[Warning] {0} replaced by {1} in 0x{2:X}", codes[code], character, code));
                        codes[code] = character;
                    }
                    else
                    {
                        codes.Add(code, character);
                    }
                }
            }

            return codes;
        }

        #region Deprecated
        // directly write to binary
        int WriteTestNameBinary(ushort[] keys, int pos, FileStream stream)
        {
            int maxChar = Math.Min(STRING_LEN, keys.Length - pos);
            for (int i = 0; i < maxChar; i++)
            {
                var key = keys[pos + i];
                if (key >= 0x21)
                {
                    stream.WriteByte((byte)key);
                }
                else
                {
                    var bytes = BitConverter.GetBytes(key);
                    stream.WriteByte(bytes[1]);
                    stream.WriteByte(bytes[0]);
                }
            }
            stream.WriteByte(0);
            return maxChar;
        }

        void WriteTestCodeBinary(string memfile, string codefile, int start)
        {
            // the character code mapping
            var codes = ReadCodes(codefile);
            var keys = codes.Keys.ToArray();

            // write test code
            int pos = 0;
            using (FileStream stream = new FileStream(memfile, FileMode.Open))
            {
                stream.Seek(0x2D4CBC, SeekOrigin.Begin); // legion
                pos += WriteTestNameBinary(keys, pos, stream);

                stream.Seek(0x2D4CD5, SeekOrigin.Begin); // hero
                pos += WriteTestNameBinary(keys, pos, stream);

                const int maxMember = 50;
                for (int i = 0; i < maxMember; i++)
                {
                    stream.Seek(0x2D4CFC + 0x48C * i, SeekOrigin.Begin); // team members
                    int exist = stream.ReadByte();
                    if (exist == 1)
                        pos += WriteTestNameBinary(keys, pos, stream);
                }
            }
        }

        void GenerateCompareList(Dictionary<ushort, char> newcodes, string memfile)
        {
            var keys = newcodes.Keys.ToArray();

            int start = 0;
            File.Delete("TO2_SplitCompare.txt");
            using (StreamWriter splitwriter = new StreamWriter("TO2_SplitCompare.txt", false, Encoding.Unicode))
            {
                while (start < keys.Length)
                {
                    int privous = start;
                    string cmf = string.Format("ULJM-05753_NameFix_{0}.CMF", start);
                    using (StreamWriter writer = new StreamWriter(cmf, false, Encoding.ASCII))
                    {
                        writer.WriteLine("_S ULJM-05753");
                        writer.WriteLine("_G Tactics Ogre PSP Chinese NameFix");
                        writer.WriteLine("");

                        writer.WriteLine(string.Format("_C0 Fix Name Batch {0}", start));
                        start = WriteTestCodeCMF(writer, memfile, keys, start);
                    }

                    splitwriter.WriteLine("");
                    splitwriter.WriteLine(cmf);
                    int split = 0;
                    for (int i = privous; i <= start && i < keys.Length; i++)
                    {
                        if (split % STRING_LEN == 0)
                            splitwriter.WriteLine(string.Format("//Char {0}-------------", (int)(split / STRING_LEN) + 1));

                        splitwriter.WriteLine(string.Format("{0:X}={1}", keys[i], newcodes[keys[i]]));
                        split++;
                    }
                }
            }
        }

        #endregion
    }
}
