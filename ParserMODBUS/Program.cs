﻿using System;

namespace ParserMODBUS
{
    class MainClass
    {
        public static int Main(string[] args)
        {
            if (args.Length == 0 || args.Length > 2)
            {
                Console.WriteLine("Usage:  ParserMODBUS <raw_logs_file.log> <parsed_log_file>");
                return 1;
            }
            string raw_log = args[0];
            string parsed_log = args[1];

            //byte[] bt = { 0x01, 0x6A, 0x02 };

            //Console.WriteLine(Parser.CRC16(bt, 3));

            //raw_log = "raw_log_example.log";
            //parsed_log = "test.xml";
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
