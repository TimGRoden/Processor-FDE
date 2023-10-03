using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ProcessorFDE
{
    internal class Program
    {
        static Dictionary<string, string> registers = new Dictionary<string, string>();
        static Dictionary<string, string> memory = new Dictionary<string, string>();
        static int timeDelay = 1000;
        static bool isClick = false;
        static void updateRegisters(string updated = "")
        {
            Console.SetCursorPosition(0, 0);
            foreach (string key in registers.Keys)
            {
                if (key == updated) Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"{key}:{new string(' ',10-key.Length)}{registers[key]}{new string(' ',25-registers[key].Length)}");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }
        static void updateMemory(string updated = "")
        {
            Console.SetCursorPosition(Console.WindowWidth - 21, 0);
            foreach (string key in memory.Keys)
            {
                if (key == updated) Console.ForegroundColor = ConsoleColor.Magenta;
                Console.Write($"{key}: {memory[key]}");
                Console.CursorTop++;
                Console.CursorLeft = Console.WindowWidth - 21;
                Console.ForegroundColor = ConsoleColor.White;
            }
        }
        static void Delay()
        {
            if (isClick) Console.ReadKey(true);
            else System.Threading.Thread.Sleep(timeDelay);
        }
        static string Increment(string binString, int n)
        {
            int val = Convert.ToInt32(binString.Substring(0, 4) + binString.Substring(5), 2);
            val += n;
            string bin = Convert.ToString(val, 2);
            while (bin.Length < 8) bin = "0" + bin; //Add leading 0's.
            return bin.Substring(0,4) + " " + bin.Substring(4);
        }
        static string Read(string location, int bytes)
        {
            string mem = "";
            for (int i = 0; i < bytes; i++)
            {
                mem += memory[location];
                if (i != bytes - 1) mem += " ";     //Add spaces if it's not the end of memory.
                location = Increment(location, 1);  //Look at next memory.
            }
            return mem;
        }
        static void Fetch()
        {   //This fetch needs to load two registers of data. Instructions are 16 bit.
            registers["MAR"] = registers["PC"];             //The contents of the PC are copied into the MAR
            updateRegisters("MAR"); Delay();

            registers["MBR"] = Read(registers["MAR"], 2);   //The address bus is used to transfer this address to main memory
            updateRegisters("MBR"); Delay();                     //The instruction held at that address in main memory is transferred via the data bus to the MBR

            registers["PC"] = Increment(registers["PC"],2); //The PC is incremented to hold the address of the next instruction to be executed
            updateRegisters("PC"); Delay();

            registers["CIR"] = registers["MBR"];            //The contents of the MBR are copied into the CIR
            updateRegisters("CIR"); Delay();
        }
        static void Decode()
        { // Take the string, break it down, re-read.
            if (registers["CIR"]=="0000 0000 0000 0000")        //If it's an end command, stop.
            {
                registers["Decode"] = "END";
                return;
            }
            string instr = "";
            string[] nibbles = registers["CIR"].Split(' ');
            switch (nibbles[0])
            { //Type of instruction
                case "0001": //Load
                    instr += "LOAD "; break;
                case "0010": //Store
                    instr += "STORE "; break;
                case "0011": //Add
                    instr += "ADD "; break;
                case "0100": //AddI
                    instr += "ADDI "; break;
                case "0101": //Sub
                    instr += "SUB "; break;
            }
            instr += "R" + Convert.ToInt32(nibbles[1], 2) + ", "; //Target register.
            
            switch (nibbles[0])
            { //Different instructions use the remaining digits differently.
                case "0001": 
                case "0010": instr += nibbles[2] + " " + nibbles[3]; break; //Both load and store need a memory location
                case "0011": //Add & Sub both take two registers.
                case "0101": instr += "R" + Convert.ToInt32(nibbles[2], 2) + ", " + "R" + Convert.ToInt32(nibbles[3], 2); break;
                case "0100": instr += "#" + Convert.ToInt32(nibbles[2] + nibbles[3], 2); break; //Literal values indicated with #
            }
            registers["Decode"] = instr;
            updateRegisters("Decode"); Delay();
        }
        static string toBinString(int val)
        {
            string bin = Convert.ToString(val, 2);
            while (bin.Length < 8) bin = "0" + bin; //Add leading 0's.
            return bin.Substring(0, 4) + " " + bin.Substring(4);
        }
        static void Execute() 
        {   //Example Commands:
            //LOAD R1, 0000 0000
            //STORE R1, 0000 0000
            //ADD R1, R0, R1
            //SUB R1, R1, R0
            //ADDL R1, #17
            string[] instr = registers["Decode"].Split(' ');
            switch (instr[0])
            {
                case "LOAD":
                    {
                        registers["MAR"] = instr[2] + " " + instr[3];
                        updateRegisters("MAR"); Delay();

                        registers["MBR"] = Read(registers["MAR"], 1);
                        updateRegisters("MBR"); Delay();

                        string bin = registers["MBR"].Substring(0,4) + registers["MBR"].Substring(5);   //Trims the ' '
                        string target = instr[1].Substring(0, 2);                                       //Trim's the ','
                        registers[target] = Convert.ToInt32(bin,2).ToString(); 
                        updateRegisters(target); Delay(); 
                        break;
                    }
                case "STORE":
                    {
                        registers["MAR"] = instr[2] + " " + instr[3];
                        updateRegisters("MAR"); Delay();

                        registers["MBR"] = toBinString(int.Parse(registers[instr[1].Substring(0, 2)])); //Picks the right register.
                        updateRegisters("MBR"); Delay();

                        memory[registers["MAR"]] = registers["MBR"];
                        updateMemory(registers["MAR"]); updateRegisters(); Delay();
                        break;
                    }
                case "ADD":
                    {
                        string target = instr[1].Substring(0, 2);
                        string first = instr[2].Substring(0, 2);
                        string second = instr[3].Substring(0, 2);
                        registers[target] = (int.Parse(registers[first]) + int.Parse(registers[second])).ToString();
                        updateRegisters(target); Delay();
                        break;
                    }
                case "ADDI":
                    {
                        string target = instr[1].Substring(0, 2);
                        int val = int.Parse(instr[2].Substring(1)); //Remove the #
                        registers[target] = (int.Parse(registers[target]) + val).ToString();
                        updateRegisters(target); Delay();
                        break;
                    }
                case "SUB":
                    {
                        string target = instr[1].Substring(0, 2);
                        string first = instr[2].Substring(0, 2);
                        string second = instr[3].Substring(0, 2);
                        registers[target] = (int.Parse(registers[first]) - int.Parse(registers[second])).ToString();
                        updateRegisters(target); Delay();
                        break;
                    }
            }
        }
        static void Main(string[] args)
        {
            Console.WindowWidth = 60;
            Console.WindowHeight = 22;
            Console.WriteLine("Click or Time-Delay? ('click' or otherwise)");
            isClick = Console.ReadLine().ToLower() == "click";
            if (!isClick)
            {
                Console.WriteLine("Please enter the desired time delay (in ms):");
                string userInput = Console.ReadLine();
                if (userInput != "") timeDelay = int.Parse(userInput);
            }

            Console.Clear();
            registers.Add("PC", "0000 0000");
            registers.Add("CIR", "0000 0000");
            registers.Add("MAR", "0000 0000");
            registers.Add("MBR", "0000 0000");
            registers.Add("R0", "0");
            registers.Add("R1", "0");
            registers.Add("Decode", " ");
            updateRegisters();
            using (StreamReader sr = new StreamReader(new FileStream("mainMemory.txt", FileMode.OpenOrCreate)))
            {
                while (!sr.EndOfStream)
                {
                    string[] mem = sr.ReadLine().Split('.');
                    memory.Add(mem[0], mem[1]);
                }
            }
            updateMemory();
            while (registers["Decode"] != "END")
            { //Loop until end command occurs.
                Fetch();
                Decode();
                Execute();
            }
            updateMemory(); updateRegisters();

            Console.WriteLine("PROGRAMM ENDED. Code 0000.");
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}
