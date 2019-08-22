using System;

namespace ParserMODBUS
{
    class MainClass
    {
        public static int Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage:  ParserMODBUS <raw_logs_file.log> <parsed_log_file>");
                return 1;
            }
            string raw_log = args[0];
            string parsed_log = args[1];

            raw_log = "raw_log_example2.log";
            parsed_log = "test.xml";

            //string str = "68 46 01 6A 02";
            //var spltStr = str.Split(' ');
            //byte[] bt = new byte[spltStr.Length];
            //int count = 0;
            //foreach (var b in spltStr)
            //{
            //    bt[count++] = Convert.ToByte(b, 16);
            //}

            //Console.WriteLine(Parser.CRC16(bt, bt.Length) == Convert.ToUInt32("8594", 16));
            //Console.WriteLine(Parser.CRC16(bt, bt.Length));
            Parser parser = new Parser(raw_log);
            var extension = parsed_log.Substring(parsed_log.LastIndexOf('.') + 1);
            if (extension.Equals("xml"))
            {
                parser.ToXML(parsed_log);
            }
            if (extension.Equals("json"))
            {
                parser.ToJSON(parsed_log);
            }
            if (extension.Equals("txt"))
            {
                parser.ToTxt(parsed_log);
            }
            return 0;
        }
    }
}
