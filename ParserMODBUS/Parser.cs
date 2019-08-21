using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace ParserMODBUS
{
    [Serializable]
    [DataContract]
    public class Line
    {
        [XmlIgnore]
        public static string fileCommands = "commands.vcb";
        [XmlIgnore]
        public static Dictionary<string, string> commands;
        [XmlIgnore]
        public static string fileException = "exception.vcb";
        [XmlIgnore]
        public static Dictionary<string, string> exceptions;
        [XmlAttribute]
        [DataMember(EmitDefaultValue = false)]
        public string direction;
        [XmlAttribute]
        [DataMember(EmitDefaultValue = false)]
        public string address;
        [XmlAttribute]
        [DataMember(EmitDefaultValue = false)]
        public string command;
        [XmlAttribute]
        [DataMember(EmitDefaultValue = false)]
        public string crc;
        [XmlAttribute]
        [DataMember(EmitDefaultValue = false)]
        public string exception;
        [XmlAttribute]
        [DataMember(EmitDefaultValue = false)]
        public string error;
        [DataMember(EmitDefaultValue = false)]
        public string raw_frame;
        [DataMember(EmitDefaultValue = false)]
        public string raw_data;

        public void Set( string _error)
        {
            error = _error;
        }

        public void Set(string _address, string _command, string _crc, string _raw_data)
        {
            if (commands == null)
            {
                commands = new Dictionary<string, string>();
                using (StreamReader sr = new StreamReader(fileCommands, Encoding.Default))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        var spltline = line.Replace("-", "").Split(' ');
                       
                        commands.Add(spltline[0], line.Substring(spltline[0].Length + 3));
                    }
                }
            }

            if (exceptions == null)
            {
                exceptions = new Dictionary<string, string>();
                using (StreamReader sr = new StreamReader(fileCommands, Encoding.Default))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        var spltline = line.Replace("-", "").Split(' ');
                        exceptions.Add(spltline[0], line.Substring(spltline[0].Length + 3));
                    }
                }
            }

            if(_raw_data.Length == 2)
            if (exceptions.ContainsKey(_raw_data))
            {
                exception = $"{_raw_data}:{exceptions[_raw_data]}";
            }
            else
            {
                exception = _raw_data;
            }

            address = _address;
            if (commands.ContainsKey(_command))
            {
                command = _command + ':' + commands[_command];
            }
            else
            {
                command = _command;
            }
            crc = _crc.Replace(" ", "");

            string reveres_crc = _crc.Split(' ')[1] + _crc.Split(' ')[0];

            var spltStr = $"{_address} {_command} {_raw_data}".Split(' ');
            byte[] bt = new byte[spltStr.Length];
            int count = 0;
            foreach (var b in spltStr)
            {
                bt[count++] = Convert.ToByte(b, 16);
            }

            if (Parser.CRC16(bt, bt.Length) != Convert.ToUInt32(reveres_crc, 16))
            {
                error = "Wrong CRC";
            }

            raw_data = _raw_data;
            raw_frame = $"{address} {_command} {raw_data} {_crc}";
        }

        public void Set(string _command, string _crc, string _raw_data)
        {
            Set(address, _command, _crc, _raw_data);
        }
    }

    [Serializable]
    [DataContract]
    public class Source
    {
        [XmlAttribute][DataMember]
        public string address;
        [XmlAttribute][DataMember]
        public string speed = "unknown";
        [XmlElement("line")][DataMember(Name = "Line")]
        public List<Line> lines = new List<Line>();
    }

    [Serializable]
    [DataContract]
    public class Data
    {
        [XmlAttribute][DataMember(Name = "SourceType")]
        public string source_type = "Com";
        [XmlElement("source")][DataMember(Name = "Source")] 
        public List<Source> m_logs = new List<Source>();
    }

    public class Parser
    {
        Data m_data;

        public Parser(string fileName)
        {
            FileToLogs(fileName);
        }

        private void FileToLogs(string fileName)
        {
            StreamReader file = new StreamReader(fileName);
            string line;
            m_data = new Data();
            bool separate = false;
            while ((line = file.ReadLine()) != null)
            {
                line = line.Replace('\t', ' ');
                var splitLine = line.Split(' ');

                int idx = 0;

                for (int i = 0; i < splitLine.Length; i++)
                {
                    if (splitLine[i] == "IRP_MJ_WRITE")
                    {
                        string address = splitLine[i + 1];
                        idx = m_data.m_logs.FindIndex(x => x.address == address);
                        if (idx == -1 )
                        {
                            m_data.m_logs.Add(new Source()
                            { address = address });
                            idx = m_data.m_logs.Count - 1;
                        }
                        if (!separate)
                        {
                            m_data.m_logs[idx].lines.Add(new Line() { direction = "request" });
                        }
                    }
                    if (splitLine[i] == "IRP_MJ_READ")
                    {
                        string address = splitLine[i + 1];
                        idx = m_data.m_logs.FindIndex(x => x.address == address);
                        if (idx == -1)
                        {
                            m_data.m_logs.Add(new Source()
                            { address = address });
                            idx = m_data.m_logs.Count - 1;
                        }
                        if (!separate)
                        {
                            m_data.m_logs[idx].lines.Add(new Line() { direction = "response" });
                        }
                    }
                    if (splitLine[i] == "Length")
                    {
                        int length = Convert.ToInt32(splitLine[i + 1].Trim(':'));
                        int idx_2 = m_data.m_logs[idx].lines.Count - 1;
                        if (length == 0)
                        {
                            m_data.m_logs[idx].lines[idx_2].Set("Timeout");                           
                            break;
                        }
                        var addr = splitLine[i + 2];
                        if (length == 1)
                        {
                            m_data.m_logs[idx].lines[idx_2].address = addr;
                            separate = true;
                            break;
                        }
                        int minus = separate ? 1 : 0;
                        var command = splitLine[i + 3 - minus];
                        var crc = splitLine[i + length] + ' ' + splitLine[i + length + 1];
                        var raw_data = splitLine[i + 4 - minus];
                        for (int j = i + 5 - minus; j < i + length; ++j)
                        {
                            raw_data += ' ' + splitLine[j];
                        }
                        if (separate)
                            m_data.m_logs[idx].lines[idx_2].Set(command, crc, raw_data);
                        else
                            m_data.m_logs[idx].lines[idx_2].Set(addr, command, crc, raw_data);

                        //m_data.m_logs[idx].lines[idx_2].raw_frame;
                        separate = false;
                        break;
                    }
                }
            }
            
        }

        public void ToJSON(string fileName)
        {
            using (StreamWriter sw = new StreamWriter(fileName))
            {
                sw.Write((JsonConvert.SerializeObject(m_data, Newtonsoft.Json.Formatting.Indented)));
            }
        }

        public void ToXML(string fileName)
        {
            XmlSerializer serializer = new XmlSerializer(m_data.GetType());
            XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
            namespaces.Add(string.Empty, string.Empty);
            using (var writer = new FileStream(fileName, FileMode.Create))
            {
                serializer.Serialize(writer, m_data, namespaces);
            }
        }

        public void ToTxt(string fileName)
        {
            using (StreamWriter sw = new StreamWriter(fileName, false, Encoding.Default))
            {              
                sw.WriteLine($"SourceType {m_data.source_type}");
                foreach(var source in m_data.m_logs)
                {
                    sw.WriteLine($"\tSource: Address={source.address} Speed={source.speed}");
                    foreach(var line in source.lines)
                    {
                        sw.Write($"\t\tLine: Direction={line.direction} ");
                        if(line.address==null && line.error != null)
                        {
                            sw.WriteLine($"Error='{line.error}'");
                            continue;
                        }
                        sw.Write($"Address={line.address} Command='{line.command}' ");
                        if(line.exception != null)
                        {
                            sw.Write($"Exception='{line.exception}' ");
                        }
                        if(line.error != null)
                        {
                            sw.Write($"Error='{line.error}' ");
                        }
                        sw.WriteLine($"CRC ={line.crc}");
                        sw.WriteLine($"\t\t\tRawFrame: {line.raw_frame}\n\t\t\tRawData: {line.raw_data}");
                    }
                }
            }
        }

        public static uint CRC16(byte[] data, int data_size)
        {
            const uint MODBUS_CRC_CONST = 0xA001;
            uint CRC = 0xFFFF;

            for (int i = 0; i < data_size; i++)
            {
                CRC ^= (uint)data[i];
                for (int k = 0; k < 8; k++)
                {
                    if ((CRC & 0x01) == 1)
                    {
                        CRC >>= 1;
                        CRC ^= MODBUS_CRC_CONST;
                    }
                    else
                        CRC >>= 1;
                }
            }

            return CRC;
        }
    }
}
